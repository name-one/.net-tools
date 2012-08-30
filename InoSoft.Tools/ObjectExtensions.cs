using System;
using System.Linq;
using System.Reflection;

namespace InoSoft.Tools
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Compares two objects of the same type by values of their properties and fields.
        /// If the objects are value types, compares them by value.
        /// If the objects are arrays, compares them elementwise.
        /// If the objects implement <see cref="IEquatable{T}"/> interface, compares them using <see cref="IEquatable{T}.Equals(T)"/>.
        /// </summary>
        /// <param name="firstObject">First object to compare.</param>
        /// <param name="secondObject">Second object to compare.</param>
        /// <returns>
        /// True if <paramref name="firstObject"/> is memberwise equal to <paramref name="secondObject"/>.
        /// </returns>
        public static bool MemberwiseEquals(this object firstObject, object secondObject)
        {
            if (Equals(firstObject, secondObject))
            {
                return true;
            }

            if (firstObject == null || secondObject == null)
            {
                return false;
            }

            Type type = firstObject.GetType();
            if (type != secondObject.GetType())
            {
                return false;
            }

            if (firstObject is ValueType)
            {
                return firstObject.Equals(secondObject);
            }

            var array = firstObject as Array;
            if (array != null)
            {
                return array.ElementwiseEquals((Array)secondObject);
            }

            Type equatable = typeof(IEquatable<>).MakeGenericType(type);
            if (type.GetInterfaces().Contains(equatable))
            {
                MethodInfo equals = equatable.GetMethod("Equals");
                return (bool)equals.Invoke(firstObject, new[] { secondObject });
            }

            return CompareProperties(firstObject, secondObject, type)
                && CompareFields(firstObject, secondObject, type);
        }

        /// <summary>
        /// Compares all fields of the provided objects.
        /// </summary>
        /// <param name="firstObject">First object to compare.</param>
        /// <param name="secondObject">Second object to compare.</param>
        /// <param name="type">Type of the objects being compared.</param>
        /// <returns>
        /// True if all fields of <paramref name="firstObject"/> are memberwise equal to the corresponding
        /// fields of <paramref name="secondObject"/>.
        /// </returns>
        private static bool CompareFields(object firstObject, object secondObject, Type type)
        {
            return type.GetFields()
                .All(field => MemberwiseEquals(field.GetValue(firstObject), field.GetValue(secondObject)));
        }

        /// <summary>
        /// Compares all properties of the provided objects.
        /// </summary>
        /// <param name="firstObject">First object to compare.</param>
        /// <param name="secondObject">Second object to compare.</param>
        /// <param name="type">Type of the objects being compared.</param>
        /// <returns>
        /// True if all properties of <paramref name="firstObject"/> are memberwise equal to the corresponding
        /// properties of <paramref name="secondObject"/>.
        /// </returns>
        private static bool CompareProperties(object firstObject, object secondObject, Type type)
        {
            return type.GetProperties()
                .Where(property => property.GetIndexParameters().Length <= 0)
                .All(property =>
                    MemberwiseEquals(property.GetValue(firstObject, null), property.GetValue(secondObject, null)));
        }
    }
}