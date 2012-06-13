using System;
using System.Reflection;

namespace InoSoft.Tools
{
    public static class ObjectExtensions
    {
        public static bool MemberwiseEquals(this object firstObject, object secondObject)
        {
            if (Object.Equals(firstObject, secondObject))
            {
                return true;
            }

            if (firstObject == null || secondObject == null ||
                firstObject.GetType() != secondObject.GetType())
            {
                return false;
            }

            foreach (var member in firstObject.GetType().GetMembers())
            {
                object firstValue;
                object secondValue;
                if (member is PropertyInfo)
                {
                    firstValue = ((PropertyInfo)member).GetValue(firstObject, null);
                    secondValue = ((PropertyInfo)member).GetValue(secondObject, null);
                }
                else if (member is FieldInfo)
                {
                    firstValue = ((FieldInfo)member).GetValue(firstObject);
                    secondValue = ((FieldInfo)member).GetValue(secondObject);
                }
                else
                {
                    continue;
                }

                bool isArray = firstObject is Array && secondObject is Array;

                if (isArray && !ArrayExtensions.ElementwiseEquals((Array)firstObject, (Array)secondObject) ||
                    !MemberwiseEquals(firstValue, secondValue))
                {
                    return false;
                }
            }

            return true;
        }
    }
}