using System;
using System.IO;

namespace InoSoft.Tools.Serialization
{
    public abstract class Serializer
    {
        internal abstract bool IsDataNullable { get; set; }

        public static Serializer FromType(Type type)
        {
            if (type == typeof(byte))
            {
                return new ByteSerializer();
            }
            else if (type == typeof(ushort))
            {
                return new UshortSerializer();
            }
            else if (type == typeof(uint))
            {
                return new UintSerializer();
            }
            else if (type == typeof(ulong))
            {
                return new UlongSerializer();
            }
            else if (type == typeof(sbyte))
            {
                return new SbyteSerializer();
            }
            else if (type == typeof(short))
            {
                return new ShortSerializer();
            }
            else if (type == typeof(int) || type.IsEnum)
            {
                return new IntSerializer();
            }
            else if (type == typeof(long))
            {
                return new LongSerializer();
            }
            else if (type == typeof(float))
            {
                return new FloatSerializer();
            }
            else if (type == typeof(double))
            {
                return new DoubleSerializer();
            }
            else if (type == typeof(decimal))
            {
                return new DecimalSerializer();
            }
            else if (type == typeof(char))
            {
                return new CharSerializer();
            }
            else if (type == typeof(string))
            {
                return new StringSerializer();
            }
            else if (type == typeof(DateTime))
            {
                return new DateTimeSerializer();
            }
            else if (type == typeof(Guid))
            {
                return new GuidSerializer();
            }
            else if (type == typeof(bool))
            {
                return new BoolSerializer();
            }
            else if (type == typeof(byte?))
            {
                return new ByteSerializer { IsDataNullable = true };
            }
            else if (type == typeof(ushort?))
            {
                return new UshortSerializer { IsDataNullable = true };
            }
            else if (type == typeof(uint?))
            {
                return new UintSerializer { IsDataNullable = true };
            }
            else if (type == typeof(ulong?))
            {
                return new UlongSerializer { IsDataNullable = true };
            }
            else if (type == typeof(sbyte?))
            {
                return new SbyteSerializer { IsDataNullable = true };
            }
            else if (type == typeof(short?))
            {
                return new ShortSerializer { IsDataNullable = true };
            }
            else if (type == typeof(int?) ||
                type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                type.GetGenericArguments()[0].IsEnum)
            {
                return new IntSerializer { IsDataNullable = true };
            }
            else if (type == typeof(long?))
            {
                return new LongSerializer { IsDataNullable = true };
            }
            else if (type == typeof(float?))
            {
                return new FloatSerializer { IsDataNullable = true };
            }
            else if (type == typeof(double?))
            {
                return new DoubleSerializer { IsDataNullable = true };
            }
            else if (type == typeof(decimal?))
            {
                return new DecimalSerializer { IsDataNullable = true };
            }
            else if (type == typeof(char?))
            {
                return new CharSerializer { IsDataNullable = true };
            }
            else if (type == typeof(DateTime?))
            {
                return new DateTimeSerializer { IsDataNullable = true };
            }
            else if (type == typeof(Guid?))
            {
                return new GuidSerializer { IsDataNullable = true };
            }
            else if (type == typeof(bool?))
            {
                return new BoolSerializer { IsDataNullable = true };
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

        public abstract void Serialize(BinaryWriter writer);

        public abstract bool IsCompatibleWithType(Type type);

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

        internal abstract void SerializeDataSpecific(object obj, BinaryWriter writer);

        internal abstract object DeserializeDataSpecific(Type type, BinaryReader reader);
    }
}