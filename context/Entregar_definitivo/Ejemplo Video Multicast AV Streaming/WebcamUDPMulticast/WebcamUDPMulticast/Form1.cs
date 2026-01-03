using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Touchless.Vision.Camera;
using NAudio.Wave;
using ALaw;

namespace WebcamUDPMulticast
{
    public partial class Form1 : Form
    {
        private CameraFrameSource _frameSource;
        private static Bitmap _latestFrame;

        // AUDIO
        private WaveInEvent waveIn;
        private UdpClient audioSender;
        private IPEndPoint audioEndPoint;

        // CHAT
        private UdpClient chatClient;
        private IPEndPoint chatEP;
        private const int CHAT_PORT = 8082;
        private const string CHAT_IP = "224.0.0.1";
        private bool escuchandoChat = true;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (Camera cam in CameraService.AvailableCameras)
            {
                comboBoxCameras.Items.Add(cam);
            }

            Task.Run(() => recibir_mensajes_chat());
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
            pictureBox2.Invalidate();
        }

        private void drawLatestImage(object sender, PaintEventArgs e)
        {
            if (_latestFrame != null)
            {
                Bitmap resized = new Bitmap(_latestFrame, new Size(320, 240));
                e.Graphics.DrawImage(resized, 0, 0, resized.Width, resized.Height);

                try
                {
                    UdpClient udpServer = new UdpClient();
                    IPAddress multicastaddress = IPAddress.Parse("224.0.0.1");
                    udpServer.JoinMulticastGroup(multicastaddress);
                    IPEndPoint remote = new IPEndPoint(multicastaddress, 8080);

                    // --- CABECERA PERSONALIZADA ---
                    uint imageNumber = (uint)Environment.TickCount; // Opcional: puedes usar contador real
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    byte[] jpegData = ImageToByteArray(resized);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.Write(BitConverter.GetBytes(imageNumber), 0, 4);
                        ms.Write(BitConverter.GetBytes(timestamp), 0, 8);
                        ms.Write(jpegData, 0, jpegData.Length);

                        byte[] paquete = ms.ToArray();
                        udpServer.Send(paquete, paquete.Length, remote);
                    }

                    udpServer.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al enviar video: " + ex.Message);
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

        private void IniciarTransmisionAudio()
        {
            audioSender = new UdpClient();
            audioEndPoint = new IPEndPoint(IPAddress.Parse("224.0.0.1"), 8081);

            waveIn = new WaveInEvent();
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new WaveFormat(8000, 16, 1);
            waveIn.BufferMilliseconds = 50;
            waveIn.DataAvailable += OnAudioCaptured;
            waveIn.StartRecording();
        }

        private void OnAudioCaptured(object sender, WaveInEventArgs e)
        {
            try
            {
                byte[] encoded = ALawEncoder.ALawEncode(e.Buffer);
                audioSender.Send(encoded, encoded.Length, audioEndPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar audio: " + ex.Message);
            }
        }

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

        private void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                Camera c = (Camera)comboBoxCameras.SelectedItem;
                setFrameSource(new CameraFrameSource(c));
                _frameSource.Camera.CaptureWidth = 320;
                _frameSource.Camera.CaptureHeight = 240;
                _frameSource.Camera.Fps = 20;
                _frameSource.NewFrame += OnImageCaptured;
                pictureBox2.Paint += new PaintEventHandler(drawLatestImage);
                _frameSource.StartFrameCapture();

                IniciarTransmisionAudio();
            }
            catch (Exception x)
            {
                Console.WriteLine(x.ToString());
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) { }

        private void pictureBox2_Click(object sender, EventArgs e) { }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            escuchandoChat = false;
            chatClient?.Close();
            waveIn?.StopRecording();
            audioSender?.Close();
        }
    }
}