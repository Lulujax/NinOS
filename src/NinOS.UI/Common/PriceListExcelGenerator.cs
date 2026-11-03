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

        private static readonly Dictionary<string, (string hex, string softHex)> BrandStyles = new(StringComparer.OrdinalIgnoreCase)
        {
            { "DEFILE",          ("FF1B3A2D", "FFE8F0E4") },
            { "BIOLINE",         ("FF00695C", "FFE0F2F1") },
            { "REMBRANDT",       ("FF6A1B9A", "FFF3E5F5") },
            { "ESTILISTA",       ("FFE65100", "FFFFF3E0") },
            { "DEPIL CLEAR",     ("FFAD1457", "FFFCE4EC") },
            { "KEDAM",           ("FF4E342E", "FFEFEBE9") },
            { "OLEOS",           ("FF1565C0", "FFE3F2FD") },
            { "AMAZONIA SECRET", ("FF2E7D32", "FFE8F5E9") },
            { "CUTIQUE",         ("FFC62828", "FFFFEBEE") }
        };

        private static (string hex, string softHex) GetBrandHex(string brand)
        {
            if (BrandStyles.TryGetValue(brand, out var s)) return s;
            return ("FF546E7A", "FFECEFF1");
        }

        private static string SanitizeSheetName(string name, int index)
        {
            if (string.IsNullOrWhiteSpace(name)) return $"Lista {index}";
            var invalid = new[] { '\\', '/', '?', '*', ':', '[', ']' };
            string clean = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(clean)) return $"Lista {index}";
            if (clean.Length > 31) clean = clean.Substring(0, 31);
            return clean;
        }

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

                AppDialog.Show(
                    "Lista de precios exportada exitosamente.",
                    "Exportación completada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLog.Error("Error al generar archivo Excel de lista de precios", ex);
                AppDialog.Show(
                    $"No se pudo exportar el archivo de Excel:\n\n{ErrorText.Get(ex)}",
                    "Error al exportar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void generate_csv(string filePath, List<price_list_item> items, List<price_list_column> columns)
        {
            var lines = new List<string>();
            lines.Add("sep=;");

            var headers = new List<string> { "CÓDIGO", "PRODUCTO", "CATEGORÍA" };
            foreach (var col in columns)
            {
                if (columns.Count == 1 || col.is_original || string.Equals(col.name, "Principal", StringComparison.OrdinalIgnoreCase))
                {
                    headers.Add("PRECIO");
                }
                else
                {
                    headers.Add(col.name.Trim().ToUpperInvariant());
                }
            }
            lines.Add(string.Join(";", headers.Select(escape_csv)));

            var ordered = items
                .Where(i => !string.IsNullOrWhiteSpace(i.product_code))
                .OrderBy(i => i.brand)
                .ThenBy(i => i.product_code);

            foreach (var item in ordered)
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

            if (columns == null || columns.Count == 0)
            {
                columns = new List<price_list_column>
                {
                    new price_list_column { key = "ORIGINAL", name = "Principal", is_original = true }
                };
            }

            var ordered = items
                .Where(i => !string.IsNullOrWhiteSpace(i.product_code))
                .OrderBy(i => i.brand)
                .ThenBy(i => i.product_code)
                .ToList();

            var groups = ordered
                .GroupBy(i => string.IsNullOrWhiteSpace(i.brand) ? "OTROS" : i.brand.Trim().ToUpperInvariant())
                .OrderBy(g => g.Key)
                .ToList();

            var brandColorList = groups.Select(g => g.Key).Distinct().ToList();
            var brandFillMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Fills: 0=none, 1=gray125, 2=table header fill, 3=zebra tint
            int currentFillId = 4;
            foreach (var b in brandColorList)
            {
                brandFillMap[b] = currentFillId++;
            }

            using var zip = ZipFile.Open(filePath, ZipArchiveMode.Create);

            // 1. [Content_Types].xml
            var ct = zip.CreateEntry("[Content_Types].xml", CompressionLevel.Fastest);
            using (var w = new StreamWriter(ct.Open(), Utf8NoBom))
            {
                w.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
                w.WriteLine(@"<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">");
                w.WriteLine(@"  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>");
                w.WriteLine(@"  <Default Extension=""xml"" ContentType=""application/xml""/>");
                w.WriteLine(@"  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>");
                w.WriteLine(@"  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>");
                for (int i = 1; i <= columns.Count; i++)
                {
                    w.WriteLine($@"  <Override PartName=""/xl/worksheets/sheet{i}.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>");
                }
                w.WriteLine(@"</Types>");
            }

            // 2. _rels/.rels
            var rels = zip.CreateEntry("_rels/.rels", CompressionLevel.Fastest);
            using (var w = new StreamWriter(rels.Open(), Utf8NoBom))
            {
                w.Write(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");
            }

            // 3. xl/_rels/workbook.xml.rels
            var wbr = zip.CreateEntry("xl/_rels/workbook.xml.rels", CompressionLevel.Fastest);
            using (var w = new StreamWriter(wbr.Open(), Utf8NoBom))
            {
                w.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
                w.WriteLine(@"<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">");
                for (int i = 1; i <= columns.Count; i++)
                {
                    w.WriteLine($@"  <Relationship Id=""rId{i}"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet{i}.xml""/>");
                }
                int stylesRid = columns.Count + 1;
                w.WriteLine($@"  <Relationship Id=""rId{stylesRid}"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>");
                w.WriteLine(@"</Relationships>");
            }

            // 4. xl/workbook.xml (una hoja por cada lista seleccionada)
            var wb = zip.CreateEntry("xl/workbook.xml", CompressionLevel.Fastest);
            using (var w = new StreamWriter(wb.Open(), Utf8NoBom))
            {
                w.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
                w.WriteLine(@"<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">");
                w.WriteLine(@"  <sheets>");
                var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < columns.Count; i++)
                {
                    string sName = SanitizeSheetName(columns[i].name, i + 1);
                    if (usedSheetNames.Contains(sName)) sName = $"{sName} ({i + 1})";
                    usedSheetNames.Add(sName);
                    w.WriteLine($@"    <sheet name=""{escape_xml(sName)}"" sheetId=""{i + 1}"" r:id=""rId{i + 1}""/>");
                }
                w.WriteLine(@"  </sheets>");
                w.WriteLine(@"</workbook>");
            }

            // 5. xl/styles.xml
            var st = zip.CreateEntry("xl/styles.xml", CompressionLevel.Fastest);
            using (var w = new StreamWriter(st.Open(), Utf8NoBom))
            {
                w.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
                w.WriteLine(@"<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");

                // numFmts: 164 = "#,##0.00" (sin signo $)
                w.WriteLine(@"  <numFmts count=""1"">");
                w.WriteLine(@"    <numFmt numFmtId=""164"" formatCode=""#,##0.00""/>");
                w.WriteLine(@"  </numFmts>");

                // fonts
                w.WriteLine(@"  <fonts count=""6"">");
                w.WriteLine(@"    <font><sz val=""10""/><name val=""Segoe UI""/><color rgb=""FF222222""/></font>");              // 0: Normal
                w.WriteLine(@"    <font><b/><sz val=""14""/><name val=""Segoe UI""/><color rgb=""FF1B3A2D""/></font>");         // 1: Title
                w.WriteLine(@"    <font><i/><sz val=""9""/><name val=""Segoe UI""/><color rgb=""FF666666""/></font>");          // 2: Subtitle
                w.WriteLine(@"    <font><b/><sz val=""11""/><name val=""Segoe UI""/><color rgb=""FFFFFFFF""/></font>");         // 3: White bold banner
                w.WriteLine(@"    <font><b/><sz val=""10""/><name val=""Segoe UI""/><color rgb=""FF1B3A2D""/></font>");         // 4: Table header
                w.WriteLine(@"    <font><b/><sz val=""10""/><name val=""Segoe UI""/><color rgb=""FF222222""/></font>");         // 5: Code bold
                w.WriteLine(@"  </fonts>");

                // fills
                int totalFills = 4 + brandColorList.Count;
                w.WriteLine($@"  <fills count=""{totalFills}"">");
                w.WriteLine(@"    <fill><patternFill patternType=""none""/></fill>");                                           // 0: none
                w.WriteLine(@"    <fill><patternFill patternType=""gray125""/></fill>");                                        // 1: gray125
                w.WriteLine(@"    <fill><patternFill patternType=""solid""><fgColor rgb=""FFE8ECE9""/><bgColor indexed=""64""/></patternFill></fill>"); // 2: Table header fill
                w.WriteLine(@"    <fill><patternFill patternType=""solid""><fgColor rgb=""FFF8FAFC""/><bgColor indexed=""64""/></patternFill></fill>"); // 3: Zebra tint
                foreach (var b in brandColorList)
                {
                    var (hex, _) = GetBrandHex(b);
                    w.WriteLine($@"    <fill><patternFill patternType=""solid""><fgColor rgb=""{hex}""/><bgColor indexed=""64""/></patternFill></fill>");
                }
                w.WriteLine(@"  </fills>");

                // borders
                w.WriteLine(@"  <borders count=""2"">");
                w.WriteLine(@"    <border><left/><right/><top/><bottom/><diagonal/></border>");                                // 0: none
                w.WriteLine(@"    <border>");                                                                                   // 1: thin light gray
                w.WriteLine(@"      <left style=""thin""><color rgb=""FFD0D7DE""/></left>");
                w.WriteLine(@"      <right style=""thin""><color rgb=""FFD0D7DE""/></right>");
                w.WriteLine(@"      <top style=""thin""><color rgb=""FFD0D7DE""/></top>");
                w.WriteLine(@"      <bottom style=""thin""><color rgb=""FFD0D7DE""/></bottom>");
                w.WriteLine(@"    </border>");
                w.WriteLine(@"  </borders>");

                w.WriteLine(@"  <cellStyleXfs count=""1"">");
                w.WriteLine(@"    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/>");
                w.WriteLine(@"  </cellStyleXfs>");

                // cellXfs
                int totalXfs = 12 + brandColorList.Count;
                w.WriteLine($@"  <cellXfs count=""{totalXfs}"">");
                w.WriteLine(@"    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>"); // 0: default
                w.WriteLine(@"    <xf numFmtId=""0"" fontId=""1"" fillId=""0"" borderId=""0"" xfId=""0""><alignment vertical=""center""/></xf>"); // 1: Title
                w.WriteLine(@"    <xf numFmtId=""0"" fontId=""2"" fillId=""0"" borderId=""0"" xfId=""0""><alignment vertical=""center""/></xf>"); // 2: Subtitle
                w.WriteLine(@"    <xf numFmtId=""0"" fontId=""4"" fillId=""2"" borderId=""1"" xfId=""0""><alignment horizontal=""center"" vertical=""center""/></xf>"); // 3: Header center
                w.WriteLine(@"    <xf numFmtId=""0"" fontId=""4"" fillId=""2"" borderId=""1"" xfId=""0""><alignment horizontal=""left"" vertical=""center""/></xf>");   // 4: Header left
                w.WriteLine(@"    <xf numFmtId=""0"" fontId=""4"" fillId=""2"" borderId=""1"" xfId=""0""><alignment horizontal=""right"" vertical=""center""/></xf>");  // 5: Header right
                w.WriteLine(@"    <xf numFmtId=""0"" fontId=""5"" fillId=""0"" borderId=""1"" xfId=""0""><alignment horizontal=""center"" vertical=""center""/></xf>"); // 6: Data code
                w.WriteLine(@"    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0""><alignment horizontal=""left"" vertical=""center""/></xf>");   // 7: Data name
                w.WriteLine(@"    <xf numFmtId=""164"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0""><alignment horizontal=""right"" vertical=""center""/></xf>"); // 8: Data price
                w.WriteLine(@"    <xf numFmtId=""0"" fontId=""5"" fillId=""3"" borderId=""1"" xfId=""0""><alignment horizontal=""center"" vertical=""center""/></xf>"); // 9: Data code zebra
                w.WriteLine(@"    <xf numFmtId=""0"" fontId=""0"" fillId=""3"" borderId=""1"" xfId=""0""><alignment horizontal=""left"" vertical=""center""/></xf>");   // 10: Data name zebra
                w.WriteLine(@"    <xf numFmtId=""164"" fontId=""0"" fillId=""3"" borderId=""1"" xfId=""0""><alignment horizontal=""right"" vertical=""center""/></xf>"); // 11: Data price zebra

                foreach (var b in brandColorList)
                {
                    int fillId = brandFillMap[b];
                    w.WriteLine($@"    <xf numFmtId=""0"" fontId=""3"" fillId=""{fillId}"" borderId=""0"" xfId=""0""><alignment horizontal=""left"" vertical=""center"" indent=""1""/></xf>");
                }
                w.WriteLine(@"  </cellXfs>");
                w.WriteLine(@"</styleSheet>");
            }

            // 6. Cada lista seleccionada en su propia hoja: xl/worksheets/sheet{i}.xml
            for (int sheetIndex = 0; sheetIndex < columns.Count; sheetIndex++)
            {
                var col = columns[sheetIndex];
                int sheetNum = sheetIndex + 1;
                string listTitle = col.is_original || string.Equals(col.name, "Principal", StringComparison.OrdinalIgnoreCase)
                    ? "PRINCIPAL"
                    : col.name.Trim().ToUpperInvariant();

                string mainTitle = $"NINOS - LISTA DE PRECIOS - {listTitle}";

                var ws = zip.CreateEntry($"xl/worksheets/sheet{sheetNum}.xml", CompressionLevel.Fastest);
                using (var w = new StreamWriter(ws.Open(), Utf8NoBom))
                {
                    w.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
                    w.WriteLine(@"<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");

                    // Inmovilizar paneles en fila 4 (los títulos quedan fijos al scrollear)
                    bool isTabSelected = sheetIndex == 0;
                    string tabSelectedAttr = isTabSelected ? @" tabSelected=""1""" : "";
                    w.WriteLine(@"  <sheetViews>");
                    w.WriteLine($@"    <sheetView{tabSelectedAttr} workbookViewId=""0"">");
                    w.WriteLine(@"      <pane ySplit=""3"" topLeftCell=""A4"" activePane=""bottomLeft"" state=""frozen""/>");
                    w.WriteLine(@"    </sheetView>");
                    w.WriteLine(@"  </sheetViews>");

                    // Anchos de columna: A=16 (Código), B=52 (Producto), C=20 (Precio)
                    w.WriteLine(@"  <cols>");
                    w.WriteLine(@"    <col min=""1"" max=""1"" width=""16"" customWidth=""1""/>");
                    w.WriteLine(@"    <col min=""2"" max=""2"" width=""52"" customWidth=""1""/>");
                    w.WriteLine(@"    <col min=""3"" max=""3"" width=""20"" customWidth=""1""/>");
                    w.WriteLine(@"  </cols>");

                    w.WriteLine(@"  <sheetData>");

                    // Fila 1: Título dinámico específico de esta lista
                    w.WriteLine($@"    <row r=""1"" ht=""28"" customHeight=""1""><c r=""A1"" t=""inlineStr"" s=""1""><is><t>{escape_xml(mainTitle)}</t></is></c></row>");

                    // Fila 2: Subtítulo (discreto, solo fecha y hora)
                    string subtitle = $"ACTUALIZADO: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    w.WriteLine($@"    <row r=""2"" ht=""18"" customHeight=""1""><c r=""A2"" t=""inlineStr"" s=""2""><is><t>{escape_xml(subtitle)}</t></is></c></row>");

                    // Fila 3: Espaciador
                    w.WriteLine(@"    <row r=""3"" ht=""6"" customHeight=""1""/>");

                    int rowIdx = 4;
                    var mergeCells = new List<string>();

                    mergeCells.Add("A1:C1");
                    mergeCells.Add("A2:C2");

                    for (int gIdx = 0; gIdx < groups.Count; gIdx++)
                    {
                        var group = groups[gIdx];
                        string brandName = group.Key;
                        int brandXf = 12 + brandColorList.IndexOf(brandName);
                        var groupItems = group.ToList();

                        // Banner de Marca con color corporativo
                        string brandLabel = $"  {brandName} ({groupItems.Count} PRODUCTOS)";
                        w.WriteLine($@"    <row r=""{rowIdx}"" ht=""24"" customHeight=""1""><c r=""A{rowIdx}"" t=""inlineStr"" s=""{brandXf}""><is><t>{escape_xml(brandLabel)}</t></is></c></row>");
                        mergeCells.Add($"A{rowIdx}:C{rowIdx}");
                        rowIdx++;

                        // Cabecera de la tabla
                        w.WriteLine($@"    <row r=""{rowIdx}"" ht=""22"" customHeight=""1""><c r=""A{rowIdx}"" t=""inlineStr"" s=""3""><is><t>CÓDIGO</t></is></c><c r=""B{rowIdx}"" t=""inlineStr"" s=""4""><is><t>DESCRIPCIÓN DEL PRODUCTO</t></is></c><c r=""C{rowIdx}"" t=""inlineStr"" s=""5""><is><t>PRECIO</t></is></c></row>");
                        rowIdx++;

                        // Filas de productos con efecto cebra
                        bool zebra = false;
                        foreach (var item in groupItems)
                        {
                            int xfCode = zebra ? 9 : 6;
                            int xfName = zebra ? 10 : 7;
                            int xfPrice = zebra ? 11 : 8;
                            zebra = !zebra;

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

                            w.WriteLine($@"    <row r=""{rowIdx}"" ht=""20"" customHeight=""1""><c r=""A{rowIdx}"" t=""inlineStr"" s=""{xfCode}""><is><t>{escape_xml(item.product_code)}</t></is></c><c r=""B{rowIdx}"" t=""inlineStr"" s=""{xfName}""><is><t>{escape_xml(item.name)}</t></is></c><c r=""C{rowIdx}"" s=""{xfPrice}""><v>{valStr}</v></c></row>");
                            rowIdx++;
                        }

                        // Espaciador entre marcas
                        if (gIdx < groups.Count - 1)
                        {
                            w.WriteLine($@"    <row r=""{rowIdx}"" ht=""10"" customHeight=""1""/>");
                            rowIdx++;
                        }
                    }

                    w.WriteLine(@"  </sheetData>");

                    if (mergeCells.Count > 0)
                    {
                        w.WriteLine($@"  <mergeCells count=""{mergeCells.Count}"">");
                        foreach (var mc in mergeCells)
                        {
                            w.WriteLine($@"    <mergeCell ref=""{mc}""/>");
                        }
                        w.WriteLine(@"  </mergeCells>");
                    }

                    w.WriteLine(@"</worksheet>");
                }
            }
        }

        private static string escape_xml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return SecurityElement.Escape(text);
        }
    }
}
