using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ClientSocket
{
    class Program
    {
        static void Main(string[] args)
        {
            StartClient();
        }
        public static void StartClient()
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            int port = 9050;
            TcpClient client = new TcpClient();
            client.Connect(ip, port);
            Console.WriteLine("client connected!!");
            NetworkStream ns = client.GetStream();
            Thread thread = new Thread(o => ReceiveData((TcpClient)o));

            thread.Start(client);

            string message;

            while (true)
            {
                message = Console.ReadLine();
                if (message == "exit")
                {
                    client.Client.Shutdown(SocketShutdown.Send);
                    thread.Join();
                    ns.Close();
                    client.Close();
                    Console.WriteLine("disconnect from server!!");
                    Console.ReadKey();
                    break;
                }
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                ns.Write(buffer, 0, buffer.Length);
            }
        }
        static void ReceiveData(TcpClient client)
        {
            NetworkStream ns = client.GetStream();
            byte[] receivedBytes = new byte[1024];
            int byte_count;

            while ((byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0)
            {
                Console.Write("Msg:" + Encoding.ASCII.GetString(receivedBytes, 0, byte_count));
            }
        }
    }
}
