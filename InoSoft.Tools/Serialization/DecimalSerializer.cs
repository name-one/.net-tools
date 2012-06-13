using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class DecimalSerializer : PrimitiveSerializer<decimal>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Decimal);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((decimal)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadDecimal();
        }
    }
}