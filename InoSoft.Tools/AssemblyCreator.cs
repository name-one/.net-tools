using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

namespace InoSoft.Tools
{
    public static class AssemblyCreator
    {
        /// <summary>
        /// Compiles temporary assembly.
        /// </summary>
        /// <param name="codeNamespace">Namespace to include in the assembly.</param>
        /// <param name="referencedAssemblies">Assemblies referenced by the assembly being compiled.</param>
        /// <returns>Compiled assembly.</returns>
        public static Assembly Create(CodeNamespace codeNamespace, Assembly[] referencedAssemblies)
        {
            var codeProvider = new CSharpCodeProvider();
            var compileUnit = new CodeCompileUnit();
            compileUnit.Namespaces.Add(codeNamespace);
            var compilerParameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
#if DEBUG
                IncludeDebugInformation = true,
#endif
            };
            foreach (var assemblyLocation in GetReferencedAssemblies(referencedAssemblies))
            {
                compilerParameters.ReferencedAssemblies.Add(assemblyLocation);
                compileUnit.ReferencedAssemblies.Add(assemblyLocation);
            }

#if DEBUG
            string sourcePath = Path.GetTempFileName() + ".cs";
            using (var writer = new StreamWriter(sourcePath))
            {
                codeProvider.GenerateCodeFromNamespace(codeNamespace, writer, null);
            }
            var compileResult = codeProvider.CompileAssemblyFromFile(compilerParameters, sourcePath);
            File.Delete(sourcePath);
#else
            var compileResult = codeProvider.CompileAssemblyFromDom(compilerParameters, compileUnit);
#endif
            return compileResult.CompiledAssembly;
        }

        /// <summary>
        /// Gets locations of all the assemblies referenced by the given collection of assemblies.
        /// </summary>
        /// <param name="assemblies">Assemblies, references of which need to be returned.</param>
        /// <returns>Locations of referenced assemblies.</returns>
        public static string[] GetReferencedAssemblies(Assembly[] assemblies)
        {
            var references = assemblies
                .SelectMany(assembly => assembly.GetReferencedAssemblies())
                .Select(assemblyName => Assembly.ReflectionOnlyLoad(assemblyName.FullName));
            return new HashSet<string>(assemblies.Concat(references).Select(a => a.Location)).ToArray();
        }
    }
}