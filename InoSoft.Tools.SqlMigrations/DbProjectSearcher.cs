using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   Reads database objects from a SQL Server project.
    /// </summary>
    public class DbProjectSearcher
    {
        private const string Functions = "Functions";
        private const string StoredProcedures = "Stored Procedures";
        private const string Views = "Views";

        private static readonly Regex ParamsRegex =
            new Regex(@"\s*(?<param>@[\S]*)\s*",
                RegexOptions.IgnoreCase);

        private static readonly Regex PathRegex =
            new Regex(@"<Build Include=""(?<path>(?<schema>[^\\""]*)\\(?<type>[^\\""]*)\\(?<name>[^\\""]*).sql)"" />",
                RegexOptions.IgnoreCase);

        private readonly string _directoryName;
        private readonly string _projectPath;

        /// <summary>
        ///   Initializes a new instance of the <see cref="DbProjectSearcher"/> class.
        /// </summary>
        /// <param name="projectPath">An absolute path to the SQL Server project.</param>
        public DbProjectSearcher(string projectPath)
        {
            if (projectPath == null || !Path.IsPathRooted(projectPath)
                || !Path.GetExtension(projectPath).Equals(".sqlproj", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Project path must be an absolute path to a *.sqlproj file.");

            _projectPath = projectPath;
            _directoryName = Path.GetDirectoryName(_projectPath);
        }

        /// <summary>
        ///   Gets all user-defined function definitions from the SQL Project.
        /// </summary>
        /// <returns>
        ///   All user-defined function definitions contained in the SQL Project.
        /// </returns>
        public DbRoutineDefinition[] GetFunctionDefinitions()
        {
            return GetDefinitions(Functions).Cast<DbRoutineDefinition>().ToArray();
        }

        /// <summary>
        ///   Gets all stored procedure definitions from the SQL Project.
        /// </summary>
        /// <returns>
        ///   All stored procedure definitions contained in the SQL Project.
        /// </returns>
        public DbRoutineDefinition[] GetProcedureDefinitions()
        {
            return GetDefinitions(StoredProcedures).Cast<DbRoutineDefinition>().ToArray();
        }

        /// <summary>
        ///   Gets all view definitions from the SQL Project.
        /// </summary>
        /// <returns>
        ///   All view definitions contained in the SQL Project.
        /// </returns>
        public DbObjectDefinition[] GetViewDefinitions()
        {
            return GetDefinitions(Views);
        }

        /// <summary>
        ///   Gets the routine parameters.
        /// </summary>
        /// <param name="type">The routine type.</param>
        /// <param name="definition">The routine definition.</param>
        /// <returns>
        ///   An array containing the routine parameters.<br/>
        ///   E.g. <c>@foo nvarchar(80)</c>
        /// </returns>
        private static string[] GetParameters(string type, string definition)
        {
            switch (type)
            {
                case Functions:
                    return ParamsRegex
                        .Matches(definition.Substring(0,
                            definition.IndexOf("\nRETURNS", StringComparison.InvariantCultureIgnoreCase)))
                        .Cast<Match>()
                        .Select(m => m.Groups["param"].Value)
                        .ToArray();

                case StoredProcedures:
                    return ParamsRegex
                        .Matches(definition.Substring(0,
                            definition.IndexOf("\nAS", StringComparison.InvariantCultureIgnoreCase)))
                        .Cast<Match>()
                        .Select(m => m.Groups["param"].Value)
                        .ToArray();

                default:
                    return new string[0];
            }
        }

        /// <summary>
        ///   Gets an object definition from a regex match.
        /// </summary>
        /// <param name="match">The regex match to get a definition from.</param>
        /// <returns>
        ///   An object definition corresponding to the specified regex match.
        /// </returns>
        private DbObjectDefinition GetDefinition(Match match)
        {
            string file = File.ReadAllText(Path.Combine(_directoryName, match.Groups["path"].Value));
            return match.Groups["type"].Value == Views
                ? new DbObjectDefinition(match.Groups["name"].Value, match.Groups["schema"].Value, file)
                : new DbRoutineDefinition(match.Groups["name"].Value, match.Groups["schema"].Value,
                    GetParameters(match.Groups["type"].Value, file), file);
        }

        /// <summary>
        ///   Gets object definitions of the specified type.
        /// </summary>
        /// <param name="type">The object type.</param>
        /// <returns>
        ///   Object definitions of the specified type.
        /// </returns>
        private DbObjectDefinition[] GetDefinitions(string type)
        {
            return PathRegex
                .Matches(File.ReadAllText(_projectPath))
                .Cast<Match>()
                .Where(m => m.Groups["type"].Value == type)
                .Select(GetDefinition)
                .OrderBy(m => m.FullName)
                .ToArray();
        }
    }
}