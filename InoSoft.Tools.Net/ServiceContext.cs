using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace InoSoft.Tools.Net
{
    /// <summary>
    /// Context for server-side communication with clients.
    /// </summary>
    /// <typeparam name="TServiceContract">Interface type, which defines service contract (remote calls definitions).</typeparam>
    public class ServiceContext<TServiceContract>
    {
        protected SymmetricAlgorithm _cryptoAlgorithm;
        private TServiceContract _contractInstance;
        private bool _isRunning;
        private int _port;
        private TcpListener _listener;
        private SortedDictionary<int, Connection> _connections = new SortedDictionary<int, Connection>();
        private int _lastClientId = 1;

        /// <summary>
        /// Creates ServiceContext.
        /// </summary>
        /// <param name="contractInstance">Object, which will be target for remote calls from clients.</param>
        public ServiceContext(TServiceContract contractInstance)
        {
            _contractInstance = contractInstance;
        }

        /// <summary>
        /// Raises when client connection fails during initialization.
        /// </summary>
        public event ExceptionHandler ConnectException;

        /// <summary>
        /// Raises when client connection fails during invocation because of network problems.
        /// </summary>
        public event ConnectionExceptionHandler InvokeException;

        /// <summary>
        /// Causes service to start listening to client connections.
        /// </summary>
        /// <param name="port">Port to listen to.</param>
        public void Start(int port)
        {
            _port = port;

            _isRunning = true;
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            Thread thread = new Thread(ListenToConnect);
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// Stops listening to incoming client connections and invocations.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Gets currently alive client connections.
        /// </summary>
        public Connection[] GetConnections()
        {
            lock (_connections)
            {
                return _connections.Values.ToArray();
            }
        }

        /// <summary>
        /// Gets client connection by ID or null if there is no connection with specified ID.
        /// </summary>
        /// <param name="id">Connection ID.</param>
        public Connection GetConnection(int id)
        {
            lock (_connections)
            {
                if (_connections.ContainsKey(id))
                {
                    return _connections[id];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Forsibly disconnects specified client.
        /// </summary>
        /// <param name="id">ID of client to disconnect.</param>
        public void Disconnect(int id)
        {
            lock (_connections)
            {
                if (_connections.ContainsKey(id))
                {
                    _connections[id].IsConnected = false;
                    _connections.Remove(id);
                }
            }
        }

        /// <summary>
        /// Called when client is connected via TCP.
        /// </summary>
        /// <param name="initSignal">Init signal is reserved for callback connection handler.</param>
        /// <param name="connection">Just created connection.</param>
        protected virtual void OnClientConnected(int initSignal, Connection connection)
        {
            // Begin to listen to invocations asynchronously
            Thread thread = new Thread(ListenToInvoke);
            connection.ThreadId = Thread.CurrentThread.ManagedThreadId;
            thread.IsBackground = true;
            thread.Start(connection);
        }

        /// <summary>
        /// Listens to clients connections.
        /// </summary>
        private void ListenToConnect()
        {
            while (_isRunning)
            {
                TcpClient tcpClient = _listener.AcceptTcpClient();
                if (!_isRunning)
                {
                    return;
                }

                try
                {
                    Connection connection = new Connection
                    {
                        Id = _lastClientId++,
                        TcpClient = tcpClient,
                        Stream = tcpClient.GetStream(),
                        Encryptor = _cryptoAlgorithm != null ? _cryptoAlgorithm.CreateEncryptor() : null,
                        Decryptor = _cryptoAlgorithm != null ? _cryptoAlgorithm.CreateDecryptor() : null,
                        IsConnected = true
                    };

                    NetworkStream stream = connection.TcpClient.GetStream();
                    int initSignal = InvokeHelper.ReceiveInt(connection.Stream, connection.Decryptor);
                    OnClientConnected(initSignal, connection);
                    InvokeHelper.SendInt(connection.Stream, connection.Encryptor, connection.Id);

                    _connections.Add(connection.Id, connection);
                }
                catch (Exception ex)
                {
                    var eventHandler = ConnectException;
                    if (eventHandler != null)
                    {
                        eventHandler(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Listens to client invocations.
        /// </summary>
        /// <param name="arg">Client connection object.</param>
        private void ListenToInvoke(object arg)
        {
            Connection connection = (Connection)arg;
            Connection.AddConnectionByThread(connection);
            try
            {
                while (_isRunning && connection.IsConnected)
                {
                    lock (connection)
                    {
                        InvokeHelper.ListenToInvoke(_contractInstance, connection.Stream, connection.Encryptor, connection.Decryptor);
                    }
                }
            }
            catch (Exception ex)
            {
                connection.IsConnected = false;
                Disconnect(connection.Id);
                var eventHandler = InvokeException;
                if (eventHandler != null)
                {
                    eventHandler(connection, ex);
                }
            }
            Connection.RemoveConnectionByThread();
        }
    }

    /// <summary>
    /// Context for server-side communication with clients with callback ability.
    /// </summary>
    /// <typeparam name="TServiceContract">Interface type, which defines service contract (remote calls definitions).</typeparam>
    /// <typeparam name="TCallbackContract">Interface type, which defines callback contract (callback calls definitions).</typeparam>
    public class ServiceContext<TServiceContract, TCallbackContract> : ServiceContext<TServiceContract>
    {
        /// <summary>
        /// Creates ServiceContext.
        /// </summary>
        /// <param name="contractInstance">Object, which will be target for remote calls from clients.</param>
        public ServiceContext(TServiceContract serviceContractInstance)
            : base(serviceContractInstance)
        {
        }

        /// <summary>
        /// Extended version of client connection handler, can handle callback connections.
        /// </summary>
        /// <param name="initSignal">
        /// Init signal indicates if connection is regular or callback. Zero is for regular,
        /// non-zero indicates callback and init signal equals to associated client ID.
        /// </param>
        /// <param name="connection">Just created connection.</param>
        protected override void OnClientConnected(int initSignal, Connection connection)
        {
            if (initSignal == 0)
            {
                // Handle regular connection
                base.OnClientConnected(initSignal, connection);
            }
            else
            {
                // Handle callback connection
                Connection clientConnection = GetConnection(initSignal);
                if (connection != null)
                {
                    Invocator invocator = new Invocator(connection.Stream, connection.Encryptor, connection.Decryptor);
                    clientConnection.CallbackContractProxy = InvokeHelper.CreateContractProxy<TCallbackContract>();
                    clientConnection.CallbackContractProxy.GetType().GetField("Invocator")
                        .SetValue(clientConnection.CallbackContractProxy, invocator);
                }
            }
        }
    }
}