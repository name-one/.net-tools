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
    public class SqlContext : AsyncProcessor<SqlBatch>, ISqlContext
    {
        private readonly DbContext _dbContext;

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
            var query = new SqlQuery
            {
                ElementType = elementType,
                Sql = sql,
                Parameters = parameters
            };
            var batch = new SqlBatch { Queries = new[] { query } };
            EnqueueItem(batch);

            // Wait until query will be executed
            batch.WaitSignal();

            if (query.Exception != null)
            {
                // Rethrow exception if it occured
                throw query.Exception;
            }

            // Return query result
            return query.Result;
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
        /// Processes batches of SQL queries from queue.
        /// </summary>
        /// <param name="item">Encapsulated batch of queries.</param>
        protected override void ProcessItem(SqlBatch item)
        {
            foreach (var query in item.Queries)
            {
                try
                {
                    if (query.ElementType != null)
                    {
                        query.Result = _dbContext.Database.SqlQuery(query.ElementType, query.Sql, query.Parameters)
                            .Cast<object>().ToArray();
                    }
                    else
                    {
                        _dbContext.Database.ExecuteSqlCommand(query.Sql, query.Parameters);
                    }
                }
                catch (Exception ex)
                {
                    query.Exception = ex;
                    break;
                }
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
        private readonly string _proxyTypeName;
        private readonly Assembly _compiledAssembly;

        /// <summary>
        /// Creates SqlContext.
        /// </summary>
        /// <param name="connectionString">SQL connection string, which context will use.</param>
        public SqlContext(string connectionString)
            : base(connectionString)
        {
            Type proceduresInterfaceType = typeof(TProcedures);

            // Using interface type is required
            if (!proceduresInterfaceType.IsInterface)
            {
                throw new Exception("Stored procedures definitions type must be an interface.");
            }

            // Generate procedures proxy code
            var codeProvider = new CSharpCodeProvider();

            // Create a namespace for the code being generated, add usings
            var namespaceCode = new CodeNamespace("InoSoft.Tools.Data");
            namespaceCode.Imports.Add(new CodeNamespaceImport("System"));
            namespaceCode.Imports.Add(new CodeNamespaceImport("System.Collections"));
            namespaceCode.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            namespaceCode.Imports.Add(new CodeNamespaceImport("System.Linq"));

            // Generate proxy class code and add it to the namespace
            _proxyTypeName = "ProceduresProxy";
            var proxyClassCode = GetProxyClassCode(proceduresInterfaceType, _proxyTypeName);
            namespaceCode.Types.Add(proxyClassCode);

#if DEBUG
            // Determine generated source code
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms, Encoding.Unicode);
            codeProvider.GenerateCodeFromNamespace(namespaceCode, sw, new CodeGeneratorOptions());
            sw.Flush();
            byte[] codeBytes = ms.ToArray();
            string code = Encoding.Unicode.GetString(codeBytes);
            Debug.WriteLine("Generated ProceduresProxy code:");
            Debug.WriteLine(code);
#endif

            // Compile temporary assembly
            var compileUnit = new CodeCompileUnit();
            compileUnit.Namespaces.Add(namespaceCode);
            compileUnit.ReferencedAssemblies.Add("System.dll");
            compileUnit.ReferencedAssemblies.Add("System.Core.dll");
            compileUnit.ReferencedAssemblies.Add("System.Data.dll");
            compileUnit.ReferencedAssemblies.Add(Assembly.GetAssembly(typeof(AsyncProcessor<>)).Location);
            compileUnit.ReferencedAssemblies.Add(Assembly.GetAssembly(typeof(SqlContext)).Location);
            compileUnit.ReferencedAssemblies.Add(Assembly.GetAssembly(typeof(TProcedures)).Location);
            var compilerParameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };
            var compileResult = codeProvider.CompileAssemblyFromDom(compilerParameters, compileUnit);
            _compiledAssembly = compileResult.CompiledAssembly;

            // Create procedures proxy
            object procedures = _compiledAssembly.CreateInstance("InoSoft.Tools.Data." + _proxyTypeName);
            if (procedures == null)
                throw new Exception("Failed to create a proxy.");
            Procedures = (TProcedures)procedures;
            Procedures.GetType().GetField("Context").SetValue(Procedures, this);
        }

        private static CodeTypeDeclaration GetProxyClassCode(Type proceduresInterfaceType, string proxyTypeName)
        {
            // Declare class ProceduresProxy
            var classCode = new CodeTypeDeclaration(proxyTypeName)
            {
                IsClass = true,
                Attributes = MemberAttributes.Public
            };
            // Inherit class from procedures definitions interface
            classCode.BaseTypes.Add(proceduresInterfaceType);
            // Add SqlContext field to access wrapped context for executing procedures
            classCode.Members.Add(new CodeMemberField(typeof(ISqlContext), "Context") { Attributes = MemberAttributes.Public });
            // Implement procedures definitions interface
            foreach (var method in proceduresInterfaceType.GetMethods())
            {
                // Determine type of elements to return and appropriate array type (e.g. String and String[])
                Type elementType = method.ReturnType.IsArray ? method.ReturnType.GetElementType() : method.ReturnType;
                Type arrayType = elementType.MakeArrayType();

                // Define method
                var methodCode = new CodeMemberMethod
                {
                    Name = method.Name,
                    Attributes = MemberAttributes.Public,
                    ReturnType = new CodeTypeReference(method.ReturnType)
                };
                // Define parameters
                var invokeParamsCode = new List<CodeExpression>();
                // SQL code for executing procedure with name
                var sqlParamsString = new StringBuilder();
                foreach (var p in method.GetParameters())
                {
                    sqlParamsString.AppendFormat("@{0},", p.Name);
                }
                if (sqlParamsString.Length > 0)
                {
                    sqlParamsString.Length--;
                }
                invokeParamsCode.Add(new CodeSnippetExpression(String.Format("\"EXEC {0} {1}\"", method.Name, sqlParamsString)));
                // Actual parameters, tranfered via SqlParameters
                foreach (var p in method.GetParameters())
                {
                    if (p.ParameterType == typeof(string))
                    {
                        invokeParamsCode.Add(new CodeSnippetExpression(String.Format(
                            "new System.Data.SqlClient.SqlParameter(\"{0}\", {0} != null ? (object){0} : DBNull.Value)",
                            p.Name)));
                    }
                    else if (p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        invokeParamsCode.Add(new CodeSnippetExpression(String.Format(
                            "new System.Data.SqlClient.SqlParameter(\"{0}\", {0}.HasValue ? (object){0}.Value : DBNull.Value)",
                            p.Name)));
                    }
                    else
                    {
                        invokeParamsCode.Add(new CodeSnippetExpression(String.Format(
                            "new System.Data.SqlClient.SqlParameter(\"{0}\", {0})", p.Name)));
                    }
                    methodCode.Parameters.Add(new CodeParameterDeclarationExpression(p.ParameterType, p.Name));
                }
                // Invoke SQL query
                var invokeCode = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(
                    new CodeSnippetExpression("Context"), "Execute",
                    elementType == typeof(void) ? new CodeTypeReference[0] : new[] { new CodeTypeReference(elementType) }),
                    invokeParamsCode.ToArray());
                if (method.ReturnType == typeof(void))
                {
                    // If method returns nothing, just invoke
                    methodCode.Statements.Add(invokeCode);
                }
                else
                {
                    // Add variable to save result
                    methodCode.Statements.Add(new CodeVariableDeclarationStatement(arrayType, "result", invokeCode));
                    if (method.ReturnType.IsArray)
                    {
                        // If method returns array, just return result
                        methodCode.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("result")));
                    }
                    else if (method.IsDefined(typeof(SingleResultRequiredAttribute), false))
                    {
                        // If method returns single value and has SingleResultRequired attribute, return Single
                        methodCode.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("result.Single()")));
                    }
                    else
                    {
                        // If method returns single value, return SingleOrDefault
                        methodCode.Statements.Add(new CodeMethodReturnStatement(
                            new CodeSnippetExpression("result.SingleOrDefault()")));
                    }
                }
                // Add defined method to class
                classCode.Members.Add(methodCode);
            }
            return classCode;
        }

        /// <summary>
        /// Gets stored procedures proxy, which is used to call them on the database.
        /// </summary>
        public TProcedures Procedures { get; private set; }

        /// <summary>
        /// Creates a context for batched SQL query execution.
        /// </summary>
        /// <returns>Context for batched SQL query execution.</returns>
        public BatchContext CreateBatch()
        {
            // Create procedures proxy
            var batchProxy = _compiledAssembly.CreateInstance("InoSoft.Tools.Data." + _proxyTypeName);
            if (batchProxy == null)
                throw new Exception("Failed to create a proxy.");
            batchProxy.GetType().GetField("Context").SetValue(batchProxy, this);
            return new BatchContext(this, (TProcedures)batchProxy);
        }

        /// <summary>
        /// Context for executing SQL queries in batched manner.
        /// </summary>
        public class BatchContext : ISqlContext
        {
            private readonly SqlContext<TProcedures> _sqlContext;
            private readonly List<SqlQuery> _queries = new List<SqlQuery>();

            internal BatchContext(SqlContext<TProcedures> sqlContext, TProcedures procedures)
            {
                _sqlContext = sqlContext;
                Procedures = procedures;
            }

            /// <summary>
            /// Gets stored procedures proxy, which is used to call them on the database when Run() is invoked.
            /// </summary>
            public TProcedures Procedures { get; private set; }

            /// <summary>
            /// Executes SQL query, which returns nothing.
            /// </summary>
            /// <param name="sql">SQL query string.</param>
            /// <param name="parameters">Optional named parameters.</param>
            public void Execute(string sql, params object[] parameters)
            {
                _queries.Add(new SqlQuery
                {
                    ElementType = null,
                    Sql = sql,
                    Parameters = parameters
                });
            }

            /// <summary>
            /// Executes SQL command, which returns array of values of type <see cref="T"/>.
            /// </summary>
            /// <param name="sql">SQL query string.</param>
            /// <param name="parameters">Optional named parameters.</param>
            /// <returns>An array with a single default(<see cref="T"/>) value.</returns>
            /// <remarks>
            /// As batch may be executed later, the method always returns an array with a single default value.
            /// The query will be executed anyway.
            /// </remarks>
            T[] ISqlContext.Execute<T>(string sql, params object[] parameters)
            {
                _queries.Add(new SqlQuery
                {
                    ElementType = typeof(T),
                    Sql = sql,
                    Parameters = parameters
                });

                return new[] { default(T) };
            }

            /// <summary>
            /// Executes the batch.
            /// </summary>
            public void Run()
            {
                // Create a batch of the SQL queries and enqueue it
                var batch = new SqlBatch { Queries = _queries.ToArray() };
                _sqlContext.EnqueueItem(batch);

                // Wait until query will be executed
                batch.WaitSignal();

                // If any error occured during batch execution, rethrow it
                var failedQuery = batch.Queries.FirstOrDefault(q => q.Exception != null);
                if (failedQuery != null)
                {
                    throw failedQuery.Exception;
                }
            }
        }
    }
}