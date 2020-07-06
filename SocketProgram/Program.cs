using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            StartServer();
        }
        public static void StartServer()
        {
            Console.OutputEncoding = Encoding.UTF8;

            int recv;

            byte[] data = new byte[1024];

            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050);

            Socket newsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            newsock.Bind(ipep);

            newsock.Listen(10);

            Console.WriteLine("Waiting for a client...");

            Socket client = newsock.Accept();

            IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;

            Console.WriteLine("Connected with {0} at port {1}", clientep.Address, clientep.Port);

            string welcome = "Welcome to the chat";

            data = Encoding.UTF8.GetBytes(welcome);

            client.Send(data, data.Length, SocketFlags.None);

            string input;

            while (true)
            {

                data = new byte[1024];

                recv = client.Receive(data);

                if (recv == 0)

                    break;

                Console.WriteLine("Client: " + Encoding.UTF8.GetString(data, 0, recv));

                input = Console.ReadLine();

                Console.WriteLine("You: " + input);

                client.Send(Encoding.UTF8.GetBytes(input));
            }

            Console.WriteLine("Disconnected from {0}", clientep.Address);

            client.Close();

            newsock.Close();

            Console.ReadLine();
        }
    }
}
