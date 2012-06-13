using System;

namespace InoSoft.Tools
{
    public static class ArrayExtensions
    {
        public static bool ElementwiseEquals(this Array firstArray, Array secondArray)
        {
            if (Object.Equals(firstArray, secondArray))
            {
                return true;
            }

            if (firstArray == null || secondArray == null ||
                firstArray.GetType() != secondArray.GetType() ||
                firstArray.Length != secondArray.Length)
            {
                return false;
            }

            for (int i = 0; i < firstArray.Length; i++)
            {
                if (!ObjectExtensions.MemberwiseEquals(firstArray.GetValue(i), secondArray.GetValue(i)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}