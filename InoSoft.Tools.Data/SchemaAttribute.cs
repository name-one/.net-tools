using System;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Describes the schema of a stored procedure or a data context.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
    public class SchemaAttribute : Attribute
    {
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaAttribute"/> class.
        /// </summary>
        /// <param name="name">The schema name.</param>
        public SchemaAttribute(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Gets the schema name.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }
    }
}