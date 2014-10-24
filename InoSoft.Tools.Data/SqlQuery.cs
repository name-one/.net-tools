using System;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Encapsulates SQL query.
    /// </summary>
    public class SqlQuery
    {
        /// <summary>
        /// Gets or sets the type of the query result elements. If <c>null</c>, the query result will be ignored.
        /// </summary>
        public Type ElementType { get; set; }

        /// <summary>
        /// Gets or sets the query type.
        /// </summary>
        public SqlQueryType QueryType { get; set; }

        /// <summary>
        /// Gets or sets the the query string.
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// Gets or sets the query timeout in seconds.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Gets or sets the query parameters.
        /// </summary>
        public object[] Parameters { get; set; }

        /// <summary>
        /// Gets or sets the query execution result. Will contain the result after the query is executed.
        /// </summary>
        public Array Result { get; set; }

        /// <summary>
        /// Gets or sets the exception that occurred during query execution if any.
        /// </summary>
        public Exception Exception { get; set; }
    }
}