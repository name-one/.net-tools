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
    public class ServiceContext<TServiceContract>
    {
        private TServiceContract _contractInstance;
        private bool _isRunning;
        private int _port;
        private TcpListener _listener;
        private SortedDictionary<int, Connection> _connections = new SortedDictionary<int, Connection>();
        private int _lastClientId = 1;
        protected SymmetricAlgorithm _cryptoAlgorithm;

        public ServiceContext(TServiceContract contractInstance)
        {
            _contractInstance = contractInstance;
        }

        public event ExceptionHandler ConnectException;

        public event ClientExceptionHandler InvokeException;

        public void Start(int port, string keyFilePath = null)
        {
            _port = port;
            if (keyFilePath != null)
            {
                using (var stream = File.OpenRead(keyFilePath))
                {
                    byte[] key = stream.ReadAll(32);
                    byte[] iv = stream.ReadAll(16);
                    _cryptoAlgorithm = new RijndaelManaged
                    {
                        Key = key,
                        IV = iv
                    };
                }
            }

            _isRunning = true;
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            Thread thread = new Thread(ListenToConnect);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public Connection[] GetConnections()
        {
            lock (_connections)
            {
                return _connections.Values.ToArray();
            }
        }

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

        public bool Disconnect(int id)
        {
            lock (_connections)
            {
                if (_connections.ContainsKey(id))
                {
                    _connections[id].IsConnected = false;
                    _connections.Remove(id);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void ListenToConnect()
        {
            while (true)
            {
                TcpClient tcpClient = _listener.AcceptTcpClient();
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

        protected virtual void OnClientConnected(int initSignal, Connection connection)
        {
            Thread thread = new Thread(ListenToInvoke);
            connection.ThreadId = Thread.CurrentThread.ManagedThreadId;
            thread.IsBackground = true;
            thread.Start(connection);
        }

        protected void ListenToInvoke(object arg)
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

    public class ServiceContext<TServiceContract, TCallbackContract> : ServiceContext<TServiceContract>
    {
        public ServiceContext(TServiceContract serviceContractInstance)
            : base(serviceContractInstance)
        {
        }

        protected override void OnClientConnected(int initSignal, Connection connection)
        {
            if (initSignal == 0)
            {
                base.OnClientConnected(initSignal, connection);
            }
            else
            {
                connection.IsCallback = true;
                Connection clientConnection = GetConnection(initSignal);
                if (connection != null)
                {
                    Invocator invocator = new Invocator(connection.Stream, connection.Encryptor, connection.Decryptor);
                    clientConnection.CallbackContractProxy = InvokeHelper.CreateContractProxy<TCallbackContract>(invocator);
                }
            }
        }
    }
}