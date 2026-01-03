using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MultiCom.Shared.Audio;
using MultiCom.Shared.Networking;
using MultiCom.Client.Audio;
using NAudio.Wave;

namespace MultiCom.Client
{
    public partial class ClientForm : Form
    {
        private const string MULTICAST_IP = "224.0.0.1";
        private const int VIDEO_PORT = 8080;
        private const int AUDIO_PORT = 8081;
        private const int CHAT_SERVER_PORT = 8082; // Cliente recibe
        private const int CHAT_CLIENT_PORT = 8083; // Cliente envía

        private string userName = "User";
        private bool isConnected;

        private UdpClient videoReceiver;
        private Task videoTask;
        private PictureBox pictureBoxVideo;  // Control para mostrar video

        private UdpClient audioReceiver;
        private Task audioTask;
        private SimpleAudioPlayer audioPlayer;

        private UdpClient chatSender;
        private IPEndPoint chatSenderEndpoint;
        private UdpClient chatReceiver;
        private IPEndPoint chatReceiverEndpoint;
        private Task chatTask;

        private readonly PerformanceTracker performanceTracker = new PerformanceTracker(100);
        private int lastReceivedSeqNum = -1;
        private int lastReceivedFrameNum = -1;

        public ClientForm()
        {
            InitializeComponent();
            this.Load += OnClientLoaded;
            this.FormClosing += OnClientClosing;
        }

        private void OnClientLoaded(object sender, EventArgs e)
        {
            btnDisconnect.Enabled = false;
            
            // Crear PictureBox para video
            pictureBoxVideo = new PictureBox
            {
                Size = new Size(640, 480),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(47, 49, 54),
                Dock = DockStyle.Fill
            };
            
            if (flowVideo != null)
            {
                flowVideo.Controls.Add(pictureBoxVideo);
            }

            // Iniciar timer de métricas
            if (uiTimer != null)
            {
                uiTimer.Start();
            }
            
            Log("[INFO] Ready. Press Connect to join.");
        }

        private void OnConnect(object sender, EventArgs e)
        {
            if (isConnected) return;

            try
            {
                isConnected = true;
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;

                // Reset métricas
                performanceTracker.Reset();
                lastReceivedSeqNum = -1;
                lastReceivedFrameNum = -1;

                // Iniciar recepción de video
                StartVideoReceiver();

                // Iniciar recepción de audio
                StartAudioReceiver();

                // Iniciar chat
                StartChat();

                // Enviar mensaje de unión
                if (chatSender != null)
                {
                    byte[] msg = Encoding.UTF8.GetBytes($";{userName} joined");
                    chatSender.Send(msg, msg.Length, chatSenderEndpoint);
                }

                Log("[INFO] Connected to conference.");
            }
            catch (Exception ex)
            {
                Log("[ERROR] Connect failed: " + ex.Message);
                OnDisconnect(null, null);
            }
        }

        private void OnDisconnect(object sender, EventArgs e)
        {
            if (!isConnected) return;

            try
            {
                isConnected = false;

                // Detener tareas
                videoTask = null;
                audioTask = null;
                chatTask = null;

                // Cerrar sockets
                videoReceiver?.Close();
                audioReceiver?.Close();
                chatSender?.Close();
                chatReceiver?.Close();
                audioPlayer?.Dispose();

                videoReceiver = null;
                audioReceiver = null;
                chatSender = null;
                chatReceiver = null;
                audioPlayer = null;

                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;

                Log("[INFO] Disconnected.");
            }
            catch (Exception ex)
            {
                Log("[ERROR] Disconnect: " + ex.Message);
            }
        }

        private void StartVideoReceiver()
        {
            try
            {
                videoReceiver = new UdpClient();
                videoReceiver.ExclusiveAddressUse = false;
                videoReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                videoReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, VIDEO_PORT));
                videoReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

                videoTask = Task.Run(() => ReceiveVideoLoop());
                Log("[INFO] Video receiver started on port " + VIDEO_PORT);
            }
            catch (Exception ex)
            {
                Log("[ERROR] Video receiver: " + ex.Message);
            }
        }

        // Estado de reensamblado de video
        private int currentImageNumber = -1;
        private byte[] imageBuffer = null;
        private int receivedPackets = 0;
        private int expectedPackets = 0;

        private void ReceiveVideoLoop()
        {
            IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, VIDEO_PORT);

            while (isConnected)
            {
                try
                {
                    byte[] packet = videoReceiver.Receive(ref remoteEp);
                    if (packet.Length < 28) continue;

                    var receivedAt = DateTime.UtcNow;

                    // Cabecera: timestamp(8) + imageNum(4) + seqNum(4) + totalPackets(4) + totalSize(4) + chunkSize(4) = 28 bytes
                    long timestampBinary = BitConverter.ToInt64(packet, 0);
                    int imageNum = BitConverter.ToInt32(packet, 8);
                    int seqNum = BitConverter.ToInt32(packet, 12);
                    int totalPackets = BitConverter.ToInt32(packet, 16);
                    int totalSize = BitConverter.ToInt32(packet, 20);
                    int chunkSize = BitConverter.ToInt32(packet, 24);

                    // Calcular latencia
                    var sentAt = DateTime.FromBinary(timestampBinary);
                    var latencyMs = Math.Max(0, (receivedAt - sentAt).TotalMilliseconds);

                    // Detectar pérdidas de paquetes
                    if (imageNum == lastReceivedFrameNum && lastReceivedSeqNum >= 0)
                    {
                        int expectedSeq = lastReceivedSeqNum + 1;
                        if (seqNum > expectedSeq)
                        {
                            int lostPackets = seqNum - expectedSeq;
                            performanceTracker.RegisterLoss(lostPackets);
                        }
                    }

                    lastReceivedSeqNum = seqNum;
                    lastReceivedFrameNum = imageNum;

                    // Payload
                    byte[] chunk = new byte[packet.Length - 28];
                    Array.Copy(packet, 28, chunk, 0, chunk.Length);

                    // Primer chunk de nueva imagen
                    if (seqNum == 0)
                    {
                        imageBuffer = new byte[totalSize];
                        currentImageNumber = imageNum;
                        receivedPackets = 1;
                        expectedPackets = totalPackets;
                        Array.Copy(chunk, 0, imageBuffer, 0, chunk.Length);
                        lastReceivedSeqNum = 0;
                    }
                    else if (imageNum == currentImageNumber && receivedPackets < expectedPackets)
                    {
                        // Chunk intermedio o final de la misma imagen
                        int bufferOffset = seqNum * chunkSize;
                        if (bufferOffset + chunk.Length <= imageBuffer.Length)
                        {
                            Array.Copy(chunk, 0, imageBuffer, bufferOffset, chunk.Length);
                            receivedPackets++;

                            // Si es el último chunk, mostrar imagen
                            if (receivedPackets == expectedPackets)
                            {
                                try
                                {
                                    using (MemoryStream ms = new MemoryStream(imageBuffer))
                                    {
                                        Bitmap bitmap = new Bitmap(ms);
                                        ShowFrame(bitmap);
                                        
                                        // Registrar frame completo con latencia
                                        performanceTracker.RegisterFrame(receivedAt, latencyMs);
                                    }
                                }
                                catch { }
                                
                                // Reset para siguiente imagen
                                currentImageNumber = -1;
                                imageBuffer = null;
                                receivedPackets = 0;
                                lastReceivedSeqNum = -1;
                            }
                        }
                    }
                    else if (imageNum != currentImageNumber && seqNum == 0)
                    {
                        // Nueva imagen comenzó pero no completamos la anterior (pérdida)
                        if (currentImageNumber >= 0 && receivedPackets < expectedPackets)
                        {
                            int lostPackets = expectedPackets - receivedPackets;
                            performanceTracker.RegisterLoss(lostPackets);
                        }
                        
                        // Iniciar nueva imagen
                        imageBuffer = new byte[totalSize];
                        currentImageNumber = imageNum;
                        receivedPackets = 1;
                        expectedPackets = totalPackets;
                        Array.Copy(chunk, 0, imageBuffer, 0, chunk.Length);
                        lastReceivedSeqNum = 0;
                    }
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch (Exception ex)
                {
                    if (isConnected)
                        Log("[ERROR] Video: " + ex.Message);
                }
            }
        }

        private void ShowFrame(Bitmap bitmap)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        var old = pictureBoxVideo?.Image;
                        if (pictureBoxVideo != null)
                        {
                            pictureBoxVideo.Image = bitmap;
                        }
                        old?.Dispose();
                    }));
                }
                else
                {
                    var old = pictureBoxVideo?.Image;
                    if (pictureBoxVideo != null)
                    {
                        pictureBoxVideo.Image = bitmap;
                    }
                    old?.Dispose();
                }
            }
            catch 
            { 
                bitmap?.Dispose(); 
            }
        }

        private void StartAudioReceiver()
        {
            try
            {
                audioReceiver = new UdpClient();
                audioReceiver.ExclusiveAddressUse = false;
                audioReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                audioReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, AUDIO_PORT));
                audioReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

                audioPlayer = new SimpleAudioPlayer();
                audioPlayer.Start();

                audioTask = Task.Run(() => ReceiveAudioLoop());
                Log("[INFO] Audio receiver started on port " + AUDIO_PORT);
            }
            catch (Exception ex)
            {
                Log("[ERROR] Audio receiver: " + ex.Message);
            }
        }

        private void ReceiveAudioLoop()
        {
            IPEndPoint audioEp = new IPEndPoint(IPAddress.Any, AUDIO_PORT);

            while (isConnected)
            {
                try
                {
                    byte[] alaw = audioReceiver.Receive(ref audioEp);
                    short[] decoded = ALawDecoder.ALawDecode(alaw);

                    byte[] pcm = new byte[decoded.Length * 2];
                    Buffer.BlockCopy(decoded, 0, pcm, 0, pcm.Length);

                    audioPlayer?.AddSamples(pcm, 0, pcm.Length);
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch (Exception ex)
                {
                    if (isConnected)
                        Log("[ERROR] Audio loop: " + ex.Message);
                }
            }
        }

        private void StartChat()
        {
            try
            {
                // Enviar mensajes a servidor
                chatSender = new UdpClient();
                chatSenderEndpoint = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), CHAT_CLIENT_PORT);
                chatSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

                // Recibir mensajes del servidor
                chatReceiver = new UdpClient();
                chatReceiver.ExclusiveAddressUse = false;
                chatReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                chatReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, CHAT_SERVER_PORT));
                chatReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

                chatTask = Task.Run(() => ReceiveChatLoop());
                Log("[INFO] Chat initialized.");
            }
            catch (Exception ex)
            {
                Log("[ERROR] Chat: " + ex.Message);
            }
        }

        private void ReceiveChatLoop()
        {
            while (isConnected)
            {
                try
                {
                    byte[] buffer = chatReceiver.Receive(ref chatReceiverEndpoint);
                    string message = Encoding.UTF8.GetString(buffer);

                    BeginInvoke(new Action(() =>
                    {
                        AppendChatMessage(message);
                    }));
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch (Exception ex)
                {
                    if (isConnected)
                        Log("[ERROR] Chat receive: " + ex.Message);
                }
            }
        }

        private void AppendChatMessage(string message)
        {
            try
            {
                // Parsear formato: "nombre;mensaje" o ";mensaje_sistema"
                string[] parts = message.Split(';');
                
                if (parts.Length >= 2)
                {
                    string sender = parts[0];
                    string text = parts[1];

                    if (string.IsNullOrEmpty(sender))
                    {
                        // Mensaje del sistema
                        listChat?.Items.Add($"[SYSTEM] {text}");
                    }
                    else
                    {
                        // Mensaje de usuario
                        listChat?.Items.Add($"[{DateTime.Now:HH:mm}] {sender}: {text}");
                    }

                    if (listChat != null && listChat.Items.Count > 200)
                        listChat.Items.RemoveAt(0);

                    if (listChat != null && listChat.Items.Count > 0)
                        listChat.TopIndex = listChat.Items.Count - 1;
                }
            }
            catch { }
        }

        private void OnSendMessage(object sender, EventArgs e)
        {
            SendChat();
        }

        private void OnMessageKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                SendChat();
            }
        }

        private void SendChat()
        {
            if (!isConnected || chatSender == null) return;

            string text = txtMessage?.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                byte[] msg = Encoding.UTF8.GetBytes($"{userName};{text}");
                chatSender.Send(msg, msg.Length, chatSenderEndpoint);

                // Mostrar en cliente local
                BeginInvoke(new Action(() =>
                {
                    listChat?.Items.Add($"[{DateTime.Now:HH:mm}] You: {text}");
                    if (listChat != null && listChat.Items.Count > 200)
                        listChat.Items.RemoveAt(0);
                    if (listChat != null && listChat.Items.Count > 0)
                        listChat.TopIndex = listChat.Items.Count - 1;
                    txtMessage?.Clear();
                }));
            }
            catch (Exception ex)
            {
                Log("[ERROR] Send chat: " + ex.Message);
            }
        }

        private void OnSettings(object sender, EventArgs e)
        {
            string newName = Microsoft.VisualBasic.Interaction.InputBox("Enter your name:", "Settings", userName);
            if (!string.IsNullOrWhiteSpace(newName))
            {
                userName = newName;
                lblProfileName.Text = $"Signed in as {userName}";
            }
        }

        private void OnOpenSettings(object sender, EventArgs e)
        {
            OnSettings(sender, e);
        }

        private void OnUiTimerTick(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                lblFps.Text = "FPS: --";
                lblLatency.Text = "Latency: --";
                lblJitter.Text = "Jitter: --";
                lblLoss.Text = "Loss: --";
                return;
            }

            try
            {
                var snapshot = performanceTracker.BuildSnapshot();
                
                if (snapshot.HasSamples)
                {
                    lblFps.Text = string.Format("FPS: {0:F1}", snapshot.FramesPerSecond);
                    lblLatency.Text = string.Format("Latency: {0:F1} ms", snapshot.AverageLatencyMs);
                    lblJitter.Text = string.Format("Jitter: {0:F1} ms", snapshot.JitterMs);
                    lblLoss.Text = string.Format("Loss: {0} pkts", snapshot.LostPackets);
                }
                else
                {
                    lblFps.Text = "FPS: 0.0";
                    lblLatency.Text = "Latency: 0.0 ms";
                    lblJitter.Text = "Jitter: 0.0 ms";
                    lblLoss.Text = "Loss: 0 pkts";
                }
            }
            catch (Exception ex)
            {
                Log("[ERROR] Update metrics: " + ex.Message);
            }
        }

        private void Log(string message)
        {
            try
            {
                BeginInvoke(new Action(() =>
                {
                    listDiagnostics?.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                    if (listDiagnostics != null && listDiagnostics.Items.Count > 100)
                        listDiagnostics.Items.RemoveAt(0);
                    if (listDiagnostics != null && listDiagnostics.Items.Count > 0)
                        listDiagnostics.TopIndex = listDiagnostics.Items.Count - 1;
                }));
            }
            catch { }
        }

        private void OnClientClosing(object sender, FormClosingEventArgs e)
        {
            OnDisconnect(null, null);
        }
    }
}
