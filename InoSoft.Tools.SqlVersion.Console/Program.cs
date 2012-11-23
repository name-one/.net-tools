using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace InoSoft.Tools.Sqlver.ConsoleApp
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
                            return Helper.Init(dictionary["repo"], dictionary["sql"]) ? 0 : 1;
                        }
                        else
                        {
                            break;
                        }
                    case "commit":
                        if (dictionary.ContainsKey("repo") && dictionary.ContainsKey("sql"))
                        {
                            return Helper.Commit(dictionary["repo"], dictionary["sql"]) ? 0 : 1;
                        }
                        else
                        {
                            break;
                        }
                    case "checkout":
                        if (dictionary.ContainsKey("copy") && dictionary.ContainsKey("repo") && dictionary.ContainsKey("connection"))
                        {
                            return Helper.Checkout(dictionary["copy"], dictionary["repo"], dictionary["connection"]) ? 0 : 1;
                        }
                        else
                        {
                            break;
                        }
                    case "update":
                        int timeout = 30;
                        if (dictionary.ContainsKey("timeout"))
                        {
                            if (!Int32.TryParse(dictionary["timeout"], out timeout))
                            {
                                Console.WriteLine("Incorrect timeout specified!!!");
                                return 1;
                            }
                        }

                        if (dictionary.ContainsKey("copy"))
                        {
                            int version = -1;
                            if (dictionary.ContainsKey("version") && !int.TryParse(dictionary["version"], out version))
                            {
                                Console.WriteLine("Incorrect version specified!!!");
                                return 1;
                            }
                            return Helper.Update(dictionary["copy"], version) ? 0 : 1;
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