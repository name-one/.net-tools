using System;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   The exception that is thrown when an error occurs while running a database migration.
    /// </summary>
    public class DbUpdateException : Exception
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="DbUpdateException"/> class
        ///   with a specified error message and a reference to the inner exception
        ///   that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DbUpdateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}