using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace InoSoft.Tools.Serialization
{
    internal class ModelSerializer : ReferenceTypeSerializer
    {
        private Dictionary<string, Serializer> _properties;

        public ModelSerializer(Type type)
        {
            //if (!Attribute.IsDefined(type, typeof(SerializableModelAttribute)))
            //{
            //    throw new Exception(string.Format("Can't create model serializer for type {0} which hasn't SerializableModel attribute", type));
            //}

            _properties = new Dictionary<string, Serializer>();
            foreach (var p in type.GetProperties())
            {
                Serializer schema = Serializer.FromType(p.PropertyType);
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
                Serializer serializer = Serializer.Deserialize(reader);
                _properties.Add(name, serializer);
            }
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)DataType.Struct);
            writer.Write((byte)_properties.Count);
            foreach (var p in _properties)
            {
                writer.Write(p.Key);
                p.Value.Serialize(writer);
            }
        }

        public override bool IsCompatibleWithType(Type type)
        {
            //if (!Attribute.IsDefined(type, typeof(SerializableModelAttribute)))
            //{
            //    return false;
            //}
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