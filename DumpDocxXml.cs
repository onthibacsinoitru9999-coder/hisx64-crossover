using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DumpDocxXml
{
    class Program
    {
        static void Main(string[] args)
        {
            string docxPath = @"d:\his\his-x64-28-11fix GDYK\his-x64\PT-01.docx";
            using (ZipArchive archive = ZipFile.OpenRead(docxPath))
            {
                var entry = archive.GetEntry("word/document.xml");
                using (var stream = entry.Open())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    File.WriteAllText("pt01_document.xml", reader.ReadToEnd(), Encoding.UTF8);
                }
            }
            Console.WriteLine("Saved pt01_document.xml successfully. Size: " + new FileInfo("pt01_document.xml").Length);
        }
    }
}
