using System;
using System.Collections.Generic;
using System.Linq;

namespace InoSoft.Tools.SqlMigrations.Sqlver
{
    /// <summary>
    ///   Converts a Sqlver repository to a set of migration files compatible with SQL Migrations.
    /// </summary>
    public abstract class SqlverMigrator
    {
        protected static readonly Version DefaultVersion = new Version(1, 0, 0);

        private readonly SortedDictionary<int, Version> _versions;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlverMigrator"/> class with a set of revision-version pairs.
        /// </summary>
        /// <param name="versions">
        ///   Pairs of Sqlver revisions and corresponding database schema versions.
        ///   If contains only some of the revisions, other revisions will be derived from them.
        ///   Leave <c>null</c> to use the default version numbers.
        /// </param>
        protected SqlverMigrator(IEnumerable<KeyValuePair<int, Version>> versions)
        {
            // Initialize versions.
            _versions = new SortedDictionary<int, Version>();
            foreach (KeyValuePair<int, Version> version in versions ?? Enumerable.Empty<KeyValuePair<int, Version>>())
            {
                _versions.Add(version.Key, version.Value);
            }
            if (!_versions.ContainsKey(0))
            {
                _versions.Add(0, DefaultVersion);
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlverMigrator"/> class with just one revision-version pair.
        /// </summary>
        /// <param name="version">The version that corresponds to the first (zero-based) Sqlver revision.</param>
        /// <exception cref="ArgumentNullException"><paramref name="version"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Version contains a revision number.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Version is less than <c>0.0.1</c>.</exception>
        protected SqlverMigrator(Version version)
            : this(CreateVersions(version))
        {
        }

        /// <summary>
        ///   Creates a set of versions from the specified database schema version.
        /// </summary>
        /// <param name="version">The version that corresponds to the first (zero-based) Sqlver revision.</param>
        /// <exception cref="ArgumentNullException"><paramref name="version"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Version contains a revision number.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Version is less than <c>0.0.1</c>.</exception>
        /// <returns>
        ///   A enumerable consisting of a pair of zeroth revision and the specified database schema version.
        /// </returns>
        private static IEnumerable<KeyValuePair<int, Version>> CreateVersions(Version version)
        {
            if (version == null)
                throw new ArgumentNullException("version");
            if (version.Revision != -1)
                throw new ArgumentException("Version must not contain a revision number.", "version");
            if (version < new Version(0, 0, 1))
                throw new ArgumentOutOfRangeException("version", "Version must be at least 0.0.1.");

            return new[] { new KeyValuePair<int, Version>(0, version) };
        }

        /// <summary>
        ///   Gets all migrations corresponding to the Sqlver revision files.
        /// </summary>
        /// <param name="filenames">The filenames of the Sqlver migrations.</param>
        /// <returns>
        ///   All migrations corresponding to the Sqlver revision files.
        /// </returns>
        protected IEnumerable<Migration> GetMigrations(IList<string> filenames)
        {
            var current = new Version(0, 0, 0, 0);
            for (int i = 0; i < filenames.Count; i++)
            {
                Version version;
                Version next = _versions.TryGetValue(i, out version)
                    ? new Version(version.Major, version.Minor, Math.Max(version.Build, 0), i)
                    : new Version(current.Major, current.Minor, current.Build, i);

                yield return new Migration(current, next, filenames[i]);

                current = next;
            }
        }

        /// <summary>
        ///   Encapsulates a single migration from a Sqlver repository.
        /// </summary>
        protected class Migration
        {
            private readonly string _filename;
            private readonly Version _from;
            private readonly Version _to;

            /// <summary>
            ///   Initializes a new instance of the <see cref="Migration"/> class.
            /// </summary>
            /// <param name="from">The database schema version before this migration.</param>
            /// <param name="to">The database schema version after this migration.</param>
            /// <param name="filename">The filename of the Sqlver script.</param>
            public Migration(Version from, Version to, string filename)
            {
                _from = from;
                _to = to;
                _filename = filename;
            }

            /// <summary>
            ///   Gets the filename of the Sqlver script.
            /// </summary>
            /// <value>
            ///   The filename of the Sqlver script.
            /// </value>
            public string Filename { get { return _filename; } }

            /// <summary>
            ///   Gets the database schema version before this migration.
            /// </summary>
            /// <value>
            ///   The database schema version before this migration.
            /// </value>
            public Version From { get { return _from; } }

            /// <summary>
            ///   Gets the database schema version before this migration.
            /// </summary>
            /// <value>
            ///   The database schema version before this migration.
            /// </value>
            public Version To { get { return _to; } }

            /// <summary>
            ///   Returns a SQL Migrations compatible filename of this migration.
            /// </summary>
            /// <returns>
            ///   A SQL Migrations compatible filename of this migration.
            /// </returns>
            public override string ToString()
            {
                return String.Format("v{0}-v{1}.sql", From.ToString(4), To.ToString(4));
            }
        }
    }
}