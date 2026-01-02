using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebcamUDPMulticast
{
    public partial class Form1 : Form
    {
        private const string MULTICAST_ADDRESS = "224.0.0.1";
        private const int UDP_PORT = 8080;

        private CancellationTokenSource receiverCts;
        private Task receiverTask;
        private UdpClient udpClient;
        private IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, UDP_PORT);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            StartReceiver();
        }

        private void StartReceiver()
        {
            StopReceiver();

            try
            {
                receiverCts = new CancellationTokenSource();
                udpClient = new UdpClient(AddressFamily.InterNetwork);
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
                udpClient.Client.ReceiveTimeout = 1000;
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, UDP_PORT));
                udpClient.JoinMulticastGroup(IPAddress.Parse(MULTICAST_ADDRESS));

                receiverTask = Task.Run(() => ReceiveLoop(receiverCts.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"[AGENT] Unable to start multicast reception: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopReceiver();
            }
        }

        private void StopReceiver()
        {
            if (receiverCts == null)
            {
                return;
            }

            receiverCts.Cancel();

            if (udpClient != null)
            {
                udpClient.Close();
                udpClient = null;
            }

            try
            {
                receiverTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException)
            {
            }

            receiverTask = null;
            receiverCts.Dispose();
            receiverCts = null;
        }

        private void ReceiveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var payload = udpClient.Receive(ref remoteEndpoint);
                    if (payload == null || payload.Length == 0)
                    {
                        continue;
                    }

                    var frame = ByteArrayToImage(payload);
                    RenderFrame(frame);
                }
                catch (SocketException ex)
                {
                    if (token.IsCancellationRequested || ex.SocketErrorCode == SocketError.Interrupted)
                    {
                        break;
                    }

                    if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        continue;
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Non-critical frame error, keep listening
                }
            }
        }

        private void RenderFrame(Image frame)
        {
            if (pictureBox1.InvokeRequired)
            {
                pictureBox1.BeginInvoke(new Action(() => RenderFrame(frame)));
                return;
            }

            var previous = pictureBox1.Image;
            pictureBox1.Image = frame;
            previous?.Dispose();
        }

        private Image ByteArrayToImage(byte[] byteArrayIn)
        {
            using (var ms = new MemoryStream(byteArrayIn))
            {
                return Image.FromStream(ms);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopReceiver();
            base.OnFormClosing(e);
        }
    }
}
