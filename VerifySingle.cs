using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace VerifySingle
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string file = @"d:\his\his-x64-28-11fix GDYK\his-x64\PT-01\PHUNG MINH HA - PT-01.docx";
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            Console.WriteLine("==================================================================================");
            Console.WriteLine("FILE: " + Path.GetFileName(file));
            Console.WriteLine("==================================================================================");

            byte[] bytes;
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                bytes = ms.ToArray();
            }

            using (var ms = new MemoryStream(bytes))
            using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                ZipArchiveEntry entry = archive.GetEntry("word/document.xml");
                using (Stream stream = entry.Open())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string xml = reader.ReadToEnd();
                    XDocument xdoc = XDocument.Parse(xml);

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
                            Console.WriteLine(string.Format("[{0,2}] {1}", i++, line));
                        }
                    }
                }
            }
        }
    }
}
