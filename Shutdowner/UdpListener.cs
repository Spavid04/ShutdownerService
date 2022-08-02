using System;
using System.Net;
using System.Net.Sockets;

namespace Shutdowner
{
    public class UdpListener : IDisposable
    {
        private readonly int Port;
        private UdpClient ReceiveClient;

        private bool Started = false;
        private bool Stopping = false;

        public UdpListener(int port)
        {
            this.Port = port;
        }

        public delegate void PacketReceivedHandler(UdpListener sender, byte[] data);
        public event PacketReceivedHandler PacketReceived;

        public void StartListening()
        {
            if (this.Started)
            {
                throw new InvalidOperationException("Already started!");
            }

            this.ReceiveClient = new UdpClient(this.Port);
            this.ReceiveClient.BeginReceive(this.DataReceived, null);
            
            this.Started = true;
        }

        private void DataReceived(IAsyncResult result)
        {
            IPEndPoint ip = null;
            byte[] data = null;

            try
            {
                data = this.ReceiveClient.EndReceive(result, ref ip);
            }
            catch (ObjectDisposedException)
            {
                if (this.Stopping)
                {
                    return;
                }
                throw;
            }
            this.ReceiveClient.BeginReceive(this.DataReceived, null);

            this.PacketReceived?.Invoke(this, data);
        }

        public void Dispose()
        {
            this.Stopping = true;
            this.ReceiveClient?.Dispose();
        }
    }
}
