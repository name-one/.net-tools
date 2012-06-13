namespace InoSoft.Tools.Serialization
{
    public enum DataType : byte
    {
        Byte = 1,
        Ushort = 2,
        Uint = 3,
        Ulong = 4,
        Sbyte = 5,
        Short = 6,
        Int = 7,
        Long = 8,
        Float = 9,
        Double = 10,
        Decimal = 11,
        Char = 12,
        String = 13,
        DateTime = 14,
        Guid = 15,
        Array = 16,
        Struct = 17,
        Bool = 18,
        Nullable = 128
    }
}