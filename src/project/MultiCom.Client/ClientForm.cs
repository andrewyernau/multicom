using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using MultiCom.Shared.Audio;
using MultiCom.Shared.Networking;

namespace MultiCom.Client
{
    public partial class ClientForm : Form
    {
        private static Bitmap _latestFrame;
        private bool receivingVideo = false;
        private Task videoTask;
        
        // Estadísticas de video
        private VideoStatistics videoStats = new VideoStatistics();
        
        // Chat multicast
        private UdpClient chatSender;
        private UdpClient chatReceiver;
        private Task chatTask;
        private bool receivingChat = false;
        private const int CHAT_PORT = 8082;
        private const string CHAT_IP = "224.0.0.1";
        
        // User settings
        private string userName = "Usuario";
        
        // Audio multicast
        private UdpClient audioReceiver;
        private Task audioTask;
        private bool receivingAudio = false;
        private NAudio.Wave.WaveOutEvent waveOut;
        private NAudio.Wave.BufferedWaveProvider waveProvider;
        private const int AUDIO_PORT = 8081;

        // Métricas
        private long? ultimaLatencia = null;
        private double jitterAcumulado = 0;
        private int muestras = 0;
        private int frameCount = 0;

        public ClientForm()
        {
            InitializeComponent();
        }

        private void OnClientLoaded(object sender, EventArgs e)
        {
            ApplyDiscordPalette();
            this.Text = "Cliente MultiCom";

            // Iniciar recepcion
            StartVideoReception();
            StartChatReception();
            StartAudioReception();
        }

        private void StartAudioReception()
        {
            if (receivingAudio) return;

            receivingAudio = true;
            audioTask = Task.Run(() => ReceiveAudio());
            Log("Audio iniciado");
        }

        private void ReceiveAudio()
        {
            try
            {
                // Configurar socket
                audioReceiver = new UdpClient();
                audioReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                
                // Bind al puerto
                IPEndPoint audioEP = new IPEndPoint(IPAddress.Any, AUDIO_PORT);
                audioReceiver.Client.Bind(audioEP);
                
                // Unirse al grupo multicast
                audioReceiver.JoinMulticastGroup(IPAddress.Parse(CHAT_IP));

                waveProvider = new NAudio.Wave.BufferedWaveProvider(new NAudio.Wave.WaveFormat(8000, 16, 1))
                {
                    DiscardOnBufferOverflow = true
                };
                
                waveOut = new NAudio.Wave.WaveOutEvent();
                waveOut.Init(waveProvider);
                waveOut.Play();

                while (receivingAudio)
                {
                    try
                    {
                        byte[] alaw = audioReceiver.Receive(ref audioEP);
                        short[] decoded = ALawDecoder.ALawDecode(alaw);

                        byte[] pcm = new byte[decoded.Length * 2];
                        Buffer.BlockCopy(decoded, 0, pcm, 0, pcm.Length);

                        waveProvider.AddSamples(pcm, 0, pcm.Length);
                    }
                    catch (SocketException)
                    {
                        // Timeout o error de socket
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error en audio: {ex.Message}");
            }
            finally
            {
                waveOut?.Dispose();
                audioReceiver?.Close();
            }
        }

        private void StartChatReception()
        {
            if (receivingChat) return;

            receivingChat = true;
            chatTask = Task.Run(() => ReceiveChat());
            Log("Chat iniciado");
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
                
                // Unirse al grupo multicast
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
                             
                             // Mostrar en el chat los mensajes
                             if (listChat.InvokeRequired)
                             {
                                 listChat.BeginInvoke(new Action(() =>
                                 {
                                     listChat.Items.Add(mensaje);
                                     listChat.TopIndex = listChat.Items.Count - 1;
                                 }));
                             }
                             else
                             {
                                 listChat.Items.Add(mensaje);
                                 listChat.TopIndex = listChat.Items.Count - 1;
                             }
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

        private void StartVideoReception()
        {
            if (receivingVideo) return;

            receivingVideo = true;
            videoTask = Task.Run(() => ReceiveVideo());
            Log("Recepción de video iniciada");
        }

        private void ReceiveVideo()
        {
            // Configurar socket
            UdpClient udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            // Bind al puerto
            IPEndPoint remoteep = new IPEndPoint(IPAddress.Any, 8080);
            udpClient.Client.Bind(remoteep);
            
            // Unirse al grupo multicast
            IPAddress multicastaddress = IPAddress.Parse("224.0.0.1");
            udpClient.JoinMulticastGroup(multicastaddress);

            while (receivingVideo)
            {
                try
                {
                    byte[] paquete = udpClient.Receive(ref remoteep);

                    // Parsear con cabecera mejorada
                    VideoHeader header;
                    byte[] payload;
                    
                    if (!VideoHeader.TryParsePacket(paquete, out header, out payload))
                    {
                        videoStats.RegisterCorrupted();
                        continue;
                    }

                    // Verificar checksum
                    if (!header.VerifyChecksum(payload))
                    {
                        videoStats.RegisterCorrupted();
                        continue;
                    }

                    // Registrar recepción y detectar pérdidas
                    int lostPackets = videoStats.RegisterPacket(header.ImageNumber);

                    // Calcular latencia usando Ticks
                    long ahoraTicks = DateTime.UtcNow.Ticks;
                    long latenciaTicks = ahoraTicks - header.TimestampUtc;
                    long latenciaMs = latenciaTicks / TimeSpan.TicksPerMillisecond;

                    // Calcular jitter
                    if (ultimaLatencia.HasValue)
                    {
                        long variacion = Math.Abs(latenciaMs - ultimaLatencia.Value);
                        jitterAcumulado += variacion;
                        muestras++;
                    }
                    ultimaLatencia = latenciaMs;

                    // Convertir a imagen
                    _latestFrame = (Bitmap)ByteArrayToImage(payload);
                    if (_latestFrame != null)
                    {
                        Bitmap resizedImage = new Bitmap(_latestFrame, new Size(640, 480));
                        
                        // Mostrar en pictureBoxVideo
                        if (pictureBoxVideo != null)
                        {
                            pictureBoxVideo.Invoke((MethodInvoker)(() => pictureBoxVideo.Image = resizedImage));
                        }
                        
                        frameCount++;

                        // Actualizar métricas
                        this.Invoke((MethodInvoker)(() =>
                        {
                            lblLatency.Text = $"Latencia: {latenciaMs} ms";
                            lblJitter.Text = $"Jitter: {(muestras > 0 ? (jitterAcumulado / muestras).ToString("F2") : "0")} ms";
                            lblFps.Text = $"Frames: {frameCount} | Perdidos: {videoStats.TotalLost} | Tasa: {videoStats.LossRate:F2}%";
                        }));
                    }
                }
                catch (Exception ex)
                {
                    if (receivingVideo)
                    {
                        Log($"Error recibiendo video: {ex.Message}");
                    }
                }
            }

            udpClient.Close();
        }

        public Image ByteArrayToImage(byte[] byteArrayIn)
        {
            using (var ms = new MemoryStream(byteArrayIn))
            {
                return Image.FromStream(ms);
            }
        }

        private void Log(string mensaje)
        {
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now:HH:mm:ss} {mensaje}");
        }

        private void ApplyDiscordPalette()
        {
            BackColor = Color.FromArgb(54, 57, 63);
        }

        private void OnConnect(object sender, EventArgs e)
        {
        }

        private void OnDisconnect(object sender, EventArgs e)
        {
            receivingVideo = false;
            receivingChat = false;
            receivingAudio = false;
            
            if (videoTask != null)
            {
                try { videoTask.Wait(1000); } catch { }
            }
            
            if (chatTask != null)
            {
                try { chatTask.Wait(1000); } catch { }
            }
            
            if (audioTask != null)
            {
                try { audioTask.Wait(1000); } catch { }
            }
            
            Log("Desconectado");
            Application.Exit();
        }

        private void OnOpenSettings(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new Form())
                {
                    dialog.Text = "Configuración de Usuario";
                    dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dialog.StartPosition = FormStartPosition.CenterParent;
                    dialog.MaximizeBox = false;
                    dialog.MinimizeBox = false;
                    dialog.BackColor = Color.FromArgb(54, 57, 63);
                    dialog.ForeColor = Color.White;
                    dialog.ClientSize = new Size(350, 120);

                    var lblName = new Label
                    {
                        Text = "Nombre de usuario:",
                        Location = new Point(20, 20),
                        AutoSize = true,
                        ForeColor = Color.White
                    };

                    var txtName = new TextBox
                    {
                        Text = userName,
                        Location = new Point(20, 45),
                        Size = new Size(310, 25),
                        BackColor = Color.FromArgb(32, 34, 37),
                        ForeColor = Color.White
                    };

                    var btnOk = new Button
                    {
                        Text = "Guardar",
                        Location = new Point(160, 80),
                        Size = new Size(80, 30),
                        BackColor = Color.FromArgb(67, 181, 129),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        DialogResult = DialogResult.OK
                    };

                    var btnCancel = new Button
                    {
                        Text = "Cancelar",
                        Location = new Point(250, 80),
                        Size = new Size(80, 30),
                        BackColor = Color.FromArgb(114, 118, 125),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        DialogResult = DialogResult.Cancel
                    };

                    dialog.Controls.Add(lblName);
                    dialog.Controls.Add(txtName);
                    dialog.Controls.Add(btnOk);
                    dialog.Controls.Add(btnCancel);
                    dialog.AcceptButton = btnOk;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string newName = txtName.Text.Trim();
                        if (!string.IsNullOrEmpty(newName))
                        {
                            userName = newName;
                            Log($"Nombre de usuario cambiado a: {userName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error al abrir configuración: {ex.Message}");
            }
        }

        private void OnSendMessage(object sender, EventArgs e)
        {
            try
            {
                string texto = txtMessage.Text.Trim();
                if (string.IsNullOrEmpty(texto)) return;

                // Crear envelope con información del usuario
                var envelope = new MultiCom.Shared.Chat.ChatEnvelope(
                    Guid.NewGuid(),
                    userName,
                    texto,
                    DateTime.UtcNow
                );
                
                byte[] datos = envelope.ToPacket();
                chatSender = new UdpClient();
                
                // Configurar TTL por si hiciera falta
                chatSender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 32);
                
                chatSender.Send(datos, datos.Length, new IPEndPoint(IPAddress.Parse(CHAT_IP), CHAT_PORT));
                chatSender.Close();

                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                Log($"Error al enviar mensaje: {ex.Message}");
            }
        }

        private void OnMessageKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                OnSendMessage(sender, EventArgs.Empty);
            }
        }

        private void OnUiTimerTick(object sender, EventArgs e)
        {
            
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            receivingVideo = false;
            receivingChat = false;
            receivingAudio = false;
            
            if (videoTask != null)
            {
                try { videoTask.Wait(1000); } catch { }
            }
            
            if (chatTask != null)
            {
                try { chatTask.Wait(1000); } catch { }
            }
            
            if (audioTask != null)
            {
                try { audioTask.Wait(1000); } catch { }
            }
            
            chatReceiver?.Close();
            waveOut?.Dispose();
            audioReceiver?.Close();
            
            base.OnFormClosing(e);
        }
    }
}
