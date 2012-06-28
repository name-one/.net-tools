using System;
using System.IO;
using System.Linq;
using InoSoft.Tools.Data;

namespace InoSoft.Tools.SqlVersion
{
    public class WorkingCopy
    {
        public int CurrentVersion { get; set; }

        public string ConnectionString { get; set; }

        public string RepositoryPath { get; set; }

        public static WorkingCopy FromFile(string path)
        {
            try
            {
                return XmlHelper.LoadObjectFromFile<WorkingCopy>(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public bool Save(string path)
        {
            try
            {
                XmlHelper.SaveObjectToFile<WorkingCopy>(this, path);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public bool Update(int version = -1)
        {
            SqlContext context = new SqlContext(ConnectionString);
            Repository repository = Repository.FromFile(RepositoryPath);
            if (repository != null)
            {
                Console.WriteLine("Repository opened successfully.");
                if (version == -1)
                {
                    version = repository.LastVersion;
                }
                if (version == this.CurrentVersion)
                {
                    Console.WriteLine("Already up-to-date!");
                }
                else if (version < 0 || version < CurrentVersion || version > repository.LastVersion)
                {
                    Console.WriteLine("Version {0} is incorrect, only from {1} to {2} are acceptable!!!", version, CurrentVersion, repository.LastVersion);
                    return false;
                }
                else
                {
                    for (int index = this.CurrentVersion + 1; index <= version; ++index)
                    {
                        try
                        {
                            Increment(repository.Versions[index], context);
                            CurrentVersion++;
                            Console.WriteLine("Updating to version {0} \t success", index);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Updating to version {0} \t fail!!!", index);
                            Console.WriteLine(ex.Message);
                            return false;
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private void Increment(string versionSql, SqlContext context)
        {
            string[] queries;
            using (var file = File.OpenText(Path.Combine(Path.GetDirectoryName(RepositoryPath), versionSql)))
            {
                queries = file.ReadToEnd().Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(q => q.Trim()).Where(q => q != "").ToArray();
            }

            foreach (var query in queries)
            {
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