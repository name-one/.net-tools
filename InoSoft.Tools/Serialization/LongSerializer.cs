using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class LongSerializer : PrimitiveSerializer<long>
    {
        internal override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Long);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((long)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadInt64();
        }
    }
}