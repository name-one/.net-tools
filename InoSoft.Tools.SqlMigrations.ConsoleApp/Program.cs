using System;
using System.IO;
using System.Reflection;
using InoSoft.Tools.SqlMigrations.Sqlver;

namespace InoSoft.Tools.SqlMigrations.ConsoleApp
{
    /// <summary>
    ///   Runs the SQL Migrations console application.
    /// </summary>
    internal class Program
    {
        private static readonly AssemblyResourceLoader _assemblyResourceLoader =
            new AssemblyResourceLoader(Assembly.GetExecutingAssembly(), typeof(Program).Namespace + ".Resources");

        /// <summary>
        ///   Initializes the <see cref="Program"/> class.
        /// </summary>
        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += _assemblyResourceLoader.OnAssemblyResolve;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += _assemblyResourceLoader.OnAssemblyResolve;
        }

        /// <summary>
        ///   Executes the program.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>
        ///   Error code. Zero if the program was executed successfully; otherwise, a non-zero value.
        /// </returns>
        public static int Main(string[] args)
        {
            CommandLineParameters p = CommandLineParameters.Read(args);
            string[] positional = p.Positional;
            string command = positional.Length > 0 ? positional[0] : null;
            bool isVerbose = p.ContainsKeys("v", "verbose");

            switch (command)
            {
                case "update":
                case "u":
                    if (positional.Length != 2)
                        return ShowReadme(true);

                    try
                    {
                        Console.Write("Loading the migration settings from {0}... ", positional[1]);
                        DbMigrationSettings settings = DbMigrationSettings.FromFile(positional[1]);
                        settings.Save(positional[1]);
                        Console.WriteLine("done.");
                        Console.WriteLine();
                        Console.WriteLine("Starting the update.");
                        var runner = new DbMigrationRunner(settings) { OutputLog = Console.Out };
                        runner.Update(p.GetNamedValue("timeout", 30));
                        Console.WriteLine();
                        Console.WriteLine("Update complete.");
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error!");
                        LogError(ex, isVerbose);
                        return 1;
                    }

                case "sqlver-migrate-repo":
                case "sr":
                    if (positional.Length != 3)
                        return ShowReadme(true);

                    try
                    {
                        Console.Write("Loading the Sqlver repository from {0}... ", positional[1]);
                        var xmlPath = p.GetNamedValue<string>("versions-xml");
                        SqlverRepositoryMigrator migrator = xmlPath != null
                            ? new SqlverRepositoryMigrator(positional[1], XmlVersionsModel.Read(xmlPath))
                            : new SqlverRepositoryMigrator(positional[1], p.GetNamedValue("version", new Version(1, 0)));
                        Console.WriteLine("done.");
                        Console.WriteLine();
                        Console.WriteLine("Starting the migration from Sqlver to SQL Migrations.");
                        migrator.Convert(positional[2]);
                        Console.WriteLine();
                        Console.WriteLine("Migration complete.");
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error!");
                        LogError(ex, isVerbose);
                        return 1;
                    }

                case "sqlver-migrate-copy":
                case "sc":
                    if (positional.Length != 3)
                        return ShowReadme(true);

                    try
                    {
                        Console.Write("Loading the Sqlver working copy from {0}... ", positional[1]);
                        var xmlPath = p.GetNamedValue<string>("versions-xml");
                        SqlverWorkingCopyMigrator migrator = xmlPath != null
                            ? new SqlverWorkingCopyMigrator(positional[1], XmlVersionsModel.Read(xmlPath))
                            : new SqlverWorkingCopyMigrator(positional[1], p.GetNamedValue("version", new Version(1, 0)));
                        Console.WriteLine("done.");
                        Console.WriteLine();
                        Console.WriteLine("Starting the migration from Sqlver to SQL Migrations.");
                        migrator.Convert(positional[2], p.GetNamedValue<string>("version-property"));
                        Console.WriteLine();
                        Console.WriteLine("Migration complete.");
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error!");
                        LogError(ex, isVerbose);
                        return 1;
                    }

                case "help":
                case "h":
                case "?":
                    return ShowReadme(false);

                case null:
                    return ShowReadme(!p.ContainsKeys("help", "h", "?"));

                default:
                    return ShowReadme(true);
            }
        }

        /// <summary>
        ///   Logs an exception to the standard error output stream.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="isVerbose">If set to <c>true</c>, logs stack trace and other details of the exception.</param>
        private static void LogError(Exception ex, bool isVerbose)
        {
            if (isVerbose)
            {
                Console.Error.WriteLine(ex);
            }
            else
            {
                Console.Error.WriteLine(ex.Message);
                var aggregate = ex as AggregateException;
                if (aggregate != null)
                {
                    for (int i = 0; i < aggregate.InnerExceptions.Count; i++)
                    {
                        Console.Error.Write("{0}. ", i + 1);
                        LogError(aggregate.InnerExceptions[i], false);
                    }
                }
                else if (ex.InnerException != null)
                {
                    LogError(ex.InnerException, false);
                }
            }
        }

        /// <summary>
        ///   Shows the readme.
        /// </summary>
        /// <param name="isError">If set to <c>true</c>, the method returns <c>-1</c> error code.</param>
        /// <returns>
        ///   Error code.
        ///   <c>0</c> if <paramref name="isError"/> is <c>false</c>; otherwise, <c>-1</c>.
        /// </returns>
        private static int ShowReadme(bool isError)
        {
            using (Stream readmeStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("InoSoft.Tools.SqlMigrations.ConsoleApp.Readme.txt"))
            {
                if (readmeStream == null)
                {
                    Console.Error.WriteLine("Help not found.");
                    return Int32.MinValue;
                }
                using (var reader = new StreamReader(readmeStream))
                {
                    Console.WriteLine(reader.ReadToEnd());
                }
            }

            return isError ? -1 : 0;
        }
    }
}