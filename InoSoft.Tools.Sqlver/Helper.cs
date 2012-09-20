using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InoSoft.Tools.Sqlver
{
    public static class Helper
    {
        public static bool Init(string repo, string sql)
        {
            Repository repository = new Repository();
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
            else
            {
                Console.WriteLine("Repository load failed!!!");
                return false;
            }
        }

        public static bool Checkout(string copy, string repo, string connection)
        {
            if (new WorkingCopy()
            {
                CurrentVersion = -1,
                RepositoryPath = repo,
                ConnectionString = connection
            }.Save(copy))
            {
                return true;
            }
            Console.WriteLine("Working copy save failed!!!");
            return false;
        }

        public static bool Update(string copy, int version = -1)
        {
            WorkingCopy workingCopy = WorkingCopy.FromFile(copy);
            if (workingCopy != null)
            {
                if (!workingCopy.Update(version))
                {
                    Console.WriteLine("Update failed!!!");
                    return false;
                }
                else
                {
                    if (workingCopy.Save(copy))
                    {
                        return true;
                    }
                    Console.WriteLine("Working copy save failed!!!");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Working copy load failed!!!");
                return false;
            }
        }
    }
}
