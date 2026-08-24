using System;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace ParseDocxXml
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            XDocument doc = XDocument.Load("pt01_document.xml");
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            int idx = 0;
            foreach (var p in doc.Descendants(w + "p"))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var t in p.Descendants(w + "t"))
                {
                    sb.Append(t.Value);
                }
                string text = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    Console.WriteLine(string.Format("[{0}] {1}", idx++, text));
                }
            }
        }
    }
}
