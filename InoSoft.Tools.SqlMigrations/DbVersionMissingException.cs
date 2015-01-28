using System;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   The exception that is thrown when the database schema version is not specified in the target database.
    /// </summary>
    public class DbVersionMissingException : InvalidOperationException
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="DbVersionMissingException"/> class
        ///   with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DbVersionMissingException(string message)
            : base(message)
        {
        }
    }
}