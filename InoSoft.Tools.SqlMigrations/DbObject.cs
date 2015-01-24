using System;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   Represents a database object.
    /// </summary>
    internal class DbObject
    {
        /// <summary>
        ///   Gets or sets the name of this database object.
        /// </summary>
        /// <value>
        ///   The name of this database object.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        ///   Gets or sets the schema of this database object.
        /// </summary>
        /// <value>
        ///   The schema of this database object.
        /// </value>
        public string Schema { get; set; }

        /// <summary>
        ///   Returns the name of this database object.
        /// </summary>
        /// <returns>
        ///   The name of this database object.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }
    }
}