using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Kirgor.Communication
{
    public class Client
    {
        public int Id { get; internal set; }

        public DateTime ConnectedTime { get; internal set; }

        public long IncomingTraffic { get; internal set; }

        public long OutgoingTraffic { get; internal set; }

        internal Thread Thread { get; set; }

        internal TcpClient TcpClient { get; set; }

        internal BinaryReader Reader { get; set; }

        internal BinaryWriter Writer { get; set; }

        internal bool IsConnected { get; set; }
    }
}