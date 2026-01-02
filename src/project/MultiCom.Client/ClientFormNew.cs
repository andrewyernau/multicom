using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MultiCom.Client
{
    public partial class ClientForm : Form
    {
        private const string MULTICAST_VIDEO = "239.50.10.1";
        private const string MULTICAST_CHAT_SERVER = "239.50.10.2";
        private const string MULTICAST_CHAT_CLIENT = "239.50.10.4";
        private const int PORT_VIDEO = 5050;
        private const int PORT_CHAT_SERVER = 5051;
        private const int PORT_CHAT_CLIENT = 5053;

        private UdpClient videoReceiver;
        private UdpClient chatReceiverServer;
        private UdpClient chatSender;
        private Task videoTask;
        private Task chatTask;
        private CancellationTokenSource cts;
        private bool isConnected = false;

        private byte[] imagenCompleta;
        private int frameActual = -1;
        private string userName = "Invitado";

        public ClientForm()
        {
            InitializeComponent();
        }

        private void OnFormLoaded(object sender, EventArgs e)
        {
            ApplyDiscordPalette();
            lblProfileName.Text = $"Usuario: {userName}";
            Log("Listo. Presiona Connect para unirte.");
        }

        private void OnConnectClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Introduce tu nombre", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                userName = txtName.Text.Trim();
                lblProfileName.Text = $"Usuario: {userName}";
                isConnected = true;
                cts = new CancellationTokenSource();

                // Video receiver
                videoReceiver = new UdpClient();
                videoReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                videoReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, PORT_VIDEO));
                videoReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_VIDEO));

                // Chat receiver (del servidor)
                chatReceiverServer = new UdpClient();
                chatReceiverServer.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                chatReceiverServer.Client.Bind(new IPEndPoint(IPAddress.Any, PORT_CHAT_SERVER));
                chatReceiverServer.JoinMulticastGroup(IPAddress.Parse(MULTICAST_CHAT_SERVER));

                // Chat sender
                chatSender = new UdpClient();
                chatSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_CHAT_CLIENT));

                // Iniciar tareas
                videoTask = Task.Run(() => VideoReceiveLoop(cts.Token));
                chatTask = Task.Run(() => ChatReceiveLoop(cts.Token));

                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
                txtName.Enabled = false;

                Log("Conectado a la transmisión");

                // Enviar mensaje de unión
                EnviarMensaje($"{userName} se ha unido a la videoconferencia");
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
                Disconnect();
            }
        }

        private void OnDisconnectClick(object sender, EventArgs e)
        {
            if (isConnected)
            {
                EnviarMensaje($"{userName} se ha desconectado");
            }
            Disconnect();
        }

        private void Disconnect()
        {
            isConnected = false;

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }

            if (videoReceiver != null)
            {
                videoReceiver.Close();
                videoReceiver = null;
            }

            if (chatReceiverServer != null)
            {
                chatReceiverServer.Close();
                chatReceiverServer = null;
            }

            if (chatSender != null)
            {
                chatSender.Close();
                chatSender = null;
            }

            if (videoTask != null && !videoTask.IsCompleted)
            {
                try { videoTask.Wait(100); } catch { }
            }

            if (chatTask != null && !chatTask.IsCompleted)
            {
                try { chatTask.Wait(100); } catch { }
            }

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            txtName.Enabled = true;

            if (pictureVideo.Image != null)
            {
                pictureVideo.Image.Dispose();
                pictureVideo.Image = null;
            }

            imagenCompleta = null;
            frameActual = -1;

            Log("Desconectado");
        }

        private void VideoReceiveLoop(CancellationToken token)
        {
            var remoteEP = new IPEndPoint(IPAddress.Any, PORT_VIDEO);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    videoReceiver.Client.ReceiveTimeout = 1000;
                    byte[] buffer = videoReceiver.Receive(ref remoteEP);

                    if (buffer.Length < 28) continue;

                    // Parse header (28 bytes)
                    long timestamp = BitConverter.ToInt64(buffer, 0);
                    int nFrame = BitConverter.ToInt32(buffer, 8);
                    int nChunk = BitConverter.ToInt32(buffer, 12);
                    int totalChunks = BitConverter.ToInt32(buffer, 16);
                    int totalSize = BitConverter.ToInt32(buffer, 20);
                    int chunkSize = BitConverter.ToInt32(buffer, 24);

                    // Calcular latencia
                    DateTime sentTime = DateTime.FromBinary(timestamp);
                    double latencyMs = (DateTime.Now - sentTime).TotalMilliseconds;

                    // Payload
                    int payloadLength = buffer.Length - 28;
                    byte[] chunk = new byte[payloadLength];
                    Buffer.BlockCopy(buffer, 28, chunk, 0, payloadLength);

                    // Reassembly
                    if (nChunk == 0)
                    {
                        imagenCompleta = new byte[totalSize];
                        frameActual = nFrame;
                    }

                    if (nFrame == frameActual && imagenCompleta != null)
                    {
                        int offset = nChunk * chunkSize;
                        if (offset + payloadLength <= imagenCompleta.Length)
                        {
                            Buffer.BlockCopy(chunk, 0, imagenCompleta, offset, payloadLength);

                            // Si es el último chunk, mostrar imagen
                            if (nChunk == totalChunks - 1)
                            {
                                try
                                {
                                    using (var ms = new MemoryStream(imagenCompleta))
                                    {
                                        var image = Image.FromStream(ms);
                                        BeginInvoke(new Action(() =>
                                        {
                                            var old = pictureVideo.Image;
                                            pictureVideo.Image = image;
                                            old?.Dispose();

                                            lblLatency.Text = $"Latencia: {latencyMs:F0} ms";
                                        }));
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch (SocketException) { }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        BeginInvoke(new Action(() => Log("ERROR video: " + ex.Message)));
                    }
                }
            }
        }

        private void ChatReceiveLoop(CancellationToken token)
        {
            var remoteEP = new IPEndPoint(IPAddress.Any, PORT_CHAT_SERVER);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    chatReceiverServer.Client.ReceiveTimeout = 1000;
                    byte[] data = chatReceiverServer.Receive(ref remoteEP);
                    string mensaje = System.Text.Encoding.UTF8.GetString(data);

                    BeginInvoke(new Action(() => MostrarMensaje(mensaje)));
                }
                catch (SocketException) { }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        BeginInvoke(new Action(() => Log("ERROR chat: " + ex.Message)));
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

        private void OnSendMessageClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text) || chatSender == null || !isConnected)
            {
                return;
            }

            EnviarMensaje(txtMessage.Text);
            
            listChat.Items.Add($"[{DateTime.Now:HH:mm}] Tú: {txtMessage.Text}");
            if (listChat.Items.Count > 100)
            {
                listChat.Items.RemoveAt(0);
            }
            listChat.TopIndex = listChat.Items.Count - 1;
            
            txtMessage.Clear();
        }

        private void EnviarMensaje(string texto)
        {
            if (chatSender == null) return;

            try
            {
                string mensaje = $"{userName};{texto}";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(mensaje);
                var endpoint = new IPEndPoint(IPAddress.Parse(MULTICAST_CHAT_CLIENT), PORT_CHAT_CLIENT);
                chatSender.Send(data, data.Length, endpoint);
            }
            catch (Exception ex)
            {
                Log("ERROR envío: " + ex.Message);
            }
        }

        private void OnMessageKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                OnSendMessageClick(sender, e);
            }
        }

        private void Log(string mensaje)
        {
            if (listDiagnostics.InvokeRequired)
            {
                listDiagnostics.BeginInvoke(new Action(() => Log(mensaje)));
                return;
            }

            listDiagnostics.Items.Insert(0, $"{DateTime.Now:HH:mm:ss} {mensaje}");
            while (listDiagnostics.Items.Count > 50)
            {
                listDiagnostics.Items.RemoveAt(listDiagnostics.Items.Count - 1);
            }
        }

        private void ApplyDiscordPalette()
        {
            BackColor = Color.FromArgb(54, 57, 63);
            btnConnect.BackColor = Color.FromArgb(67, 181, 129);
            btnConnect.ForeColor = Color.White;
            btnDisconnect.BackColor = Color.FromArgb(240, 71, 71);
            btnDisconnect.ForeColor = Color.White;
            btnDisconnect.Enabled = false;
            btnSendMessage.BackColor = Color.FromArgb(88, 101, 242);
            btnSendMessage.ForeColor = Color.White;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isConnected)
            {
                EnviarMensaje($"{userName} se ha desconectado");
            }
            Disconnect();
            base.OnFormClosing(e);
        }
    }
}
