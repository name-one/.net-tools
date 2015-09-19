using System;

namespace InoSoft.Tools.Data
{
    /// <summary>
    ///   Defines a column in a data table.
    /// </summary>
    public class SqlColumn
    {
        private readonly string _name;
        private readonly Type _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlColumn"/> class.
        /// </summary>
        /// <param name="name">The column name.</param>
        /// <param name="type">The column data type.</param>
        public SqlColumn(string name, Type type)
        {
            _name = name;
            _type = type;
        }

        /// <summary>
        ///   Gets the column name.
        /// </summary>
        /// <value>
        ///   The column name.
        /// </value>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        ///   Gets the column data type.
        /// </summary>
        /// <value>
        ///   The column data type.
        /// </value>
        public Type Type
        {
            get { return _type; }
        }
    }
}