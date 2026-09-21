using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace HisPt01UiUploader
{
    public static class WordCleaner
    {
        public static string EnsureCleanDocx(string originalDocxPath)
        {
            if (!File.Exists(originalDocxPath)) return originalDocxPath;

            try
            {
                // Create a clean copy so we don't destroy original if not needed
                string dir = Path.GetDirectoryName(originalDocxPath);
                string name = Path.GetFileNameWithoutExtension(originalDocxPath);
                string cleanPath = Path.Combine(dir, name + "_clean.docx");

                File.Copy(originalDocxPath, cleanPath, true);

                bool modified = false;
                using (ZipArchive zip = ZipFile.Open(cleanPath, ZipArchiveMode.Update))
                {
                    var entry = zip.GetEntry("word/document.xml");
                    if (entry != null)
                    {
                        string xmlContent;
                        using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                        {
                            xmlContent = reader.ReadToEnd();
                        }

                        if (xmlContent.IndexOf("Evaluation Only. Created with Aspose.Words", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // Remove the paragraph containing the watermark
                            string cleanedXml = Regex.Replace(xmlContent, 
                                @"<w:p\b[^>]*>(?:(?!</w:p>).)*?Evaluation Only\. Created with Aspose\.Words.*?</w:p>", 
                                string.Empty, 
                                RegexOptions.Singleline | RegexOptions.IgnoreCase);

                            // Fallback simple replace if paragraph regex missed
                            if (cleanedXml.IndexOf("Evaluation Only", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                cleanedXml = cleanedXml.Replace("Evaluation Only. Created with Aspose.Words. Copyright 2003-2011 Aspose Pty Ltd.", "");
                            }

                            entry.Delete();
                            var newEntry = zip.CreateEntry("word/document.xml", CompressionLevel.Optimal);
                            using (var writer = new StreamWriter(newEntry.Open(), Encoding.UTF8))
                            {
                                writer.Write(cleanedXml);
                            }
                            modified = true;
                        }
                    }
                }

                if (modified)
                {
                    Console.WriteLine("[+] Đã tự động loại bỏ watermark Aspose thành công: " + Path.GetFileName(cleanPath));
                    return cleanPath;
                }
                else
                {
                    File.Delete(cleanPath);
                    return originalDocxPath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[!] Lưu ý khi lọc watermark: " + ex.Message);
                return originalDocxPath;
            }
        }
    }
}
