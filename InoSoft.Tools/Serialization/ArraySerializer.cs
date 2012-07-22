using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    internal class ArraySerializer : ReferenceTypeSerializer
    {
        private Serializer _elementSerializer;

        public ArraySerializer(Type type)
        {
            if (!type.IsArray)
            {
                throw new Exception(string.Format("Can't create array serializer for non-array type {0}", type));
            }
            _elementSerializer = Serializer.FromType(type.GetElementType());
        }

        public ArraySerializer(BinaryReader reader)
        {
            _elementSerializer = Serializer.Deserialize(reader);
        }

        internal override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Array);
            _elementSerializer.Serialize(writer);
        }

        internal override bool IsCompatibleWithType(Type type)
        {
            return type.IsArray && _elementSerializer.IsCompatibleWithType(type.GetElementType());
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            Array array = (Array)obj;
            writer.Write(array.Length);
            foreach (var item in array)
            {
                _elementSerializer.SerializeDataSpecific(item, writer);
            }
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Type elementType = type.GetElementType();
            Array array = Array.CreateInstance(elementType, count);
            for (int i = 0; i < count; i++)
            {
                array.SetValue(_elementSerializer.DeserializeDataSpecific(elementType, reader), i);
            }
            return array;
        }
    }
}