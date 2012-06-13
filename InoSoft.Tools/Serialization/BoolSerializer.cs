using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class BoolSerializer : PrimitiveSerializer<bool>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Bool);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((bool)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadBoolean();
        }
    }
}