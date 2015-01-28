using System;
using System.Collections.Generic;
using System.IO;
using InoSoft.Tools.Sqlver;

namespace InoSoft.Tools.SqlMigrations.Sqlver
{
    /// <summary>
    ///   Converts a Sqlver repository to a set of migration files compatible with SQL Migrations.
    /// </summary>
    public class SqlverRepositoryMigrator : SqlverMigrator
    {
        private readonly Repository _repo;
        private readonly string _repoDir;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlverRepositoryMigrator"/> class
        ///   with a set of revision-version pairs.
        /// </summary>
        /// <param name="repositoryPath">The Sqlver repository path.</param>
        /// <param name="versions">
        ///   Pairs of Sqlver revisions and corresponding database database schema.
        ///   If contains only some of the revisions, other revisions will be derived from them.
        ///   Leave <c>null</c> to use the default version numbers.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="repositoryPath"/> in <c>null</c>.</exception>
        /// <exception cref="FileNotFoundException">Sqlver repository not found.</exception>
        /// <exception cref="IOException">
        ///   Failed to locate Sqlver repository.
        ///   <br/>or<br/>
        ///   Failed to load Sqlver repository.
        /// </exception>
        public SqlverRepositoryMigrator(string repositoryPath, IEnumerable<KeyValuePair<int, Version>> versions)
            : base(versions)
        {
            if (repositoryPath == null)
                throw new ArgumentNullException("repositoryPath");

            // Initialize repository.
            _repoDir = InitializeRepoDir(repositoryPath);
            _repo = InitializeRepo(repositoryPath);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlverRepositoryMigrator"/> class
        ///   with just one revision-version pair.
        /// </summary>
        /// <param name="repositoryPath">The Sqlver repository path.</param>
        /// <param name="version">The version that corresponds to the first (zero-based) Sqlver revision.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="repositoryPath"/> in <c>null</c>.
        ///   <br/>or<br/>
        ///   <paramref name="version"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="FileNotFoundException">Sqlver repository not found.</exception>
        /// <exception cref="IOException">
        ///   Failed to locate Sqlver repository.
        ///   <br/>or<br/>
        ///   Failed to load Sqlver repository.
        /// </exception>
        public SqlverRepositoryMigrator(string repositoryPath, Version version)
            : base(version)
        {
            if (repositoryPath == null)
                throw new ArgumentNullException("repositoryPath");

            // Initialize repository.
            _repoDir = InitializeRepoDir(repositoryPath);
            _repo = InitializeRepo(repositoryPath);
        }

        /// <summary>
        ///   Converts the Sqlver repository to a set of migration files compatible with SQL Migrations.
        ///   All existing files in the output directory are deleted.
        /// </summary>
        /// <param name="outputDir">The output directory to put the migrations into.</param>
        /// <exception cref="DirectoryNotFoundException">Output directory not found.</exception>
        /// <exception cref="IOException">
        ///   Failed delete a file in the output directory.
        ///   <br/>or<br/>
        ///   Failed to copy a migration file.
        /// </exception>
        public void Convert(string outputDir)
        {
            if (!Directory.Exists(outputDir))
                throw new DirectoryNotFoundException(String.Format("Output directory not found: {0}", outputDir));

            foreach (string file in Directory.GetFiles(outputDir))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    throw new IOException(String.Format("Failed delete a file in the output directory: {0}", file), ex);
                }
            }

            foreach (Migration migration in GetMigrations(_repo.Versions))
            {
                try
                {
                    File.Copy(Path.Combine(_repoDir, migration.Filename), Path.Combine(outputDir, migration.ToString()));
                }
                catch (Exception ex)
                {
                    throw new IOException("Failed to copy a migration file.", ex);
                }
            }
        }

        /// <summary>
        ///   Loads the Sqlver repository.
        /// </summary>
        /// <param name="repositoryPath">The Sqlver repository path.</param>
        /// <returns>
        ///   The Sqlver repository.
        /// </returns>
        /// <exception cref="IOException">Failed to load Sqlver repository.</exception>
        private static Repository InitializeRepo(string repositoryPath)
        {
            try
            {
                return XmlHelper.FromXml<Repository>(repositoryPath);
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to load Sqlver repository.", ex);
            }
        }

        /// <summary>
        ///   Gets the Sqlver repository root directory.
        /// </summary>
        /// <param name="repositoryPath">The Sqlver repository path.</param>
        /// <returns>
        ///   The Sqlver repository root directory.
        /// </returns>
        /// <exception cref="FileNotFoundException">Sqlver repository not found.</exception>
        /// <exception cref="IOException">Failed to locate Sqlver repository.</exception>
        private static string InitializeRepoDir(string repositoryPath)
        {
            try
            {
                var repoFileInfo = new FileInfo(repositoryPath);
                if (!repoFileInfo.Exists)
                    throw new FileNotFoundException(String.Format("Sqlver repository not found: {0}", repositoryPath));

                return repoFileInfo.DirectoryName;
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to locate Sqlver repository.", ex);
            }
        }
    }
}