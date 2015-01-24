using System;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   The exception that is thrown when a SQL command fails while running a database migration.
    /// </summary>
    public class DbUpdateCommandException : DbUpdateException
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="DbUpdateCommandException"/> class
        ///   with a specified error message and a reference to the inner exception
        ///   that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="command">
        ///   The command that failed. This will be appended to the error message for reference.
        /// </param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DbUpdateCommandException(string message, string command, Exception innerException)
            : base(String.Join(Environment.NewLine, message, "", command, ""), innerException)
        {
        }
    }
}