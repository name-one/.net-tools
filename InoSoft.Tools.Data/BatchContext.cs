using System.Collections.Generic;
using System.Linq;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Context for executing SQL queries in batched manner.
    /// </summary>
    public class BatchContext<TProcedures> : ISqlContext
    {
        private readonly SqlContext<TProcedures> _sqlContext;
        private readonly List<SqlQuery> _queries = new List<SqlQuery>();

        internal BatchContext(SqlContext<TProcedures> sqlContext, TProcedures procedures)
        {
            _sqlContext = sqlContext;
            Procedures = procedures;
        }

        /// <summary>
        /// Gets stored procedures proxy, which is used to call them on the database when Run() is invoked.
        /// </summary>
        public TProcedures Procedures { get; private set; }

        /// <summary>
        /// Executes a SQL query and ignores its results.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="parameters">Optional named parameters.</param>
        public void Execute(string sql, params object[] parameters)
        {
            Execute(sql, SqlQueryType.General, parameters);
        }

        /// <summary>
        /// Executes a SQL query and ignores its results.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="queryType">Query type.</param>
        /// <param name="parameters">Optional named parameters.</param>
        public void Execute(string sql, SqlQueryType queryType, params object[] parameters)
        {
            _queries.Add(new SqlQuery
            {
                ElementType = null,
                QueryType = queryType,
                Sql = sql,
                Parameters = parameters,
                Timeout = _sqlContext.CommandTimeout,
            });
        }

        /// <summary>
        /// Executes a SQL query and returns its results mapped onto type <see cref="T"/>.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <returns>An array with a single default(<see cref="T"/>) value.</returns>
        /// <remarks>
        /// As batch may be executed later, the method always returns an array with a single default value.
        /// The query will be executed anyway.
        /// </remarks>
        T[] ISqlContext.Execute<T>(string sql, params object[] parameters)
        {
            return ((ISqlContext)this).Execute<T>(sql, SqlQueryType.General, parameters);
        }

        /// <summary>
        /// Executes a SQL query and returns its results mapped onto type <see cref="T"/>.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="queryType">Query type.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <returns>An array with a single default(<see cref="T"/>) value.</returns>
        /// <remarks>
        /// As batch may be executed later, the method always returns an array with a single default value.
        /// The query will be executed anyway.
        /// </remarks>
        T[] ISqlContext.Execute<T>(string sql, SqlQueryType queryType, params object[] parameters)
        {
            _queries.Add(new SqlQuery
            {
                ElementType = typeof(T),
                QueryType = queryType,
                Sql = sql,
                Parameters = parameters,
                Timeout = _sqlContext.CommandTimeout,
            });

            return new[] { default(T) };
        }

        /// <summary>
        /// Executes the batch.
        /// </summary>
        public void Run()
        {
            // Create a batch of the SQL queries and enqueue it
            var batch = new SqlBatch { Queries = _queries.ToArray() };
            _sqlContext.EnqueueItem(batch);

            // Wait until query will be executed
            batch.WaitSignal();

            // If any error occured during batch execution, rethrow it
            var failedQuery = batch.Queries.FirstOrDefault(q => q.Exception != null);
            if (failedQuery != null)
            {
                throw failedQuery.Exception;
            }
        }
    }
}