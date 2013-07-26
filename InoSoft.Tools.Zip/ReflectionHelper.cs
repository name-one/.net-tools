using System;
using System.Reflection;

namespace InoSoft.Tools.Zip
{
    internal static class ReflectionHelper
    {
        public const BindingFlags InstanceBinding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        public const BindingFlags StaticBinding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public static T ConvertEnum<T>(this object obj)
        {
            return (T)obj.ConvertEnum(typeof(T));
        }

        public static object ConvertEnum(this object obj, Type destType)
        {
            if (!destType.IsEnum)
                throw new ArgumentException("The destination type must be an enumeration.");

            Type sourceType = obj.GetType();
            if (!sourceType.IsEnum)
                throw new ArgumentException("The source object must be an enumeration instance.");

            object underlyier = Convert.ChangeType(obj, sourceType.GetEnumUnderlyingType());
            return Enum.ToObject(destType, underlyier);
        }
    }
}