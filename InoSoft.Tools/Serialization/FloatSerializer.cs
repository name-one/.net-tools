using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class FloatSerializer : PrimitiveSerializer<float>
    {
        internal override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Float);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((float)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadSingle();
        }
    }
}