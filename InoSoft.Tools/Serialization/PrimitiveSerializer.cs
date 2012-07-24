using System;

namespace InoSoft.Tools.Serialization
{
    internal abstract class PrimitiveSerializer<T> : Serializer where T : struct
    {
        internal override bool IsDataNullable { get; set; }

        internal override bool IsCompatibleWithType(Type type)
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

            return type == typeof(T) && (!IsDataNullable || IsDataNullable && isNullable);
        }
    }
}