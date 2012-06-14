using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Executes SQL queries synchronously, but in separate thread.
    /// </summary>
    public class SqlContext : AsyncProcessor<SqlQuery>
    {
        private DbContext _dbContext;

        /// <summary>
        /// Creates executor, which will work using connection string.
        /// </summary>
        /// <param name="connectionString">SQL connection string, which executor will use.</param>
        public SqlContext(string connectionString)
        {
            _dbContext = new DbContext(connectionString);
            Start();
        }

        /// <summary>
        /// Executes SQL command, which returns array of elements.
        /// </summary>
        /// <param name="elementType">Type of elements or null if we don't need to return query result</param>
        /// <param name="sql">SQL query string</param>
        /// <param name="parameters">Optional named parameters</param>
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
        /// <param name="sql">SQL query string</param>
        /// <param name="parameters">Optional named parameters</param>
        public void Execute(string sql, params object[] parameters)
        {
            Execute(null, sql, parameters);
        }

        /// <summary>
        /// Executes SQL command, which returns array of elements.
        /// </summary>
        /// <param name="sql">SQL query string</param>
        /// <param name="parameters">Optional named parameters</param>
        public T[] Execute<T>(string sql, params object[] parameters)
        {
            return Execute(typeof(T), sql, parameters).Cast<T>().ToArray();
        }

        /// <summary>
        /// Processes SQL queries from queue
        /// </summary>
        /// <param name="item">Encapsulated query</param>
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

    public class SqlContext<TProcedures> : SqlContext
    {
        public SqlContext(string connectionString)
            : base(connectionString)
        {
            Type proceduresProxyType = typeof(TProcedures);
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CodeTypeDeclaration classCode = new CodeTypeDeclaration("ProceduresProxy")
            {
                IsClass = true,
                Attributes = MemberAttributes.Public
            };
            classCode.BaseTypes.Add(proceduresProxyType);
            classCode.Members.Add(new CodeMemberField(typeof(SqlContext), "SqlContext") { Attributes = MemberAttributes.Public });
            foreach (var method in proceduresProxyType.GetMethods())
            {
                Type elementType = method.ReturnType.IsArray ? method.ReturnType.GetElementType() : method.ReturnType;
                Type arrayType = elementType.MakeArrayType();
                CodeMemberMethod methodCode = new CodeMemberMethod
                {
                    Name = method.Name,
                    Attributes = MemberAttributes.Public,
                    ReturnType = new CodeTypeReference(method.ReturnType)
                };

                CodeExpression[] paramsCode = method.GetParameters().Select(p => new CodeSnippetExpression(p.Name)).ToArray();
                List<CodeExpression> invokeParamsCode = new List<CodeExpression>();
                invokeParamsCode.Add(new CodeSnippetExpression(string.Format("\"EXEC {0}\"", method.Name)));
                foreach (var p in method.GetParameters())
                {
                    invokeParamsCode.Add(new CodeSnippetExpression(string.Format("new System.Data.SqlClient.SqlParameter(\"{0}\", {0})", p.Name)));
                    methodCode.Parameters.Add(new CodeParameterDeclarationExpression(p.ParameterType, p.Name));
                }
                var invokeCode = new CodeMethodInvokeExpression(new CodeSnippetExpression("SqlContext"), string.Format("Execute<{0}>", elementType), invokeParamsCode.ToArray());
                if (method.ReturnType == typeof(void))
                {
                    methodCode.Statements.Add(invokeCode);
                }
                else
                {
                    methodCode.Statements.Add(new CodeVariableDeclarationStatement(arrayType, "result", invokeCode));
                    if (method.ReturnType.IsArray)
                    {
                        methodCode.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("result")));
                    }
                    else
                    {
                        methodCode.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("result[0]")));
                    }
                }

                classCode.Members.Add(methodCode);
            }

            CodeNamespace namespaceCode = new CodeNamespace("InoSoft.Tools.Data");
            namespaceCode.Imports.Add(new CodeNamespaceImport("System"));
            namespaceCode.Imports.Add(new CodeNamespaceImport("System.Collections"));
            namespaceCode.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            namespaceCode.Imports.Add(new CodeNamespaceImport("System.Linq"));
            namespaceCode.Types.Add(classCode);

            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms, Encoding.Unicode);
            codeProvider.GenerateCodeFromType(classCode, sw, new CodeGeneratorOptions());
            sw.Flush();
            byte[] codeBytes = ms.ToArray();
            string code = Encoding.Unicode.GetString(codeBytes);

            CodeCompileUnit compileUnit = new CodeCompileUnit();
            compileUnit.Namespaces.Add(namespaceCode);
            compileUnit.ReferencedAssemblies.Add("System.dll");
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
            Procedures = (TProcedures)compileResult.CompiledAssembly.CreateInstance("InoSoft.Tools.Data.ProceduresProxy");
            Procedures.GetType().GetField("SqlContext").SetValue(Procedures, this);
        }

        public TProcedures Procedures { get; private set; }
    }
}