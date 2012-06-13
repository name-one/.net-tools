using System;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace InoSoft.Tools.Net
{
    public class Invocator
    {
        private NetworkStream _stream;
        private ICryptoTransform _encryptor;
        private ICryptoTransform _decryptor;

        public Invocator(NetworkStream stream, ICryptoTransform encryptor, ICryptoTransform decryptor)
        {
            _stream = stream;
            _encryptor = encryptor;
            _decryptor = decryptor;
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