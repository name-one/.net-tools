using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class IntSerializer : PrimitiveSerializer<int>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Int);
        }

        public override bool IsCompatibleWithType(Type type)
        {
            return type == typeof(int) || IsDataNullable && type == typeof(int?) ||
                type.IsEnum || IsDataNullable && type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>) && type.GetGenericArguments()[0].IsEnum;
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            writer.Write((int)obj);
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            return reader.ReadInt32();
        }
    }
}