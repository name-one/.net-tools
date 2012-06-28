using System;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Indicates that method executes stored procedure and returns query result only if it's single.
    /// Otherwise, exception will be thrown. Use only in methods which return single object, not array.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SingleResultAttribute : Attribute
    {
    }
}