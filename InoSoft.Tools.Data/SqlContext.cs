using System;
using System.Data.Entity;
using System.Linq;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Context for executing SQL queries.
    /// </summary>
    /// <remarks>
    /// Works synchronously, but in separate thread. Only one thread and one execution queue are created,
    /// so single context can operate only single SQL query at the same time. For simultaneous access
    /// miltiple contexts must be used. Note that some versions of MSSQL can't operate queries in parallel.
    /// In this case using single context for the whole application is recommended.
    /// This context is asynchronous wrapper of EF 4.1 DbContext and its best advantage is that it works in single
    /// thread, as mentioned above. This technique solves problem when EntityConnection reconnects each time
    /// it's used in a different thread.
    /// </remarks>
    public class SqlContext : AsyncProcessor<SqlBatch>, ISqlContext
    {
        private readonly DbContext _dbContext;

        /// <summary>
        /// Creates SqlContext.
        /// </summary>
        /// <param name="connectionString">SQL connection string, which context will use.</param>
        public SqlContext(string connectionString)
        {
            _dbContext = new DbContext(connectionString);
            Start();
        }

        /// <summary>
        /// Executes SQL command, which returns array of elements.
        /// </summary>
        /// <param name="elementType">Type of elements or null if we don't need to return query result.</param>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        public Array Execute(Type elementType, string sql, params object[] parameters)
        {
            // Check if query return type contains enum values and thus needs a proxy type.
            bool needsProxy = false;
            Type realType = elementType;
            if (elementType != null && !SqlTypeHelper.IsSqlType(elementType) && elementType.ContainsEnums())
            {
                needsProxy = true;
                realType = elementType.GetEnumlessProxy();
            }

            // Create encapsulated query and push it into queue.
            var query = new SqlQuery
            {
                ElementType = realType,
                Sql = sql,
                Parameters = parameters
            };
            var batch = new SqlBatch { Queries = new[] { query } };
            EnqueueItem(batch);

            // Wait until query is executed.
            batch.WaitSignal();

            if (query.Exception != null)
            {
                // Rethrow the exception if it occured.
                throw query.Exception;
            }

            // Return query result.
            if (needsProxy)
            {
                return ReflectionHelper.CloneArray(query.Result, realType, elementType);
            }
            return query.Result;
        }

        /// <summary>
        /// Executes SQL query, which returns nothing.
        /// </summary>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        public void Execute(string sql, params object[] parameters)
        {
            Execute(null, sql, parameters);
        }

        /// <summary>
        /// Executes SQL command, which returns array of elements.
        /// </summary>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        public T[] Execute<T>(string sql, params object[] parameters)
        {
            return Execute(typeof(T), sql, parameters).Cast<T>().ToArray();
        }

        /// <summary>
        /// Processes batches of SQL queries from queue.
        /// </summary>
        /// <param name="item">Encapsulated batch of queries.</param>
        protected override void ProcessItem(SqlBatch item)
        {
            foreach (var query in item.Queries)
            {
                try
                {
                    if (query.ElementType != null)
                    {
                        query.Result = _dbContext.Database.SqlQuery(query.ElementType, query.Sql, query.Parameters)
                            .Cast<object>().ToArray();
                    }
                    else
                    {
                        _dbContext.Database.ExecuteSqlCommand(query.Sql, query.Parameters);
                    }
                }
                catch (Exception ex)
                {
                    query.Exception = ex;
                    break;
                }
            }
            item.Signal();
        }
    }
}