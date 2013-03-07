using System;
using System.Xml;

namespace InoSoft.Tools.Xml
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "/?" || args[0] == "--help" || args.Length % 2 != 1)
            {
                PrintUsage();
                return 0;
            }
            string path = args[0];
            var document = new XmlDocument();
            try
            {
                document.Load(path);
                Console.WriteLine("Opened the file '{0}'.", path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to open the file '{0}': {1}", path, ex.Message);
                return 1;
            }
            bool succeeded = true;
            for (int i = 1; i < args.Length; i += 2)
            {
                try
                {
                    Console.WriteLine("Setting the values for '{0}'...", args[i]);
                    int count = XmlHelper.SetValue(document, args[i], args[i + 1]);
                    Console.WriteLine("{0} node{1} affected.", count, count != 1 ? "s" : String.Empty);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to set the value: {0}", ex.Message);
                    succeeded = false;
                }
            }
            try
            {
                document.Save(path);
                Console.WriteLine("Saved the file '{0}'.", path);
                return succeeded ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save the file '{0}': {1}", path, ex.Message);
                return 1;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"    xmltools <filepath> <xpath> <value> [<xpath> <value> [...]]");
            Console.WriteLine(@"Example:");
            Console.WriteLine(@"    xmltools file.xml //el/@attr foo ""//el[@attr2 = \""bar\""]/@attr2"" ""foo bar""");
        }
    }
}