using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace InoSoft.Tools
{
    /// <summary>
    ///   Dynamically loads assemblies from embedded resources of an already loaded assembly.
    /// </summary>
    internal class AssemblyResourceLoader
    {
        private readonly Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>();
        private readonly Assembly _assembly;
        private readonly string _namespace;

        /// <summary>
        ///   Initializes a new instance of the <see cref="AssemblyResourceLoader"/> class with an assembly
        ///   and a default resource namespace to look for assemblies in. The default namespace is the name of
        ///   the assembly with <c>".Resources"</c> appended.
        /// </summary>
        /// <param name="assembly">The assembly to look for embedded resources in.</param>
        public AssemblyResourceLoader(Assembly assembly)
            : this(assembly, assembly.GetName().Name + ".Resources")
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AssemblyResourceLoader"/> class with an assembly
        ///   and a specified resource namespace to look for assemblies in.
        /// </summary>
        /// <param name="assembly">The assembly to look for embedded resources in.</param>
        /// <param name="resourceNamespace">The resource namespace to look for assemblies in.</param>
        public AssemblyResourceLoader(Assembly assembly, string resourceNamespace)
        {
            _assembly = assembly;
            _namespace = resourceNamespace;
        }

        /// <summary>
        ///   Gets the assembly to look for embedded resources in.
        /// </summary>
        /// <value>
        ///   The assembly to look for embedded resources in.
        /// </value>
        public Assembly Assembly
        {
            get { return _assembly; }
        }

        /// <summary>
        ///   Gets the resource namespace to look for assemblies in.
        /// </summary>
        /// <value>
        ///   The resource namespace to look for assemblies in.
        /// </value>
        public string Namespace
        {
            get { return _namespace; }
        }

        /// <summary>
        ///   Gets an assembly from embedded resources, loading it into the current <see cref="AppDomain"/>
        ///   if it has not been loaded yet.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly to get.</param>
        /// <returns>
        ///   The assembly with the specified name, or <c>null</c> if it is not found among the embedded resources.
        /// </returns>
        public Assembly GetAssembly(string assemblyName)
        {
            lock (_assemblies)
            {
                string name = new AssemblyName(assemblyName).Name;

                if (_assemblies.ContainsKey(name))
                    return _assemblies[name];

                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.FullName == assemblyName);
                if (loadedAssembly != null)
                {
                    _assemblies[name] = loadedAssembly;
                    return loadedAssembly;
                }

                return GetAssembly(name, String.Join(".", name, "dll"))
                    ?? GetAssembly(name, String.Join(".", name, "exe"));
            }
        }

        /// <summary>
        ///   Handles <see cref="AppDomain.AssemblyResolve"/> and <see cref="AppDomain.ReflectionOnlyAssemblyResolve"/>
        ///   events. Tries to resolve the assembly using the embedded resources.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The event data.</param>
        /// <returns>
        ///   The assembly with the specified name, or <c>null</c> if it is not found among the embedded resources.
        /// </returns>
        public Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args == null || args.Name == null)
                return null;

            return GetAssembly(args.Name);
        }

        /// <summary>
        ///   Reads contents of a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <returns>
        ///   A byte array with the contents of the stream.
        /// </returns>
        protected static byte[] ReadStream(Stream stream)
        {
            if (stream == null)
                return null;

            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            return data;
        }

        /// <summary>
        ///   Gets the assembly with the specified name, loading it into the <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>
        ///   An assembly with the specified name.
        /// </returns>
        protected virtual Assembly GetAssembly(string assemblyName, string fileName)
        {
            byte[] assembly = GetResource(fileName);
            return assembly != null ? LoadAssembly(assemblyName, assembly) : null;
        }

        /// <summary>
        ///   Gets the resource file with the specified name.
        /// </summary>
        /// <param name="fileName">The name of the resource file.</param>
        /// <returns>
        ///   A byte array with the contents of the resource file, or <c>null</c> if it is not found
        ///   among the embedded resources or could not be loaded.
        /// </returns>
        protected byte[] GetResource(string fileName)
        {
            try
            {
                using (Stream stream = GetResourceStream(fileName))
                {
                    return ReadStream(stream);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///   Gets a stream for the resource file with the specified name.
        /// </summary>
        /// <param name="fileName">The name of the resource file.</param>
        /// <returns>
        ///   A stream for the resource file.
        /// </returns>
        protected Stream GetResourceStream(string fileName)
        {
            return _assembly.GetManifestResourceStream(String.Join(".", _namespace, fileName));
        }

        /// <summary>
        ///   Loads an assembly from a byte array with its contents into the current <see cref="AppDomain"/>.
        ///   Tries to load all of the dependencies of the assembly.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly to load.</param>
        /// <param name="data">The contents of the assembly.</param>
        /// <returns>
        ///   A loaded assembly.
        /// </returns>
        protected Assembly LoadAssembly(string assemblyName, byte[] data)
        {
            Assembly assembly = Assembly.Load(data);
            _assemblies[assemblyName] = assembly;
            return assembly;
        }
    }
}