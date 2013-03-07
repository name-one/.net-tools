using System;
using System.IO;

namespace InoSoft.Tools.Sqlver
{
    public static class Helper
    {
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

        public static bool Checkout(string copy, string repo, string connection)
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
                    ConnectionString = connection
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