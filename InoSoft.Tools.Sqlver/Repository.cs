using System;
using System.Collections.Generic;

namespace InoSoft.Tools.Sqlver
{
    public class Repository
    {
        public int LastVersion { get; set; }

        public List<string> Versions { get; set; }

        public Repository()
        {
            LastVersion = -1;
            Versions = new List<string>();
        }

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

        public void AddVersion(string scriptPath)
        {
            Versions.Add(scriptPath);
            LastVersion = Versions.Count - 1;
        }

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