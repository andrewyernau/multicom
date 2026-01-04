using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Touchless.Vision.Camera;
using NAudio.Wave;
using MultiCom.Shared.Audio;
using MultiCom.Shared.Networking;

namespace MultiCom.Server
{
    public partial class ServerForm : Form
    {
        private CameraFrameSource _frameSource;
        private static Bitmap _latestFrame;
        
        // Contador de imágenes
        private uint imageCounter = 0;
        
        // Audio
        private WaveInEvent waveIn;
        private UdpClient audioSender;
        private IPEndPoint audioEndPoint;
        
        // Chat multicast
        private UdpClient chatReceiver;
        private Task chatTask;
        private bool receivingChat = false;
        private const int CHAT_PORT = 8082;
        private const string CHAT_IP = "224.0.0.1";

        public ServerForm()
        {
            InitializeComponent();
        }

        private void OnFormLoaded(object sender, EventArgs e)
        {
            ApplyDiscordPalette();
            LoadCameras();
            StartChatReception();
        }

        private void LoadCameras()
        {
            try
            {
                comboBoxCameras.Items.Clear();
                
                foreach (Camera cam in CameraService.AvailableCameras)
                {
                    comboBoxCameras.Items.Add(cam);
                }
                
                if (comboBoxCameras.Items.Count > 0)
                    comboBoxCameras.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log($"Error al cargar cámaras: {ex.Message}");
            }
        }

        private void setFrameSource(CameraFrameSource cameraFrameSource)
        {
            if (_frameSource == cameraFrameSource)
                return;

            _frameSource = cameraFrameSource;
        }

        public void OnImageCaptured(Touchless.Vision.Contracts.IFrameSource frameSource, Touchless.Vision.Contracts.Frame frame, double fps)
        {
            _latestFrame = frame.Image;
            pictureBox.Invalidate();
        }

        private void drawLatestImage(object sender, PaintEventArgs e)
        {
            if (_latestFrame != null)
            {
                Bitmap resized = new Bitmap(_latestFrame, new Size(640, 480));
                e.Graphics.DrawImage(resized, 0, 0, resized.Width, resized.Height);

                try
                {
                    UdpClient udpServer = new UdpClient();
                    IPAddress multicastaddress = IPAddress.Parse("224.0.0.1");
                    
                    // Configurar TTL
                    udpServer.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 32);
                    
                    udpServer.JoinMulticastGroup(multicastaddress);
                    IPEndPoint remote = new IPEndPoint(multicastaddress, 8080);

                    // Incrementar contador de imagen
                    imageCounter++;

                    // Convertir a JPEG
                    byte[] jpegData = ImageToByteArray(resized);

                    // Crear cabecera
                    var header = new VideoHeader
                    {
                        ImageNumber = imageCounter,
                        TimestampUtc = DateTime.UtcNow.Ticks,
                        PayloadSize = jpegData.Length,
                        Width = (ushort)resized.Width,
                        Height = (ushort)resized.Height,
                        Quality = 85,
                        Checksum = VideoHeader.CalculateChecksum(jpegData)
                    };

                    // Crear paquete completo
                    byte[] packet = VideoHeader.CreatePacket(header, jpegData);
                    udpServer.Send(packet, packet.Length, remote);

                    udpServer.Close();
                }
                catch (Exception ex)
                {
                    Log($"Error al enviar video: {ex.Message}");
                }
            }
        }

        public byte[] ImageToByteArray(Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        private void OnStartClick(object sender, EventArgs e)
        {
            try
            {
                Camera c = (Camera)comboBoxCameras.SelectedItem;
                
                if (c == null)
                {
                    MessageBox.Show("Selecciona una cámara", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                setFrameSource(new CameraFrameSource(c));
                _frameSource.Camera.CaptureWidth = 320;
                _frameSource.Camera.CaptureHeight = 240;
                _frameSource.Camera.Fps = 20;
                _frameSource.NewFrame += OnImageCaptured;
                pictureBox.Paint += new PaintEventHandler(drawLatestImage);
                _frameSource.StartFrameCapture();

                // Iniciar audio
                StartAudioTransmission();

                btnStart.Enabled = false;
                btnStop.Enabled = true;
                
                Log("Transmisión de video y audio iniciada");
            }
            catch (Exception x)
            {
                Log($"Error al iniciar: {x.Message}");
                MessageBox.Show($"Error: {x.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnStopClick(object sender, EventArgs e)
        {
            try
            {
                if (_frameSource != null)
                {
                    _frameSource.NewFrame -= OnImageCaptured;
                    pictureBox.Paint -= drawLatestImage;
                    _frameSource.StopFrameCapture();
                    _frameSource = null;
                }
                
                waveIn?.StopRecording();
                audioSender?.Close();
                
                receivingChat = false;
                if (chatTask != null)
                {
                    try { chatTask.Wait(1000); } catch { }
                }
                chatReceiver?.Close();
                
                _latestFrame = null;
                
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                
                Log("Transmisión detenida");
            }
            catch (Exception ex)
            {
                Log($"Error al detener: {ex.Message}");
            }
        }

        private void StartAudioTransmission()
        {
            try
            {
                audioSender = new UdpClient();
                
                // Configurar TTL
                audioSender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 32);
                
                // Unirse al grupo multicast para audio
                IPAddress multicastAddress = IPAddress.Parse("224.0.0.1");
                audioSender.JoinMulticastGroup(multicastAddress);
                audioEndPoint = new IPEndPoint(multicastAddress, 8081);

                waveIn = new WaveInEvent();
                waveIn.WaveFormat = new WaveFormat(8000, 16, 1);
                waveIn.BufferMilliseconds = 50;
                waveIn.DataAvailable += OnAudioCaptured;
                waveIn.StartRecording();
                Log("Audio iniciado correctamente");
            }
            catch (Exception ex)
            {
                Log($"Error iniciando audio: {ex.Message}");
            }
        }

        private void OnAudioCaptured(object sender, WaveInEventArgs e)
        {
            try
            {
                short[] pcm = new short[e.BytesRecorded / 2];
                Buffer.BlockCopy(e.Buffer, 0, pcm, 0, e.BytesRecorded);

                byte[] encoded = ALawEncoder.ALawEncode(pcm);
                audioSender.Send(encoded, encoded.Length, audioEndPoint);
            }
            catch
            {
            }
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            LoadCameras();
        }
        
        private void StartChatReception()
        {
            if (receivingChat) return;

            receivingChat = true;
            chatTask = Task.Run(() => ReceiveChat());
            Log("Chat iniciado - recibiendo mensajes");
        }

        private void ReceiveChat()
        {
            try
            {
                // Configurar socket
                chatReceiver = new UdpClient();
                chatReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                
                // Bind al puerto
                IPEndPoint chatEP = new IPEndPoint(IPAddress.Any, CHAT_PORT);
                chatReceiver.Client.Bind(chatEP);
                
                // Unirse al grupo multicas
                chatReceiver.JoinMulticastGroup(IPAddress.Parse(CHAT_IP));

                while (receivingChat)
                {
                    try
                    {
                        byte[] buffer = chatReceiver.Receive(ref chatEP);
                        
                        // Parsear mensaje con ChatEnvelope
                        MultiCom.Shared.Chat.ChatEnvelope envelope;
                        if (MultiCom.Shared.Chat.ChatEnvelope.TryParse(buffer, out envelope))
                        {
                            string mensaje = $"[{envelope.TimestampUtc.ToLocalTime():HH:mm:ss}] {envelope.Sender}: {envelope.Message}";
                            Log(mensaje);
                        }
                    }
                    catch (SocketException)
                    {
                        // Timeout o error de socket
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error en chat: {ex.Message}");
            }
            finally
            {
                chatReceiver?.Close();
            }
        }

        private void OnMetricsTick(object sender, EventArgs e)
        {
            // No
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
            receivingChat = false;
            OnStopClick(null, null);
            base.OnFormClosing(e);
        }
    }
}
