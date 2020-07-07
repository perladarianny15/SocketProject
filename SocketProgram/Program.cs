using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                if (client.Connected)
                {

                    var random = new Random();
                    List<string> ClientsName = new List<string> { "Juan", "Pedro", "Andrea" };
                    Dictionary<int, string> ClientsList = new Dictionary<int, string>();

                    Console.WriteLine("OK.");

                    Console.WriteLine("Incoming connection from: " + client.Client.RemoteEndPoint);

                    lock (_lock) list_clients.Add(count, client);

                    //foreach (var clientes in list_clients)
                    //{
                    //    int index = random.Next(ClientsName.Count);

                    //    if (!ClientsList.ContainsKey(clientes.Key))
                    //    {
                    //        ClientsList.Add(clientes.Key, ClientsName[index]);
                    //    }
                    //}

                    //string clientname = ClientsList.Where(x => x.Key == count).Select(c => c.Value).FirstOrDefault();


                    Thread t = new Thread(handle_clients);
                    t.Start(count);
                    count++;
                }else
                {
                    Console.WriteLine("Out of service.");
                }
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
                Console.WriteLine(client.Client.RemoteEndPoint + " Says: "
                    + data);
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
