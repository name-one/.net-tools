using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class SbyteSerializer : PrimitiveSerializer<sbyte>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Sbyte);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((sbyte)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadSByte();
        }
    }
}