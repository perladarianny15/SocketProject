using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketProgram
{
    class Program
    {
        static readonly object _lock = new object();
        static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();

        static void Main(string[] args)
        {
            StartServer();
        }
       
        public static void StartServer()
        {
            int count = 1;

            Console.WriteLine("Waiting for a client...");

            TcpListener ServerSocket = new TcpListener(IPAddress.Any, 9050);
            ServerSocket.Start();

            while (true)
            {
                TcpClient client = ServerSocket.AcceptTcpClient();

                var iPAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                var iPPort = ((IPEndPoint)client.Client.RemoteEndPoint).Port;

                Console.WriteLine("Someone connected!!");

                Console.WriteLine("Connected with {0} at port {1}", iPAddress, iPPort);

                lock (_lock) list_clients.Add(count, client);
                
                Thread t = new Thread(handle_clients);
                t.Start(count);
                count++;
            }
        }
        public static void handle_clients(object o)
        {
            int id = (int)o;
            TcpClient client;

            lock (_lock) client = list_clients[id];

            while (true)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int byte_count = stream.Read(buffer, 0, buffer.Length);

                if (byte_count == 0)
                {
                    break;
                }

                string data = Encoding.ASCII.GetString(buffer, 0, byte_count);
                broadcast(data);
                Console.WriteLine(data);
            }

            lock (_lock) list_clients.Remove(id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        public static void broadcast(string data)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(data + Environment.NewLine);

            lock (_lock)
            {
                foreach (TcpClient c in list_clients.Values)
                {
                    NetworkStream stream = c.GetStream();

                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
