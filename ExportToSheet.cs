using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string mdPath = ""patient_table_report_markdown.md"";
        if (!File.Exists(mdPath))
        {
            Console.WriteLine(""File not found: "" + mdPath);
            return;
        }

        var lines = File.ReadAllLines(mdPath, Encoding.UTF8);
        List<string[]> rows = new List<string[]>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.Trim().StartsWith(""|"")) continue;
            if (line.Contains("":---"") || line.Contains(""---:"")) continue;

            var parts = line.Split('|');
            if (parts.Length < 9) continue;

            string col1 = CleanCell(parts[1]);
            string col2 = CleanCell(parts[2]);
            string col3 = CleanCell(parts[3]);
            string col4 = CleanCell(parts[4]);
            string col5 = CleanCell(parts[5]);
            string col6 = CleanCell(parts[6]);
            string col7 = CleanCell(parts[7]);
            string col8 = CleanCell(parts[8]);

            rows.Add(new string[] { col1, col2, col3, col4, col5, col6, col7, col8 });
        }

        Console.WriteLine(""Parsed "" + rows.Count + "" rows."");

        // 1. Export CSV (UTF-8 with BOM)
        using (var sw = new StreamWriter(""BaoCao_BenhNhan_Khoa57_26082026.csv"", false, new UTF8Encoding(true)))
        {
            foreach (var r in rows)
            {
                var escaped = r.Select(x => ""\"""" + x.Replace(""\"""", ""\""\"""") + ""\"""");
                sw.WriteLine(string.Join("","", escaped));
            }
        }
        Console.WriteLine(""Created: BaoCao_BenhNhan_Khoa57_26082026.csv"");

        // 2. Export TSV (UTF-8 with BOM)
        using (var sw = new StreamWriter(""BaoCao_BenhNhan_Khoa57_26082026.tsv"", false, new UTF8Encoding(true)))
        {
            foreach (var r in rows)
            {
                var escaped = r.Select(x => ""\"""" + x.Replace(""\"""", ""\""\"" "").Replace(""\t"", "" "") + ""\"""");
                sw.WriteLine(string.Join(""\t"", escaped));
            }
        }
        Console.WriteLine(""Created: BaoCao_BenhNhan_Khoa57_26082026.tsv"");

        // 3. Export Excel XML 2003 (.xls)
        StringBuilder sbXml = new StringBuilder();
        sbXml.AppendLine(""<?xml version=\""1.0\"" encoding=\""UTF-8\""?>"");
        sbXml.AppendLine(""<?mso-application progid=\""Excel.Sheet\""?>"");
        sbXml.AppendLine(""<Workbook xmlns=\""urn:schemas-microsoft-com:office:spreadsheet\"""");
        sbXml.AppendLine("" xmlns:o=\""urn:schemas-microsoft-com:office:office\"""");
        sbXml.AppendLine("" xmlns:x=\""urn:schemas-microsoft-com:office:excel\"""");
        sbXml.AppendLine("" xmlns:ss=\""urn:schemas-microsoft-com:office:spreadsheet\"""");
        sbXml.AppendLine("" xmlns:html=\""http://www.w3.org/TR/REC-html40\"">"");
        sbXml.AppendLine("" <Styles>"");
        sbXml.AppendLine(""  <Style ss:ID=\""Default\"" ss:Name=\""Normal\"">"");
        sbXml.AppendLine(""   <Alignment ss:Vertical=\""Center\"" ss:WrapText=\""1\""/>"");
        sbXml.AppendLine(""   <Font ss:FontName=\""Segoe UI\"" ss:Size=\""10\""/>"");
        sbXml.AppendLine(""  </Style>"");
        sbXml.AppendLine(""  <Style ss:ID=\""Header\"">"");
        sbXml.AppendLine(""   <Alignment ss:Horizontal=\""Center\"" ss:Vertical=\""Center\"" ss:WrapText=\""1\""/>"");
        sbXml.AppendLine(""   <Borders>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Bottom\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#8EA9DB\""/>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Left\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#8EA9DB\""/>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Right\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#8EA9DB\""/>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Top\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#8EA9DB\""/>"");
        sbXml.AppendLine(""   </Borders>"");
        sbXml.AppendLine(""   <Font ss:FontName=\""Segoe UI\"" ss:Size=\""10\"" ss:Bold=\""1\"" ss:Color=\""#1F4E79\""/>"");
        sbXml.AppendLine(""   <Interior ss:Color=\""#D9E1F2\"" ss:Pattern=\""Solid\""/>"");
        sbXml.AppendLine(""  </Style>"");
        sbXml.AppendLine(""  <Style ss:ID=\""DataCell\"">"");
        sbXml.AppendLine(""   <Alignment ss:Vertical=\""Top\"" ss:WrapText=\""1\""/>"");
        sbXml.AppendLine(""   <Borders>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Bottom\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#D9D9D9\""/>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Left\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#D9D9D9\""/>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Right\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#D9D9D9\""/>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Top\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#D9D9D9\""/>"");
        sbXml.AppendLine(""   </Borders>"");
        sbXml.AppendLine(""   <Font ss:FontName=\""Segoe UI\"" ss:Size=\""10\""/>"");
        sbXml.AppendLine(""  </Style>"");
        sbXml.AppendLine(""  <Style ss:ID=\""DataCenter\"">"");
        sbXml.AppendLine(""   <Alignment ss:Horizontal=\""Center\"" ss:Vertical=\""Top\"" ss:WrapText=\""1\""/>"");
        sbXml.AppendLine(""   <Borders>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Bottom\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#D9D9D9\""/>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Left\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#D9D9D9\""/>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Right\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#D9D9D9\""/>"");
        sbXml.AppendLine(""    <Border ss:Position=\""Top\"" ss:LineStyle=\""Continuous\"" ss:Weight=\""1\"" ss:Color=\""#D9D9D9\""/>"");
        sbXml.AppendLine(""   </Borders>"");
        sbXml.AppendLine(""   <Font ss:FontName=\""Segoe UI\"" ss:Size=\""10\""/>"");
        sbXml.AppendLine(""  </Style>"");
        sbXml.AppendLine("" </Styles>"");
        sbXml.AppendLine("" <Worksheet ss:Name=\""Khoa 57 - 26.08.2026\"">"");
        sbXml.AppendLine(""  <Table>"");
        sbXml.AppendLine(""   <Column ss:Width=\""80\""/>"");
        sbXml.AppendLine(""   <Column ss:Width=\""130\""/>"");
        sbXml.AppendLine(""   <Column ss:Width=\""40\""/>"");
        sbXml.AppendLine(""   <Column ss:Width=\""45\""/>"");
        sbXml.AppendLine(""   <Column ss:Width=\""180\""/>"");
        sbXml.AppendLine(""   <Column ss:Width=\""180\""/>"");
        sbXml.AppendLine(""   <Column ss:Width=\""180\""/>"");
        sbXml.AppendLine(""   <Column ss:Width=\""180\""/>"");

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            bool isHeader = (i == 0);

            sbXml.AppendLine(""   <Row>"");
            for (int j = 0; j < r.Length; j++)
            {
                string cellVal = EscapeXml(r[j]);
                string cellStyle = isHeader ? ""Header"" : (j == 0 || j == 2 || j == 3 ? ""DataCenter"" : ""DataCell"");
                string cellType = (j == 2 && !isHeader && int.TryParse(cellVal, out _)) ? ""Number"" : ""String"");

                sbXml.AppendLine(string.Format(""    <Cell ss:StyleID=\""{0}\""><Data ss:Type=\""{1}\"">{2}</Data></Cell>"", cellStyle, cellType, cellVal));
            }
            sbXml.AppendLine(""   </Row>"");
        }

        sbXml.AppendLine(""  </Table>"");
        sbXml.AppendLine("" </Worksheet>"");
        sbXml.AppendLine(""</Workbook>"");

        File.WriteAllText(""BaoCao_BenhNhan_Khoa57_26082026.xls"", sbXml.ToString(), Encoding.UTF8);
        Console.WriteLine(""Created: BaoCao_BenhNhan_Khoa57_26082026.xls"");

        // 4. Export HTML
        StringBuilder sbHtml = new StringBuilder();
        sbHtml.AppendLine(""<!DOCTYPE html>"");
        sbHtml.AppendLine(""<html lang=\""vi\"">"");
        sbHtml.AppendLine(""<head>"");
        sbHtml.AppendLine(""  <meta charset=\""UTF-8\"">"");
        sbHtml.AppendLine(""  <title>Báo Cáo Bệnh Nhân Khoa 57 - 26/08/2026</title>"");
        sbHtml.AppendLine(""  <style>"");
        sbHtml.AppendLine(""    body { font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; background-color: #f8f9fa; }"");
        sbHtml.AppendLine(""    h2 { color: #1a365d; text-align: center; margin-bottom: 20px; }"");
        sbHtml.AppendLine(""    table { width: 100%; border-collapse: collapse; background: white; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }"");
        sbHtml.AppendLine(""    th, td { border: 1px solid #cbd5e1; padding: 10px 12px; font-size: 13px; vertical-align: top; }"");
        sbHtml.AppendLine(""    th { background-color: #e2e8f0; color: #0f172a; font-weight: 600; text-align: center; }"");
        sbHtml.AppendLine(""    tr:nth-child(even) { background-color: #f8fafc; }"");
        sbHtml.AppendLine(""    tr:hover { background-color: #f1f5f9; }"");
        sbHtml.AppendLine(""    .center { text-align: center; }"");
        sbHtml.AppendLine(""  </style>"");
        sbHtml.AppendLine(""</head>"");
        sbHtml.AppendLine(""<body>"");
        sbHtml.AppendLine(""  <h2>BÁO CÁO BỆNH NHÂN NỘI TRÚ KHOA CTCH &amp; CỘT SỐNG (KHOA 57) - 26/08/2026</h2>"");
        sbHtml.AppendLine(""  <table>"");
        
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            sbHtml.AppendLine(""    <tr>"");
            if (i == 0)
            {
                foreach (var c in r) sbHtml.AppendLine(""      <th>"" + c.Replace(""\n"", ""<br>"") + ""</th>"");
            }
            else
            {
                for (int j = 0; j < r.Length; j++)
                {
                    string alignClass = (j == 0 || j == 2 || j == 3) ? "" class=\""center\"""" : """";
                    sbHtml.AppendLine(string.Format(""      <td{0}>{1}</td>"", alignClass, r[j].Replace(""\n"", ""<br>"")));
                }
            }
            sbHtml.AppendLine(""    </tr>"");
        }
        sbHtml.AppendLine(""  </table>"");
        sbHtml.AppendLine(""</body>"");
        sbHtml.AppendLine(""</html>"");

        File.WriteAllText(""BaoCao_BenhNhan_Khoa57_26082026.html"", sbHtml.ToString(), Encoding.UTF8);
        Console.WriteLine(""Created: BaoCao_BenhNhan_Khoa57_26082026.html"");
    }

    static string CleanCell(string input)
    {
        if (string.IsNullOrEmpty(input)) return """";
        string s = input.Trim();
        s = s.Replace(""<br>"", ""\n"");
        s = s.Replace(""**"", """");
        return s.Trim();
    }

    static string EscapeXml(string input)
    {
        if (string.IsNullOrEmpty(input)) return """";
        return input.Replace(""&"", ""&amp;"").Replace(""<"", ""&lt;"").Replace("">"", ""&gt;"").Replace(""\"""", ""&quot;"").Replace(""'"", ""&apos;"");
    }
}
