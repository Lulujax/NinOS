using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing Excel generation...");

        var items = new List<Item>();
        for (int i = 1; i <= 200; i++)
        {
            items.Add(new Item
            {
                ProductCode = $"PROD-{i:D3}",
                Name = $"Producto de prueba {i} con caracteres especiales & < > \" y acentos áéíóú",
                Brand = i % 2 == 0 ? "DEFILE" : "BIOLINE",
                UnitPrice = 12.50m * i,
                PricesByKey = new Dictionary<string, decimal>
                {
                    { "1", 10.00m * i },
                    { "2", 11.25m * i }
                }
            });
        }

        var columns = new List<Col>
        {
            new Col { Key = "ORIGINAL", Name = "General", IsOriginal = true },
            new Col { Key = "1", Name = "Mayorista", IsOriginal = false },
            new Col { Key = "2", Name = "Especial", IsOriginal = false }
        };

        string xlsxPath = "test_output.xlsx";
        string csvPath = "test_output.csv";

        GenerateXlsx(xlsxPath, items, columns);
        Console.WriteLine($"XLSX generated: {new FileInfo(xlsxPath).Length} bytes");

        GenerateCsv(csvPath, items, columns);
        Console.WriteLine($"CSV generated: {new FileInfo(csvPath).Length} bytes");

        Console.WriteLine("ALL PASSED!");
    }

    class Item
    {
        public string ProductCode { get; set; } = "";
        public string Name { get; set; } = "";
        public string Brand { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public Dictionary<string, decimal> PricesByKey { get; set; } = new();
    }

    class Col
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsOriginal { get; set; }
    }

    static void GenerateCsv(string filePath, List<Item> items, List<Col> columns)
    {
        var lines = new List<string>();
        lines.Add("sep=;");

        var headers = new List<string> { "CÓDIGO", "PRODUCTO", "CATEGORÍA" };
        foreach (var col in columns)
        {
            headers.Add(col.Name.ToUpperInvariant() + " (USD)");
        }
        lines.Add(string.Join(";", headers.Select(EscapeCsv)));

        foreach (var item in items)
        {
            var row = new List<string>
            {
                EscapeCsv(item.ProductCode),
                EscapeCsv(item.Name),
                EscapeCsv(item.Brand)
            };

            foreach (var col in columns)
            {
                decimal price = col.IsOriginal ? item.UnitPrice : (item.PricesByKey.TryGetValue(col.Key, out decimal p) ? p : item.UnitPrice);
                row.Add(price.ToString("N2", CultureInfo.InvariantCulture));
            }

            lines.Add(string.Join(";", row));
        }

        File.WriteAllLines(filePath, lines, new UTF8Encoding(true));
    }

    static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(";") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    static void GenerateXlsx(string filePath, List<Item> items, List<Col> columns)
    {
        if (File.Exists(filePath)) File.Delete(filePath);

        using var zip = ZipFile.Open(filePath, ZipArchiveMode.Create);

        var contentTypesEntry = zip.CreateEntry("[Content_Types].xml", CompressionLevel.Fastest);
        using (var writer = new StreamWriter(contentTypesEntry.Open(), Encoding.UTF8))
        {
            writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
</Types>");
        }

        var relsEntry = zip.CreateEntry("_rels/.rels", CompressionLevel.Fastest);
        using (var writer = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
        {
            writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");
        }

        var wbRelsEntry = zip.CreateEntry("xl/_rels/workbook.xml.rels", CompressionLevel.Fastest);
        using (var writer = new StreamWriter(wbRelsEntry.Open(), Encoding.UTF8))
        {
            writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>");
        }

        var wbEntry = zip.CreateEntry("xl/workbook.xml", CompressionLevel.Fastest);
        using (var writer = new StreamWriter(wbEntry.Open(), Encoding.UTF8))
        {
            writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Lista de Precios"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>");
        }

        var stylesEntry = zip.CreateEntry("xl/styles.xml", CompressionLevel.Fastest);
        using (var writer = new StreamWriter(stylesEntry.Open(), Encoding.UTF8))
        {
            writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""2"">
    <font><sz val=""11""/><name val=""Calibri""/></font>
    <font><b/><sz val=""11""/><name val=""Calibri""/></font>
  </fonts>
  <fills count=""2"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
  </fills>
  <borders count=""1"">
    <border><left/><right/><top/><bottom/><diagonal/></border>
  </borders>
  <cellStyleXfs count=""1"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/>
  </cellStyleXfs>
  <cellXfs count=""3"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""2"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
  </cellXfs>
</styleSheet>");
        }

        var sheetEntry = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest);
        using (var writer = new StreamWriter(sheetEntry.Open(), Encoding.UTF8))
        {
            writer.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
            writer.WriteLine(@"<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");
            writer.WriteLine(@"  <cols>");
            writer.WriteLine(@"    <col min=""1"" max=""1"" width=""18"" customWidth=""1""/>");
            writer.WriteLine(@"    <col min=""2"" max=""2"" width=""45"" customWidth=""1""/>");
            writer.WriteLine(@"    <col min=""3"" max=""3"" width=""22"" customWidth=""1""/>");
            int totalCols = 3 + columns.Count;
            writer.WriteLine($@"    <col min=""4"" max=""{Math.Max(4, totalCols)}"" width=""18"" customWidth=""1""/>");
            writer.WriteLine(@"  </cols>");
            writer.WriteLine(@"  <sheetData>");

            writer.Write(@"    <row r=""1"">");
            writer.Write($@"<c r=""A1"" t=""inlineStr"" s=""1""><is><t>{EscapeXml("CÓDIGO")}</t></is></c>");
            writer.Write($@"<c r=""B1"" t=""inlineStr"" s=""1""><is><t>{EscapeXml("PRODUCTO")}</t></is></c>");
            writer.Write($@"<c r=""C1"" t=""inlineStr"" s=""1""><is><t>{EscapeXml("CATEGORÍA")}</t></is></c>");

            for (int c = 0; c < columns.Count; c++)
            {
                string colLetter = GetColumnLetter(4 + c);
                string colName = columns[c].Name.ToUpperInvariant() + " (USD)";
                writer.Write($@"<c r=""{colLetter}1"" t=""inlineStr"" s=""1""><is><t>{EscapeXml(colName)}</t></is></c>");
            }
            writer.WriteLine(@"</row>");

            int rowIndex = 2;
            foreach (var item in items)
            {
                writer.Write($@"    <row r=""{rowIndex}"">");
                writer.Write($@"<c r=""A{rowIndex}"" t=""inlineStr"" s=""0""><is><t>{EscapeXml(item.ProductCode)}</t></is></c>");
                writer.Write($@"<c r=""B{rowIndex}"" t=""inlineStr"" s=""0""><is><t>{EscapeXml(item.Name)}</t></is></c>");
                writer.Write($@"<c r=""C{rowIndex}"" t=""inlineStr"" s=""0""><is><t>{EscapeXml(item.Brand)}</t></is></c>");

                for (int c = 0; c < columns.Count; c++)
                {
                    string colLetter = GetColumnLetter(4 + c);
                    var col = columns[c];
                    decimal price = col.IsOriginal ? item.UnitPrice : (item.PricesByKey.TryGetValue(col.Key, out decimal p) ? p : item.UnitPrice);
                    string valStr = price.ToString("0.00", CultureInfo.InvariantCulture);
                    writer.Write($@"<c r=""{colLetter}{rowIndex}"" s=""2""><v>{valStr}</v></c>");
                }

                writer.WriteLine(@"</row>");
                rowIndex++;
            }

            writer.WriteLine(@"  </sheetData>");
            writer.WriteLine(@"</worksheet>");
        }
    }

    static string GetColumnLetter(int colIndex)
    {
        string result = "";
        while (colIndex > 0)
        {
            int modulo = (colIndex - 1) % 26;
            result = Convert.ToChar(65 + modulo) + result;
            colIndex = (colIndex - modulo) / 26;
        }
        return result;
    }

    static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return SecurityElement.Escape(text);
    }
}
