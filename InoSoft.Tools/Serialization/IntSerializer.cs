using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class IntSerializer : PrimitiveSerializer<int>
    {
        internal override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Int);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((int)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadInt32();
        }
    }
}