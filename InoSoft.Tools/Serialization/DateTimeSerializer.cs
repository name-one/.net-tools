using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class DateTimeSerializer : PrimitiveSerializer<DateTime>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.DateTime);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write(((DateTime)obj).Ticks);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return new DateTime(reader.ReadInt64());
        }
    }
}