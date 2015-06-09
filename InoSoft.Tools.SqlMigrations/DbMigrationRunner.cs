using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InoSoft.Tools.Data;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   Runs database migrations based on a SQL project and migration scripts.
    /// </summary>
    public class DbMigrationRunner
    {
        private readonly DbMigrationSettings _settings;

        /// <summary>
        ///   Initializes a new instance of the <see cref="DbMigrationRunner"/> class.
        /// </summary>
        /// <param name="settings">The migration settings.</param>
        public DbMigrationRunner(DbMigrationSettings settings)
        {
            _settings = settings;
            OutputLog = new StringWriter();
        }

        /// <summary>
        ///   Gets or sets the log to use as an output of this instance.
        /// </summary>
        /// <value>
        ///   The log to use as an output of this instance.
        /// </value>
        public TextWriter OutputLog { get; set; }

        /// <summary>
        ///   Gets the migration settings.
        /// </summary>
        /// <value>
        ///   The migration settings.
        /// </value>
        public DbMigrationSettings Settings
        {
            get { return _settings; }
        }

        /// <summary>
        ///   Updates the database according to the project specified in <see cref="Settings"/>.
        /// </summary>
        /// <param name="commandTimeout">The timeout of a single SQL command, in seconds.</param>
        /// <exception cref="InvalidOperationException">
        ///   <see cref="Settings"/>.<see cref="DbMigrationSettings.ProjectPath"/> is not an absolute path.
        ///   <br/>or<br/>
        ///   The previous migration was not completed.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        ///   Project directory not found.
        ///   <br/>or<br/>
        ///   Migrations directory not found.
        /// </exception>
        /// <exception cref="IOException">Failed to read the migrations.</exception>
        /// <exception cref="FileNotFoundException">SQL project not found.</exception>
        /// <exception cref="SqlCommandException">
        ///   A SQL error occurred while reading existing objects.
        ///   <br/>or<br/>
        ///   Failed to write version to the database.
        /// </exception>
        /// <exception cref="DbUpdateCommandException">An error occurred while executing a migration command.</exception>
        /// <exception cref="AggregateException">
        ///   Update of views, functions or procedures completed with errors.
        ///   Contains exceptions that occurred during the update.
        /// </exception>
        /// <exception cref="DbUpdateException">An error occurred while reading database schema version.</exception>
        public void Update(int commandTimeout = 30)
        {
            // Find the project directory.
            string projectDirPath = Path.GetDirectoryName(Settings.ProjectPath);
            if (projectDirPath == null)
                throw new InvalidOperationException("Settings.ProjectPath is not an absolute path.");
            var rootDir = new DirectoryInfo(projectDirPath);
            if (!rootDir.Exists)
                throw new DirectoryNotFoundException(String.Format("Project directory not found: {0}", projectDirPath));

            // Select the project file.
            var projectFile = new FileInfo(Settings.ProjectPath);
            if (!projectFile.Exists)
                throw new FileNotFoundException(String.Format("SQL project not found: {0}", Settings.ProjectPath));

            // Read schema migrations.
            DbMigration[] migrations = ReadMigrations(rootDir.FullName);

            // Read the object definitions from the project.
            var projectSearcher = new DbProjectSearcher(projectFile.FullName);
            DbObjectDefinition[] viewDefinitions = projectSearcher.GetViewDefinitions();
            DbRoutineDefinition[] procedureDefinitions = projectSearcher.GetProcedureDefinitions();
            DbRoutineDefinition[] functionDefinitions = projectSearcher.GetFunctionDefinitions();

            // Update the database.
            using (var context = new SqlContext(Settings.ConnectionString, commandTimeout, true))
            {
                // Run schema migrations.
                OutputLog.WriteLine();
                OutputLog.WriteLine("Updating schema...");
                RunMigrations(context, migrations);

                var exceptions = new List<Exception>();

                const string sql =
                    "SELECT SCHEMA_NAME(schema_id) AS [Schema], name AS [Name], OBJECT_DEFINITION(object_id) AS [Definition] FROM sys.objects WHERE {0}";

                // Update views.
                OutputLog.WriteLine();
                OutputLog.WriteLine("Updating views...");
                try
                {
                    DbObject[] views = context.Execute<DbObject>(String.Format(sql, "type = 'V'"));
                    ReplaceObjects(context, "VIEW", viewDefinitions,
                        views.Select(DbObjectDefinition.FromDbObject));
                }
                catch (AggregateException ex)
                {
                    exceptions.AddRange(ex.InnerExceptions);
                }

                // Update user-defined functions.
                OutputLog.WriteLine();
                OutputLog.WriteLine("Updating user-defined functions...");
                try
                {
                    DbObject[] functions = context.Execute<DbObject>(String.Format(sql, "type IN ('FN', 'IF', 'TF')"));
                    ReplaceObjects(context, "FUNCTION", functionDefinitions,
                        functions.Select(DbObjectDefinition.FromDbObject));
                }
                catch (AggregateException ex)
                {
                    exceptions.AddRange(ex.InnerExceptions);
                }

                // Update stored procedures.
                OutputLog.WriteLine();
                OutputLog.WriteLine("Updating stored procedures...");
                try
                {
                    DbObject[] procedures = context.Execute<DbObject>(String.Format(sql, "type = 'P'"));
                    ReplaceObjects(context, "PROCEDURE", procedureDefinitions,
                        procedures.Select(DbObjectDefinition.FromDbObject));
                }
                catch (AggregateException ex)
                {
                    exceptions.AddRange(ex.InnerExceptions);
                }

                if (exceptions.Count > 0)
                    throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        ///   Creates the object in the database.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="type">The type of the object to create.</param>
        /// <param name="obj">The definition of the object to create.</param>
        /// <param name="exceptions">The list of exceptions to log any occurring exceptions to.</param>
        private void CreateObject(ISqlContext context, string type, DbObjectDefinition obj,
            ICollection<Exception> exceptions)
        {
            OutputLog.Write("Creating {0}... ", obj.FullName);
            try
            {
                context.Execute(obj.Definition);
                OutputLog.WriteLine("done.");
            }
            catch (SqlCommandException ex)
            {
                exceptions.Add(new DbUpdateException(String.Format("Failed to create {0} {1}.",
                    type.ToLowerInvariant(), obj.FullName), ex.InnerException));
                OutputLog.WriteLine("error.");
                OutputLog.WriteLine(ex.Message);
            }
        }

        /// <summary>
        ///   Drops the object in the database.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="type">The type of the object to drop.</param>
        /// <param name="obj">The definition of the object to drop.</param>
        /// <param name="exceptions">The list of exceptions to log any occurring exceptions to.</param>
        private void DropObject(ISqlContext context, string type, DbObjectDefinition obj,
            ICollection<Exception> exceptions)
        {
            OutputLog.Write("Dropping {0}... ", obj.FullName);
            try
            {
                context.Execute(String.Format("DROP {0} {1}", type, obj.FullName));
                OutputLog.WriteLine("done.");
            }
            catch (SqlCommandException ex)
            {
                exceptions.Add(new DbUpdateException(String.Format("Failed to drop {0} {1}.",
                    type.ToLowerInvariant(), obj.FullName), ex.InnerException));
                OutputLog.WriteLine("error.");
                OutputLog.WriteLine(ex.Message);
            }
        }

        /// <summary>
        ///   Reads the migrations from a migrations directory of a SQL project.
        /// </summary>
        /// <param name="rootDirPath">The root directory of the SQL project to read the migrations from.</param>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException">Migrations directory not found.</exception>
        /// <exception cref="IOException">Failed to read the migrations.</exception>
        private DbMigration[] ReadMigrations(string rootDirPath)
        {
            string migrationsDirPath = Path.Combine(rootDirPath, Settings.MigrationsDir);
            var migrationsDir = new DirectoryInfo(migrationsDirPath);
            if (!migrationsDir.Exists)
                throw new DirectoryNotFoundException(String.Format("Migrations directory not found: {0}",
                    migrationsDirPath));

            try
            {
                return migrationsDir
                    .GetFiles("v*.*.*.*-v*.*.*.*.sql")
                    .Select(DbMigration.Read)
                    .ToArray();
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to read the migrations.", ex);
            }
        }

        /// <summary>
        ///   Refreshes the view in the database.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="fullName">The full name of the view to refresh.</param>
        /// <param name="exceptions">The list of exceptions to log any occurring exceptions to.</param>
        private void RefreshView(ISqlContext context, string fullName, ICollection<Exception> exceptions)
        {
            OutputLog.Write("Refreshing {0}... ", fullName);
            try
            {
                context.Execute(String.Format("EXEC sp_refreshview '{0}'", fullName));
                OutputLog.WriteLine("done.");
            }
            catch (SqlCommandException ex)
            {
                exceptions.Add(new DbUpdateException(String.Format("Failed to refresh view {0}.",
                    fullName), ex.InnerException));
                OutputLog.WriteLine("error.");
                OutputLog.WriteLine(ex.Message);
            }
        }

        /// <summary>
        ///   Replaces the existing database objects with new ones.
        /// </summary>
        /// <param name="context">The database context to replace objects in.</param>
        /// <param name="type">The type of the objects.</param>
        /// <param name="newObjects">The new object definitions.</param>
        /// <param name="oldObjects">The old object definitions.</param>
        /// <exception cref="AggregateException">
        ///   Update completed with errors. Contains exceptions that occurred during the update.
        /// </exception>
        private void ReplaceObjects(ISqlContext context, string type,
            IEnumerable<DbObjectDefinition> newObjects, IEnumerable<DbObjectDefinition> oldObjects)
        {
            var newDefs = new HashSet<DbObjectDefinition>(newObjects);
            var oldDefs = new HashSet<DbObjectDefinition>(oldObjects);
            var sameDefs = HashSetHelper.SplitSets(newDefs, oldDefs);
            var addedDefs = new HashSet<DbObjectDefinition>(newDefs, DbObjectDefinition.FullNameComparer);
            var removedDefs = new HashSet<DbObjectDefinition>(oldDefs, DbObjectDefinition.FullNameComparer);
            var modifiedDefs = HashSetHelper.SplitSets(addedDefs, removedDefs);
            var exceptions = new List<Exception>();

            foreach (DbObjectDefinition definition in removedDefs.OrderBy(d => d.FullName))
            {
                // Drop the deleted objects.
                DropObject(context, type, definition, exceptions);
            }
            foreach (DbObjectDefinition definition in modifiedDefs.OrderBy(d => d.FullName))
            {
                // Drop and recreate the modified objects.
                DropObject(context, type, definition, exceptions);
                CreateObject(context, type, definition, exceptions);
            }
            foreach (DbObjectDefinition definition in addedDefs.OrderBy(d => d.FullName))
            {
                // Create the added objects.
                CreateObject(context, type, definition, exceptions);
            }

            if (type == "VIEW")
            {
                foreach (DbObjectDefinition definition in sameDefs.OrderBy(d => d.FullName))
                {
                    // Refresh the views that were not modified.
                    RefreshView(context, definition.FullName, exceptions);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException("Update completed with errors.", exceptions);
        }

        /// <summary>
        ///   Runs migrations against a database context.
        /// </summary>
        /// <param name="context">The database context to run migrations against.</param>
        /// <param name="migrations">The migrations to run.</param>
        /// <exception cref="InvalidOperationException">The previous migration was not completed.</exception>
        /// <exception cref="DbUpdateException">An error occurred while reading database schema version.</exception>
        /// <exception cref="DbUpdateCommandException">An error occurred while executing a migration command.</exception>
        /// <exception cref="SqlCommandException">Failed to write version to the database.</exception>
        private void RunMigrations(SqlContext context, DbMigration[] migrations)
        {
            // Read the current schema version from the database.
            DbVersion currentVersion;
            try
            {
                currentVersion = DbVersion.Read(context, Settings.VersionProperty);
            }
            catch (DbVersionMissingException ex)
            {
                OutputLog.WriteLine(ex.Message);
                currentVersion = new DbVersion(new Version(0, 0, 0, 0));
                currentVersion.Write(context, Settings.VersionProperty);
                OutputLog.WriteLine("Initialized database schema version to v{0}.", currentVersion);
            }
            catch (Exception ex)
            {
                throw new DbUpdateException("An error occurred while reading database schema version.", ex);
            }
            OutputLog.WriteLine("The current version is v{0}.", currentVersion);

            for (; ; )
            {
                if (currentVersion.Comment != null && currentVersion.Comment.StartsWith("updating-to-v"))
                    throw new InvalidOperationException("The previous migration was not completed.");

                // Find the next migration.
                DbMigration migration = migrations
                    .Where(m => m.From.Equals(currentVersion))
                    .OrderByDescending(m => m.To)
                    .FirstOrDefault();

                // No more migrations found.
                if (migration == null)
                {
                    OutputLog.WriteLine("No migrations found from v{0}.", currentVersion);
                    return;
                }

                OutputLog.Write("Migrating from v{0} to v{1}... ", migration.From, migration.To);

                // Update the current schema version to a temporary value.
                currentVersion = new DbVersion(currentVersion.Version, "updating-to-v" + migration.To);
                currentVersion.Write(context, Settings.VersionProperty);

                // Run the migration.
                migration.Run(context);

                // Update the current schema version to the new one.
                currentVersion = migration.To;
                currentVersion.Write(context, Settings.VersionProperty);

                OutputLog.WriteLine("done.");
            }
        }
    }
}