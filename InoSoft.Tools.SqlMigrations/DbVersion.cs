using System;
using System.Linq;
using InoSoft.Tools.Data;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   Represents the version of the database schema.
    /// </summary>
    public class DbVersion : IEquatable<DbVersion>, IComparable<DbVersion>
    {
        private readonly string _comment;
        private readonly Version _version;

        /// <summary>
        ///   Initializes a new instance of the <see cref="DbVersion"/> class.
        /// </summary>
        /// <param name="version">The numeric database schema version.</param>
        /// <param name="comment">The database schema version comment string.</param>
        public DbVersion(Version version, string comment = null)
        {
            _version = version;
            _comment = comment;
        }

        /// <summary>
        ///   Gets the database schema version comment string.
        /// </summary>
        /// <value>
        ///   The database schema version comment string.
        /// </value>
        public string Comment
        {
            get { return _comment; }
        }

        /// <summary>
        ///   Gets the numeric database schema version.
        /// </summary>
        /// <value>
        ///   The numeric database schema version.
        /// </value>
        public Version Version
        {
            get { return _version; }
        }

        /// <summary>
        ///   Converts the string representation of a version to an equivalent <see cref="DbVersion"/> object.
        /// </summary>
        /// <param name="input">
        ///   A string that contains a version to convert.<br/>
        ///   Example input: <c>"1.0.0.0~comment"</c>
        /// </param>
        /// <returns>
        ///   An object that is equivalent to the specified version.
        /// </returns>
        /// <exception cref="FormatException">Schema version is not in a valid format.</exception>
        public static DbVersion Parse(string input)
        {
            try
            {
                int splitIndex = input.IndexOf('~');
                return splitIndex > 0
                    ? new DbVersion(Version.Parse(input.Substring(0, splitIndex)), input.Substring(splitIndex + 1))
                    : new DbVersion(Version.Parse(input));
            }
            catch (Exception ex)
            {
                throw new FormatException("Schema version is not in a valid format.", ex);
            }
        }

        /// <summary>
        ///   Reads the database schema version from a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the database to read the schema version of.</param>
        /// <param name="versionProperty">
        ///   The name of the SQL Server extended property that contains the current schema version.
        /// </param>
        /// <returns>
        ///   The schema version of the specified database.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="connectionString"/> or <paramref name="versionProperty"/> in <c>null</c>.
        /// </exception>
        /// <exception cref="SqlCommandException">Failed to read from the database.</exception>
        /// <exception cref="InvalidOperationException">Schema version is not specified in the database.</exception>
        /// <exception cref="FormatException">Schema version is not in a valid format.</exception>
        public static DbVersion Read(string connectionString, string versionProperty)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            using (var context = new SqlContext(connectionString))
            {
                return Read(context, versionProperty);
            }
        }

        /// <summary>
        ///   Reads the database schema version from the specified database context.
        /// </summary>
        /// <param name="context">The database context to read the schema version from.</param>
        /// <param name="versionProperty">
        ///   The name of the SQL Server extended property that contains the current schema version.
        /// </param>
        /// <returns>
        ///   The schema version of the database underlying the specified context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> or <paramref name="versionProperty"/> in <c>null</c>.
        /// </exception>
        /// <exception cref="SqlCommandException">Failed to read from the database.</exception>
        /// <exception cref="InvalidOperationException">Schema version is not specified in the database.</exception>
        /// <exception cref="FormatException">Schema version is not in a valid format.</exception>
        public static DbVersion Read(SqlContext context, string versionProperty)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (versionProperty == null)
                throw new ArgumentNullException("versionProperty");

            string versionString = context
                .Execute<string>(String.Format(
                    "SELECT value FROM fn_listextendedproperty('{0}', NULL, NULL, NULL, NULL, NULL, NULL)",
                    versionProperty))
                .FirstOrDefault();

            if (versionString == null)
                throw new InvalidOperationException("Schema version is not specified in the database.");

            return Parse(versionString);
        }

        /// <summary>
        ///   Compares the current version with another one.
        /// </summary>
        /// <param name="other">A version to compare with this one.</param>
        /// <returns>
        ///   A value that indicates the relative order of the versions being compared.<br/>
        ///   The return value has the following meanings:<br/>
        ///   Less than zero - This version is before the <paramref name="other"/>.<br/>
        ///   Zero - This version is the same as the <paramref name="other"/>.<br/>
        ///   Greater than zero - This version is after the <paramref name="other"/>.
        /// </returns>
        public int CompareTo(DbVersion other)
        {
            int result = _version.CompareTo(other._version);
            return result != 0 ? result : String.Compare(_comment, other._comment, StringComparison.Ordinal);
        }

        /// <summary>
        ///   Indicates whether the current version is equal to another one.
        /// </summary>
        /// <param name="other">A version to compare with this one.</param>
        /// <returns>
        ///   <c>true</c> if the current version is the same as the <paramref name="other" />; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(DbVersion other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(_version, other._version) && _comment == other._comment;
        }

        /// <summary>
        ///   Determines whether the specified object is equal to this version.
        /// </summary>
        /// <param name="obj">The object to compare with this version.</param>
        /// <returns>
        ///   <c>true</c> if the specified object is equal to this version; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is DbVersion && Equals((DbVersion)obj);
        }

        /// <summary>
        ///   Calculates a hash code for this version.
        /// </summary>
        /// <returns>
        ///   A hash code for this version.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (_version.GetHashCode() * 397) ^ (_comment != null ? _comment.GetHashCode() : 0);
            }
        }

        /// <summary>
        ///   Returns a <see cref="String"/> that represents this schema database version.
        /// </summary>
        /// <returns>
        ///   A <see cref="String"/> that represents this schema database version.<br/>
        ///   Example output: <c>"1.0.0.0~comment"</c>
        /// </returns>
        public override string ToString()
        {
            return _comment == null
                ? _version.ToString()
                : String.Join("~", _version, _comment);
        }

        /// <summary>
        ///   Writes the database schema version to a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the database to write the schema version to.</param>
        /// <param name="versionProperty">
        ///   The name of the SQL Server extended property that contains the current schema version.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="connectionString"/> or <paramref name="versionProperty"/> in <c>null</c>.
        /// </exception>
        /// <exception cref="SqlCommandException">Failed to write to the database.</exception>
        public void Write(string connectionString, string versionProperty)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            using (var context = new SqlContext(connectionString))
            {
                Write(context, versionProperty);
            }
        }

        /// <summary>
        ///   Writes the database schema version to the specified database context.
        /// </summary>
        /// <param name="context">The database context to read the schema version from.</param>
        /// <param name="versionProperty">
        ///   The name of the SQL Server extended property that contains the current schema version.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> or <paramref name="versionProperty"/> in <c>null</c>.
        /// </exception>
        /// <exception cref="SqlCommandException">Failed to write to the database.</exception>
        public void Write(SqlContext context, string versionProperty)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (versionProperty == null)
                throw new ArgumentNullException("versionProperty");

            context.Execute(String.Format(String.Join(Environment.NewLine,
                "IF EXISTS (SELECT 1 FROM fn_listextendedproperty('{0}', NULL, NULL, NULL, NULL, NULL, NULL))",
                "  EXEC sp_dropextendedproperty '{0}', NULL, NULL, NULL, NULL, NULL, NULL",
                "EXEC sp_addextendedproperty '{0}', '{1}', NULL, NULL, NULL, NULL, NULL, NULL"),
                versionProperty, this));
        }
    }
}