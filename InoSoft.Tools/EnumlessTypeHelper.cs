using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InoSoft.Tools
{
    public static class EnumlessTypeHelper
    {
        private static readonly Dictionary<Type, Type> _enumlessProxies = new Dictionary<Type, Type>();
        private const BindingFlags ClonedPropertyBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty;

        public static bool ContainsEnums(this Type type)
        {
            return type.GetProperties()
                .Select(property => property.PropertyType)
                .Any(propertyType => (Nullable.GetUnderlyingType(propertyType) ?? propertyType).IsEnum);
        }

        public static CodeTypeDeclaration GetEnumlessClassCode(Type elementType)
        {
            var modelCode = new CodeTypeDeclaration(elementType.Name);
            foreach (var propertyInfo in elementType.GetProperties(ClonedPropertyBindingFlags))
            {
                CodeTypeReference typeReference;
                // Handle nullable type.
                Type nullableUnderlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType);
                if (nullableUnderlyingType != null && nullableUnderlyingType.IsEnum)
                {
                    typeReference = new CodeTypeReference(typeof(Nullable<>).Name,
                        new[] { new CodeTypeReference(Enum.GetUnderlyingType(nullableUnderlyingType)) });
                }
                else
                {
                    typeReference = new CodeTypeReference(propertyInfo.PropertyType.IsEnum
                        ? Enum.GetUnderlyingType(propertyInfo.PropertyType)
                        : propertyInfo.PropertyType);
                }
                // Create backing field for the property.
                var backingFieldCode = new CodeMemberField(typeReference, String.Format("m_{0}", propertyInfo.Name));
                // Create the prorepty.
                var property = new CodeMemberProperty
                {
                    Name = propertyInfo.Name,
                    Type = typeReference,
                    HasGet = true,
                    HasSet = true,
                    Attributes = MemberAttributes.Public
                };
                // Implement property getter and setter.
                property.GetStatements.Add(new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), backingFieldCode.Name)));
                property.SetStatements.Add(new CodeAssignStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), backingFieldCode.Name),
                    new CodePropertySetValueReferenceExpression()));
                // Add the property and its backing field to the class code.
                modelCode.Members.Add(backingFieldCode);
                modelCode.Members.Add(property);
            }
            return modelCode;
        }

        public static Type GetEnumlessProxy(this Type type)
        {
            lock (_enumlessProxies)
            {
                Type enumlessType;
                if (_enumlessProxies.TryGetValue(type, out enumlessType))
                    return enumlessType;

                enumlessType = CreateEnumlessProxy(type);
                _enumlessProxies.Add(type, enumlessType);
                return enumlessType;
            }
        }

        private static Type CreateEnumlessProxy(Type type)
        {
            // Create a namespace for the code being generated.
            var codeNamespace = new CodeNamespace("InoSoft.Tools.Data");
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Types.Add(GetEnumlessClassCode(type));
            Assembly proxyAssembly = AssemblyCreator.Create(codeNamespace, new[] { Assembly.GetAssembly(type) });
            return proxyAssembly.GetType("InoSoft.Tools.Data." + type.Name);
        }
    }
}