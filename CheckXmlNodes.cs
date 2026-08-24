using System;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace CheckXmlNodes
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            XDocument doc = XDocument.Load("pt01_document.xml");
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            foreach (var p in doc.Descendants(w + "p"))
            {
                string text = "";
                foreach (var t in p.Descendants(w + "t"))
                {
                    text += t.Value;
                }
                if (text.Contains("Họ và tên") || text.Contains("Ngày sinh") || text.Contains("Địa chỉ") ||
                    text.Contains("Vào viện") || text.Contains("Chẩn đoán") || text.Contains("Tiền sử") ||
                    text.Contains("Bệnh sử") || text.Contains("Thời gian") || text.Contains("Tóm tắt") ||
                    text.Contains("Các xét nghiệm") || text.Contains("Phương pháp phẫu thuật") ||
                    text.Contains("Phương pháp vô cảm") || text.Contains("Phẫu  thuật  viên") ||
                    text.Contains("Ngày, giờ phẫu thuật") || text.Contains("LÊ THỊ VINH"))
                {
                    Console.WriteLine("---------------------------------------------");
                    Console.WriteLine("TEXT: " + text);
                    Console.WriteLine("XML:  " + p.ToString());
                }
            }
        }
    }
}
