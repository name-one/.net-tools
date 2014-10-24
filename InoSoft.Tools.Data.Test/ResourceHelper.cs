using System;
using System.IO;
using System.Reflection;

namespace InoSoft.Tools.Data.Test
{
    internal class ResourceHelper
    {
        public static readonly string Root = typeof(ResourceHelper).Namespace;

        public static string ReadText(string path)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(String.Join(".", Root, path)))
            {
                if (stream == null)
                    return null;

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}