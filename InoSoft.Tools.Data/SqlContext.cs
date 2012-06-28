using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Context for executing SQL queries.
    /// </summary>
    /// <remarks>
    /// Works synchronously, but in separate thread. Only one thread and one execution queue are created,
    /// so single context can operate only single SQL query at the same time. For simultaneous access
    /// miltiple contexts must be used. Note, that some versions of MSSQL can't operate queries in parallel.
    /// In this case using single context for the whole application is recommended.
    /// This context is async wrapper of EF 4.1 DbContext and its best advantage is that it works in single
    /// thread, as mentioned before. This technique solves problem, when EntityConnection reconnects each time
    /// it's used in different thread.
    /// </remarks>
    public class SqlContext : AsyncProcessor<SqlQuery>
    {
        private DbContext _dbContext;

        /// <summary>
        /// Creates SqlContext.
        /// </summary>
        /// <param name="connectionString">SQL connection string, which context will use.</param>
        public SqlContext(string connectionString)
        {
            _dbContext = new DbContext(connectionString);
            Start();
        }

        /// <summary>
        /// Executes SQL command, which returns array of elements.
        /// </summary>
        /// <param name="elementType">Type of elements or null if we don't need to return query result.</param>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        public Array Execute(Type elementType, string sql, params object[] parameters)
        {
            // Create encapsulated query and push it into queue
            SqlQuery query = new SqlQuery
            {
                ElementType = elementType,
                Sql = sql,
                Parameters = parameters
            };
            EnqueueItem(query);

            // Wait until query will be executed
            query.WaitSignal();

            if (query.Exception == null)
            {
                // Return query result
                return query.Result;
            }
            else
            {
                // Rethrow exception if it occured
                throw query.Exception;
            }
        }

        /// <summary>
        /// Executes SQL query, which returns nothing.
        /// </summary>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        public void Execute(string sql, params object[] parameters)
        {
            Execute(null, sql, parameters);
        }

        /// <summary>
        /// Executes SQL command, which returns array of elements.
        /// </summary>
        /// <param name="sql">SQL query string.</param>
        /// <param name="parameters">Optional named parameters.</param>
        public T[] Execute<T>(string sql, params object[] parameters)
        {
            return Execute(typeof(T), sql, parameters).Cast<T>().ToArray();
        }

        /// <summary>
        /// Processes SQL queries from queue.
        /// </summary>
        /// <param name="item">Encapsulated query.</param>
        protected override void ProcessItem(SqlQuery item)
        {
            try
            {
                if (item.ElementType != null)
                {
                    item.Result = _dbContext.Database.SqlQuery(item.ElementType, item.Sql, item.Parameters).Cast<object>().ToArray();
                }
                else
                {
                    _dbContext.Database.ExecuteSqlCommand(item.Sql, item.Parameters);
                }
            }
            catch (Exception ex)
            {
                item.Exception = ex;
            }
            item.Signal();
        }
    }

    /// <summary>
    /// Context for executing SQL queries with ability to call stored procedures using interface with definitons.
    /// </summary>
    /// <typeparam name="TProcedures">Type, which has stored procedures definitions (using interface is required).</typeparam>
    /// <remarks>
    /// Procedures definitions interface is very convenient to use - you don't have to write or generate
    /// lots of repeatable code to call stored procedures in CLR style. All code is automatically generated
    /// and builded in runtime, you need only to provide stored procedures headers in declarative style.
    /// </remarks>
    public class SqlContext<TProcedures> : SqlContext
    {
        /// <summary>
        /// Creates SqlContext.
        /// </summary>
        /// <param name="connectionString">SQL connection string, which context will use.</param>
        public SqlContext(string connectionString)
            : base(connectionString)
        {
            Type proceduresProxyType = typeof(TProcedures);

            // Using interface type is required
            if (!proceduresProxyType.IsInterface)
            {
                throw new Exception("Stored procedures definitions type must interface");
            }

            // Generate procedures proxy code
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            // Declare class ProceduresProxy
            CodeTypeDeclaration classCode = new CodeTypeDeclaration("ProceduresProxy")
            {
                IsClass = true,
                Attributes = MemberAttributes.Public
            };
            // Inherit class from procedures definitions interface
            classCode.BaseTypes.Add(proceduresProxyType);
            // Add SqlContext field to access wrapped context for executing procedures
            classCode.Members.Add(new CodeMemberField(typeof(SqlContext), "SqlContext") { Attributes = MemberAttributes.Public });
            // Implement procedures definitions interface
            foreach (var method in proceduresProxyType.GetMethods())
            {
                // Determine type of elements to return and appropriate array type (e.g. String and String[])
                Type elementType = method.ReturnType.IsArray ? method.ReturnType.GetElementType() : method.ReturnType;
                Type arrayType = elementType.MakeArrayType();
                // Define method
                CodeMemberMethod methodCode = new CodeMemberMethod
                {
                    Name = method.Name,
                    Attributes = MemberAttributes.Public,
                    ReturnType = new CodeTypeReference(method.ReturnType)
                };
                // Define parameters
                List<CodeExpression> invokeParamsCode = new List<CodeExpression>();
                // SQL code for executing procedure with name
                StringBuilder sqlParamsString = new StringBuilder();
                foreach (var p in method.GetParameters())
                {
                    sqlParamsString.AppendFormat("@{0},", p.Name);
                }
                if (sqlParamsString.Length > 0)
                {
                    sqlParamsString.Length--;
                }
                invokeParamsCode.Add(new CodeSnippetExpression(string.Format("\"EXEC {0} {1}\"", method.Name, sqlParamsString)));
                // Actual parameters, tranfered via SqlParameters
                foreach (var p in method.GetParameters())
                {
                    if (p.ParameterType == typeof(string))
                    {
                        invokeParamsCode.Add(new CodeSnippetExpression(
                            string.Format("new System.Data.SqlClient.SqlParameter(\"{0}\", {0} != null ? (object){0} : DBNull.Value)", p.Name)));
                    }
                    else if (p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        invokeParamsCode.Add(new CodeSnippetExpression(
                            string.Format("new System.Data.SqlClient.SqlParameter(\"{0}\", {0}.HasValue ? (object){0}.Value : DBNull.Value)", p.Name)));
                    }
                    else
                    {
                        invokeParamsCode.Add(new CodeSnippetExpression(string.Format("new System.Data.SqlClient.SqlParameter(\"{0}\", {0})", p.Name)));
                    }
                    methodCode.Parameters.Add(new CodeParameterDeclarationExpression(p.ParameterType, p.Name));
                }
                // Invoke SQL query
                string invokeMethodName = elementType != typeof(void) ? string.Format("Execute<{0}>", elementType) : "Execute";
                var invokeCode = new CodeMethodInvokeExpression(new CodeSnippetExpression("SqlContext"),
                    invokeMethodName, invokeParamsCode.ToArray());
                if (method.ReturnType == typeof(void))
                {
                    // If method returns nothing - just invoke
                    methodCode.Statements.Add(invokeCode);
                }
                else
                {
                    // Add variable to save result
                    methodCode.Statements.Add(new CodeVariableDeclarationStatement(arrayType, "result", invokeCode));
                    if (method.ReturnType.IsArray)
                    {
                        // If method returns array - just return result
                        methodCode.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("result")));
                    }
                    else if (method.IsDefined(typeof(SingleResultAttribute), false))
                    {
                        // If method returns single value and has SingleResult attribute - return Single
                        methodCode.Statements.Add(new CodeMethodReturnStatement(
                            new CodeSnippetExpression(string.Format("result.Single<{0}>()", elementType))));
                    }
                    else
                    {
                        // If method returns single value - return SingleOrDefault
                        methodCode.Statements.Add(new CodeMethodReturnStatement(
                            new CodeSnippetExpression(string.Format("result.SingleOrDefault<{0}>()", elementType))));
                    }
                }
                // Add defined method to class
                classCode.Members.Add(methodCode);
            }

            // Put proxy type code into namespace and add usings
            CodeNamespace namespaceCode = new CodeNamespace("InoSoft.Tools.Data");
            namespaceCode.Imports.Add(new CodeNamespaceImport("System"));
            namespaceCode.Imports.Add(new CodeNamespaceImport("System.Collections"));
            namespaceCode.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            namespaceCode.Imports.Add(new CodeNamespaceImport("System.Linq"));
            namespaceCode.Types.Add(classCode);

#if DEBUG
            // Determine generated source code
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms, Encoding.Unicode);
            codeProvider.GenerateCodeFromNamespace(namespaceCode, sw, new CodeGeneratorOptions());
            sw.Flush();
            byte[] codeBytes = ms.ToArray();
            string code = Encoding.Unicode.GetString(codeBytes);
            Debug.WriteLine("Generated ProceduresProxy code:");
            Debug.WriteLine(code);
#endif

            // Compile temporary assembly
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            compileUnit.Namespaces.Add(namespaceCode);
            compileUnit.ReferencedAssemblies.Add("System.dll");
            compileUnit.ReferencedAssemblies.Add("System.Core.dll");
            compileUnit.ReferencedAssemblies.Add("System.Linq.dll");
            compileUnit.ReferencedAssemblies.Add("System.Data.dll");
            compileUnit.ReferencedAssemblies.Add(Assembly.GetAssembly(typeof(AsyncProcessor<>)).Location);
            compileUnit.ReferencedAssemblies.Add(Assembly.GetAssembly(typeof(SqlContext)).Location);
            compileUnit.ReferencedAssemblies.Add(Assembly.GetAssembly(typeof(TProcedures)).Location);
            CompilerParameters compilerParameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };
            var compileResult = codeProvider.CompileAssemblyFromDom(compilerParameters, compileUnit);

            // Create procedures proxy
            Procedures = (TProcedures)compileResult.CompiledAssembly.CreateInstance("InoSoft.Tools.Data.ProceduresProxy");
            Procedures.GetType().GetField("SqlContext").SetValue(Procedures, this);
        }

        /// <summary>
        /// Gets stored procedures proxy, which is used to call them on the database.
        /// </summary>
        public TProcedures Procedures { get; private set; }
    }
}