using System;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace InoSoft.Tools.Net
{
    /// <summary>
    /// Encapsulates network stream and crypto objects to make remote calls.
    /// </summary>
    public class Invocator
    {
        private NetworkStream _stream;
        private ICryptoTransform _encryptor;
        private ICryptoTransform _decryptor;

        /// <summary>
        /// Creates Invocator.
        /// </summary>
        /// <param name="stream">NetworkStream from TCP connection.</param>
        /// <param name="encryptor">Encryption object or null if connection is unsecure.</param>
        /// <param name="decryptor">Decryption object or null if connection is unsecure.</param>
        public Invocator(NetworkStream stream, ICryptoTransform encryptor, ICryptoTransform decryptor)
        {
            _stream = stream;
            _encryptor = encryptor;
            _decryptor = decryptor;
        }

        /// <summary>
        /// Calls remote method using encapsulated connection and crypto objects.
        /// </summary>
        /// <param name="resultType">Type of result object.</param>
        /// <param name="contractType">Type of contract interface, which contains method being invoked.</param>
        /// <param name="name">Name of method being invoked.</param>
        /// <param name="args">Arguments of method being invoked.</param>
        /// <returns></returns>
        public object Invoke(Type resultType, Type contractType, string name, params object[] args)
        {
            lock (this)
            {
                return InvokeHelper.Invoke(resultType, contractType, _stream, _encryptor, _decryptor, name, args);
            }
        }
    }
}