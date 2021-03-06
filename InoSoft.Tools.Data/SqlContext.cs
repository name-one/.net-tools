﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Context for executing SQL queries.
    /// </summary>
    /// <remarks>
    /// Works synchronously, but in a separate thread. Only one thread and one execution queue a created,
    /// so single context can operate only single SQL query at the same time. For simultaneous access
    /// multiple contexts must be used. Note that some versions of MSSQL cannot operate queries in parallel.
    /// In this case using single context for the whole application is recommended.
    /// </remarks>
    public class SqlContext : AsyncProcessor<SqlBatch>, ISqlContext, IDisposable
    {
        private static readonly HashSet<Type> SqlTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(byte),
            typeof(byte[]),
            typeof(decimal),
            typeof(double),
            typeof(float),
            typeof(int),
            typeof(long),
            typeof(short),
            typeof(string),
            typeof(DateTime),
            typeof(Guid)
        };

        private readonly string _connectionString;
        private readonly bool _createDatabase;
        private int _commandTimeout;
        private int _createDatabaseRetryCount = 30;
        private int _createDatabaseRetryInterval = 1000;
        private SqlConnection _sqlConnection;

        /// <summary>
        /// Creates an instance of <see cref="SqlContext"/>.
        /// </summary>
        /// <param name="connectionString">The connection used to open the SQL Server database.</param>
        /// <param name="commandTimeout">
        /// The time in seconds to wait for the command to execute. The default is 30 seconds.
        /// </param>
        /// <param name="createDatabase">
        /// Specifies whether a database should be created if it does not exist. The default is <c>false</c>.
        /// </param>
        public SqlContext(string connectionString, int commandTimeout, bool createDatabase)
        {
            _connectionString = connectionString;
            _commandTimeout = commandTimeout;
            _createDatabase = createDatabase;
            CreateConnection();
            Start();
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlContext"/>.
        /// </summary>
        /// <param name="connectionString">The connection used to open the SQL Server database.</param>
        /// <param name="createDatabase">
        /// Specifies whether a database should be created if it does not exist. The default is <c>false</c>.
        /// </param>
        public SqlContext(string connectionString, bool createDatabase)
            : this(connectionString, 30, createDatabase)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlContext"/>.
        /// </summary>
        /// <param name="connectionString">The connection used to open the SQL Server database.</param>
        /// <param name="commandTimeout">
        /// The time in seconds to wait for the command to execute. The default is 30 seconds.
        /// </param>
        public SqlContext(string connectionString, int commandTimeout)
            : this(connectionString, commandTimeout, false)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="SqlContext"/>.
        /// </summary>
        /// <param name="connectionString">The connection used to open the SQL Server database.</param>
        public SqlContext(string connectionString)
            : this(connectionString, 30, false)
        {
        }

        /// <summary>
        /// Query timeout in seconds.
        /// </summary>
        public int CommandTimeout
        {
            get { return _commandTimeout; }
            set { _commandTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the amount of retries to connect to just created database.
        /// </summary>
        public int CreateDatabaseRetryCount
        {
            get { return _createDatabaseRetryCount; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value", "Value must be greater than zero.");
                _createDatabaseRetryCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the time in milliseconds to wait between connection attempts to just created database.
        /// </summary>
        public int CreateDatabaseRetryInterval
        {
            get { return _createDatabaseRetryInterval; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value", "Value must be greater than zero.");
                _createDatabaseRetryInterval = value;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            Stop();
            _sqlConnection.Dispose();
        }

        /// <summary>
        /// Executes an SQL command that returns an array of elements.
        /// </summary>
        /// <param name="elementType">Type of the elements, or <c>null</c> if no query result is expected.</param>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <exception cref="SqlCommandException">A SQL error occurred while executing the SQL command.</exception>
        public Array Execute(Type elementType, string sql, params object[] parameters)
        {
            return Execute(elementType, sql, SqlQueryType.General, parameters);
        }

        /// <summary>
        /// Executes an SQL command that returns an array of elements.
        /// </summary>
        /// <param name="elementType">Type of the elements, or <c>null</c> if no query result is expected.</param>
        /// <param name="sql">SQL query string.</param>
        /// <param name="queryType">Query type.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <exception cref="SqlCommandException">A SQL error occurred while executing the SQL command.</exception>
        public Array Execute(Type elementType, string sql, SqlQueryType queryType, params object[] parameters)
        {
            // Create an encapsulated query and push it into the queue.
            var query = new SqlQuery
            {
                ElementType = elementType == null ? null : Nullable.GetUnderlyingType(elementType) ?? elementType,
                QueryType = queryType,
                Sql = sql,
                Parameters = parameters,
                Timeout = _commandTimeout,
            };
            var batch = new SqlBatch { Queries = new[] { query } };
            EnqueueItem(batch);

            // Wait until the query is executed.
            batch.WaitSignal();

            if (query.Exception != null)
            {
                // Rethrow the exception if one has occurred.
                throw new SqlCommandException(query.Exception);
            }

            return query.Result;
        }

        /// <summary>
        /// Executes SQL query, which returns nothing.
        /// </summary>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <exception cref="SqlCommandException">A SQL error occurred while executing the SQL command.</exception>
        public void Execute(string sql, params object[] parameters)
        {
            Execute(null, sql, parameters);
        }

        /// <summary>
        /// Executes SQL query, which returns nothing.
        /// </summary>
        /// <param name="sql">SQL query string.</param>
        /// <param name="queryType">Query type.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <exception cref="SqlCommandException">A SQL error occurred while executing the SQL command.</exception>
        public void Execute(string sql, SqlQueryType queryType, params object[] parameters)
        {
            Execute(null, sql, queryType, parameters);
        }

        /// <summary>
        /// Executes SQL command, which returns array of elements.
        /// </summary>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <exception cref="SqlCommandException">A SQL error occurred while executing the SQL command.</exception>
        public T[] Execute<T>(string sql, params object[] parameters)
        {
            return Execute<T>(sql, SqlQueryType.General, parameters);
        }

        /// <summary>
        /// Executes SQL command, which returns array of elements.
        /// </summary>
        /// <param name="sql">SQL query string.</param>
        /// <param name="queryType">Query type.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <exception cref="SqlCommandException">A SQL error occurred while executing the SQL command.</exception>
        public T[] Execute<T>(string sql, SqlQueryType queryType, params object[] parameters)
        {
            return Execute(typeof(T), sql, queryType, parameters).Cast<T>().ToArray();
        }

        /// <summary>
        /// Processes batches of SQL queries from queue.
        /// </summary>
        /// <param name="item">Encapsulated batch of queries.</param>
        protected override void ProcessItem(SqlBatch item)
        {
            item.Queries = CompressBatch(item).ToArray();
            foreach (SqlQuery query in item.Queries)
            {
                try
                {
                    // Connection may be used first time or closed by several exceptions, so try to open/reopen if it's so.
                    if (_sqlConnection.State != ConnectionState.Open)
                    {
                        try
                        {
                            OpenConnection(1);
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Number == 4060 && _createDatabase)
                            {
                                CreateDatabase();
                                OpenConnection(CreateDatabaseRetryCount);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }

                    using (SqlCommand command = _sqlConnection.CreateCommand())
                    {
                        // Initialize SQL command.
                        command.CommandType = query.QueryType == SqlQueryType.Procedure
                            ? CommandType.StoredProcedure
                            : CommandType.Text;
                        command.CommandText = query.Sql;
                        command.Parameters.AddRange(query.Parameters);
                        command.CommandTimeout = query.Timeout;

                        if (query.ElementType != null)
                        {
                            // Convert/map the result to the desired type.
                            using (DbDataReader reader = command.ExecuteReader())
                            {
                                IEnumerable<object> result = SqlTypes.Contains(query.ElementType)
                                    ? ReadSqlTypeResult(reader)
                                    : ReadCustomTypeResult(reader, query.ElementType);

                                query.Result = result.ToArray();
                            }
                        }
                        else
                        {
                            // Just execute query, there is no need to return result.
                            command.ExecuteNonQuery();
                        }
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

        /// <summary>
        /// Converts the query to the <see cref="SqlQueryType.General"/> type, renames its parameters.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="paramPrefix">The parameter prefix.</param>
        /// <returns>
        /// A SQL query of the <see cref="SqlQueryType.General"/> type.
        /// </returns>
        private static SqlQuery GeneralizeQuery(SqlQuery query, string paramPrefix)
        {
            if (query.QueryType != SqlQueryType.Procedure)
                // TODO: Replace parameters of general queries in a batch.
                return query;

            int i = 0;
            var parameters = new StringBuilder();
            foreach (SqlParameter parameter in query.Parameters)
            {
                string name = String.Format("q{0}p{1}", paramPrefix, i++);
                if (parameters.Length > 0)
                {
                    parameters.Append(", ");
                }
                parameters.AppendFormat("@{0} = @{1}", parameter.ParameterName, name);
                parameter.ParameterName = name;
            }

            query.QueryType = SqlQueryType.General;
            query.Sql = String.Format("EXEC {0} {1}", query.Sql, parameters);

            return query;
        }

        /// <summary>
        /// Reads a result set of the specified custom type.
        /// </summary>
        /// <param name="reader">Data reader to read the result from.</param>
        /// <param name="elementType">Custom element type.</param>
        /// <returns>
        /// A result set of the specified type.
        /// </returns>
        private static IEnumerable<object> ReadCustomTypeResult(DbDataReader reader, Type elementType)
        {
            // Get properties that match column names in the result set.
            var properties = new ResultProperty[reader.VisibleFieldCount];
            for (int i = 0; i < reader.VisibleFieldCount; i++)
            {
                properties[i] = ResultProperty.Get(elementType.GetProperty(reader.GetName(i)));
            }

            // Fill result list with items.
            while (reader.Read())
            {
                // Create object of desired type and set its properties.
                object resultItem = Activator.CreateInstance(elementType);
                for (int i = 0; i < properties.Length; i++)
                {
                    ResultProperty property = properties[i];

                    // Property may be null if the column has no matching property.
                    if (property == null) continue;

                    object value = reader.GetValue(i);

                    // Convert the value to the target type.
                    Type type = property.UnderlyingType;
                    if (value is string && property.Info.GetAttributes<SqlXmlAttribute>().Length > 0)
                    {
                        value = XmlHelper.Deserialize(type, (string)value) ?? DBNull.Value;
                    }
                    value = value != DBNull.Value
                        ? type.IsEnum
                            ? Enum.ToObject(type, value)
                            : Convert.ChangeType(value, type)
                        : null;

                    property.Info.SetValue(resultItem, value, null);
                }
                yield return resultItem;
            }
        }

        /// <summary>
        /// Reads a result set of one of the default SQL types.
        /// </summary>
        /// <param name="reader">Data reader to read the result from.</param>
        /// <returns>
        /// A result set.
        /// </returns>
        /// <seealso cref="SqlTypes"/>
        private static IEnumerable<object> ReadSqlTypeResult(IDataReader reader)
        {
            while (reader.Read())
            {
                object sqlValue = reader.GetValue(0);
                yield return sqlValue == DBNull.Value ? null : sqlValue;
            }
        }

        /// <summary>
        /// Compresses the queries in a batch into large multi-statement queries.
        /// </summary>
        /// <param name="batch">The batch to compress.</param>
        /// <returns>
        /// A collection of multi-statement queries.
        /// </returns>
        private IEnumerable<SqlQuery> CompressBatch(SqlBatch batch)
        {
            if (batch.Queries.Length == 0)
                yield break;

            if (batch.Queries.Length == 1)
            {
                yield return batch.Queries[0];
                yield break;
            }

            int i = 0;
            var commandText = new StringBuilder();
            var parameters = new List<object>();
            foreach (SqlQuery query in batch.Queries)
            {
                const int maxSqlParameters = 2100;
                if (parameters.Count + query.Parameters.Length >= maxSqlParameters)
                {
                    yield return CreateQuery(commandText.ToString(), parameters.ToArray<object>());
                    i = 0;
                    commandText.Clear();
                    parameters.Clear();
                }

                SqlQuery generalizedQuery = GeneralizeQuery(query, (i++).ToString(CultureInfo.InvariantCulture));

                commandText.Append(generalizedQuery.Sql).AppendLine(";");
                parameters.AddRange(generalizedQuery.Parameters);
            }

            yield return CreateQuery(commandText.ToString(), parameters.ToArray<object>());
        }

        /// <summary>
        /// Creates a connection from the connection string.
        /// </summary>
        private void CreateConnection()
        {
            if (_sqlConnection != null)
            {
                _sqlConnection.Dispose();
            }
            _sqlConnection = new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Creates a database from the connection string.
        /// </summary>
        private void CreateDatabase()
        {
            var connectionString = new SqlConnectionStringBuilder(_connectionString);
            var dbName = connectionString.InitialCatalog;
            connectionString.InitialCatalog = "master";
            using (var connection = new SqlConnection(connectionString.ToString()))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = String.Format("CREATE DATABASE [{0}]", dbName);
                    command.CommandTimeout = _commandTimeout;
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Creates a SQL query.
        /// </summary>
        /// <param name="sql">The query string.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>
        /// A new SQL query with the specified query string and parameters.
        /// </returns>
        private SqlQuery CreateQuery(string sql, object[] parameters)
        {
            return new SqlQuery
            {
                ElementType = null,
                QueryType = SqlQueryType.General,
                Sql = sql,
                Parameters = parameters,
                Timeout = _commandTimeout,
            };
        }

        /// <summary>
        /// Tries to connect to the database specified in the connection string the specified amount of times.
        /// </summary>
        /// <param name="retryCount">Number of connection attempts before throwing exception.</param>
        /// <remarks>
        /// SQL server may not create a database immediately, thus some retries can be performed.
        /// </remarks>
        private void OpenConnection(int retryCount)
        {
            for (int i = 0; ; )
            {
                try
                {
                    _sqlConnection.Open();

                    // Set connection options.
                    using (SqlCommand command = _sqlConnection.CreateCommand())
                    {
                        command.CommandText = "SET ARITHABORT ON";
                        command.ExecuteNonQuery();
                    }
                    break;
                }
                catch (SqlException ex)
                {
                    // Rethrow exception if we the reason of the exception is not that the database does not exist,
                    // or this is the last attempt.
                    if (ex.Number != 4060 || ++i == retryCount)
                        throw;

                    // Otherwise, wait and try again.
                    Thread.Sleep(_createDatabaseRetryInterval);
                }
            }
        }

        private class ResultProperty
        {
            public readonly PropertyInfo Info;
            public readonly Type UnderlyingType;

            private ResultProperty(PropertyInfo propertyInfo)
            {
                Info = propertyInfo;
                UnderlyingType = Nullable.GetUnderlyingType(Info.PropertyType) ?? Info.PropertyType;
            }

            public static ResultProperty Get(PropertyInfo propertyInfo)
            {
                return propertyInfo != null ? new ResultProperty(propertyInfo) : null;
            }
        }
    }
}