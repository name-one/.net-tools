using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class CharSerializer : PrimitiveSerializer<char>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Char);
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((char)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadChar();
        }
    }
}