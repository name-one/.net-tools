using System;
using System.IO;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   Contains migration settings used by <see cref="DbMigrationRunner"/>.
    /// </summary>
    public class DbMigrationSettings
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="DbMigrationSettings"/> class.
        /// </summary>
        public DbMigrationSettings()
        {
            MigrationsDir = "Migrations";
            VersionProperty = "Database Schema Version";
        }

        /// <summary>
        ///   Gets or sets the connection string to the database that is to undergo migrations.
        /// </summary>
        /// <value>
        ///   The connection string to the database that is to undergo migrations.
        /// </value>
        public string ConnectionString { get; set; }

        /// <summary>
        ///   Gets or sets the name of the directory that contains migration scripts.
        /// </summary>
        /// <value>
        ///   The name of the directory that contains migration scripts.
        ///   Note that this is a name, not a path.
        ///   <br/>
        ///   The default value is <c>"Migrations"</c>.
        /// </value>
        public string MigrationsDir { get; set; }

        /// <summary>
        ///   Gets or sets the absolute path to the SQL project file to use for migrations.
        /// </summary>
        /// <value>
        ///   The absolute path to the SQL project file to use for migrations.
        /// </value>
        public string ProjectPath { get; set; }

        /// <summary>
        ///   Gets or sets the name of the SQL Server extended property that contains the current schema version.
        /// </summary>
        /// <value>
        ///   The name of the SQL Server extended property that contains the current schema version.
        ///   <br/>
        ///   The default value is <c>"Database Schema Version"</c>.
        /// </value>
        public string VersionProperty { get; set; }

        /// <summary>
        ///   Reads settings from the specified file.
        /// </summary>
        /// <param name="path">The path to the file to read settings from.</param>
        /// <returns>
        ///   Settings read from the specified file.
        /// </returns>
        /// <exception cref="FileNotFoundException">Migration settings file not found.</exception>
        /// <exception cref="IOException">Failed to load migration settings.</exception>
        public static DbMigrationSettings FromFile(string path)
        {
            try
            {
                var s = XmlHelper.FromXml<DbMigrationSettings>(path);

                if (s.ConnectionString == null || s.ProjectPath == null)
                    throw new InvalidDataException(String.Format("{0} is not a valid migration settings file.", path));

                return s;
            }
            catch (FileNotFoundException ex)
            {
                throw new FileNotFoundException(String.Format("Migration settings file not found: {0}", path), path, ex);
            }
            catch (Exception ex)
            {
                throw new IOException(String.Format("Failed to load migration settings: {0}", path), ex);
            }
        }

        /// <summary>
        ///   Saves current settings to the specified file.
        /// </summary>
        /// <param name="path">The path to the file to save settings to.</param>
        /// <exception cref="IOException">Failed to save migration settings.</exception>
        public void Save(string path)
        {
            try
            {
                XmlHelper.ToXml(this, path);
            }
            catch (Exception ex)
            {
                throw new IOException(String.Format("Failed to save migration settings: {0}", path), ex);
            }
        }
    }
}