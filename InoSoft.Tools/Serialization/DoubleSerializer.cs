using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class DoubleSerializer : PrimitiveSerializer<double>
    {
        internal override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Double);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((double)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadDouble();
        }
    }
}