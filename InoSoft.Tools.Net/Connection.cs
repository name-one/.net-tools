using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace InoSoft.Tools.Net
{
    public class Connection
    {
        private static SortedDictionary<int, Connection> _connectionsByThreadId = new SortedDictionary<int, Connection>();

        public static Connection Current
        {
            get
            {
                lock (_connectionsByThreadId)
                {
                    int id = Thread.CurrentThread.ManagedThreadId;
                    if (_connectionsByThreadId.ContainsKey(id))
                    {
                        return _connectionsByThreadId[id];
                    }
                }
                return null;
            }
        }

        public int Id { get; internal set; }

        public bool IsCallback { get; internal set; }

        public object CallbackContractProxy { get; internal set; }

        internal TcpClient TcpClient { get; set; }

        internal NetworkStream Stream { get; set; }

        internal ICryptoTransform Encryptor { get; set; }

        internal ICryptoTransform Decryptor { get; set; }

        internal bool IsConnected { get; set; }

        internal int ThreadId { get; set; }

        internal static void AddConnectionByThread(Connection connection)
        {
            lock (_connectionsByThreadId)
            {
                _connectionsByThreadId.Add(Thread.CurrentThread.ManagedThreadId, connection);
            }
        }

        internal static void RemoveConnectionByThread()
        {
            lock (_connectionsByThreadId)
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                if (_connectionsByThreadId.ContainsKey(id))
                {
                    _connectionsByThreadId.Remove(id);
                }
            }
        }
    }
}