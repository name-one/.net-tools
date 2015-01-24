using System;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   Contains a definition of a database object.
    /// </summary>
    public class DbObjectDefinition
    {
        private readonly string _definition;
        private readonly string _name;
        private readonly string _schema;

        /// <summary>
        ///   Initializes a new instance of the <see cref="DbRoutineDefinition"/> class.
        /// </summary>
        /// <param name="name">The object name.</param>
        /// <param name="schema">The object schema name.</param>
        /// <param name="definition">The object definition SQL script.</param>
        public DbObjectDefinition(string name, string schema, string definition)
        {
            _name = name;
            _schema = schema;
            _definition = definition;
        }

        /// <summary>
        ///   Gets the object definition SQL script.
        /// </summary>
        /// <value>
        ///   The object definition SQL script.
        /// </value>
        public string Definition
        {
            get { return _definition; }
        }

        /// <summary>
        ///   Gets the schema-prefixed name of this database object.
        /// </summary>
        /// <value>
        ///   The the schema-prefixed name of this database object.
        /// </value>
        public string FullName
        {
            get { return String.Format("[{0}].[{1}]", _schema, _name); }
        }

        /// <summary>
        ///   Gets the object name.
        /// </summary>
        /// <value>
        ///   The object name.
        /// </value>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        ///   Gets the schema name of the object.
        /// </summary>
        /// <value>
        ///   The schema name of the object.
        /// </value>
        public string Schema
        {
            get { return _schema; }
        }

        /// <summary>
        ///   Returns the schema-prefixed name of this database object.
        /// </summary>
        /// <returns>
        ///   The schema-prefixed name of this database object.
        /// </returns>
        public override string ToString()
        {
            return FullName;
        }
    }
}