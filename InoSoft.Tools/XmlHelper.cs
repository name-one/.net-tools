using System;
using System.IO;
using System.Xml.Serialization;

namespace InoSoft.Tools
{
    public static class XmlHelper
    {
        public static T LoadObjectFromFile<T>(string path)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open))
            {
                return (T)new XmlSerializer(typeof(T)).Deserialize((Stream)fileStream);
            }
        }

        public static void SaveObjectToFile<T>(T obj, string path)
        {
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");
            using (FileStream fileStream = new FileStream(path, FileMode.Create))
            {
                new XmlSerializer(typeof(T)).Serialize((Stream)fileStream, (object)obj, namespaces);
            }
        }

        /// <summary>
        /// Deserializes an object from an XML file with specified path.
        /// </summary>
        /// <typeparam name="T">Type of the object to deserialize.</typeparam>
        /// <param name="path">Path of the file to deserialize object from.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="PathTooLongException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static T FromXml<T>(string path)
            where T : class
        {
            using (var file = File.OpenRead(path))
            {
                var ser = new XmlSerializer(typeof(T));
                var item = ser.Deserialize(file) as T;
                if (item == null)
                {
                    throw new InvalidDataException(String.Format("The file must contain a {0} instance.", typeof(T).Name));
                }
                return item;
            }
        }

        /// <summary>
        /// Serializes an object to an XML file with specified path. If the file and/or directory does not exist, creates them.
        /// If the file already exists, overwrites it.
        /// </summary>
        /// <typeparam name="T">Type of the object to serialize.</typeparam>
        /// <param name="item">Object to serialize</param>
        /// <param name="path">Path of the file to save the serialized object to.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="PathTooLongException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public static void ToXml<T>(T item, string path)
            where T : class
        {
            var directory = Path.GetDirectoryName(path);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            using (var file = File.Create(path))
            {
                var serializer = new XmlSerializer(typeof(T));
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");
                serializer.Serialize(file, item, namespaces);
            }
        }
    }
}