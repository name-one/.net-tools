using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class UlongSerializer : PrimitiveSerializer<ulong>
    {
        internal override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Ulong);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((ulong)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadUInt64();
        }
    }
}