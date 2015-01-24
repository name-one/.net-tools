using System;

namespace InoSoft.Tools.SqlMigrations
{
    /// <summary>
    ///   Contains a definition of a database routine.
    /// </summary>
    public class DbRoutineDefinition : DbObjectDefinition
    {
        private readonly string[] _parameters;

        /// <summary>
        ///   Initializes a new instance of the <see cref="DbRoutineDefinition"/> class.
        /// </summary>
        /// <param name="name">The routine name.</param>
        /// <param name="schema">The routine schema name.</param>
        /// <param name="parameters">The routine parameters.</param>
        /// <param name="definition">The routine definition SQL script.</param>
        public DbRoutineDefinition(string name, string schema, string[] parameters, string definition)
            : base(name, schema, definition)
        {
            _parameters = parameters;
        }

        /// <summary>
        ///   Gets the routine signature.
        /// </summary>
        /// <returns>
        ///   The routine signature in the following format:<br/>
        ///   <c>[Schema].[RountineName](@param1 type1, @param2 type2, ...)</c><br/>
        ///   e.g.<br/>
        ///   <c>[dbo].[Foo](@bar int, @baz nvarchar(80))</c>
        /// </returns>
        public string GetSignature()
        {
            return String.Format("[{0}].[{1}]({2})", Schema, Name, String.Join(", ", _parameters));
        }

        /// <summary>
        ///   Returns the signature of this database routine.
        /// </summary>
        /// <returns>
        ///   The signature of this database routine.
        /// </returns>
        public override string ToString()
        {
            return GetSignature();
        }
    }
}