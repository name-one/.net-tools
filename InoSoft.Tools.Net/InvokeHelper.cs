using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using InoSoft.Tools.Serialization;
using Microsoft.CSharp;

namespace InoSoft.Tools.Net
{
    internal static class InvokeHelper
    {
        public static void ListenToInvoke(object instance, Stream stream, ICryptoTransform encryptor, ICryptoTransform decryptor)
        {
            int blockSize = encryptor == null ? 0 : encryptor.InputBlockSize;
            byte[] headBytes = stream.ReadAll(64, blockSize);
            if (decryptor != null)
            {
                headBytes = decryptor.Decrypt(headBytes);
            }
            byte nameLength = headBytes[0];
            string name = Encoding.Unicode.GetString(headBytes, 1, nameLength * 2);
            int argsLength = BitConverter.ToInt32(headBytes, 60);

            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(name);
            if (methodInfo != null)
            {
                ParameterInfo[] parametersInfo = methodInfo.GetParameters();
                object[] args = new object[parametersInfo.Length];
                byte[] argsBytes = stream.ReadAll(argsLength, blockSize);
                if (decryptor != null)
                {
                    argsBytes = decryptor.Decrypt(argsBytes);
                }
                MemoryStream memoryStream = new MemoryStream(argsBytes);
                BinaryReader reader = new BinaryReader(memoryStream);
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = Serializer.FromType(parametersInfo[i].ParameterType)
                        .DeserializeData(parametersInfo[i].ParameterType, reader);
                }
                reader.Close();

                memoryStream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(memoryStream);
                object result = null;

                try
                {
                    result = methodInfo.Invoke(instance, args);
                    SendInt(stream, encryptor, 0);
                }
                catch (RequestException ex)
                {
                    SendInt(stream, encryptor, 1);
                    SendInt(stream, encryptor, ex.ErrorCode);
                    return;
                }
                catch
                {
                    SendInt(stream, encryptor, 2);
                    return;
                }

                if (methodInfo.ReturnType != typeof(void))
                {
                    Serializer.FromType(methodInfo.ReturnType).SerializeData(result, writer);
                }
                byte[] resultBytes = memoryStream.ToArray();
                byte[] resultLengthBytes = BitConverter.GetBytes(resultBytes.Length);
                if (encryptor != null)
                {
                    resultLengthBytes = encryptor.Encrypt(resultLengthBytes);
                    resultBytes = encryptor.Encrypt(resultBytes);
                }
                writer.Close();

                stream.Write(resultLengthBytes, 0, resultLengthBytes.Length);
                stream.Write(resultBytes, 0, resultBytes.Length);
            }
            else
            {
                throw new Exception(string.Format("Method {0} does not exist", name));
            }
        }

        public static object Invoke(Type resultType, Type contractType, Stream stream, ICryptoTransform encryptor, ICryptoTransform decryptor, string name, params object[] args)
        {
            int blockSize = encryptor == null ? 0 : encryptor.InputBlockSize;
            MethodInfo methodInfo = contractType.GetMethod(name);
            if (methodInfo != null)
            {
                MemoryStream memoryStream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(memoryStream);
                ParameterInfo[] parametersInfo = methodInfo.GetParameters();
                for (int i = 0; i < args.Length; i++)
                {
                    Serializer.FromType(parametersInfo[i].ParameterType).SerializeData(args[i], writer);
                }
                byte[] argsBytes = memoryStream.ToArray();
                writer.Close();

                byte[] nameBytes = Encoding.Unicode.GetBytes(name);
                byte[] argsLengthBytes = BitConverter.GetBytes(argsBytes.Length);
                byte[] headBytes = new byte[64];
                headBytes[0] = (byte)name.Length;
                Array.Copy(nameBytes, 0, headBytes, 1, name.Length * 2);
                Array.Copy(argsLengthBytes, 0, headBytes, 60, 4);
                if (encryptor != null)
                {
                    headBytes = encryptor.Encrypt(headBytes);
                    argsBytes = encryptor.Encrypt(argsBytes);
                }

                stream.Write(headBytes, 0, headBytes.Length);
                stream.Write(argsBytes, 0, argsBytes.Length);

                byte[] resultLengthBytes = stream.ReadAll(4, blockSize);
                if (methodInfo.ReturnType != typeof(void))
                {
                    if (decryptor != null)
                    {
                        resultLengthBytes = decryptor.Decrypt(resultLengthBytes);
                    }
                    int resultLength = BitConverter.ToInt32(resultLengthBytes, 0);
                    byte[] resultBytes = stream.ReadAll(resultLength, blockSize);
                    if (decryptor != null)
                    {
                        resultBytes = decryptor.Decrypt(resultBytes);
                    }

                    int errorCode = ReceiveInt(stream, decryptor);
                    if (errorCode == 1)
                    {
                        errorCode = ReceiveInt(stream, decryptor);
                        throw new RequestException(errorCode);
                    }
                    else if (errorCode == 2)
                    {
                        throw new Exception("Remote method encountered unhandled exception.");
                    }

                    memoryStream = new MemoryStream(resultBytes);
                    BinaryReader reader = new BinaryReader(memoryStream);
                    object result = Serializer.FromType(methodInfo.ReturnType).DeserializeData(resultType, reader);
                    reader.Close();

                    return result;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                throw new Exception(string.Format("Method {0} does not exist", name));
            }
        }

        public static void SendInt(Stream stream, ICryptoTransform encryptor, int value)
        {
            int blockSize = encryptor == null ? 0 : encryptor.InputBlockSize;
            byte[] bytes = BitConverter.GetBytes(value);
            if (encryptor != null)
            {
                bytes = encryptor.Encrypt(bytes);
            }
            stream.Write(bytes, 0, bytes.Length);
        }

        public static int ReceiveInt(Stream stream, ICryptoTransform decryptor)
        {
            int blockSize = decryptor == null ? 0 : decryptor.InputBlockSize;
            byte[] clientIdBytes = stream.ReadAll(4, blockSize);
            if (decryptor != null)
            {
                clientIdBytes = decryptor.Decrypt(clientIdBytes);
            }
            return BitConverter.ToInt32(clientIdBytes, 0);
        }

        public static TContract CreateContractProxy<TContract>(Invocator invocator)
        {
            Type serviceContractType = typeof(TContract);
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CodeTypeDeclaration classCode = new CodeTypeDeclaration("Proxy")
            {
                IsClass = true,
                Attributes = MemberAttributes.Public
            };
            classCode.BaseTypes.Add(serviceContractType);
            classCode.Members.Add(new CodeMemberField(typeof(Invocator), "Invocator") { Attributes = MemberAttributes.Public });
            classCode.Members.Add(new CodeMemberField(typeof(Type), "ContractType") { Attributes = MemberAttributes.Public });
            foreach (var method in serviceContractType.GetMethods())
            {
                CodeMemberMethod methodCode = new CodeMemberMethod
                {
                    Name = method.Name,
                    Attributes = MemberAttributes.Public,
                    ReturnType = new CodeTypeReference(method.ReturnType)
                };

                CodeExpression[] paramsCode = method.GetParameters().Select(p => new CodeSnippetExpression(p.Name)).ToArray();
                List<CodeExpression> invokeParamsCode = new List<CodeExpression>();
                invokeParamsCode.Add(new CodeTypeOfExpression(method.ReturnType));
                invokeParamsCode.Add(new CodeSnippetExpression("ContractType"));
                invokeParamsCode.Add(new CodeSnippetExpression(string.Format("\"{0}\"", method.Name)));
                foreach (var p in method.GetParameters())
                {
                    invokeParamsCode.Add(new CodeSnippetExpression(p.Name));
                    methodCode.Parameters.Add(new CodeParameterDeclarationExpression(p.ParameterType, p.Name));
                }
                var invokeCode = new CodeMethodInvokeExpression(new CodeSnippetExpression("Invocator"), "Invoke", invokeParamsCode.ToArray());
                if (method.ReturnType == typeof(void))
                {
                    methodCode.Statements.Add(invokeCode);
                }
                else
                {
                    methodCode.Statements.Add(new CodeMethodReturnStatement(new CodeCastExpression(method.ReturnType, invokeCode)));
                }

                classCode.Members.Add(methodCode);
            }

            CodeNamespace namespaceCode = new CodeNamespace("InoSoft.Tools.Net");
            namespaceCode.Imports.Add(new CodeNamespaceImport("System"));
            namespaceCode.Types.Add(classCode);

            CodeCompileUnit compileUnit = new CodeCompileUnit();
            compileUnit.Namespaces.Add(namespaceCode);
            compileUnit.ReferencedAssemblies.Add("System.dll");
            compileUnit.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            compileUnit.ReferencedAssemblies.Add(Assembly.GetAssembly(typeof(TContract)).Location);
            CompilerParameters compilerParameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };
            var compileResult = codeProvider.CompileAssemblyFromDom(compilerParameters, compileUnit);
            var result = (TContract)compileResult.CompiledAssembly.CreateInstance("InoSoft.Tools.Net.Proxy");
            result.GetType().GetField("Invocator").SetValue(result, invocator);
            result.GetType().GetField("ContractType").SetValue(result, typeof(TContract));
            return result;
        }
    }
}