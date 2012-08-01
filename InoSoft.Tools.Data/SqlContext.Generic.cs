using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;

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

            // Using interface type is required.
            if (!proceduresInterfaceType.IsInterface)
            {
                throw new Exception("Stored procedures definitions type must be an interface.");
            }

            // Create a namespace for the code being generated, add usings.
            var codeNamespace = new CodeNamespace("InoSoft.Tools.Data");
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Linq"));

            // Generate proxy class code and add it to the namespace.
            _proxyTypeName = "ProceduresProxy";
            var proxyClassCode = GetProxyClassCode(proceduresInterfaceType, _proxyTypeName);
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
            object procedures = _compiledAssembly.CreateInstance("InoSoft.Tools.Data." + _proxyTypeName);
            if (procedures == null)
                throw new Exception("Failed to create a proxy.");
            Procedures = (TProcedures)procedures;
            Procedures.GetType().GetField("Context").SetValue(Procedures, this);
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

            // Create custom model classes for those models that have Enum properties.
            var customModelsCode = new Dictionary<Type, CodeTypeDeclaration>();

            // Implement procedure definitions interface.
            foreach (var method in proceduresInterfaceType.GetMethods())
            {
                // Determine type of elements to return and appropriate array type (e.g. String and String[]).
                Type elementType = method.ReturnType.IsArray ? method.ReturnType.GetElementType() : method.ReturnType;
                Type arrayType = elementType.MakeArrayType();

                // Define method.
                var methodCode = new CodeMemberMethod
                {
                    Name = method.Name,
                    Attributes = MemberAttributes.Public,
                    ReturnType = new CodeTypeReference(method.ReturnType)
                };
                // Define parameters.
                var invokeParamsCode = new List<CodeExpression>();
                // SQL code for executing procedure with name.
                var sqlParamsString = new StringBuilder();
                foreach (var p in method.GetParameters())
                {
                    if (p.IsOut)
                    {
                        bool isStringParam = p.ParameterType.GetElementType() == typeof(string);

                        sqlParamsString.AppendFormat("@{0} output,", p.Name);
                        string sqlParamVar = String.Format("{0}SqlParameter", p.Name);
                        methodCode.Statements.Add(new CodeVariableDeclarationStatement(
                            typeof(SqlParameter), sqlParamVar,
                            new CodeSnippetExpression("new System.Data.SqlClient.SqlParameter()")));
                        methodCode.Statements.Add(new CodeAssignStatement(
                            new CodeSnippetExpression(String.Format("{0}.ParameterName", sqlParamVar)),
                            new CodeSnippetExpression(String.Format("\"{0}\"", p.Name))));
                        CodeExpression valueExpression;
                        if (isStringParam)
                        {
                            valueExpression = new CodeSnippetExpression("DBNull.Value");
                        }
                        else
                        {
                            valueExpression = new CodeDefaultValueExpression(new CodeTypeReference(p.ParameterType.GetElementType()));
                        }
                        methodCode.Statements.Add(new CodeAssignStatement(
                            new CodeSnippetExpression(String.Format("{0}.Value", sqlParamVar)),
                            valueExpression));
                        methodCode.Statements.Add(new CodeAssignStatement(
                            new CodeSnippetExpression(String.Format("{0}.Direction", sqlParamVar)),
                            new CodeSnippetExpression("System.Data.ParameterDirection.Output")));
                        if (isStringParam)
                        {
                            methodCode.Statements.Add(new CodeAssignStatement(
                                new CodeSnippetExpression(String.Format("{0}.Size", sqlParamVar)),
                                new CodeSnippetExpression("Int32.MaxValue")));
                        }
                    }
                    else
                    {
                        sqlParamsString.AppendFormat("@{0},", p.Name);
                    }
                }
                if (sqlParamsString.Length > 0)
                {
                    sqlParamsString.Length--;
                }
                invokeParamsCode.Add(new CodeSnippetExpression(String.Format("\"EXEC {0} {1}\"", method.Name, sqlParamsString)));
                // Actual parameters, tranferred via SqlParameters.
                foreach (var p in method.GetParameters())
                {
                    if (p.IsOut)
                    {
                        invokeParamsCode.Add(new CodeSnippetExpression(String.Format("{0}SqlParameter", p.Name)));
                        var paramCode = new CodeParameterDeclarationExpression(p.ParameterType.GetElementType(), p.Name)
                        {
                            Direction = FieldDirection.Out
                        };
                        methodCode.Parameters.Add(paramCode);
                    }
                    else
                    {
                        string paramCasted = p.ParameterType.IsEnum ? String.Format("(({0}){1})", Enum.GetUnderlyingType(p.ParameterType), p.Name) : p.Name;
                        if (p.ParameterType == typeof(string))
                        {
                            invokeParamsCode.Add(new CodeSnippetExpression(String.Format(
                                "new System.Data.SqlClient.SqlParameter(\"{0}\", {0} != null ? (object){0} : DBNull.Value)", p.Name)));
                        }
                        else if (p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            invokeParamsCode.Add(new CodeSnippetExpression(String.Format(
                                "new System.Data.SqlClient.SqlParameter(\"{0}\", {0}.HasValue ? (object){1}.Value : DBNull.Value)", p.Name, paramCasted)));
                        }
                        else
                        {
                            invokeParamsCode.Add(new CodeSnippetExpression(String.Format(
                                "new System.Data.SqlClient.SqlParameter(\"{0}\", {1})", p.Name, paramCasted)));
                        }
                        methodCode.Parameters.Add(new CodeParameterDeclarationExpression(p.ParameterType, p.Name));
                    }
                }

                // Check if element type contains Enum properties.
                bool cloneNeeded = false;
                CodeTypeDeclaration customModel;
                if (customModelsCode.TryGetValue(elementType, out customModel))
                {
                    cloneNeeded = true;
                }
                else if (elementType.IsClass)
                {
                    if (elementType.ContainsEnums())
                    {
                        customModel = EnumlessTypeHelper.GetEnumlessClassCode(elementType);
                        customModelsCode.Add(elementType, customModel);
                        cloneNeeded = true;
                    }
                }
                var modelTypeRef = cloneNeeded ? new CodeTypeReference(customModel.Name) : new CodeTypeReference(elementType);

                // Invoke SQL query.
                var invokeCode = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(
                    new CodeSnippetExpression("Context"), "Execute",
                    elementType == typeof(void) ? new CodeTypeReference[0] : new[] { modelTypeRef }),
                    invokeParamsCode.ToArray());

                // Clone result if needed.
                if (cloneNeeded)
                {
                    invokeCode = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(
                        new CodeTypeReferenceExpression(typeof(ReflectionHelper)), "CloneArray",
                        new[] { modelTypeRef, new CodeTypeReference(elementType) }), invokeCode);
                }

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
                foreach (var p in method.GetParameters())
                {
                    if (p.IsOut)
                    {
                        methodCode.Statements.Add(new CodeAssignStatement(
                            new CodeSnippetExpression(p.Name),
                            new CodeCastExpression(p.ParameterType.GetElementType(),
                                new CodeSnippetExpression(String.Format("{0}SqlParameter.Value", p.Name)))));
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

            // Add custom model definitions to the class.
            foreach (var kvp in customModelsCode)
            {
                classCode.Members.Add(kvp.Value);
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
        public BatchContext<TProcedures> CreateBatch()
        {
            // Create procedures proxy
            var batchProxy = _compiledAssembly.CreateInstance("InoSoft.Tools.Data." + _proxyTypeName);
            if (batchProxy == null)
                throw new Exception("Failed to create a proxy.");
            var batchContext = new BatchContext<TProcedures>(this, (TProcedures)batchProxy);
            batchProxy.GetType().GetField("Context").SetValue(batchProxy, batchContext);
            return batchContext;
        }
    }
}