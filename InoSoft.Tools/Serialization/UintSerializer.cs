using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class UintSerializer : PrimitiveSerializer<uint>
    {
        internal override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Uint);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((uint)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadUInt32();
        }
    }
}