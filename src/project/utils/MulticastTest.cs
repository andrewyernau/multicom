using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MulticastTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("PRUEBA DE MULTICAST - MultiCom");
            Console.WriteLine("==============================================");
            Console.WriteLine();
            Console.WriteLine("Selecciona modo:");
            Console.WriteLine("1 - EMISOR (ejecutar en PC servidor)");
            Console.WriteLine("2 - RECEPTOR (ejecutar en PC cliente)");
            Console.Write("Opcion: ");
            
            string opcion = Console.ReadLine();
            
            if (opcion == "1")
            {
                IniciarEmisor();
            }
            else if (opcion == "2")
            {
                IniciarReceptor();
            }
            else
            {
                Console.WriteLine("Opcion invalida");
            }
        }
        
        static void IniciarEmisor()
        {
            Console.WriteLine();
            Console.WriteLine("[EMISOR] Iniciando envio multicast...");
            Console.WriteLine("[EMISOR] IP: 224.0.0.1, Puerto: 9999");
            Console.WriteLine("[EMISOR] TTL: 32 (cruza red local)");
            Console.WriteLine("[EMISOR] Presiona CTRL+C para detener");
            Console.WriteLine();
            
            UdpClient sender = new UdpClient();
            
            // CRITICO: Configurar TTL ANTES de enviar
            sender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 32);
            
            IPAddress multicastAddress = IPAddress.Parse("224.0.0.1");
            sender.JoinMulticastGroup(multicastAddress);
            IPEndPoint endPoint = new IPEndPoint(multicastAddress, 9999);
            
            int contador = 0;
            while (true)
            {
                string mensaje = string.Format("[TEST] Mensaje #{0} desde {1} - {2}", 
                    contador, Environment.MachineName, DateTime.Now.ToString("HH:mm:ss"));
                byte[] datos = Encoding.UTF8.GetBytes(mensaje);
                
                sender.Send(datos, datos.Length, endPoint);
                
                Console.WriteLine("[ENVIADO] " + mensaje);
                contador++;
                
                System.Threading.Thread.Sleep(2000);
            }
        }
        
        static void IniciarReceptor()
        {
            Console.WriteLine();
            Console.WriteLine("[RECEPTOR] Iniciando escucha multicast...");
            Console.WriteLine("[RECEPTOR] IP: 224.0.0.1, Puerto: 9999");
            Console.WriteLine("[RECEPTOR] Esperando mensajes...");
            Console.WriteLine("[RECEPTOR] Presiona CTRL+C para detener");
            Console.WriteLine();
            
            UdpClient receiver = new UdpClient();
            
            // CRITICO: Orden correcto para recepcion
            receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 9999);
            receiver.Client.Bind(localEP);
            
            IPAddress multicastAddress = IPAddress.Parse("224.0.0.1");
            receiver.JoinMulticastGroup(multicastAddress);
            
            Console.WriteLine("[OK] Suscrito al grupo multicast 224.0.0.1");
            Console.WriteLine();
            
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            
            int contador = 0;
            while (true)
            {
                try
                {
                    byte[] datos = receiver.Receive(ref remoteEP);
                    string mensaje = Encoding.UTF8.GetString(datos);
                    
                    contador++;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[RECIBIDO #" + contador + "] " + mensaje);
                    Console.WriteLine("                  Desde: " + remoteEP.Address + ":" + remoteEP.Port);
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERROR] " + ex.Message);
                    Console.ResetColor();
                }
            }
        }
    }
}
