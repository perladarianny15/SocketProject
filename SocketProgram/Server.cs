﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketProgram
{
    class Server
    {
        private const string commandString = "Welcome to chat.\n!users - prints online users.\n!msgto [user] [message] - sends a message to someone.\n!rename - changes your name.\n";

        public Thread Thread { get; private set; }

        public Socket ListeningSocket { get; private set; }

        public List<Client> ClientList { get; private set; }

        public IPEndPoint EndPoint { get; private set; }

        public bool ServerStatus { get; private set; }

        public int UserIdCounter { get; set; }

        public int Port { get; set; }

        public static bool IsSocketConnected(Socket s)
        {
            if (!s.Connected)
                return false;

            if (s.Available == 0)
                if (s.Poll(1000, SelectMode.SelectRead))
                    return false;

            return true;
        }

        public Server()
        {
            Port = 0;
            UserIdCounter = 0;
            ServerStatus = false;
            ClientList = new List<Client>();
        }

        private bool SendMessage(Client client, string msg)
        {
            try
            {
                client.Socket.Send(Encoding.Unicode.GetBytes(msg));
                return true;
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unable to send the message.");
            }
            return false;
        }

        private void WaitForConnections()
        {
            while (ServerStatus)
            {
                Client client = new Client();
                try
                {
                    client.Socket = ListeningSocket.Accept();

                    // Receive username from client
                    byte[] buff = new byte[512];

                    int res = client.Socket.Receive(buff);

                    string response = string.Empty;
                    string strMessage = Encoding.Unicode.GetString(buff);

                    client.Username = strMessage.Trim('\0');

                    client.thread = new Thread(() => ProcessMessaging(client));

                    Console.WriteLine($"{client.Username} has joined the chat.");

                    ClientList.Add(client);

                    client.thread.Start();
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to accept connections.");
                }
            }
        }

        public void ProcessMessaging(Client client)
        {
            try
            {
                SendMessage(client, commandString);
            }
            catch
            {
                Console.WriteLine("Connection error...");
                return;
            }
            while (ServerStatus)
            {
                try
                {
                    byte[] buff = new byte[512];
                    if (!IsSocketConnected(client.Socket))
                    {
                        ClientList.Remove(client);
                        Console.WriteLine($"{client.Username} has left the chat.");
                        client.Dispose();
                        return;
                    }
                    int res = client.Socket.Receive(buff);
                    if (res > 0)
                    {
                        string response = string.Empty;
                        string strMessage = Encoding.Unicode.GetString(buff);
                        Console.WriteLine($"{client.Username}: {strMessage.Trim('\0')}");
                        if (strMessage.Substring(0, 7) == "!rename")
                        {
                            strMessage = strMessage.Trim('\0');
                            int pos = strMessage.IndexOf(" ");
                            if (pos > 0)
                            {
                                string username = strMessage.Substring(pos + 1);
                                bool IsFree = true;
                                foreach (Client user in ClientList)
                                {
                                    if (user.Username == username)
                                    {
                                        IsFree = false;
                                        response = "That name has already been used.\n";
                                        break;
                                    }
                                }
                                if (IsFree)
                                {
                                    client.Username = username;
                                    response = $"Your name has been updated to: {username}\n";
                                }
                            }
                        }
                        if (strMessage.Substring(0, 6) == "!users")
                        {
                            response = "Users online:\n";
                            foreach (Client user in ClientList)
                            {
                                response = $"{response} - {user.Username} \n";
                            }
                        }
                        if (strMessage.Substring(0, 6) == "!msgto")
                        {
                            bool IsSend = false;
                            strMessage = strMessage.Trim('\0');
                            strMessage = strMessage.Replace("!msgto ", "");
                            int pos = strMessage.IndexOf(" ");
                            if (pos > 0)
                            {
                                string msgto = client.Username + ":" + strMessage.Substring(pos);
                                string username = strMessage.Substring(0, pos);
                                foreach (Client user in ClientList)
                                {
                                    if (user.Username == username)
                                    {
                                        IsSend = SendMessage(user, msgto);
                                    }
                                }
                                if (!IsSend)
                                {
                                    response = "Can't send message to " + username;
                                }
                            }
                            else
                            {
                                response = "/msgto [user] [message] - to send message";
                            }
                        }
                        SendMessage(client, response);
                    }
                }
                catch (SocketException)
                {
                    ClientList.Remove(client);
                    Console.WriteLine($"{client.Username} disconnected.");
                    client.Dispose();
                    return;
                }
            }
        }

        public void Init()
        {
            Console.WriteLine("Initializing server...");
            Port = 3000;
            this.Start();
            if (!ServerStatus)
            {
                Console.WriteLine("Server is not running. Try again");
            }
            while (true)
            {
                string input = Console.ReadLine();
                if (input.IndexOf("!users") >= 0)
                {
                    foreach (Client client in ClientList)
                    {
                        Console.WriteLine($"{client.ID} - {client.Username}");
                    }
                }

                if (input.IndexOf("!stop") >= 0)
                {
                    this.Stop();
                }

                if (input.IndexOf("!exit") >= 0)
                {
                    this.Stop();
                    return;
                }
            }
        }

        private void Start()
        {
            if (ServerStatus) return;
            ListeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                EndPoint = new IPEndPoint(IPAddress.Any, Port);
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error in creating EndPoint.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            try
            {
                ListeningSocket.Bind(EndPoint);
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Can't bind to local address.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            ListeningSocket.Listen(5);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Server was started on port: {Port}. Waiting for connections...");
            Console.ForegroundColor = ConsoleColor.Gray;
            Thread = new Thread(WaitForConnections);
            Thread.Start();
            ServerStatus = true;
        }

        private void Stop()
        {
            if (!ServerStatus) return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Stopping server...");
            ServerStatus = false;
            while (ClientList.Count != 0)
            {
                Client client = ClientList[0];
                ClientList.Remove(client);
                client.Dispose();
            }
            ListeningSocket.Close();
            ListeningSocket = null;
            Console.WriteLine("Server stopped.");
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
