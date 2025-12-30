using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Touchless.Vision.Camera;

// UDP y Multicast.
using System.Net.Sockets;
using System.Net;
using System.IO;

// IMAGE
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace WebcamUDPMulticast
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Task t1 = new Task(visualizar_imagen);
            t1.Start();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            

            
        }

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
                    Byte[] payload = udpClient.Receive(ref remoteep);

                    pictureBox1.Image = byteArrayToImage(payload);
                }
                catch (Exception x)
                {
                    x.ToString();
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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        // Crear la comunicación multicast cliente

        IPEndPoint localEp = new IPEndPoint(IPAddress.Any, UDP_PORT);
        // configurar socket para reutilizar puerto / aceptar tráfico multicast
        // cliente.SetSocketOption(...); client.Bind(localEp);


        // Almacenar en memorystream para reconstruir imagen
        using (var ms = new MemoryStream(buffer))
        {
	        var img = Image.FromStream(ms);
            pictureBoxDisplay.Image = img;
        }

        /////////////
        // Visualizar
        /////////////
        Byte[] buffer = udpClient.Receive(ref localEp);

        Task t1 = new Task(visualizar_imagen);
        t1.Start();

        private void visualizar_imagen()
        {
            while (true)
            {
                try
                {
                    // recepción y procesamiento
                }
                catch (Exception) { /* manejo */ }
            }
        }
    }
}
