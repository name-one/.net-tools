using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace InoSoft.Tools.Serialization
{
    internal class ModelSerializer : ReferenceTypeSerializer
    {
        private readonly Dictionary<string, Serializer> _properties;

        public ModelSerializer(Type type)
        {
            _properties = new Dictionary<string, Serializer>();
            foreach (var p in type.GetProperties())
            {
                Serializer schema = FromType(p.PropertyType);
                if (schema != null)
                {
                    _properties.Add(p.Name, schema);
                }
            }
        }

        public ModelSerializer(BinaryReader reader)
        {
            int count = reader.ReadByte();
            _properties = new Dictionary<string, Serializer>();
            for (int i = 0; i < count; i++)
            {
                string name = reader.ReadString();
                Serializer serializer = Deserialize(reader);
                _properties.Add(name, serializer);
            }
        }

        internal override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Struct);
            writer.Write((byte)_properties.Count);
            foreach (var p in _properties)
            {
                writer.Write(p.Key);
                p.Value.Serialize(writer);
            }
        }

        internal override bool IsCompatibleWithType(Type type)
        {
            foreach (var p in _properties)
            {
                PropertyInfo propertyInfo = type.GetProperty(p.Key);
                if (propertyInfo == null)
                    return false;
            }
            return true;
        }

        internal override void SerializeDataSpecific(object obj, BinaryWriter writer)
        {
            Type type = obj.GetType();
            foreach (var p in _properties)
            {
                p.Value.SerializeData(type.GetProperty(p.Key).GetValue(obj, null), writer);
            }
        }

        internal override object DeserializeDataSpecific(Type type, BinaryReader reader)
        {
            object result = Activator.CreateInstance(type);
            foreach (var p in _properties)
            {
                PropertyInfo propertyInfo = type.GetProperty(p.Key);
                propertyInfo.SetValue(result, p.Value.DeserializeData(propertyInfo.PropertyType, reader), null);
            }
            return result;
        }
    }
}