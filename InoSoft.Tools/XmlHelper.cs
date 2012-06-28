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
    }
}