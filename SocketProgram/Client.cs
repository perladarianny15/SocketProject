using System.Net.Sockets;
using System.Threading;

namespace SocketProgram
{
    public class Client
    {
        public int ID { get; set; }

        public string Username { get; set; }

        public Socket Socket { get; set; }

        public Thread thread;

        private bool _isDisposed = false;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (this.Socket != null)
                {
                    this.Socket.Shutdown(SocketShutdown.Both);
                    this.Socket.Close();
                    this.Socket = null;
                }
                if (this.thread != null)
                    this.thread = null;
                _isDisposed = true;
            }
        }
    }
}