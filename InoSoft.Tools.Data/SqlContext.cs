﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Context for executing SQL queries.
    /// </summary>
    /// <remarks>
    /// Works synchronously, but in a separate thread. Only one thread and one execution queue a created,
    /// so single context can operate only single SQL query at the same time. For simultaneous access
    /// miltiple contexts must be used. Note that some versions of MSSQL can't operate queries in parallel.
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
            CreateConnection();
            _commandTimeout = commandTimeout;
            _createDatabase = createDatabase;
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
            _sqlConnection.Dispose();
        }

        /// <summary>
        /// Executes an SQL command that returns an array of elements.
        /// </summary>
        /// <param name="elementType">Type of the elements, or <c>null</c> if no query result is expected.</param>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        public Array Execute(Type elementType, string sql, params object[] parameters)
        {
            // Create an encapsulated query and push it into the queue.
            var query = new SqlQuery
            {
                ElementType = elementType,
                Sql = sql,
                Parameters = parameters,
                Timeout = _commandTimeout
            };
            var batch = new SqlBatch { Queries = new[] { query } };
            EnqueueItem(batch);

            // Wait until the query is executed.
            batch.WaitSignal();

            if (query.Exception != null)
            {
                // Rethrow the exception if one has occured.
                throw query.Exception;
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
                    // Connection may be used first time or closed by several exceptions, so try to open/reopen if it's so.
                    if (_sqlConnection.State != ConnectionState.Open)
                    {
                        try
                        {
                            _sqlConnection.Open();
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Number == 4060 && _createDatabase)
                            {
                                CreateDatabase();
                                OpenConnection();
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }

                    using (var command = _sqlConnection.CreateCommand())
                    {
                        // Init command SQL text and parameters.
                        command.CommandText = query.Sql;
                        command.Parameters.AddRange(query.Parameters);
                        command.CommandTimeout = query.Timeout;

                        if (query.ElementType != null)
                        {
                            // We want command to have result of desired type.
                            using (var reader = command.ExecuteReader(CommandBehavior.KeyInfo))
                            {
                                List<object> result = SqlTypes.Contains(query.ElementType)
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
        /// Reads a result set of the specified custom type.
        /// </summary>
        /// <param name="reader">Data reader to read the result from.</param>
        /// <param name="elementType">Custom element type.</param>
        /// <returns>
        /// A result set of the specified type.
        /// </returns>
        private static List<object> ReadCustomTypeResult(SqlDataReader reader, Type elementType)
        {
            var result = new List<object>();

            // Build list of property-infos, which match result set column names.
            var properties = new List<PropertyInfo>();
            for (int i = 0; i < reader.VisibleFieldCount; i++)
            {
                var prop = elementType.GetProperty(reader.GetName(i));
                properties.Add(prop);
            }

            // Fill result list with items.
            while (reader.Read())
            {
                // Create object of desired type and set its properties.
                var resultItem = Activator.CreateInstance(elementType);
                for (int i = 0; i < properties.Count; i++)
                {
                    // Property info may be null if there is column, which has no property to match.
                    if (properties[i] != null)
                    {
                        var sqlValue = reader.GetValue(i);
                        properties[i].SetValue(resultItem, sqlValue == DBNull.Value ? null : sqlValue, null);
                    }
                }
                result.Add(resultItem);
            }
            return result;
        }

        /// <summary>
        /// Reads a result set of one of the default SQL types.
        /// </summary>
        /// <param name="reader">Data reader to read the result from.</param>
        /// <returns>
        /// A result set.
        /// </returns>
        /// <seealso cref="SqlTypes"/>
        private static List<object> ReadSqlTypeResult(SqlDataReader reader)
        {
            var result = new List<object>();
            while (reader.Read())
            {
                var sqlValue = reader.GetValue(0);
                result.Add(sqlValue == DBNull.Value ? null : sqlValue);
            }
            return result;
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
                var command = connection.CreateCommand();
                command.CommandText = "CREATE DATABASE " + dbName;
                command.CommandTimeout = _commandTimeout;
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Tries to connect to the database specified in the connection string
        /// <see cref="CreateDatabaseRetryCount"/> times.
        /// </summary>
        /// <remarks>
        /// SQL server may not create a database immediately, thus some retries can be performed.
        /// </remarks>
        private void OpenConnection()
        {
            for (int i = 0; i < _createDatabaseRetryCount; i++)
            {
                try
                {
                    _sqlConnection.Open();
                    break;
                }
                catch (SqlException ex)
                {
                    // Rethrow exception if we couldn't connect not because the database does not exist.
                    if (ex.Number != 4060)
                    {
                        throw;
                    }
                }
                Thread.Sleep(_createDatabaseRetryInterval);
            }
        }
    }
}