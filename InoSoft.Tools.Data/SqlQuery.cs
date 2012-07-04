using System;
using System.Threading;

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

    /// <summary>
    /// Encapsulates a batch of SQL queries that will be executed asynchronously.
    /// </summary>
    public class SqlBatch
    {
        private readonly EventWaitHandle _eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        /// <summary>
        /// SQL queries in the batch.
        /// </summary>
        public SqlQuery[] Queries { get; set; }

        /// <summary>
        /// Signals waiting thread, which called WaitSignal(), to continue.
        /// </summary>
        public void Signal()
        {
            _eventWaitHandle.Set();
        }

        /// <summary>
        /// Causes calling thread to block and wait other thread to call Signal().
        /// </summary>
        public void WaitSignal()
        {
            _eventWaitHandle.WaitOne();
        }
    }

    /// <summary>
    /// Interface for SQL query execution.
    /// </summary>
    public interface ISqlContext
    {
        /// <summary>
        /// Executes SQL query, which returns nothing.
        /// </summary>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        void Execute(string sql, params object[] parameters);

        /// <summary>
        /// Executes SQL command, which returns array of values of type <see cref="T"/>.
        /// </summary>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <returns>An array of <see cref="T"/> values.</returns>
        T[] Execute<T>(string sql, params object[] parameters);
    }
}