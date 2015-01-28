using System.Xml.Serialization;

namespace InoSoft.Tools.SqlMigrations.ConsoleApp
{
    /// <summary>
    ///   Encapsulates a revision-version pair to be used for migration from a Sqlver repository.
    /// </summary>
    public class XmlPairModel
    {
        /// <summary>
        ///   Gets or sets the Sqlver revision number.
        /// </summary>
        /// <value>
        ///   The Sqlver revision number.
        /// </value>
        [XmlAttribute]
        public int Revision { get; set; }

        /// <summary>
        ///   Gets or sets the database schema version.
        /// </summary>
        /// <value>
        ///   The database schema version.
        /// </value>
        [XmlAttribute]
        public string Version { get; set; }
    }
}