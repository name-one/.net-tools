namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Interface for SQL query execution.
    /// </summary>
    public interface ISqlContext
    {
        /// <summary>
        /// Executes a SQL query and ignores its results.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="parameters">Optional named parameters.</param>
        void Execute(string sql, params object[] parameters);

        /// <summary>
        /// Executes a SQL query and ignores its results.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="queryType">Query type.</param>
        /// <param name="parameters">Optional named parameters.</param>
        void Execute(string sql, SqlQueryType queryType, params object[] parameters);

        /// <summary>
        /// Executes a SQL query and returns its results mapped onto type <see cref="T"/>.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <returns>
        /// An array of <see cref="T"/> values.
        /// </returns>
        T[] Execute<T>(string sql, params object[] parameters);

        /// <summary>
        /// Executes a SQL query and returns its results mapped onto type <see cref="T"/>.
        /// </summary>
        /// <param name="sql">SQL query.</param>
        /// <param name="queryType">Query type.</param>
        /// <param name="parameters">Optional named parameters.</param>
        /// <returns>
        /// An array of <see cref="T"/> values.
        /// </returns>
        T[] Execute<T>(string sql, SqlQueryType queryType, params object[] parameters);
    }
}