using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

class Prog {
    static void Main() {
        QuestPDF.Settings.License = LicenseType.Community;
        var style = TextStyle.Default.FontFamily("Arial").FontSize(8).Bold().Italic();
        var m1 = TextMeasurer.MeasureText("DESCUENTO 10% . CONTADO\nSOLO CONTRA DESPACHO", style);
        var m2 = TextMeasurer.MeasureText("DESCUENTO 10% . CONTADO", style);
        Console.WriteLine($"cond full: {m1.Width:F1}x{m1.Height:F1}");
        Console.WriteLine($"cond line: {m2.Width:F1}x{m2.Height:F1}");
    }
}
