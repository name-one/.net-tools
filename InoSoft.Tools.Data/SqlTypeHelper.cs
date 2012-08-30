using System;
using System.Collections.Generic;

namespace InoSoft.Tools.Data
{
    internal static class SqlTypeHelper
    {
        private static readonly HashSet<Type> SqlTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(byte),
            typeof(byte[]),
            typeof(decimal),
            typeof(float),
            typeof(int),
            typeof(long),
            typeof(short),
            typeof(string),
            typeof(DateTime),
            typeof(Guid)
        };

        public static bool IsSqlType(Type type)
        {
            return SqlTypes.Contains(type);
        }
    }
}