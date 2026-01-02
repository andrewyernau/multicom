using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Touchless.Vision.Camera;

namespace MultiCom.Server
{
    public partial class ServerForm : Form
    {
        private const string MULTICAST_VIDEO = "239.50.10.1";
        private const string MULTICAST_CHAT_OUT = "239.50.10.2";
        private const string MULTICAST_CHAT_IN = "239.50.10.4";
        private const int PORT_VIDEO = 5050;
        private const int PORT_CHAT_OUT = 5051;
        private const int PORT_CHAT_IN = 5053;
        private const int CHUNK_SIZE = 2500;

        private CameraFrameSource frameSource;
        private UdpClient videoSender;
        private UdpClient chatSender;
        private UdpClient chatReceiver;
        private IPEndPoint videoEndpoint;
        private IPEndPoint chatOutEndpoint;
        private IPEndPoint chatInEndpoint;
        private int frameNumber = 0;
        private bool isStreaming = false;
        private Task chatTask;
        private CancellationTokenSource cts;

        public ServerForm()
        {
            InitializeComponent();
        }

        private void OnFormLoaded(object sender, EventArgs e)
        {
            ApplyDiscordPalette();
            LoadCameras();
            Log("Servidor listo. Presiona Start para transmitir.");
        }

        private void LoadCameras()
        {
            comboCameras.Items.Clear();
            foreach (Camera cam in CameraService.AvailableCameras)
            {
                comboCameras.Items.Add(cam);
            }

            if (comboCameras.Items.Count > 0)
            {
                comboCameras.SelectedIndex = 0;
            }
        }

        private void OnStartClick(object sender, EventArgs e)
        {
            if (comboCameras.SelectedItem == null)
            {
                MessageBox.Show("Selecciona una cámara primero", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                isStreaming = true;
                cts = new CancellationTokenSource();

                // Configurar multicast
                videoEndpoint = new IPEndPoint(IPAddress.Parse(MULTICAST_VIDEO), PORT_VIDEO);
                chatOutEndpoint = new IPEndPoint(IPAddress.Parse(MULTICAST_CHAT_OUT), PORT_CHAT_OUT);
                chatInEndpoint = new IPEndPoint(IPAddress.Parse(MULTICAST_CHAT_IN), PORT_CHAT_IN);

                videoSender = new UdpClient();
                videoSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_VIDEO));

                chatSender = new UdpClient();
                chatSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_CHAT_OUT));

                // Chat receiver
                chatReceiver = new UdpClient();
                chatReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                chatReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, PORT_CHAT_IN));
                chatReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_CHAT_IN));

                // Iniciar cámara
                Camera cam = (Camera)comboCameras.SelectedItem;
                frameSource = new CameraFrameSource(cam);
                frameSource.Camera.CaptureWidth = 320;
                frameSource.Camera.CaptureHeight = 240;
                frameSource.Camera.Fps = 15;
                frameSource.NewFrame += OnCameraFrame;
                frameSource.StartFrameCapture();

                // Iniciar chat
                chatTask = Task.Run(() => ChatReceiveLoop(cts.Token));

                btnStart.Enabled = false;
                btnStop.Enabled = true;
                comboCameras.Enabled = false;

                Log("Transmisión iniciada");
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
                StopStreaming();
            }
        }

        private void OnStopClick(object sender, EventArgs e)
        {
            StopStreaming();
        }

        private void StopStreaming()
        {
            isStreaming = false;

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }

            if (frameSource != null)
            {
                frameSource.NewFrame -= OnCameraFrame;
                frameSource.StopFrameCapture();
                frameSource = null;
            }

            if (videoSender != null)
            {
                videoSender.Close();
                videoSender = null;
            }

            if (chatSender != null)
            {
                chatSender.Close();
                chatSender = null;
            }

            if (chatReceiver != null)
            {
                chatReceiver.Close();
                chatReceiver = null;
            }

            if (chatTask != null && !chatTask.IsCompleted)
            {
                try { chatTask.Wait(100); } catch { }
            }

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            comboCameras.Enabled = true;
            frameNumber = 0;

            Log("Transmisión detenida");
        }

        private void OnCameraFrame(Touchless.Vision.Contracts.IFrameSource source, Touchless.Vision.Contracts.Frame frame, double fps)
        {
            if (!isStreaming || videoSender == null) return;

            try
            {
                Bitmap bitmap = (Bitmap)frame.Image.Clone();
                
                // Mostrar en preview
                if (picturePreview.InvokeRequired)
                {
                    picturePreview.BeginInvoke(new Action(() =>
                    {
                        var old = picturePreview.Image;
                        picturePreview.Image = (Bitmap)bitmap.Clone();
                        old?.Dispose();
                    }));
                }

                // Enviar por multicast
                Task.Run(() => SendFrame(bitmap));
            }
            catch (Exception ex)
            {
                Log("ERROR frame: " + ex.Message);
            }
        }

        private void SendFrame(Bitmap bitmap)
        {
            try
            {
                byte[] imageData;
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    imageData = ms.ToArray();
                }
                bitmap.Dispose();

                // Split en chunks
                int totalChunks = (int)Math.Ceiling((double)imageData.Length / CHUNK_SIZE);
                byte[] timestamp = BitConverter.GetBytes(DateTime.Now.ToBinary());
                byte[] frameBytes = BitConverter.GetBytes(frameNumber);
                byte[] totalChunksBytes = BitConverter.GetBytes(totalChunks);
                byte[] totalSizeBytes = BitConverter.GetBytes(imageData.Length);
                byte[] chunkSizeBytes = BitConverter.GetBytes(CHUNK_SIZE);

                for (int i = 0; i < totalChunks; i++)
                {
                    int offset = i * CHUNK_SIZE;
                    int length = Math.Min(CHUNK_SIZE, imageData.Length - offset);
                    
                    byte[] chunk = new byte[length];
                    Array.Copy(imageData, offset, chunk, 0, length);

                    byte[] chunkIndexBytes = BitConverter.GetBytes(i);

                    // Header: timestamp(8) + frame(4) + chunkIndex(4) + totalChunks(4) + totalSize(4) + chunkSize(4) = 28 bytes
                    byte[] packet = new byte[28 + length];
                    Buffer.BlockCopy(timestamp, 0, packet, 0, 8);
                    Buffer.BlockCopy(frameBytes, 0, packet, 8, 4);
                    Buffer.BlockCopy(chunkIndexBytes, 0, packet, 12, 4);
                    Buffer.BlockCopy(totalChunksBytes, 0, packet, 16, 4);
                    Buffer.BlockCopy(totalSizeBytes, 0, packet, 20, 4);
                    Buffer.BlockCopy(chunkSizeBytes, 0, packet, 24, 4);
                    Buffer.BlockCopy(chunk, 0, packet, 28, length);

                    videoSender.Send(packet, packet.Length, videoEndpoint);
                }

                frameNumber++;
            }
            catch (Exception ex)
            {
                Log("ERROR envío: " + ex.Message);
            }
        }

        private void ChatReceiveLoop(CancellationToken token)
        {
            var remoteEP = new IPEndPoint(IPAddress.Any, PORT_CHAT_IN);
            
            while (!token.IsCancellationRequested)
            {
                try
                {
                    chatReceiver.Client.ReceiveTimeout = 1000;
                    byte[] data = chatReceiver.Receive(ref remoteEP);
                    string mensaje = System.Text.Encoding.UTF8.GetString(data);
                    
                    BeginInvoke(new Action(() => MostrarMensaje(mensaje)));
                }
                catch (SocketException) { }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Log("ERROR chat: " + ex.Message);
                    }
                }
            }
        }

        private void MostrarMensaje(string mensaje)
        {
            string[] partes = mensaje.Split(';');
            if (partes.Length == 2)
            {
                listChat.Items.Add($"[{DateTime.Now:HH:mm}] {partes[0]}: {partes[1]}");
                if (listChat.Items.Count > 100)
                {
                    listChat.Items.RemoveAt(0);
                }
                listChat.TopIndex = listChat.Items.Count - 1;
            }
        }

        private void OnSendChatClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text) || chatSender == null)
            {
                return;
            }

            try
            {
                string mensaje = $"Servidor;{txtMessage.Text}";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(mensaje);
                chatSender.Send(data, data.Length, chatOutEndpoint);
                
                listChat.Items.Add($"[{DateTime.Now:HH:mm}] Tú: {txtMessage.Text}");
                if (listChat.Items.Count > 100)
                {
                    listChat.Items.RemoveAt(0);
                }
                listChat.TopIndex = listChat.Items.Count - 1;
                
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                Log("ERROR envío chat: " + ex.Message);
            }
        }

        private void Log(string mensaje)
        {
            if (listEvents.InvokeRequired)
            {
                listEvents.BeginInvoke(new Action(() => Log(mensaje)));
                return;
            }

            listEvents.Items.Insert(0, $"{DateTime.Now:HH:mm:ss} {mensaje}");
            while (listEvents.Items.Count > 50)
            {
                listEvents.Items.RemoveAt(listEvents.Items.Count - 1);
            }
        }

        private void ApplyDiscordPalette()
        {
            BackColor = Color.FromArgb(54, 57, 63);
            btnStart.BackColor = Color.FromArgb(67, 181, 129);
            btnStart.ForeColor = Color.White;
            btnStop.BackColor = Color.FromArgb(240, 71, 71);
            btnStop.ForeColor = Color.White;
            btnStop.Enabled = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopStreaming();
            base.OnFormClosing(e);
        }
    }
}
