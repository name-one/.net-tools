using System;
using System.Reflection;

namespace InoSoft.Tools
{
    public static class ReflectionHelper
    {
        /// <summary>
        /// Clones a source object of type <typeparamref name="TSource"/> to an object of type <typeparamref name="TDest"/>
        /// by copying every property value of the source object to a corresponding property of the destination object,
        /// converting types where necessary.
        /// </summary>
        /// <typeparam name="TSource">Source object type.</typeparam>
        /// <typeparam name="TDest">Destination object type.</typeparam>
        /// <param name="source">Object to clone.</param>
        /// <returns>Cloned object of type <typeparamref name="TDest"/>.</returns>
        public static TDest Clone<TSource, TDest>(TSource source)
            where TSource : class
            where TDest : class
        {
            if (source == null)
                return null;
            Type sourceType = typeof(TSource);
            Type destType = typeof(TDest);
            var result = (TDest)Activator.CreateInstance(destType);
            foreach (var sourceProp in sourceType.GetProperties())
            {
                PropertyInfo destProp = destType.GetProperty(sourceProp.Name);
                if (destProp == null)
                    continue;
                object value = sourceProp.GetValue(source, null);
                if (value != null)
                {
                    Type targetType = Nullable.GetUnderlyingType(destProp.PropertyType) ?? destProp.PropertyType;
                    value = targetType.IsEnum
                        ? Enum.ToObject(targetType, value)
                        : Convert.ChangeType(value, targetType);
                }
                destProp.SetValue(result, value, null);
            }
            return result;
        }

        /// <summary>
        /// Clones an array of source objects of type <typeparamref name="TSource"/> to an array of objects
        /// of type <typeparamref name="TDest"/> by copying every property value of every source object to
        /// a corresponding property of the destination object, converting types where necessary.
        /// </summary>
        /// <typeparam name="TSource">Source object type.</typeparam>
        /// <typeparam name="TDest">Destination object type.</typeparam>
        /// <param name="source">Array to clone.</param>
        /// <returns>Cloned array of objects of type <typeparamref name="TDest"/>.</returns>
        public static TDest[] CloneArray<TSource, TDest>(TSource[] source)
            where TSource : class
            where TDest : class
        {
            if (source == null)
                return null;
            var result = new TDest[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = Clone<TSource, TDest>(source[i]);
            }
            return result;
        }
    }
}