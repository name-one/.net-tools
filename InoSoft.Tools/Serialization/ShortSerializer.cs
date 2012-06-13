using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class ShortSerializer : PrimitiveSerializer<short>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Short);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((short)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadInt16();
        }
    }
}