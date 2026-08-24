using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace VerifyDocx
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string docxPath = @"d:\his\his-x64-28-11fix GDYK\his-x64\PT-01\1. HOANG HUU CUONG - PT-01.docx";
            using (ZipArchive archive = ZipFile.OpenRead(docxPath))
            {
                ZipArchiveEntry entry = archive.GetEntry("word/document.xml");
                using (Stream stream = entry.Open())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string xml = reader.ReadToEnd();
                    XDocument xdoc = XDocument.Parse(xml);
                    XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                    
                    int i = 0;
                    foreach (var p in xdoc.Descendants(w + "p"))
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var t in p.Descendants(w + "t"))
                        {
                            sb.Append(t.Value);
                        }
                        string line = sb.ToString().Trim();
                        if (!string.IsNullOrEmpty(line))
                        {
                            Console.WriteLine(string.Format("[{0}] {1}", i++, line));
                        }
                    }
                }
            }
        }
    }
}
