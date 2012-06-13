using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Kirgor.Communication
{
    public class ClientInvocator
    {
        private string _host;
        private int _port;
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private SymmetricAlgorithm _cryptoAlgorithm;
        private ICryptoTransform _encryptor;
        private ICryptoTransform _decryptor;

        public void Connect(string host, int port, string keyFilePath)
        {
            lock (this)
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
            }
        }

        public object Invoke(Type resultType, Type contractType, string name, params object[] args)
        {
            lock (this)
            {
                return InvokeHelper.Invoke(resultType, contractType, _stream, _encryptor, _decryptor, name, args);
            }
        }
    }
}