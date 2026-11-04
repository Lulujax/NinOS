using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using NinOS.UI.Common;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Views
{
    public partial class PdfPreviewWindow : Window
    {
        private readonly IDocument? _document;
        private string? _tempPdfPath;
        private readonly string _defaultPdfFileName;
        private readonly Action? _onExportExcel;
        private readonly bool _autoPrint;
        private bool _isDisposed;

        public PdfPreviewWindow(
            IDocument document,
            string title,
            string defaultPdfFileName = "documento.pdf",
            Action? onExportExcel = null,
            bool autoPrint = false)
        {
            InitializeComponent();
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _defaultPdfFileName = string.IsNullOrWhiteSpace(defaultPdfFileName) ? "documento.pdf" : defaultPdfFileName;
            _onExportExcel = onExportExcel;
            _autoPrint = autoPrint;

            TxtTitle.Text = title;
            Title = $"Vista Previa - {title}";

            if (_onExportExcel != null)
            {
                BtnDownloadExcel.Visibility = Visibility.Visible;
            }

            Loaded += OnWindowLoaded;
            Closed += OnWindowClosed;
        }

        public PdfPreviewWindow(
            string pdfFilePath,
            string title,
            string defaultPdfFileName = "documento.pdf",
            Action? onExportExcel = null,
            bool autoPrint = false)
        {
            InitializeComponent();
            _tempPdfPath = pdfFilePath;
            _defaultPdfFileName = string.IsNullOrWhiteSpace(defaultPdfFileName) ? "documento.pdf" : defaultPdfFileName;
            _onExportExcel = onExportExcel;
            _autoPrint = autoPrint;

            TxtTitle.Text = title;
            Title = $"Vista Previa - {title}";

            if (_onExportExcel != null)
            {
                BtnDownloadExcel.Visibility = Visibility.Visible;
            }

            Loaded += OnWindowLoaded;
            Closed += OnWindowClosed;
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_document != null && string.IsNullOrWhiteSpace(_tempPdfPath))
                {
                    _tempPdfPath = TempFileManager.GetTempFilePath("preview", ".pdf");
                    await Task.Run(() =>
                    {
                        QuestPDF.Settings.License = LicenseType.Community;
                        _document.GeneratePdf(_tempPdfPath);
                    });
                }

                if (string.IsNullOrWhiteSpace(_tempPdfPath) || !File.Exists(_tempPdfPath))
                {
                    throw new FileNotFoundException("No se pudo generar el archivo PDF temporal.");
                }

                string userDataFolder = Path.Combine(Path.GetTempPath(), "NinOS", "WebView2Data");
                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await PdfWebView.EnsureCoreWebView2Async(environment);

                PdfWebView.NavigationCompleted += OnNavigationCompleted;
                PdfWebView.CoreWebView2.Navigate(_tempPdfPath);
            }
            catch (Exception ex)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                FallbackOverlay.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"[WebView2 Error] {ex}");
            }
        }

        private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;

            if (_autoPrint && PdfWebView.CoreWebView2 != null)
            {
                try
                {
                    await Task.Delay(250);
                    PdfWebView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AutoPrint Error] {ex}");
                }
            }
        }

        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (PdfWebView.CoreWebView2 != null)
            {
                try
                {
                    PdfWebView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System);
                }
                catch (Exception ex)
                {
                    AppDialog.Show($"No se pudo abrir el diálogo de impresión: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (!string.IsNullOrWhiteSpace(_tempPdfPath) && File.Exists(_tempPdfPath))
            {
                ReportPrintService.DirectPrintPdfFile(_tempPdfPath, this);
            }
        }

        private void BtnDownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_tempPdfPath) || !File.Exists(_tempPdfPath))
            {
                AppDialog.Show("El archivo PDF aún no está listo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "Guardar documento PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = _defaultPdfFileName
            };

            if (saveDialog.ShowDialog(this) == true)
            {
                try
                {
                    File.Copy(_tempPdfPath, saveDialog.FileName, true);
                    AppDialog.Show("El archivo PDF se guardó exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    AppDialog.Show($"Error al guardar el archivo: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDownloadExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _onExportExcel?.Invoke();
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al exportar a Excel: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenExternal_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_tempPdfPath) && File.Exists(_tempPdfPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _tempPdfPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    AppDialog.Show($"No se pudo abrir el archivo: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                PdfWebView.Dispose();
            }
            catch
            {
                // Ignorar error al desechar WebView2
            }

            TempFileManager.TryDelete(_tempPdfPath);
        }
    }
}
