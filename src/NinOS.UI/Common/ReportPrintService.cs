using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using NinOS.UI.Views;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Common
{
    public static class ReportPrintService
    {
        public static void DirectPrint(
            IDocument document,
            Window? owner = null,
            string title = "Documento",
            string defaultFileName = "documento.pdf")
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            try
            {
                var previewWindow = new PdfPreviewWindow(
                    document,
                    title,
                    defaultFileName,
                    onExportExcel: null,
                    autoPrint: true);

                if (owner != null)
                {
                    previewWindow.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    previewWindow.Owner = Application.Current.MainWindow;
                }

                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al preparar la impresión: {ErrorText.Get(ex)}", "Error de Impresión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void DirectPrintPdfFile(string pdfFilePath, Window? owner = null, string title = "Documento")
        {
            if (string.IsNullOrWhiteSpace(pdfFilePath) || !File.Exists(pdfFilePath))
            {
                AppDialog.Show("El archivo para imprimir no existe.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var previewWindow = new PdfPreviewWindow(
                    pdfFilePath,
                    title,
                    Path.GetFileName(pdfFilePath),
                    onExportExcel: null,
                    autoPrint: true);

                if (owner != null)
                {
                    previewWindow.Owner = owner;
                }
                else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    previewWindow.Owner = Application.Current.MainWindow;
                }

                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                // Fallback a intentar con el proceso del sistema
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = pdfFilePath,
                        Verb = "print",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                }
                catch
                {
                    AppDialog.Show($"No se pudo imprimir el documento: {ErrorText.Get(ex)}", "Error de Impresión", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
