using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace InoSoft.Tools.Net
{
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

        public TServiceContract Proxy { get; private set; }

        public int ClientId { get; private set; }

        public virtual void Connect(string host, int port, string keyFilePath = null)
        {
            _host = host;
            _port = port;

            _tcpClient = new TcpClient(host, port);
            _stream = _tcpClient.GetStream();
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

            _encryptor = _cryptoAlgorithm != null ? _cryptoAlgorithm.CreateEncryptor() : null;
            _decryptor = _cryptoAlgorithm != null ? _cryptoAlgorithm.CreateDecryptor() : null;

            InvokeHelper.SendInt(_stream, _encryptor, 0);
            ClientId = InvokeHelper.ReceiveInt(_stream, _decryptor);

            _invocator = new Invocator(_stream, _encryptor, _decryptor);
            Proxy = InvokeHelper.CreateContractProxy<TServiceContract>(_invocator);
        }

        public virtual void Disconnect()
        {
            lock (_invocator)
            {
                _stream.Close();
            }
        }
    }

    public class ClientContext<TServiceContract, TCallbackContract> : ClientContext<TServiceContract>
    {
        private TcpClient _callbackTcpClient;
        private NetworkStream _callbackStream;
        private ICryptoTransform _callbackEncryptor;
        private ICryptoTransform _callbackDecryptor;
        private TCallbackContract _callbackContractInstance;
        private bool _isRunning;

        public ClientContext(TCallbackContract callbackContractInstance)
        {
            _callbackContractInstance = callbackContractInstance;
        }

        public event ExceptionHandler CallbackInvokeException;

        public override void Connect(string host, int port, string keyFilePath = null)
        {
            base.Connect(host, port, keyFilePath);

            _callbackTcpClient = new TcpClient(host, port);
            _callbackStream = _callbackTcpClient.GetStream();

            _callbackEncryptor = _cryptoAlgorithm != null ? _cryptoAlgorithm.CreateEncryptor() : null;
            _callbackDecryptor = _cryptoAlgorithm != null ? _cryptoAlgorithm.CreateDecryptor() : null;

            InvokeHelper.SendInt(_callbackStream, _callbackEncryptor, ClientId);
            InvokeHelper.ReceiveInt(_callbackStream, _callbackDecryptor);

            Thread thread = new Thread(ListenToInvoke);
            thread.IsBackground = true;
            thread.Start();
            _isRunning = true;
        }

        public override void Disconnect()
        {
            base.Disconnect();
            _isRunning = false;
        }

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