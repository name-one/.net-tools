using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace InoSoft.Tools.Net
{
    /// <summary>
    /// Defines client-service connection.
    /// </summary>
    /// <remarks>
    /// Also, this class encapsulates TCP connection, crypto objects, connection status etc. for internal use.
    /// </remarks>
    public class Connection
    {
        private static SortedDictionary<int, Connection> _connectionsByThreadId = new SortedDictionary<int, Connection>();

        /// <summary>
        /// Gets connection, which is assotiated with executing thread. Useful to determine which client called service method.
        /// </summary>
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

        /// <summary>
        /// Gets unique ID of client, which has this connection.
        /// </summary>
        /// <remarks>
        /// IDs are counted from 1.
        /// </remarks>
        public int Id { get; internal set; }

        /// <summary>
        /// Gets proxy object for callbacks or null if connection has no callback ability.
        /// </summary>
        public object CallbackContractProxy { get; internal set; }

        /// <summary>
        /// TCP socket, connected with client.
        /// </summary>
        internal TcpClient TcpClient { get; set; }

        /// <summary>
        /// NetworkStream made from TcpClient.
        /// </summary>
        internal NetworkStream Stream { get; set; }

        /// <summary>
        /// Encryption object or null if connection is unsecure.
        /// </summary>
        internal ICryptoTransform Encryptor { get; set; }

        /// <summary>
        /// Decryption object or null if connection is unsecure.
        /// </summary>
        internal ICryptoTransform Decryptor { get; set; }

        /// <summary>
        /// Indicates if connection is alive.
        /// </summary>
        internal bool IsConnected { get; set; }

        /// <summary>
        /// ID of service thread, which is listening to calls from client.
        /// </summary>
        internal int ThreadId { get; set; }

        /// <summary>
        /// Registers thread-connection assotiation using executing thread.
        /// </summary>
        /// <param name="connection">Connection object, which must be assotiated.</param>
        internal static void AddConnectionByThread(Connection connection)
        {
            lock (_connectionsByThreadId)
            {
                _connectionsByThreadId.Add(Thread.CurrentThread.ManagedThreadId, connection);
            }
        }

        /// <summary>
        /// Unregisters thread-connection assotiation using executing thread.
        /// </summary>
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