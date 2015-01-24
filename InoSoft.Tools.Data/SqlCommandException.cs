using System;

namespace InoSoft.Tools.Data
{
    /// <summary>
    ///   The exception that is thrown when a SQL command fails.
    /// </summary>
    public sealed class SqlCommandException : Exception
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlCommandException"/> class
        ///   with a reference to the inner exception that is the cause of this exception
        ///   and the same error message as the inner exception has.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        internal SqlCommandException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }
    }
}