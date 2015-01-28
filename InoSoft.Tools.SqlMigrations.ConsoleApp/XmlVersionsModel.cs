using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace InoSoft.Tools.SqlMigrations.ConsoleApp
{
    /// <summary>
    ///   Encapsulates revision-version pairs to be used for migration from a Sqlver repository.
    /// </summary>
    [XmlRoot(ElementName = "Versions")]
    public class XmlVersionsModel
    {
        /// <summary>
        ///   Gets or sets the revision-version pairs.
        /// </summary>
        /// <value>
        ///   The revision-version pairs.
        /// </value>
        [XmlElement("Pair")]
        public XmlPairModel[] Pairs { get; set; }

        /// <summary>
        ///   Reads revision-version pairs from an XML file at the specified path.
        /// </summary>
        /// <param name="path">The path to the XML file to read revision-version pairs from.</param>
        /// <returns>
        ///   A dictionary containing revision-version pairs read from the XML file.
        /// </returns>
        /// <exception cref="IOException">Failed to read revision-version pairs from an XML file.</exception>
        public static IDictionary<int, Version> Read(string path)
        {
            try
            {
                return XmlHelper.FromXml<XmlVersionsModel>(path)
                    .Pairs
                    .ToDictionary(v => v.Revision, v => Version.Parse(v.Version));
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to read revision-version pairs from an XML file.", ex);
            }
        }
    }
}