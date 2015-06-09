using System;
using System.Collections.Generic;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   Contains a definition of a database object.
    /// </summary>
    public class DbObjectDefinition : IEquatable<DbObjectDefinition>
    {
        private static readonly IEqualityComparer<DbObjectDefinition> FullNameComparerInstance =
            new FullNameEqualityComparer();

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
        ///   Gets the full name <see cref="DbRoutineDefinition"/> comparer.
        /// </summary>
        /// <value>
        ///   The full name <see cref="DbRoutineDefinition"/> comparer.
        /// </value>
        public static IEqualityComparer<DbObjectDefinition> FullNameComparer
        {
            get { return FullNameComparerInstance; }
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
        ///   Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///   <c>true</c> if the current object is equal to the <paramref name="other" /> parameter;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(DbObjectDefinition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return String.Equals(_schema, other._schema)
                && String.Equals(_name, other._name)
                && String.Equals(_definition.Trim(), other._definition.Trim());
        }

        /// <summary>
        ///   Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as DbObjectDefinition);
        }

        /// <summary>
        ///   Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        ///   A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (_schema != null ? _schema.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_name != null ? _name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_definition != null ? _definition.Trim().GetHashCode() : 0);
                return hashCode;
            }
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

        /// <summary>
        ///   Converts a <see cref="DbObject"/> to a <see cref="DbObjectDefinition"/>.
        /// </summary>
        /// <param name="obj">The database object to convert.</param>
        /// <returns>
        ///   A <see cref="DbObjectDefinition"/> converted from a <see cref="DbObject"/>.
        /// </returns>
        internal static DbObjectDefinition FromDbObject(DbObject obj)
        {
            return new DbObjectDefinition(obj.Name, obj.Schema, obj.Definition);
        }

        /// <summary>
        ///   Compares <see cref="DbObjectDefinition"/> objects using their full name i.e., schema name and object name.
        /// </summary>
        private sealed class FullNameEqualityComparer : IEqualityComparer<DbObjectDefinition>
        {
            /// <summary>
            ///   Determines whether the specified definitions are equal.
            /// </summary>
            /// <param name="x">The first definition to compare.</param>
            /// <param name="y">The second definition to compare.</param>
            /// <returns>
            ///   <c>true</c> if the specified definitions are equal; otherwise, <c>false</c>.
            /// </returns>
            public bool Equals(DbObjectDefinition x, DbObjectDefinition y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                return String.Equals(x._name, y._name) && String.Equals(x._schema, y._schema);
            }

            /// <summary>
            ///   Returns a hash code for the specified definition.
            /// </summary>
            /// <param name="obj">The definition for which a hash code is to be returned.</param>
            /// <returns>
            ///   A hash code for the specified definition.
            /// </returns>
            public int GetHashCode(DbObjectDefinition obj)
            {
                unchecked
                {
                    return (obj._name.GetHashCode() * 397) ^ obj._schema.GetHashCode();
                }
            }
        }
    }
}