using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using InoSoft.Tools.Data;

namespace InoSoft.Tools.Sqlver
{
    /// <summary>
    /// Serializable working copy, which indicates current version and connection parameters.
    /// </summary>
    public class WorkingCopy
    {
        /// <summary>
        /// Connection string to the database.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Current version within repository.
        /// </summary>
        public int CurrentVersion { get; set; }

        /// <summary>
        /// Path to the repository file.
        /// </summary>
        public string RepositoryPath { get; set; }

        /// <summary>
        /// Indicates whether to use Unicode when update the working copy.
        /// </summary>
        public bool Unicode { get; set; }

        /// <summary>
        /// Loads working copy from XML file.
        /// </summary>
        /// <param name="path">Path to XML file.</param>
        /// <returns>Loaded working copy or null depending on operation success.</returns>
        public static WorkingCopy FromFile(string path)
        {
            try
            {
                return XmlHelper.FromXml<WorkingCopy>(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Saves working copy to an XML file.
        /// </summary>
        /// <param name="path">Path to the XML file.</param>
        /// <returns>Value, indicating save success.</returns>
        public bool Save(string path)
        {
            try
            {
                XmlHelper.ToXml(this, path);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Updates working copy.
        /// </summary>
        /// <param name="version">
        /// Version within repository to which we want to update.
        /// Minus one stands for the latest.
        /// </param>
        /// <param name="commandTimeout">Command timeout in seconds.</param>
        /// <returns>True if successful.</returns>
        public bool Update(int version = -1, int commandTimeout = 30)
        {
            Repository repository = Repository.FromFile(RepositoryPath);
            if (repository == null)
                return false;

            Console.WriteLine("Repository opened successfully.");
            if (version == -1)
            {
                version = repository.Versions.Count - 1;
            }
            if (version == CurrentVersion)
            {
                Console.WriteLine("Already up-to-date!");
                return true;
            }
            if (version < 0 || version < CurrentVersion || version >= repository.Versions.Count)
            {
                Console.WriteLine("Version {0} is incorrect, only from {1} to {2} are acceptable!!!",
                    version, CurrentVersion, repository.Versions.Count - 1);
                return false;
            }

            using (var context = new SqlContext(ConnectionString, commandTimeout, true))
            {
                for (int index = CurrentVersion + 1; index <= version; ++index)
                {
                    try
                    {
                        Increment(repository.Versions[index], context);
                        CurrentVersion++;
                        Console.WriteLine("Update to version {0} \t has succeeded.", index);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Update to version {0} \t has failed!!!", index);
                        Console.WriteLine(ex.Message);
                        return false;
                    }
                }

                return true;
            }
        }

        private void Increment(string versionSql, SqlContext context)
        {
            var queries = new List<string>();
            using (var file = new StreamReader(Path.Combine(Path.GetDirectoryName(RepositoryPath), versionSql), Unicode ? Encoding.Unicode : Encoding.Default))
            {
                var sb = new StringBuilder();
                for (var line = file.ReadLine(); line != null; line = file.ReadLine())
                {
                    if (line.Trim().ToUpper() != "GO")
                    {
                        sb.AppendLine(line);
                    }
                    else
                    {
                        queries.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                queries.Add(sb.ToString());
            }

            foreach (var query in queries)
            {
                if (query.Trim() == String.Empty)
                    continue;

                try
                {
                    context.Execute(query);
                }
                catch
                {
                    Console.WriteLine("Error executing query:");
                    Console.WriteLine(query);
                    throw;
                }
            }
        }
    }
}