using System;

namespace InoSoft.Tools.Data
{ 
    [AttributeUsage(AttributeTargets.Method)]
    public class FunctionAttribute : Attribute
    {
        private readonly bool _isTableValued;
        private readonly string _schema;

        /// <summary>
        /// Creates attribute.
        /// </summary>
        /// <param name="isTableValued">Determines that SQL function is table-valued or scalar-valued.</param>
        public FunctionAttribute(bool isTableValued = true, string schema = "dbo")
        {
            _isTableValued = isTableValued;
            _schema = schema;
        }

        /// <summary>
        /// Gets SQL query with invoking function.
        /// </summary>
        /// <param name="functionName">SQL function name.</param>
        /// <param name="paramsString">PArameters of the SQL function.</param>
        /// <returns></returns>
        internal string GetQuery(string functionName, string paramsString)
        {
            return String.Format(
                _isTableValued ? "SELECT * FROM {0}.{1}({2})" : "SELECT {0}.{1}({2})",
                _schema,
                functionName,
                paramsString);
        }
    }
}
