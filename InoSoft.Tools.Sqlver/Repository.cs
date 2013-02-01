using System;
using System.Collections.Generic;

namespace InoSoft.Tools.Sqlver
{
    /// <summary>
    /// Serializable repository, which is collection of versions.
    /// </summary>
    public class Repository
    {
        /// <summary>
        /// Creates an instance of <see cref="Repository"/>.
        /// </summary>
        public Repository()
        {
            Versions = new List<string>();
        }

        public List<string> Versions { get; set; }

        /// <summary>
        /// Loads repository from XML file.
        /// </summary>
        /// <param name="path">Path to XML file.</param>
        /// <returns>Loaded repository or null depending on operation success.</returns>
        public static Repository FromFile(string path)
        {
            try
            {
                return XmlHelper.FromXml<Repository>(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Adds version to the repository.
        /// </summary>
        /// <param name="scriptPath">Path to SQL script, which is delta to the next version.</param>
        public void AddVersion(string scriptPath)
        {
            Versions.Add(scriptPath);
        }

        /// <summary>
        /// Saves repository to an XML file.
        /// </summary>
        /// <param name="path">Path to the XML file.</param>
        /// <returns>Value, indicating save success.</returns>
        public bool Save(string path)
        {
            try
            {
                XmlHelper.ToXml(this, path);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}