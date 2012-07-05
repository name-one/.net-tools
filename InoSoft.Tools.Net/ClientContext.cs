using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace InoSoft.Tools.Net
{
    /// <summary>
    /// Context for client-side communication with server.
    /// </summary>
    /// <typeparam name="TServiceContract">Interface type, which defines service contract (remote calls definitions).</typeparam>
    public class ClientContext<TServiceContract>
    {
        protected string _host;
        protected int _port;
        protected SymmetricAlgorithm _cryptoAlgorithm;
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private ICryptoTransform _encryptor;
        private ICryptoTransform _decryptor;
        private Invocator _invocator;

        /// <summary>
        /// Creates ClientContext.
        /// </summary>
        public ClientContext()
        {
            Proxy = InvokeHelper.CreateContractProxy<TServiceContract>();
        }

        /// <summary>
        /// Gets proxy object, which performs remote calls.
        /// </summary>
        public TServiceContract Proxy { get; private set; }

        /// <summary>
        /// Gets client ID, assigned by service after connection. Gets 0 if not connected.
        /// </summary>
        public int ClientId { get; private set; }

        /// <summary>
        /// Establishes connection with service, inits ClientId and Proxy.
        /// </summary>
        /// <param name="host">DNS host name to connect.</param>
        /// <param name="port">Port to connect.</param>
        public virtual void Connect(string host, int port)
        {
            _host = host;
            _port = port;
            // Connects to remote host
            _tcpClient = new TcpClient(host, port);
            _stream = _tcpClient.GetStream();
            // Create crypto objects
            _encryptor = _cryptoAlgorithm != null ? _cryptoAlgorithm.CreateEncryptor() : null;
            _decryptor = _cryptoAlgorithm != null ? _cryptoAlgorithm.CreateDecryptor() : null;
            // Send zero, which means that regular connection is establishing (not callback connection)
            InvokeHelper.SendInt(_stream, _encryptor, 0);
            // Acquire own client ID
            ClientId = InvokeHelper.ReceiveInt(_stream, _decryptor);
            // Set new invocator to proxy and make remote calls possible  through new connection
            _invocator = new Invocator(_stream, _encryptor, _decryptor);
            Proxy.GetType().GetField("Invocator").SetValue(Proxy, _invocator);
        }

        /// <summary>
        /// Forcibly closes connection with service.
        /// </summary>
        public virtual void Disconnect()
        {
            lock (_invocator)
            {
                _stream.Close();
            }
        }
    }

    /// <summary>
    /// Context for client-side communication with server with callback ability.
    /// </summary>
    /// <typeparam name="TServiceContract">Interface type, which defines service contract (remote calls definitions).</typeparam>
    /// <typeparam name="TCallbackContract">Interface type, which defines callback contract (callback calls definitions).</typeparam>
    public class ClientContext<TServiceContract, TCallbackContract> : ClientContext<TServiceContract>
    {
        private TcpClient _callbackTcpClient;
        private NetworkStream _callbackStream;
        private ICryptoTransform _callbackEncryptor;
        private ICryptoTransform _callbackDecryptor;
        private TCallbackContract _callbackContractInstance;
        private bool _isRunning;

        /// <summary>
        /// Creates ClientContext.
        /// </summary>
        /// <param name="callbackContractInstance">Object, which will be target for remote callbacks from service.</param>
        public ClientContext(TCallbackContract callbackContractInstance)
        {
            _callbackContractInstance = callbackContractInstance;
        }

        /// <summary>
        /// Raises when callback execution suffers exception.
        /// </summary>
        public event ExceptionHandler CallbackInvokeException;

        /// <summary>
        /// Establishes connection with service, inits ClientId and Proxy.
        /// </summary>
        /// <param name="host">DNS host name to connect.</param>
        /// <param name="port">Port to connect.</param>
        public override void Connect(string host, int port)
        {
            base.Connect(host, port);

            // Create connection for callback
            _callbackTcpClient = new TcpClient(host, port);
            _callbackStream = _callbackTcpClient.GetStream();
            // Init callback crypto objects
            _callbackEncryptor = _cryptoAlgorithm != null ? _cryptoAlgorithm.CreateEncryptor() : null;
            _callbackDecryptor = _cryptoAlgorithm != null ? _cryptoAlgorithm.CreateDecryptor() : null;
            // Send client ID via callback connection, so service will know that it's callback for appropriate client
            InvokeHelper.SendInt(_callbackStream, _callbackEncryptor, ClientId);
            // Receive value just to syncronize connection
            InvokeHelper.ReceiveInt(_callbackStream, _callbackDecryptor);

            // Run callback listening thread
            var thread = new Thread(ListenToInvoke);
            thread.IsBackground = true;
            thread.Start();
            _isRunning = true;
        }

        /// <summary>
        /// Forcibly closes connection with service.
        /// </summary>
        public override void Disconnect()
        {
            base.Disconnect();
            _isRunning = false;
        }

        /// <summary>
        /// Executes in separate thread and listens to callbacks from service.
        /// </summary>
        private void ListenToInvoke()
        {
            try
            {
                while (_isRunning)
                {
                    InvokeHelper.ListenToInvoke(_callbackContractInstance, _callbackStream, _callbackEncryptor, _callbackDecryptor);
                }
            }
            catch (Exception ex)
            {
                var eventHandler = CallbackInvokeException;
                if (eventHandler != null)
                {
                    eventHandler(ex);
                }
            }
        }
    }
}