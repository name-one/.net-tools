using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class GuidSerializer : PrimitiveSerializer<Guid>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Guid);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write(((Guid)obj).ToByteArray());
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return new Guid(reader.ReadBytes(16));
        }
    }
}