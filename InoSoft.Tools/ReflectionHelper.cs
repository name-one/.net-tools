using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Compares property values of two objects and sets values from source to destination if they differ.
        /// </summary>
        /// <param name="dest">Destination object.</param>
        /// <param name="source">Source object.</param>
        /// <param name="propertySelector">
        /// Function returning true for those properties that need to be updated.
        /// By default, all the properties are updated.
        /// </param>
        /// <returns>Array of PropertyInfo instances representing those destination properties that were changed.</returns>
        public static PropertyInfo[] UpdateProperties(object dest, object source,
            Func<PropertyInfo, bool> propertySelector = null)
        {
            if (source == null)
                return new PropertyInfo[0];
            var result = new List<PropertyInfo>();
            Type sourceType = source.GetType();
            Type destType = dest.GetType();
            foreach (var sourceProp in sourceType.GetProperties())
            {
                var property = destType.GetProperty(sourceProp.Name);
                if (property == null || !property.CanWrite)
                    continue;
                var sourcePropertyValue = sourceProp.GetValue(source, null);
                if (sourcePropertyValue == null)
                    continue;
                var destPropertyValue = property.GetValue(dest, null);
                if ((propertySelector == null || propertySelector(property))
                    && !destPropertyValue.MemberwiseEquals(sourcePropertyValue))
                {
                    property.SetValue(dest, sourcePropertyValue, null);
                    result.Add(property);
                }
            }

            return result.ToArray();
        }
    }
}