namespace InoSoft.Tools.Zip
{
    public enum DeflateOption : byte
    {
        Normal = 0,
        Maximum = 2,
        Fast = 4,
        SuperFast = 6,
        None = 255,
    }
}