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
        static void Main()
        {
            Server my_server;
            my_server = new Server();
            my_server.Work();
        }
    }
}
