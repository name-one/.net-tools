using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class ByteSerializer : PrimitiveSerializer<byte>
    {
        internal override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Byte);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((byte)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadByte();
        }
    }
}