using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace InoSoft.Tools.SqlVersion
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                for (int index = 1; index < args.Length; ++index)
                {
                    int length = args[index].IndexOf('=');
                    if (length >= 1)
                    {
                        if (length != args[index].Length - 1)
                        {
                            try
                            {
                                dictionary.Add(args[index].Substring(0, length).ToLower(), args[index].Substring(length + 1));
                                continue;
                            }
                            catch
                            {
                                Console.WriteLine("Duplicated keys in parameters!!!");
                                return 1;
                            }
                        }
                    }
                    Console.WriteLine("Parameters are not presented correctly!!!");
                    return 1;
                }
                switch (args[0].ToLower())
                {
                    case "init":
                        if (dictionary.ContainsKey("repo") && dictionary.ContainsKey("sql"))
                        {
                            Repository repository = new Repository();
                            repository.AddVersion(dictionary["sql"], true, true);
                            if (repository.Save(dictionary["repo"]))
                            {
                                return 0;
                            }
                            Console.WriteLine("Repository save failed!!!");
                            return 1;
                        }
                        else
                        {
                            break;
                        }
                    case "commit":
                        if (dictionary.ContainsKey("repo") && dictionary.ContainsKey("sql") && dictionary.ContainsKey("content"))
                        {
                            Repository repository = Repository.FromFile(dictionary["repo"]);
                            if (repository != null)
                            {
                                bool hasSchema = dictionary["content"].ToLower().Contains("s");
                                bool hasData = dictionary["content"].ToLower().Contains("d");
                                if (!hasSchema && !hasData)
                                {
                                    Console.WriteLine("Versions with no data and no schema are not allowed!!!");
                                    return 1;
                                }
                                else
                                {
                                    repository.AddVersion(dictionary["sql"], hasSchema, hasData);
                                    if (repository.Save(dictionary["repo"]))
                                    {
                                        return 0;
                                    }
                                    Console.WriteLine("Repository save failed!!!");
                                    return 1;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Repository load failed!!!");
                                return 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    case "checkout":
                        if (dictionary.ContainsKey("copy") && dictionary.ContainsKey("repo") && dictionary.ContainsKey("connection"))
                        {
                            if (new WorkingCopy()
                            {
                                CurrentVersion = -1,
                                RepositoryPath = dictionary["repo"],
                                ConnectionString = dictionary["connection"]
                            }.Save(dictionary["copy"]))
                                return 0;
                            Console.WriteLine("Working copy save failed!!!");
                            return 1;
                        }
                        else
                        {
                            break;
                        }
                    case "update":
                        if (dictionary.ContainsKey("copy"))
                        {
                            WorkingCopy workingCopy = WorkingCopy.FromFile(dictionary["copy"]);
                            if (workingCopy != null)
                            {
                                int result = -1;
                                if (dictionary.ContainsKey("version") && !int.TryParse(dictionary["version"], out result))
                                {
                                    Console.WriteLine("Incorrect version specified!!!");
                                    return 1;
                                }
                                else if (!workingCopy.Update(result))
                                {
                                    Console.WriteLine("Update failed!!!");
                                    return 1;
                                }
                                else
                                {
                                    if (workingCopy.Save(dictionary["copy"]))
                                    {
                                        return 0;
                                    }
                                    Console.WriteLine("Working copy save failed!!!");
                                    return 1;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Working copy load failed!!!");
                                return 1;
                            }
                        }
                        else
                        {
                            break;
                        }
                    default:
                        Console.WriteLine("Unrecognized command!!!");
                        return 1;
                }
            }

            Console.WriteLine("Not enough parameters!!!");
            Console.WriteLine();
            using (var readmeStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("InoSoft.Tools.SqlVersion.Readme.txt"))
            using (var reader = new StreamReader(readmeStream))
            {
                Console.WriteLine(reader.ReadToEnd());
            }

            return 1;
        }
    }
}