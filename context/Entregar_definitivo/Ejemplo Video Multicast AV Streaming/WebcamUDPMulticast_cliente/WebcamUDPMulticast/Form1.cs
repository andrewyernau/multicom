using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using ALaw;

namespace WebcamUDPMulticast
{
    public partial class Form1 : Form
    {
        private static Bitmap _latestFrame;

        // AUDIO
        private UdpClient audioClient;
        private IPEndPoint audioEP;
        private WaveOut waveOut;
        private BufferedWaveProvider waveProvider;
        private bool escuchandoAudio = true;

        // CHAT
        private UdpClient chatClient;
        private IPEndPoint chatEP;
        private const int CHAT_PORT = 8082;
        private const string CHAT_IP = "224.0.0.1";
        private bool escuchandoChat = true;

        // LATENCIA Y JITTER
        private long? ultimaLatencia = null;
        private double jitterAcumulado = 0;
        private int muestras = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Task.Run(() => visualizar_imagen());
            Task.Run(() => recibir_audio());
            Task.Run(() => recibir_mensajes_chat());
        }

        // ---------------------------- VIDEO ----------------------------
        private void visualizar_imagen()
        {
            UdpClient udpClient = new UdpClient();
            IPAddress multicastaddress = IPAddress.Parse("224.0.0.1");
            udpClient.JoinMulticastGroup(multicastaddress);

            IPEndPoint remoteep = new IPEndPoint(IPAddress.Any, 8080);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(remoteep);

            while (true)
            {
                try
                {
                    byte[] paquete = udpClient.Receive(ref remoteep);
                    if (paquete.Length <= 12) continue;

                    uint imageNumber = BitConverter.ToUInt32(paquete, 0);
                    long timestampEmisor = BitConverter.ToInt64(paquete, 4);
                    long ahora = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    long latencia = ahora - timestampEmisor;

                    if (ultimaLatencia.HasValue)
                    {
                        long variacion = Math.Abs(latencia - ultimaLatencia.Value);
                        jitterAcumulado += variacion;
                        muestras++;
                    }

                    ultimaLatencia = latencia;

                    byte[] jpeg = new byte[paquete.Length - 12];
                    Array.Copy(paquete, 12, jpeg, 0, jpeg.Length);

                    _latestFrame = (Bitmap)byteArrayToImage(jpeg);
                    if (_latestFrame != null)
                    {
                        Bitmap resizedImage = new Bitmap(_latestFrame, new Size(320, 240));
                        pictureBox1.Invoke((MethodInvoker)(() => pictureBox1.Image = resizedImage));

                        this.Invoke((MethodInvoker)(() =>
                        {
                            this.Text = $"Cliente - Latencia: {latencia} ms - Jitter: {(muestras > 0 ? (jitterAcumulado / muestras).ToString("F2") : "0")} ms";
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error recibiendo video: " + ex.Message);
                }
            }
        }

        public Image byteArrayToImage(byte[] byteArrayIn)
        {
            using (var ms = new MemoryStream(byteArrayIn))
            {
                return Image.FromStream(ms);
            }
        }

        // ---------------------------- AUDIO ----------------------------
        private void recibir_audio()
        {
            try
            {
                audioClient = new UdpClient();
                audioClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                audioEP = new IPEndPoint(IPAddress.Any, 8081);
                audioClient.Client.Bind(audioEP);
                audioClient.JoinMulticastGroup(IPAddress.Parse("224.0.0.1"));

                waveOut = new WaveOut();
                waveProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
                waveProvider.DiscardOnBufferOverflow = true;

                waveOut.Init(waveProvider);
                waveOut.Play();

                while (escuchandoAudio)
                {
                    byte[] alaw = audioClient.Receive(ref audioEP);
                    short[] decoded = ALawDecoder.ALawDecode(alaw);

                    byte[] pcm = new byte[decoded.Length * 2];
                    Buffer.BlockCopy(decoded, 0, pcm, 0, pcm.Length);

                    waveProvider.AddSamples(pcm, 0, pcm.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error recibiendo audio: " + ex.Message);
            }
        }

        // ---------------------------- CHAT ----------------------------
        private void recibir_mensajes_chat()
        {
            try
            {
                chatClient = new UdpClient(CHAT_PORT);
                chatClient.JoinMulticastGroup(IPAddress.Parse(CHAT_IP));
                chatClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                chatEP = new IPEndPoint(IPAddress.Any, CHAT_PORT);

                while (escuchandoChat)
                {
                    byte[] buffer = chatClient.Receive(ref chatEP);
                    string mensaje = Encoding.Unicode.GetString(buffer);

                    listBoxChat.Invoke((MethodInvoker)(() =>
                    {
                        listBoxChat.Items.Add($"[Otro] {mensaje}");
                    }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error recibiendo chat: " + ex.Message);
            }
        }

        private void buttonEnviar_Click(object sender, EventArgs e)
        {
            try
            {
                string texto = textBoxMensaje.Text.Trim();
                if (string.IsNullOrEmpty(texto)) return;

                byte[] datos = Encoding.Unicode.GetBytes(texto);
                UdpClient clienteEnvio = new UdpClient();
                clienteEnvio.Send(datos, datos.Length, new IPEndPoint(IPAddress.Parse(CHAT_IP), CHAT_PORT));
                clienteEnvio.Close();

                listBoxChat.Items.Add($"[Yo] {texto}");
                textBoxMensaje.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al enviar mensaje: " + ex.Message);
            }
        }

        // ---------------------------- FORM EVENTS ----------------------------
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            escuchandoAudio = false;
            escuchandoChat = false;
            audioClient?.Close();
            chatClient?.Close();
            waveOut?.Dispose();
        }

        private void pictureBox1_Click(object sender, EventArgs e) { }
        private void pictureBox1_Click_1(object sender, EventArgs e) { }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void button1_Click(object sender, EventArgs e) { }
    }
}