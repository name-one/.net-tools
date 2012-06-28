using System;
using System.Collections.Generic;

namespace InoSoft.Tools.SqlVersion
{
    public class Repository
    {
        public int LastVersion { get; set; }

        public List<string> Versions { get; set; }

        public Repository()
        {
            this.LastVersion = -1;
            this.Versions = new List<string>();
        }

        public static Repository FromFile(string path)
        {
            try
            {
                return XmlHelper.LoadObjectFromFile<Repository>(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public void AddVersion(string scriptPath, bool hasSchema, bool hasData)
        {
            this.Versions.Add(scriptPath);
            ++this.LastVersion;
        }

        public bool Save(string path)
        {
            try
            {
                XmlHelper.SaveObjectToFile<Repository>(this, path);
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