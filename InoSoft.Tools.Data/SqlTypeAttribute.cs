using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace InoSoft.Tools.Data
{
    /// <summary>
    ///   Indicates that a parameter should be passed to a SQL command as a table-valued parameter.
    /// </summary>
    public abstract class SqlTypeAttribute : Attribute
    {
        private readonly SqlColumn[] _columns;
        private readonly bool _isSimpleType;
        private readonly string _typeName;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlTypeAttribute"/> class for a complex type.
        /// </summary>
        /// <param name="columns">The column definitions of the SQL type that the parameter has.</param>
        protected SqlTypeAttribute(params SqlColumn[] columns)
            : this(null, columns)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlTypeAttribute"/> class for a complex type.
        /// </summary>
        /// <param name="typeName">The name of the SQL type that the parameter has.</param>
        /// <param name="columns">The column definitions of the SQL type that the parameter has.</param>
        protected SqlTypeAttribute(string typeName, params SqlColumn[] columns)
        {
            _typeName = typeName;
            _columns = columns;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlTypeAttribute"/> class for a simple type.
        /// </summary>
        /// <param name="columnType">The type of the single column in the SQL type to map the simple type to.</param>
        /// <param name="columnName">The name of the single column in the SQL type to map the simple type to.</param>
        protected SqlTypeAttribute(string columnName, Type columnType)
            : this(null, columnName, columnType)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlTypeAttribute"/> class for a simple type.
        /// </summary>
        /// <param name="typeName">The name of the SQL type that the parameter has.</param>
        /// <param name="columnType">The type of the single column in the SQL type to map the simple type to.</param>
        /// <param name="columnName">The name of the single column in the SQL type to map the simple type to.</param>
        protected SqlTypeAttribute(string typeName, string columnName, Type columnType)
        {
            _typeName = typeName;
            _isSimpleType = true;
            _columns = new[] { new SqlColumn(columnName, columnType) };
        }

        /// <summary>
        ///   Gets the column definitions of the SQL type that the parameter has.
        /// </summary>
        /// <value>
        ///   The column definitions of the SQL type that the parameter has.
        /// </value>
        public SqlColumn[] Columns
        {
            get { return _columns; }
        }

        /// <summary>
        ///   Gets the name of the SQL type that the parameter has.
        /// </summary>
        /// <value>
        ///   The name of the SQL type that the parameter has.
        /// </value>
        public string TypeName
        {
            get { return _typeName; }
        }

        /// <summary>
        ///   Creates a SQL parameter with the specified name from an array of items.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="name">The parameter name.</param>
        /// <param name="items">An array of items to map to a table-valued parameter type.</param>
        /// <returns>
        ///   A SQL parameter with the specified name that contains the data from <paramref name="items"/>.
        /// </returns>
        public SqlParameter CreateParameter<T>(string name, T[] items)
        {
            return new SqlParameter(name, CreateTable(items))
            {
                SqlDbType = SqlDbType.Structured,
                TypeName = TypeName,
            };
        }

        /// <summary>
        ///   Creates a table with the same columns as the SQL type.
        /// </summary>
        /// <returns>
        ///   A table with the same columns as the SQL type.
        /// </returns>
        public DataTable CreateTable()
        {
            var table = new DataTable();
            foreach (SqlColumn column in _columns)
            {
                table.Columns.Add(column.Name, column.Type);
            }
            return table;
        }

        /// <summary>
        ///   Creates a table with the same columns as the SQL type, filled by the data from an array of items.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">An array of items to map to a table-valued parameter type.</param>
        /// <returns>
        ///   A table filled by the data from <paramref name="items"/>.
        /// </returns>
        public DataTable CreateTable<T>(T[] items)
        {
            DataTable table = CreateTable();
            var properties = new PropertyInfo[_columns.Length];

            if (!_isSimpleType)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    properties[i] = typeof(T).GetProperty(_columns[i].Name);
                }
            }

            foreach (T item in items)
            {
                var row = new object[properties.Length];
                for (int i = 0; i < row.Length; i++)
                {
                    row[i] = _isSimpleType ? item : properties[i].GetValue(item, null);
                }
                table.Rows.Add(row);
            }
            return table;
        }
    }
}