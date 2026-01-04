using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using MultiCom.Shared.Audio;

namespace MultiCom.Client
{
    public partial class ClientForm : Form
    {
        private static Bitmap _latestFrame;
        private bool receivingVideo = false;
        private Task videoTask;
        
        // Chat multicast
        private UdpClient chatSender;
        private UdpClient chatReceiver;
        private Task chatTask;
        private bool receivingChat = false;
        private const int CHAT_PORT = 8082;
        private const string CHAT_IP = "224.0.0.1";
        
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
                        string mensaje = System.Text.Encoding.Unicode.GetString(buffer);

                        // Mostrar en el cjat los mensajes
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
                    if (paquete.Length <= 12) continue;

                    // Leer cabecera
                    uint imageNumber = BitConverter.ToUInt32(paquete, 0);
                    long timestampEmisor = BitConverter.ToInt64(paquete, 4);
                    long ahora = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    long latencia = ahora - timestampEmisor;

                    // Calcular jitter
                    if (ultimaLatencia.HasValue)
                    {
                        long variacion = Math.Abs(latencia - ultimaLatencia.Value);
                        jitterAcumulado += variacion;
                        muestras++;
                    }
                    ultimaLatencia = latencia;

                    // Extraer JPEG
                    byte[] jpeg = new byte[paquete.Length - 12];
                    Array.Copy(paquete, 12, jpeg, 0, jpeg.Length);

                    // Convertir a imagen
                    _latestFrame = (Bitmap)ByteArrayToImage(jpeg);
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
                            lblLatency.Text = $"Latencia: {latencia} ms";
                            lblJitter.Text = $"Jitter: {(muestras > 0 ? (jitterAcumulado / muestras).ToString("F2") : "0")} ms";
                            lblFps.Text = $"Frames: {frameCount}";
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
            
        }

        private void OnSendMessage(object sender, EventArgs e)
        {
            try
            {
                string texto = txtMessage.Text.Trim();
                if (string.IsNullOrEmpty(texto)) return;

                // Enviar al grupo multicast
                byte[] datos = System.Text.Encoding.Unicode.GetBytes($"[Usuario] {texto}");
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
