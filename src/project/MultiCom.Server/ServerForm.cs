using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Touchless.Vision.Camera;
using MultiCom.Shared.Audio;
using MultiCom.Server.Audio;

namespace MultiCom.Server
{
    public partial class ServerForm : Form
    {
        private const string MULTICAST_IP = "224.0.0.1";
        private const int PORT_VIDEO = 8080;
        private const int PORT_AUDIO = 8081;
        private const int PORT_CHAT_OUT = 8082;
        private const int PORT_CHAT_IN = 8083;
        private const int CHUNK_SIZE = 2500;

        private CameraFrameSource frameSource;
        private UdpClient videoSender;
        private UdpClient audioSender;
        private UdpClient chatSender;
        private UdpClient chatReceiver;
        private IPEndPoint videoEndpoint;
        private IPEndPoint audioEndpoint;
        private IPEndPoint chatOutEndpoint;
        private IPEndPoint chatInEndpoint;
        private int frameNumber = 0;
        private bool isStreaming = false;
        private Task chatTask;
        private CancellationTokenSource cts;
        
        private SimpleAudioCapture audioCapture;

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
            try
            {
                comboBoxCameras.Items.Clear();
                
                int count = CameraService.AvailableCameras.Count();
                Log($"Cámaras detectadas: {count}");
                
                foreach (Camera cam in CameraService.AvailableCameras)
                {
                    comboBoxCameras.Items.Add(cam);
                    Log($"  - {cam.ToString()}");
                }
                
                if (count == 0)
                {
                    Log("⚠️ No se detectaron cámaras. Verifica que la cámara esté conectada.");
                }
                else
                {
                    comboBoxCameras.SelectedIndex = 0; // Seleccionar primera cámara
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error detectando cámaras: {ex.Message}");
            }
        }

        private void OnStartClick(object sender, EventArgs e)
        {
            try
            {
                // Obtener cámara seleccionada del ComboBox
                Camera cam = comboBoxCameras.SelectedItem as Camera;
                
                if (cam == null)
                {
                    MessageBox.Show("Por favor, selecciona una cámara antes de iniciar.\n\nSi no hay cámaras disponibles:\n- Verifica conexión\n- Drivers instalados\n- Presiona 'Refresh Cameras'", 
                        "No hay cámara seleccionada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Log("❌ No hay cámara seleccionada");
                    return;
                }
                Log($"Usando cámara: {cam.ToString()}");

                isStreaming = true;
                cts = new CancellationTokenSource();

                // Configurar multicast
                Log("Configurando endpoints multicast...");
                videoEndpoint = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), PORT_VIDEO);
                audioEndpoint = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), PORT_AUDIO);
                chatOutEndpoint = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), PORT_CHAT_OUT);
                chatInEndpoint = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), PORT_CHAT_IN);

                Log("Creando sockets UDP...");
                videoSender = new UdpClient();
                videoSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));
                videoSender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);

                audioSender = new UdpClient();
                audioSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));
                audioSender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);

                chatSender = new UdpClient();
                chatSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));
                chatSender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);

                // Chat receiver
                Log("Configurando receptor de chat...");
                chatReceiver = new UdpClient();
                chatReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                chatReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, PORT_CHAT_IN));
                chatReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

                // Iniciar cámara
                Log($"Configurando cámara {cam.ToString()}...");
                frameSource = new CameraFrameSource(cam);
                frameSource.Camera.CaptureWidth = 320;
                frameSource.Camera.CaptureHeight = 240;
                frameSource.Camera.Fps = 15;
                frameSource.NewFrame += OnCameraFrame;
                
                Log("Iniciando captura de cámara...");
                frameSource.StartFrameCapture();
                Log("✅ Cámara iniciada");

                // Iniciar audio
                Log("Iniciando captura de audio...");
                audioCapture = new SimpleAudioCapture();
                audioCapture.DataAvailable += OnAudioData;
                audioCapture.StartRecording(8000, 16, 1); // 8kHz, 16-bit, mono
                Log("✅ Audio capturando");

                // Iniciar chat
                Log("Iniciando receptor de chat...");
                chatTask = Task.Run(() => ChatReceiveLoop(cts.Token));

                btnStart.Enabled = false;
                btnStop.Enabled = true;

                Log("✅ Transmisión iniciada correctamente");
            }
            catch (Exception ex)
            {
                Log("❌ ERROR: " + ex.Message);
                Log("Stack: " + ex.StackTrace);
                MessageBox.Show($"Error al iniciar:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopStreaming();
            }
        }

        private void OnStopClick(object sender, EventArgs e)
        {
            StopStreaming();
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            Log("Refrescando lista de cámaras...");
            LoadCameras();
        }

        private void OnMetricsTick(object sender, EventArgs e)
        {
            // Actualizar métricas si es necesario
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

            if (audioCapture != null)
            {
                audioCapture.DataAvailable -= OnAudioData;
                audioCapture.Stop();
                audioCapture.Dispose();
                audioCapture = null;
            }

            if (videoSender != null)
            {
                videoSender.Close();
                videoSender = null;
            }

            if (audioSender != null)
            {
                audioSender.Close();
                audioSender = null;
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
            frameNumber = 0;

            Log("Transmisión detenida");
        }

        private void OnAudioData(object sender, AudioDataEventArgs e)
        {
            if (!isStreaming || audioSender == null) return;

            try
            {
                // Codificar a A-Law (tomar solo los bytes necesarios)
                byte[] audioData = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, audioData, e.BytesRecorded);
                byte[] encoded = ALawEncoder.ALawEncode(audioData);
                
                // Enviar por multicast
                audioSender.Send(encoded, encoded.Length, audioEndpoint);
            }
            catch (Exception ex)
            {
                Log($"ERROR audio: {ex.Message}");
            }
        }

        private void OnCameraFrame(Touchless.Vision.Contracts.IFrameSource source, Touchless.Vision.Contracts.Frame frame, double fps)
        {
            if (!isStreaming || videoSender == null) return;

            try
            {
                Bitmap bitmap = (Bitmap)frame.Image.Clone();
                
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
                Log($"Chat - {partes[0]}: {partes[1]}");
            }
        }

        private void OnSendChatClick(object sender, EventArgs e)
        {
            // Chat simplificado - no hay UI de chat implementada
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
