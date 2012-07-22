using System;

namespace InoSoft.Tools.Serialization
{
    internal abstract class PrimitiveSerializer<T> : Serializer where T : struct
    {
        internal override bool IsDataNullable { get; set; }

        internal override bool IsCompatibleWithType(Type type)
        {
            return type == typeof(T) || IsDataNullable && type == typeof(T?);
        }
    }
}