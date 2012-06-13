using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class UshortSerializer : PrimitiveSerializer<ushort>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Ushort);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((ushort)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadUInt16();
        }
    }
}