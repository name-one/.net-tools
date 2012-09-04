using System;
using System.IO;
using System.Xml.Serialization;
using System.Net;

namespace InoSoft.Tools
{
    public static class XmlHelper
    {
        /// <summary>
        /// Deserializes an object from an XML data stream.
        /// </summary>
        /// <typeparam name="T">Type of the object to deserialize.</typeparam>
        /// <param name="stream">Stream to deserialize object from.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="InvalidDataException" />
        public static T FromXml<T>(Stream stream)
            where T : class
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            var ser = new XmlSerializer(typeof(T));
            var item = ser.Deserialize(stream) as T;
            if (item == null)
            {
                throw new InvalidDataException(String.Format("The file must contain a {0} instance.", typeof(T).Name));
            }
            return item;
        }

        /// <summary>
        /// Deserializes an object from an XML file with specified path.
        /// </summary>
        /// <typeparam name="T">Type of the object to deserialize.</typeparam>
        /// <param name="path">Path of the file to deserialize object from.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="IOException" />
        /// <exception cref="FileNotFoundException" />
        /// <exception cref="PathTooLongException" />
        /// <exception cref="UnauthorizedAccessException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="InvalidDataException" />
        /// <exception cref="NotSupportedException" />
        public static T FromXml<T>(string path)
            where T : class
        {
            if (path == null)
                throw new ArgumentNullException("path");

            using (var file = File.OpenRead(path))
            {
                return FromXml<T>(file);
            }
        }
		
		/// <summary>
        /// Deserializes an object from an XML, which is requested from specified URI via HTTP.
        /// </summary>
        /// <typeparam name="T">Type of the object to deserialize.</typeparam>
        /// <param name="uri">URI of the HTTP request.</param>
		public static T FromXml<T>(Uri uri)
            where T : class
        {
            if (uri == null)
                throw new ArgumentNullException("uri");
			
			var request = WebRequest.Create(uri);
			using (var response = request.GetResponse())
			{
				return FromXml<T>(response.GetResponseStream());				
			}
        }

        /// <summary>
        /// Serializes an object to an XML data stream.
        /// </summary>
        /// <typeparam name="T">Type of the object to serialize.</typeparam>
        /// <param name="item">Object to serialize</param>
        /// <param name="stream">Stream to write the serialized object to.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        public static void ToXml<T>(T item, Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            var serializer = new XmlSerializer(typeof(T));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");
            serializer.Serialize(stream, item, namespaces);
        }

        /// <summary>
        /// Serializes an object to an XML file with specified path. If the file and/or directory does not exist, creates them.
        /// If the file already exists, overwrites it.
        /// </summary>
        /// <typeparam name="T">Type of the object to serialize.</typeparam>
        /// <param name="item">Object to serialize</param>
        /// <param name="path">Path of the file to save the serialized object to.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="IOException" />
        /// <exception cref="DirectoryNotFoundException" />
        /// <exception cref="PathTooLongException" />
        /// <exception cref="UnauthorizedAccessException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="NotSupportedException" />
        public static void ToXml<T>(T item, string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            var directory = Path.GetDirectoryName(path);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            using (var file = File.Create(path))
            {
                ToXml(item, file);
            }
        }

        #region Obsolete

        /// <summary>
        /// Deserializes an object from an XML file with specified path.
        /// </summary>
        /// <typeparam name="T">Type of the object to deserialize.</typeparam>
        /// <param name="path">Path of the file to deserialize object from.</param>
        [Obsolete("Use XmlHelper.FromXml<T>(string) instead.")]
        public static T LoadObjectFromFile<T>(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open))
            {
                return (T)new XmlSerializer(typeof(T)).Deserialize(fileStream);
            }
        }

        /// <summary>
        /// Serializes an object to an XML file with specified path. If the file and/or directory does not exist, creates them.
        /// If the file already exists, overwrites it.
        /// </summary>
        /// <typeparam name="T">Type of the object to serialize.</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <param name="path">Path of the file to save the serialized object to.</param>
        [Obsolete("Use ToXml<T>(T, string) instead.")]
        public static void SaveObjectToFile<T>(T obj, string path)
        {
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                new XmlSerializer(typeof(T)).Serialize(fileStream, obj, namespaces);
            }
        }

        #endregion Obsolete
    }
}