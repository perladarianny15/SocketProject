using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace ClientSocket
{
    public class Client
    {
        private const string StrHelp = "Client commands.\n!signup [username] [password] \n!signin [username] [password] - connects you to the server.\n!exit - to exit program\n";

        string connectionString = @"YOUR CONNECTION STRING";

        public Socket Socket { get; private set; }

        public IPAddress ServerIp { get; private set; }

        public int ServerPort { get; private set; }

        private bool IsConnected { get; set; }

        private Thread Thread { get; set; }

        private bool IsChatting { get; set; }

        private string Username { get; set; }

        public static bool IsSocketConnected(Socket s)
        {
            if (!s.Connected)
                return false;

            if (s.Available == 0)
                if (s.Poll(1000, SelectMode.SelectRead))
                    return false;

            return true;
        }

        public Client()
        {
            IsConnected = false;
            IsChatting = false;
        }

        public void Init()
        {
            Console.WriteLine(StrHelp);
            while (true)
            {
                string input = Console.ReadLine();
                if (!IsChatting)
                {
                    if (input.IndexOf("!signin") >= 0)
                    {
                        string[] args = input.Split(" ");

                        Username = args[1];
                        string password = args[2];
                        bool doesUserExist = UserExists(Username);

                        if (doesUserExist)
                        {
                            if (VerifyUser(Username, password))
                            {
                                Console.WriteLine("You have logged in succesfully.");
                                this.Connect();
                            }
                            else
                            {
                                Console.WriteLine("Your credentials are not correct.");
                            }
                        }
                    }

                    if (input.IndexOf("!signup") >= 0)
                    {
                        string[] args = input.Split(" ");
                        Username = args[1];
                        string password = args[2];

                        CreateUser(Username, password);

                    }


                    if (input.IndexOf("!disconnect") >= 0)
                    {
                        this.Disconnect();
                    }

                    if (input.IndexOf("!exit") >= 0)
                    {
                        this.Disconnect();
                        return;
                    }

                    if (input.IndexOf("!joinchat") >= 0)
                    {
                        StartChatting();
                    }

                    if (input.IndexOf("!help") >= 0)
                    {
                        Console.WriteLine(StrHelp);
                    }
                }
            }
        }


        private bool VerifyUser(string username, string password)
        {
            bool result = false;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand(
                        "Select * from Users where username=@Username", connection))
                    {
                        command.Parameters.Add(new SqlParameter("Username", username));
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            string dbUsername = reader.GetString(1);
                            string dbPassword = reader.GetString(2);

                            if (username == dbUsername && password == dbPassword)
                            {
                                result = true;
                            }
                        }

                        connection.Close();

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            }

            return result;
        }

        private bool UserExists(string username)
        {
            bool result = false;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand(
                        "Select * from Users where username=@Username", connection))
                    {
                        command.Parameters.Add(new SqlParameter("Username", username));
                        command.ExecuteNonQuery();
                        // int result = command.ExecuteNonQuery();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                result = true;
                            }
                        }
                    }

                    connection.Close();

                }
                catch
                {
                    Console.WriteLine("Count not search for the user.");
                }

            }

            return result;
        }

        private void CreateUser(string username, string password)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    using (SqlCommand command = new SqlCommand(
                        "INSERT INTO Users VALUES(@Username, @Password)", connection))
                    {
                        command.Parameters.Add(new SqlParameter("Username", username));
                        command.Parameters.Add(new SqlParameter("Password", password));
                        command.ExecuteNonQuery();
                    }
                    Console.WriteLine("The user has been created.");
                }
                catch
                {
                    Console.WriteLine("Count not insert.");
                }
            }

        }

        private void Connect()
        {
            if (IsConnected) return;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            if (IPAddress.TryParse("YourServerIP", out IPAddress serverip))
            {
                ServerIp = serverip;
            }
            else
            {
                Console.WriteLine("You must enter valid IP. Try again.");
                return;
            }
            if (Int32.TryParse("3000", out int port))
            {
                ServerPort = port;
            }
            else
            {
                Console.WriteLine("You must enter valid port. Try again.");
                return;
            }
            try
            {
                Socket.Connect(ServerIp, ServerPort);
                IsConnected = true;
                Console.WriteLine("Connected.\n!joinchat - to start chatting.\n");
            }
            catch
            {
                Console.WriteLine("Can't connect to server. Try again...");
                return;
            }
        }

        private void Disconnect()
        {
            if (!IsConnected) return;
            if (Socket != null && Thread != null)
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
                Socket = null;
                Thread = null;
                IsConnected = false;
                IsChatting = false;
                Console.WriteLine("Disconnected.");
            }
        }

        private void StartChatting()
        {
            if (!IsConnected)
            {
                Console.WriteLine("You are not connected.\n!connect - to conect to server.\n");
                return;
            }
            Console.WriteLine("Start chatting.\n!stop - to stop chatting.\n");
            IsChatting = true;
            Thread = new Thread(GetMessages);
            Thread.Start();
            byte[] clientUser = Encoding.Unicode.GetBytes(Username);
            Socket.Send(clientUser);

            while (IsChatting)
            {
                string input = Console.ReadLine();
                if (!IsSocketConnected(Socket))
                {
                    Console.WriteLine("Connection lost.");
                    break;
                }
                if (input.IndexOf("!stop") >= 0)
                {
                    break;
                }
                else
                {
                    byte[] message = Encoding.Unicode.GetBytes(input);
                    Socket.Send(message);
                }
            }
            Disconnect();
        }

        private void GetMessages()
        {
            while (IsChatting && IsConnected)
            {
                try
                {
                    byte[] data = new byte[512];
                    int res = Socket.Receive(data);
                    if (res > 0)
                    {
                        string message = Encoding.Unicode.GetString(data);
                        Console.WriteLine(message.Trim('\0'));
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }
        }
    }
}
