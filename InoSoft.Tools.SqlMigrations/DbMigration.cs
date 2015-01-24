using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using InoSoft.Tools.Data;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   Migrates a database schema from one version to another.
    /// </summary>
    public class DbMigration
    {
        private readonly string _body;
        private readonly DbVersion _from;
        private readonly DbVersion _to;

        /// <summary>
        ///   Initializes a new instance of the <see cref="DbMigration"/> class.
        /// </summary>
        /// <param name="from">The database schema version before the migration is performed.</param>
        /// <param name="to">The database schema version after the migration is performed.</param>
        /// <param name="body">The body of the migration SQL script.</param>
        private DbMigration(DbVersion from, DbVersion to, string body)
        {
            if (from == null)
                throw new ArgumentNullException("from");
            if (to == null)
                throw new ArgumentNullException("to");
            if (body == null)
                throw new ArgumentNullException("body");

            _from = from;
            _to = to;
            _body = body;
        }

        /// <summary>
        ///   Gets the body of the migration SQL script.
        /// </summary>
        /// <value>
        ///   The body of the migration SQL script.
        /// </value>
        public string Body
        {
            get { return _body; }
        }

        /// <summary>
        ///   Gets the database schema version before the migration is performed.
        /// </summary>
        /// <value>
        ///   The database schema version before the migration is performed.
        /// </value>
        public DbVersion From
        {
            get { return _from; }
        }

        /// <summary>
        ///   Gets the database schema version after the migration is performed.
        /// </summary>
        /// <value>
        ///   The database schema version after the migration is performed.
        /// </value>
        public DbVersion To
        {
            get { return _to; }
        }

        /// <summary>
        ///   Reads a migration from the specified file.
        /// </summary>
        /// <param name="file">The file to read a migration from.</param>
        /// <returns>
        ///   A migration loaded from the file.
        /// </returns>
        /// <exception cref="FormatException">
        ///   Incorrect migration file name format.
        ///   <br/>or<br/>
        ///   Schema version is not in a valid format.
        /// </exception>
        /// <exception cref="IOException">Failed to read a migration from a file.</exception>
        public static DbMigration Read(FileInfo file)
        {
            string[] parts = Path.GetFileNameWithoutExtension(file.Name).Split('-');
            if (parts.Length != 2)
                throw new FormatException("Incorrect migration file name format.");

            string body;
            try
            {
                using (StreamReader reader = file.OpenText())
                {
                    body = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new IOException(String.Format("Failed to read a migration from a file: {0}", file.FullName), ex);
            }

            return new DbMigration(
                DbVersion.Parse(parts[0].Substring(1)),
                DbVersion.Parse(parts[1].Substring(1)),
                body);
        }

        /// <summary>
        ///   Runs this migration against the specified database context.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
        /// <exception cref="DbUpdateCommandException">An error occurred while executing a migration command.</exception>
        public void Run(SqlContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            // Parse the migration body into separate SQL commands.
            var commands = new List<string>();
            var sb = new StringBuilder();
            using (TextReader reader = new StringReader(Body))
            {
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    if (line.Trim().ToUpper() != "GO")
                    {
                        sb.AppendLine(line);
                    }
                    else
                    {
                        commands.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                commands.Add(sb.ToString());
            }

            // Execute all non-empty commands.
            foreach (string command in commands)
            {
                if (String.IsNullOrWhiteSpace(command))
                    continue;

                try
                {
                    context.Execute(command);
                }
                catch (Exception ex)
                {
                    throw new DbUpdateCommandException("An error occurred while executing a migration command.",
                        command, ex);
                }
            }
        }

        /// <summary>
        ///   Returns a <see cref="String"/> that describes this database schema migration.
        /// </summary>
        /// <returns>
        ///   A <see cref="String"/> that describes this database schema migration.
        /// </returns>
        public override string ToString()
        {
            return String.Join(" - ", From, To);
        }
    }
}