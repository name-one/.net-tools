using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;

namespace InoSoft.Tools.Data
{
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
        private readonly Assembly _compiledAssembly;
        private readonly string _proxyTypeName;

        /// <summary>
        /// Creates SqlContext.
        /// </summary>
        /// <param name="connectionString">SQL connection string, which context will use.</param>
        public SqlContext(string connectionString)
            : base(connectionString)
        {
            Type proceduresInterfaceType = typeof(TProcedures);

            // Using interface type is required.
            if (!proceduresInterfaceType.IsInterface)
            {
                throw new Exception("Stored procedures definitions type must be an interface.");
            }

            // Create a namespace for the code being generated, add usings.
            var codeNamespace = new CodeNamespace(proceduresInterfaceType.Namespace);
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Linq"));

            // Generate proxy class code and add it to the namespace.
            string proxyTypeName = proceduresInterfaceType.FullName.Replace('.', '_') + "_ProceduresProxy";
            _proxyTypeName = proceduresInterfaceType.Namespace + "." + proxyTypeName;
            CodeTypeDeclaration proxyClassCode = GetProxyClassCode(proceduresInterfaceType, proxyTypeName);
            codeNamespace.Types.Add(proxyClassCode);

            // Add assembly references.
            var assemblies = new[]
            {
                Assembly.GetAssembly(typeof(SqlParameter)),
                Assembly.GetAssembly(typeof(AsyncProcessor<>)),
                Assembly.GetAssembly(typeof(SqlContext)),
                Assembly.GetAssembly(typeof(TProcedures))
            };

            // Compile the assembly.
            _compiledAssembly = AssemblyCreator.Create(codeNamespace, assemblies);

            // Create procedures proxy.
            object procedures = _compiledAssembly.CreateInstance(_proxyTypeName);
            if (procedures == null)
                throw new Exception("Failed to create a proxy.");
            Procedures = (TProcedures)procedures;
            Procedures.GetType().GetField("Context").SetValue(Procedures, this);
        }

        /// <summary>
        /// Gets stored procedures proxy, which is used to call them on the database.
        /// </summary>
        public TProcedures Procedures { get; private set; }

        /// <summary>
        /// Creates a context for batched SQL query execution.
        /// </summary>
        /// <returns>Context for batched SQL query execution.</returns>
        public BatchContext<TProcedures> CreateBatch()
        {
            // Create procedures proxy
            var batchProxy = _compiledAssembly.CreateInstance(_proxyTypeName);
            if (batchProxy == null)
                throw new Exception("Failed to create a proxy.");
            var batchContext = new BatchContext<TProcedures>(this, (TProcedures)batchProxy);
            batchProxy.GetType().GetField("Context").SetValue(batchProxy, batchContext);
            return batchContext;
        }

        private static string AddOutParameter(ParameterInfo parameter, CodeStatementCollection statements)
        {
            Type elementType = parameter.ParameterType.GetElementType();
            Type underlyingType = Nullable.GetUnderlyingType(elementType);
            bool isNullable = underlyingType != null;
            if (isNullable)
            {
                elementType = underlyingType;
            }

            string sqlParamVar = String.Format("{0}SqlParameter", parameter.Name);
            statements.Add(new CodeVariableDeclarationStatement(
                typeof(SqlParameter), sqlParamVar,
                new CodeSnippetExpression("new System.Data.SqlClient.SqlParameter()")));
            statements.Add(new CodeAssignStatement(
                new CodeSnippetExpression(String.Format("{0}.ParameterName", sqlParamVar)),
                new CodeSnippetExpression(String.Format("\"{0}\"", parameter.Name))));
            CodeExpression valueExpression = elementType.IsValueType
                ? new CodeDefaultValueExpression(new CodeTypeReference(elementType))
                : (CodeExpression)new CodeSnippetExpression("DBNull.Value");
            statements.Add(new CodeAssignStatement(
                new CodeSnippetExpression(String.Format("{0}.Value", sqlParamVar)),
                valueExpression));
            statements.Add(new CodeAssignStatement(
                new CodeSnippetExpression(String.Format("{0}.IsNullable", sqlParamVar)),
                new CodeSnippetExpression(isNullable || !elementType.IsValueType ? "true" : "false")));
            statements.Add(new CodeAssignStatement(
                new CodeSnippetExpression(String.Format("{0}.Direction", sqlParamVar)),
                new CodeSnippetExpression("System.Data.ParameterDirection.Output")));
            if (elementType == typeof(string))
            {
                statements.Add(new CodeAssignStatement(
                    new CodeSnippetExpression(String.Format("{0}.Size", sqlParamVar)),
                    new CodeSnippetExpression("Int32.MaxValue")));
            }

            return String.Format("@{0} output", parameter.Name);
        }

        private static void AddProxyClassMethod(MethodInfo method, CodeTypeDeclaration classCode)
        {
            // Determine type of elements to return and appropriate array type (e.g. String and String[]).
            Type elementType = method.ReturnType.IsArray ? method.ReturnType.GetElementType() : method.ReturnType;
            Type arrayType = elementType.MakeArrayType();

            // Define method.
            var methodCode = new CodeMemberMethod
            {
                Name = method.Name,
                Attributes = MemberAttributes.Public,
                PrivateImplementationType = method.DeclaringType != null
                    ? new CodeTypeReference(method.DeclaringType)
                    : null,
                ReturnType = new CodeTypeReference(method.ReturnType)
            };

            // Define parameters.
            var invokeParamsCode = new List<CodeExpression>();

            // SQL code for executing procedure with name.
            var sqlParams = new List<string>();
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                sqlParams.Add(parameter.IsOut
                    ? AddOutParameter(parameter, methodCode.Statements)
                    : String.Format("@{0}", parameter.Name));
            }

            FunctionAttribute[] funcAttribs = method.GetAttributes<FunctionAttribute>();
            SqlQueryType sqlQueryType = funcAttribs.Length == 0 ? SqlQueryType.Procedure : SqlQueryType.General;
            var sqlQueryText = sqlQueryType == SqlQueryType.Procedure
                ? String.Format("[{0}].[{1}]",
                    GetSchemaName(method) ?? GetSchemaName(method.DeclaringType) ?? "dbo", method.Name)
                : funcAttribs[0].GetQuery(method.Name, String.Join(", ", sqlParams));

            // Specify SQL query.
            invokeParamsCode.Add(new CodeSnippetExpression(String.Format(@"""{0}""", sqlQueryText)));

            // Specify SQL query type.
            invokeParamsCode.Add(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(SqlQueryType)),
                sqlQueryType.ToString()));

            // Actual parameters, tranferred via SqlParameters.
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (parameter.IsOut)
                {
                    invokeParamsCode.Add(new CodeSnippetExpression(String.Format("{0}SqlParameter", parameter.Name)));
                    var paramCode = new CodeParameterDeclarationExpression(parameter.ParameterType.GetElementType(),
                        parameter.Name)
                    {
                        Direction = FieldDirection.Out
                    };
                    methodCode.Parameters.Add(paramCode);
                }
                else
                {
                    string paramCasted = parameter.ParameterType.IsEnum
                        ? String.Format("(({0}){1})", Enum.GetUnderlyingType(parameter.ParameterType), parameter.Name)
                        : parameter.Name;
                    if (parameter.ParameterType == typeof(string))
                    {
                        invokeParamsCode.Add(new CodeSnippetExpression(String.Format(
                            "new System.Data.SqlClient.SqlParameter(\"{0}\", {0} != null ? (object){0} : DBNull.Value)", parameter.Name)));
                    }
                    else if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        invokeParamsCode.Add(new CodeSnippetExpression(String.Format(
                            "new System.Data.SqlClient.SqlParameter(\"{0}\", {0}.HasValue ? (object){1}.Value : DBNull.Value)",
                            parameter.Name, paramCasted)));
                    }
                    else
                    {
                        invokeParamsCode.Add(new CodeSnippetExpression(String.Format(
                            "new System.Data.SqlClient.SqlParameter(\"{0}\", {1})", parameter.Name, paramCasted)));
                    }
                    methodCode.Parameters.Add(new CodeParameterDeclarationExpression(parameter.ParameterType, parameter.Name));
                }
            }

            // Invoke SQL query.
            var invokeCode = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(
                new CodeSnippetExpression("Context"), "Execute",
                elementType == typeof(void) ? new CodeTypeReference[0] : new[] { new CodeTypeReference(elementType) }),
                invokeParamsCode.ToArray());

            if (method.ReturnType == typeof(void))
            {
                // If method returns nothing, just invoke.
                methodCode.Statements.Add(invokeCode);
            }
            else
            {
                // Add variable to save result.
                methodCode.Statements.Add(new CodeVariableDeclarationStatement(arrayType, "sqlQueryResult", invokeCode));
            }

            // Set out parameters.
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (parameter.IsOut)
                {
                    methodCode.Statements.Add(new CodeAssignStatement(
                        new CodeSnippetExpression(parameter.Name),
                        new CodeCastExpression(parameter.ParameterType.GetElementType(),
                            new CodeSnippetExpression(String.Format("{0}SqlParameter.Value != DBNull.Value ? {0}SqlParameter.Value : null", parameter.Name)))));
                }
            }

            // Return result is exist.
            if (method.ReturnType != typeof(void))
            {
                if (method.ReturnType.IsArray)
                {
                    // If method returns array, just return result.
                    methodCode.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("sqlQueryResult")));
                }
                else if (method.IsDefined(typeof(SingleResultRequiredAttribute), false))
                {
                    // If method returns single value and has SingleResultRequired attribute, return Single.
                    methodCode.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("sqlQueryResult.Single()")));
                }
                else
                {
                    // If method returns single value, return SingleOrDefault.
                    methodCode.Statements.Add(new CodeMethodReturnStatement(
                        new CodeSnippetExpression("sqlQueryResult.SingleOrDefault()")));
                }
            }

            // Add defined method to the class.
            classCode.Members.Add(methodCode);
        }

        private static CodeTypeDeclaration GetProxyClassCode(Type proceduresInterfaceType, string proxyTypeName)
        {
            // Declare class ProceduresProxy.
            var classCode = new CodeTypeDeclaration(proxyTypeName)
            {
                IsClass = true,
                Attributes = MemberAttributes.Public
            };

            // Inherit class from procedures definitions interface.
            classCode.BaseTypes.Add(proceduresInterfaceType);

            // Add SqlContext field to access wrapped context for executing procedures.
            classCode.Members.Add(new CodeMemberField(typeof(ISqlContext), "Context") { Attributes = MemberAttributes.Public });

            // Implement procedure definitions interface.
            foreach (MethodInfo method in proceduresInterfaceType.GetInterfaceMethods())
            {
                AddProxyClassMethod(method, classCode);
            }

            return classCode;
        }

        private static string GetSchemaName(MemberInfo memberInfo)
        {
            object[] attributes = memberInfo.GetCustomAttributes(typeof(SchemaAttribute), true);
            return attributes.Length > 0 ? ((SchemaAttribute)attributes[0]).Name : null;
        }
    }
}