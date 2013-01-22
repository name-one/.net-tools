﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using InoSoft.Tools.Data;

namespace InoSoft.Tools.Sqlver
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
                return XmlHelper.FromXml<WorkingCopy>(path);
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
                XmlHelper.ToXml(this, path);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public bool Update(int version = -1, int commandTimeout = 30)
        {
            var context = new SqlContext(ConnectionString, commandTimeout, true);
            Repository repository = Repository.FromFile(RepositoryPath);
            if (repository == null)
                return false;

            Console.WriteLine("Repository opened successfully.");
            if (version == -1)
            {
                version = repository.LastVersion;
            }
            if (version == CurrentVersion)
            {
                Console.WriteLine("Already up-to-date!");
            }
            else if (version < 0 || version < CurrentVersion || version > repository.LastVersion)
            {
                Console.WriteLine("Version {0} is incorrect, only from {1} to {2} are acceptable!!!",
                    version, CurrentVersion, repository.LastVersion);
                return false;
            }
            else
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
            }

            return true;
        }

        private void Increment(string versionSql, SqlContext context)
        {
            var queries = new List<string>();
            using (var file = File.OpenText(Path.Combine(Path.GetDirectoryName(RepositoryPath), versionSql)))
            {
                var sb = new StringBuilder();
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine();
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
                if (query.Trim() == "")
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