using System;

namespace InoSoft.Tools.Data
{
    /// <summary>
    ///   Indicates that a method should be treated as a SQL function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class FunctionAttribute : Attribute
    {
        private readonly bool _isTableValued;
        private readonly string _schema;

        /// <summary>
        ///   Initializes a new instance of the <see cref="FunctionAttribute"/> class.
        /// </summary>
        /// <param name="isTableValued">
        ///   <c>true</c> if the function is table-valued;
        ///   <br />
        ///   <c>false</c> if the function is scalar-valued.
        /// </param>
        /// <param name="schema">The schema that contains the function.</param>
        public FunctionAttribute(bool isTableValued = true, string schema = "dbo")
        {
            _isTableValued = isTableValued;
            _schema = schema;
        }

        /// <summary>
        ///   Gets a value indicating whether the function is table-valued.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the function is table-valued;
        ///   <br />
        ///   <c>false</c> if the function is scalar-valued.
        /// </value>
        public bool IsTableValued
        {
            get { return _isTableValued; }
        }

        /// <summary>
        ///   Gets the schema that contains the function.
        /// </summary>
        /// <value>
        ///   The schema that contains the function.
        /// </value>
        public string Schema
        {
            get { return _schema; }
        }

        /// <summary>
        ///   Gets a SQL query invoking the function.
        /// </summary>
        /// <param name="functionName">The function name.</param>
        /// <param name="paramsString">The parameters of the SQL function.</param>
        /// <returns>
        ///   A SQL query invoking the function.
        /// </returns>
        internal string GetQuery(string functionName, string paramsString)
        {
            return String.Format(
                _isTableValued ? "SELECT * FROM [{0}].[{1}]({2})" : "SELECT [{0}].[{1}]({2})",
                _schema,
                functionName,
                paramsString);
        }
    }
}