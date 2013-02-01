using System;
using System.IO;

namespace InoSoft.Tools.Sqlver
{
    /// <summary>
    /// Contains methods, which perform all issqlver operations.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Initializes a repository.
        /// </summary>
        /// <param name="repo">Path to the repository file.</param>
        /// <param name="sql">Path to the initial SQL script.</param>
        /// <returns>True if successful.</returns>
        public static bool Init(string repo, string sql)
        {
            var repository = new Repository();
            repository.AddVersion(sql);
            if (repository.Save(repo))
            {
                return true;
            }
            Console.WriteLine("Repository save failed!!!");
            return false;
        }

        /// <summary>
        /// Commits new version to a repositoty.
        /// </summary>
        /// <param name="repo">Path to the repository file.</param>
        /// <param name="sql">Path to the new version SQL file.</param>
        /// <returns>True if successful.</returns>
        public static bool Commit(string repo, string sql)
        {
            Repository repository = Repository.FromFile(repo);
            if (repository != null)
            {
                repository.AddVersion(sql);
                if (repository.Save(repo))
                {
                    return true;
                }
                Console.WriteLine("Repository save failed!!!");
                return false;
            }
            Console.WriteLine("Repository load failed!!!");
            return false;
        }

        /// <summary>
        /// Creates new working copy and binds it with repository.
        /// </summary>
        /// <param name="copy">Path to the working copy file.</param>
        /// <param name="repo">Path to the repository.</param>
        /// <param name="connection">SQL server connection string.</param>
        /// <returns>True if successful.</returns>
        public static bool Checkout(string copy, string repo, string connection, bool unicode = false)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(copy)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(copy));
                }
                if (new WorkingCopy
                {
                    CurrentVersion = -1,
                    RepositoryPath = repo,
                    ConnectionString = connection,
                    Unicode = unicode,
                }.Save(copy))
                {
                    return true;
                }
            }
            catch
            {
            }
            Console.WriteLine("Working copy save failed!!!");
            return false;
        }

        /// <summary>
        /// Updates existing working copy.
        /// </summary>
        /// <param name="copy">Path to the working copy file.</param>
        /// <param name="version">Version to which we want to update. Minus one stands for the latest.</param>
        /// <param name="commandTimeout">Command timeout in seconds.</param>
        /// <returns>True if successful.</returns>
        public static bool Update(string copy, int version = -1, int commandTimeout = 30)
        {
            WorkingCopy workingCopy = WorkingCopy.FromFile(copy);
            if (workingCopy != null)
            {
                if (!workingCopy.Update(version, commandTimeout))
                {
                    Console.WriteLine("Update failed!!!");
                    return false;
                }
                if (workingCopy.Save(copy))
                {
                    return true;
                }
                Console.WriteLine("Working copy save failed!!!");
                return false;
            }
            Console.WriteLine("Working copy load failed!!!");
            return false;
        }
    }
}