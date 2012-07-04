using System;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Encapsulates SQL query.
    /// </summary>
    public class SqlQuery
    {
        /// <summary>
        /// Type of query result elements or null no need to return anything.
        /// </summary>
        public Type ElementType { get; set; }

        /// <summary>
        /// Query string representation.
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// Input parameters of query.
        /// </summary>
        public object[] Parameters { get; set; }

        /// <summary>
        /// Result of execution will be set there.
        /// </summary>
        public Array Result { get; set; }

        /// <summary>
        /// If exception will occur during execution, it will be set here.
        /// </summary>
        public Exception Exception { get; set; }
    }
}