using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    /// <summary>
    /// Can serialize and deserialize data of given type.
    /// </summary>
    /// <typeparam name="T">Type, which serializer should be compatible with.</typeparam>
    /// <remarks>
    /// This is more convenient form of serializer for end user. Technically, it wraps regular Serializer.
    /// </remarks>
    public class Serializer<T>
    {
        private Serializer _baseSerializer;

        /// <summary>
        /// Creates serializer.
        /// </summary>
        public Serializer()
        {
            _baseSerializer = Serializer.FromType(typeof(T));
        }

        /// <summary>
        /// Serializes data using binary writer.
        /// </summary>
        /// <param name="obj">Input data object.</param>
        /// <param name="writer">Binary writer, which wraps output stream.</param>
        public void SerializeData(T obj, BinaryWriter writer)
        {
            _baseSerializer.SerializeData(obj, writer);
        }

        /// <summary>
        /// Deserializes data using binary reader and returns result object.
        /// </summary>
        /// <param name="reader">Binary reader, which wraps input stream.</param>
        public T DeserializeData(BinaryReader reader)
        {
            return _baseSerializer.DeserializeData<T>(reader);
        }
    }
}