using System;
using System.Collections.Generic;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    public abstract class Serializer
    {
        private static readonly Dictionary<Type, Serializer> _nullableSerializersByType = new Dictionary<Type, Serializer>
        {
            { typeof(byte), new ByteSerializer { IsDataNullable = true } },
            { typeof(ushort), new UshortSerializer { IsDataNullable = true } },
            { typeof(uint), new UintSerializer { IsDataNullable = true } },
            { typeof(ulong), new UlongSerializer { IsDataNullable = true } },
            { typeof(sbyte), new SbyteSerializer { IsDataNullable = true } },
            { typeof(short), new ShortSerializer { IsDataNullable = true } },
            { typeof(int), new IntSerializer { IsDataNullable = true } },
            { typeof(long), new LongSerializer { IsDataNullable = true } },
            { typeof(float), new FloatSerializer { IsDataNullable = true } },
            { typeof(double), new DoubleSerializer { IsDataNullable = true } },
            { typeof(decimal), new DecimalSerializer { IsDataNullable = true } },
            { typeof(bool), new BoolSerializer { IsDataNullable = true } },
            { typeof(char), new CharSerializer { IsDataNullable = true } },
            { typeof(DateTime), new DateTimeSerializer { IsDataNullable = true } },
            { typeof(Guid), new GuidSerializer { IsDataNullable = true } }
        };

        private static readonly Dictionary<Type, Serializer> _serializersByType = new Dictionary<Type, Serializer>
        {
            { typeof(byte), new ByteSerializer() },
            { typeof(ushort), new UshortSerializer() },
            { typeof(uint), new UintSerializer() },
            { typeof(ulong), new UlongSerializer() },
            { typeof(sbyte), new SbyteSerializer() },
            { typeof(short), new ShortSerializer() },
            { typeof(int), new IntSerializer() },
            { typeof(long), new LongSerializer() },
            { typeof(float), new FloatSerializer() },
            { typeof(double), new DoubleSerializer() },
            { typeof(decimal), new DecimalSerializer() },
            { typeof(bool), new BoolSerializer() },
            { typeof(char), new CharSerializer() },
            { typeof(DateTime), new DateTimeSerializer() },
            { typeof(Guid), new GuidSerializer() }
        };

        internal abstract bool IsDataNullable { get; set; }

        public static Serializer Deserialize(BinaryReader reader)
        {
            DataType dataType = (DataType)reader.ReadByte();
            switch (dataType)
            {
                case DataType.Byte:
                    return new ByteSerializer();
                case DataType.Ushort:
                    return new UshortSerializer();
                case DataType.Uint:
                    return new UintSerializer();
                case DataType.Ulong:
                    return new UlongSerializer();
                case DataType.Sbyte:
                    return new SbyteSerializer();
                case DataType.Short:
                    return new ShortSerializer();
                case DataType.Int:
                    return new IntSerializer();
                case DataType.Long:
                    return new LongSerializer();
                case DataType.Float:
                    return new FloatSerializer();
                case DataType.Double:
                    return new DoubleSerializer();
                case DataType.Decimal:
                    return new DecimalSerializer();
                case DataType.Char:
                    return new CharSerializer();
                case DataType.String:
                    return new StringSerializer();
                case DataType.DateTime:
                    return new DateTimeSerializer();
                case DataType.Guid:
                    return new GuidSerializer();
                case DataType.Array:
                    return new ArraySerializer(reader);
                case DataType.Struct:
                    return new ModelSerializer(reader);
                case DataType.Bool:
                    return new BoolSerializer();
                default:
                    throw new Exception("Can't deserialize serializer");
            }
        }

        public static Serializer FromType(Type type)
        {
            var nullableUnderlying = Nullable.GetUnderlyingType(type);
            bool isNullable = nullableUnderlying != null;
            if (isNullable)
            {
                type = nullableUnderlying;
            }

            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            var serializersByType = isNullable ? _nullableSerializersByType : _serializersByType;
            if (serializersByType.ContainsKey(type))
            {
                return serializersByType[type];
            }
            else if (type.IsArray)
            {
                return new ArraySerializer(type);
            }
            else if (type.IsClass)
            {
                return new ModelSerializer(type);
            }
            else
            {
                throw new Exception(string.Format("Can't create serializer from type {0}", type));
            }
        }

        public object DeserializeData(Type type, BinaryReader reader)
        {
            if (!IsCompatibleWithType(type))
            {
                throw new Exception(string.Format("Can't deserialize data because specified type {0} is incompatible with {1}", type, this));
            }
            if (IsDataNullable && reader.ReadByte() == 0)
            {
                return null;
            }
            return DeserializeDataSpecific(type, reader);
        }

        public T DeserializeData<T>(BinaryReader reader)
        {
            return (T)DeserializeData(typeof(T), reader);
        }

        public abstract bool IsCompatibleWithType(Type type);

        public abstract void Serialize(BinaryWriter writer);

        public void SerializeData(object obj, BinaryWriter writer)
        {
            if (IsDataNullable)
            {
                writer.Write((byte)(obj == null ? 0 : 1));
                if (obj == null)
                {
                    return;
                }
            }
            if (obj == null)
            {
                throw new Exception("Can't serialize null because current serializer doesn't support nullable data");
            }
            if (!IsCompatibleWithType(obj.GetType()))
            {
                throw new Exception(string.Format("Can't serialize data because its type {0} is incompatible with {1}", obj.GetType(), this));
            }
            SerializeDataSpecific(obj, writer);
        }

        internal abstract object DeserializeDataSpecific(Type type, BinaryReader reader);

        internal abstract void SerializeDataSpecific(object obj, BinaryWriter writer);
    }
}