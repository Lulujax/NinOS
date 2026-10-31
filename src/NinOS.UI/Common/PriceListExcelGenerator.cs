using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using NinOS.Infrastructure.Common;
using NinOS.Infrastructure.Logging;

namespace NinOS.UI.Common
{
    public static class PriceListExcelGenerator
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly UTF8Encoding Utf8Bom = new UTF8Encoding(true);

        public static void generate(List<price_list_item> items, List<price_list_column> columns)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Guardar lista de precios",
                    Filter = "Libro de Excel (*.xlsx)|*.xlsx|Archivo CSV (*.csv)|*.csv",
                    FileName = $"Lista_Precios_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };

                Window? owner = Application.Current?.MainWindow;
                bool? result = owner != null ? saveDialog.ShowDialog(owner) : saveDialog.ShowDialog();
                if (result != true) return;

                string ext = Path.GetExtension(saveDialog.FileName).ToLowerInvariant();
                if (ext == ".csv")
                {
                    generate_csv(saveDialog.FileName, items, columns);
                }
                else
                {
                    generate_xlsx(saveDialog.FileName, items, columns);
                }

                AppDialog.Show("Lista de precios exportada exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLog.Error("Error al generar archivo Excel de lista de precios", ex);
                AppDialog.Show($"No se pudo exportar el archivo de Excel:\n\n{ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void generate_csv(string filePath, List<price_list_item> items, List<price_list_column> columns)
        {
            var lines = new List<string>();
            lines.Add("sep=;");

            var headers = new List<string> { "CÓDIGO", "PRODUCTO", "CATEGORÍA" };
            foreach (var col in columns)
            {
                headers.Add(col.name.ToUpperInvariant() + " (USD)");
            }
            lines.Add(string.Join(";", headers.Select(escape_csv)));

            foreach (var item in items)
            {
                var row = new List<string>
                {
                    escape_csv(item.product_code),
                    escape_csv(item.name),
                    escape_csv(item.brand)
                };

                foreach (var col in columns)
                {
                    decimal price = 0;
                    if (col.is_original)
                    {
                        price = item.unit_price_usd;
                    }
                    else if (item.prices_by_key != null && item.prices_by_key.TryGetValue(col.key, out decimal p))
                    {
                        price = p;
                    }
                    else
                    {
                        price = item.unit_price_usd;
                    }

                    row.Add(price.ToString("N2", CultureInfo.InvariantCulture));
                }

                lines.Add(string.Join(";", row));
            }

            File.WriteAllLines(filePath, lines, Utf8Bom);
        }

        private static string escape_csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private static void generate_xlsx(string filePath, List<price_list_item> items, List<price_list_column> columns)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using var zip = ZipFile.Open(filePath, ZipArchiveMode.Create);

            // 1. [Content_Types].xml
            var contentTypesEntry = zip.CreateEntry("[Content_Types].xml", CompressionLevel.Fastest);
            using (var writer = new StreamWriter(contentTypesEntry.Open(), Utf8NoBom))
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

            // 2. _rels/.rels
            var relsEntry = zip.CreateEntry("_rels/.rels", CompressionLevel.Fastest);
            using (var writer = new StreamWriter(relsEntry.Open(), Utf8NoBom))
            {
                writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");
            }

            // 3. xl/_rels/workbook.xml.rels
            var wbRelsEntry = zip.CreateEntry("xl/_rels/workbook.xml.rels", CompressionLevel.Fastest);
            using (var writer = new StreamWriter(wbRelsEntry.Open(), Utf8NoBom))
            {
                writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>");
            }

            // 4. xl/workbook.xml
            var wbEntry = zip.CreateEntry("xl/workbook.xml", CompressionLevel.Fastest);
            using (var writer = new StreamWriter(wbEntry.Open(), Utf8NoBom))
            {
                writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Lista de Precios"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>");
            }

            // 5. xl/styles.xml
            var stylesEntry = zip.CreateEntry("xl/styles.xml", CompressionLevel.Fastest);
            using (var writer = new StreamWriter(stylesEntry.Open(), Utf8NoBom))
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

            // 6. xl/worksheets/sheet1.xml
            var sheetEntry = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest);
            using (var writer = new StreamWriter(sheetEntry.Open(), Utf8NoBom))
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

                // Header row (r=1)
                writer.Write(@"    <row r=""1"">");
                writer.Write($@"<c r=""A1"" t=""inlineStr"" s=""1""><is><t>{escape_xml("CÓDIGO")}</t></is></c>");
                writer.Write($@"<c r=""B1"" t=""inlineStr"" s=""1""><is><t>{escape_xml("PRODUCTO")}</t></is></c>");
                writer.Write($@"<c r=""C1"" t=""inlineStr"" s=""1""><is><t>{escape_xml("CATEGORÍA")}</t></is></c>");

                for (int c = 0; c < columns.Count; c++)
                {
                    string colLetter = get_column_letter(4 + c);
                    string colName = columns[c].name.ToUpperInvariant() + " (USD)";
                    writer.Write($@"<c r=""{colLetter}1"" t=""inlineStr"" s=""1""><is><t>{escape_xml(colName)}</t></is></c>");
                }
                writer.WriteLine(@"</row>");

                // Data rows (r=2..)
                int rowIndex = 2;
                foreach (var item in items)
                {
                    writer.Write($@"    <row r=""{rowIndex}"">");
                    writer.Write($@"<c r=""A{rowIndex}"" t=""inlineStr"" s=""0""><is><t>{escape_xml(item.product_code)}</t></is></c>");
                    writer.Write($@"<c r=""B{rowIndex}"" t=""inlineStr"" s=""0""><is><t>{escape_xml(item.name)}</t></is></c>");
                    writer.Write($@"<c r=""C{rowIndex}"" t=""inlineStr"" s=""0""><is><t>{escape_xml(item.brand)}</t></is></c>");

                    for (int c = 0; c < columns.Count; c++)
                    {
                        string colLetter = get_column_letter(4 + c);
                        var col = columns[c];
                        decimal price = 0;
                        if (col.is_original)
                        {
                            price = item.unit_price_usd;
                        }
                        else if (item.prices_by_key != null && item.prices_by_key.TryGetValue(col.key, out decimal p))
                        {
                            price = p;
                        }
                        else
                        {
                            price = item.unit_price_usd;
                        }

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

        private static string get_column_letter(int colIndex)
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

        private static string escape_xml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return SecurityElement.Escape(text);
        }
    }
}
