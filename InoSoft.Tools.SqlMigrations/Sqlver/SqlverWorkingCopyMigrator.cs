using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InoSoft.Tools.Data;
using InoSoft.Tools.Sqlver;

namespace InoSoft.Tools.SqlMigrations.Sqlver
{
    /// <summary>
    ///   Converts a Sqlver working copy to a SQL Migrations-managed database.
    /// </summary>
    public class SqlverWorkingCopyMigrator : SqlverMigrator
    {
        private readonly WorkingCopy _workingCopy;
        private readonly string _workingCopyPath;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlverWorkingCopyMigrator"/> class
        ///   with a set of revision-version pairs.
        /// </summary>
        /// <param name="workingCopyPath">The Sqlver working copy path.</param>
        /// <param name="versions">
        ///   Pairs of Sqlver revisions and corresponding database database schema.
        ///   If contains only some of the revisions, other revisions will be derived from them.
        ///   Leave <c>null</c> to use the default version numbers.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="workingCopyPath"/> in <c>null</c>.</exception>
        /// <exception cref="IOException">Failed to load Sqlver working copy.</exception>
        public SqlverWorkingCopyMigrator(string workingCopyPath, IEnumerable<KeyValuePair<int, Version>> versions)
            : base(versions)
        {
            if (workingCopyPath == null)
                throw new ArgumentNullException("workingCopyPath");

            _workingCopyPath = workingCopyPath;
            _workingCopy = InitializeWorkingCopy(workingCopyPath);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlverWorkingCopyMigrator"/> class
        ///   with just one revision-version pair.
        /// </summary>
        /// <param name="workingCopyPath">The Sqlver working copy path.</param>
        /// <param name="version">The version that corresponds to the first (zero-based) Sqlver revision.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="workingCopyPath"/> in <c>null</c>.
        ///   <br/>or<br/>
        ///   <paramref name="version"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="IOException">Failed to load Sqlver working copy.</exception>
        public SqlverWorkingCopyMigrator(string workingCopyPath, Version version)
            : base(version)
        {
            if (workingCopyPath == null)
                throw new ArgumentNullException("workingCopyPath");

            _workingCopyPath = workingCopyPath;
            _workingCopy = InitializeWorkingCopy(workingCopyPath);
        }

        /// <summary>
        ///   Loads a Sqlver working copy.
        /// </summary>
        /// <param name="workingCopyPath">The Sqlver working copy path.</param>
        /// <returns>
        ///   A Sqlver working copy.
        /// </returns>
        /// <exception cref="IOException">Failed to load Sqlver working copy.</exception>
        private static WorkingCopy InitializeWorkingCopy(string workingCopyPath)
        {
            try
            {
                return XmlHelper.FromXml<WorkingCopy>(workingCopyPath);
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to load Sqlver working copy.", ex);
            }
        }

        /// <summary>
        ///   Converts the Sqlver working copy to a SQL Migrations-managed database.
        ///   Overwrites the working copy with migration settings file.
        /// </summary>
        /// <param name="projectPath">The path to the SQL project file that contains the migrations.</param>
        /// <param name="versionProperty">
        ///   The name of the SQL Server extended property that contains the current schema version.
        ///   Leave <c>null</c> to use the default value.
        /// </param>
        /// <exception cref="SqlCommandException">Failed to write to the database.</exception>
        /// <exception cref="IOException">Failed to save migration settings.</exception>
        /// <returns>
        ///   Migration settings converted from the Sqlver working copy.
        /// </returns>
        public DbMigrationSettings Convert(string projectPath, string versionProperty = null)
        {
            var settings = new DbMigrationSettings
            {
                ProjectPath = projectPath,
                ConnectionString = _workingCopy.ConnectionString,
            };
            if (versionProperty != null)
            {
                settings.VersionProperty = versionProperty;
            }

            Migration current = GetMigrations(new string[_workingCopy.CurrentVersion + 1])
                .ElementAt(_workingCopy.CurrentVersion);

            new DbVersion(current.To).Write(_workingCopy.ConnectionString, settings.VersionProperty);

            settings.Save(_workingCopyPath);

            return settings;
        }
    }
}