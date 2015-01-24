using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InoSoft.Tools
{
    /// <summary>
    ///   Stores command line parameters in an easily readable format.
    /// </summary>
    public class CommandLineParameters
    {
        private readonly HashSet<string> _keys = new HashSet<string>();
        private readonly Dictionary<string, string> _named = new Dictionary<string, string>();
        private readonly List<string> _positional = new List<string>();

        /// <summary>
        ///   Gets the key-type parameters.
        /// </summary>
        /// <value>
        ///   The key-type parameters.
        /// </value>
        /// <remarks>
        ///   Key-type parameters are parameters that start with <c>'/'</c>.
        /// </remarks>
        public string[] Keys
        {
            get { return _keys.ToArray(); }
        }

        /// <summary>
        ///   Gets the names of the named parameters.
        /// </summary>
        /// <value>
        ///   The names of the named parameters.
        /// </value>
        public string[] NamedKeys
        {
            get { return _named.Keys.ToArray(); }
        }

        /// <summary>
        ///   Gets the positional parameters.
        /// </summary>
        /// <value>
        ///   An array containing the positional parameters in the original order.
        /// </value>
        public string[] Positional
        {
            get { return _positional.ToArray(); }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CommandLineParameters"/> class and
        ///   fills it with the parameters parsed from the specified arguments.
        /// </summary>
        /// <param name="args">The command line arguments to parse.</param>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="args"/> contains an item that is <c>null</c> or contains only whitespace.
        /// </exception>
        public static CommandLineParameters Read(IEnumerable<string> args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            var parameters = new CommandLineParameters();
            foreach (string arg in args)
            {
                parameters.Add(arg);
            }
            return parameters;
        }

        /// <summary>
        ///   Adds a new parameter parsed from the specified argument.
        /// </summary>
        /// <param name="arg">The argument to parse.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="arg"/> is <c>null</c> or contains only whitespace.
        /// </exception>
        public void Add(string arg)
        {
            if (String.IsNullOrWhiteSpace(arg))
                throw new ArgumentException("The argument cannot be null or contain only whitespace.", "arg");

            string[] parts = arg.Split('=');
            string name = parts[0].TrimStart('-', '/');
            if (parts.Length == 1)
            {
                if (parts[0].StartsWith("-") || parts[0].StartsWith("/"))
                {
                    _keys.Add(name);
                }
                else
                {
                    _positional.Add(parts[0]);
                }
            }
            else
            {
                _named.Add(name, String.Join("=", parts.Skip(1)));
            }
        }

        /// <summary>
        ///   Determines whether the one of specified keys is present in this instance.
        /// </summary>
        /// <param name="keys">The keys to look for.</param>
        /// <returns>
        ///   <c>true</c> if this instance contains the one of the specified keys; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="keys"/> is <c>null</c> or empty.
        /// </exception>
        public bool ContainsKeys(params string[] keys)
        {
            if (keys == null || keys.Length == 0)
                throw new ArgumentException("keys must be a non-empty array.", "keys");

            return keys.Any(key => _keys.Contains(key));
        }

        /// <summary>
        ///   Gets the value of a named parameter or the default value if the parameter is not found
        ///   or its value cannot be parsed.
        /// </summary>
        /// <typeparam name="T">The type to parse the parameter value to.</typeparam>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>
        ///   If <typeparamref name="T"/> is <see cref="String"/>, returns the value of the named parameter.
        ///   <br/>
        ///   If <typeparamref name="T"/> contains a static <c>bool TryParse(string, out T)</c> method
        ///   and the value can be parsed by it, returns the parsed value.
        ///   <br/>
        ///   If the named parameter is not found or the value cannot be parsed, returns <paramref name="defaultValue"/>.
        /// </returns>
        public T GetNamedValue<T>(string name, T defaultValue = default(T))
        {
            string value;
            if (!_named.TryGetValue(name, out value))
                return defaultValue;

            if (value is T)
                return (T)((object)value);

            Type type = typeof(T);
            MethodInfo tryParse = type.GetMethod("TryParse",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                null, new[] { typeof(string), type.MakeByRefType() }, null);
            if (tryParse == null || tryParse.ReturnType != typeof(bool))
                return defaultValue;

            object[] args = { value, default(T) };
            return (bool)tryParse.Invoke(null, args) ? (T)args[1] : defaultValue;
        }
    }
}