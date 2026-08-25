using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace InspectTableStructure
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string docxPath = @"d:\his\his-x64-28-11fix GDYK\his-x64\PT-01.docx";
            byte[] bytes;
            using (var fs = new FileStream(docxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                    XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                    
                    int tblIdx = 0;
                    foreach (var tbl in xdoc.Descendants(w + "tbl"))
                    {
                        Console.WriteLine("=== TABLE " + tblIdx++ + " ===");
                        int rIdx = 0;
                        foreach (var tr in tbl.Elements(w + "tr"))
                        {
                            int cIdx = 0;
                            StringBuilder rowStr = new StringBuilder();
                            foreach (var tc in tr.Elements(w + "tc"))
                            {
                                StringBuilder cellText = new StringBuilder();
                                foreach (var t in tc.Descendants(w + "t"))
                                {
                                    cellText.Append(t.Value);
                                }
                                rowStr.Append(string.Format("[C{0}: '{1}'] ", cIdx++, cellText.ToString().Trim()));
                            }
                            Console.WriteLine(string.Format("R{0}: {1}", rIdx++, rowStr.ToString()));
                        }
                    }
                }
            }
        }
    }
}
