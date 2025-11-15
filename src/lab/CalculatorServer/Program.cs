using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using CalculatorLib;

namespace CalculatorServer
{
    /// <summary>
    /// Server application that exposes the Calculator service via .NET Remoting over HTTP.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("===========================================");
                Console.WriteLine("  Calculator Remote Server");
                Console.WriteLine("===========================================");
                Console.WriteLine();

                // Register HTTP channel on port 8090
                HttpChannel channel = new HttpChannel(8090);
                ChannelServices.RegisterChannel(channel, false);
                Console.WriteLine("[INFO] HTTP Channel registered on port 8090");

                // Set application name for the remoting service
                RemotingConfiguration.ApplicationName = "CalculatorService";
                Console.WriteLine("[INFO] Application name set: CalculatorService");

                // Register the Operaciones class as a well-known service type
                // Using Singleton mode: single instance serves all clients
                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(Operaciones),
                    "Operaciones",
                    WellKnownObjectMode.Singleton
                );

                Console.WriteLine("[INFO] Service registered: Operaciones");
                Console.WriteLine("[INFO] Mode: Singleton");
                Console.WriteLine();
                Console.WriteLine("Service URI: http://localhost:8090/CalculatorService/Operaciones");
                Console.WriteLine();
                Console.WriteLine("Available operations:");
                Console.WriteLine("  - Sumar(double a, double b)");
                Console.WriteLine("  - Restar(double a, double b)");
                Console.WriteLine("  - Multiplicar(double a, double b)");
                Console.WriteLine("  - Dividir(double a, double b)");
                Console.WriteLine();
                Console.WriteLine("===========================================");
                Console.WriteLine("Server is running. Press ENTER to stop...");
                Console.WriteLine("===========================================");

                // Running until user presses Enter
                Console.ReadLine();

                Console.WriteLine();
                Console.WriteLine("[INFO] Shutting down server...");
                ChannelServices.UnregisterChannel(channel);
                Console.WriteLine("[INFO] Server stopped successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error starting server:");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();
            }
        }
    }
}
