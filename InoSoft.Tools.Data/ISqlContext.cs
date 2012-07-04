namespace InoSoft.Tools.Data
{
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