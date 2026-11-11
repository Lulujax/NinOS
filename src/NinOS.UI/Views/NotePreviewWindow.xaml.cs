using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using NinOS.Domain.ViewModels;
using NinOS.UI.Common;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace NinOS.UI.Views
{
    /// <summary>
    /// Ventana de confirmación y vista previa en alta fidelidad (WebView2 con PDF vectorial)
    /// mostrada al guardar una nueva nota de entrega.
    /// </summary>
    public partial class NotePreviewWindow : Window
    {
        public bool Confirmed { get; private set; }
        public bool PdfRequested { get; private set; }
        public bool ExcelRequested { get; private set; }

        private readonly note_print_dto _note;
        private readonly note_print_dto? _pair_original;
        private readonly bool _view_only;
        private string? _tempPdfPath;
        private bool _isDisposed;

        public NotePreviewWindow(note_print_dto note, bool view_only = false)
            : this(note, null, view_only)
        {
        }

        public NotePreviewWindow(note_print_dto note, note_print_dto? pair_original, bool view_only = false)
        {
            InitializeComponent();
            WindowSizingHelper.FitToScreen(this, startMaximizedIfScreenSmall: true);
            _note = note ?? throw new ArgumentNullException(nameof(note));
            _pair_original = pair_original;
            _view_only = view_only;

            string label = string.IsNullOrWhiteSpace(note.document_label) ? "Nota de Entrega" : note.document_label;
            TxtTitle.Text = $"Confirmar Creación - {label} {note.note_number}";
            Title = $"Vista Previa - {label} {note.note_number}";

            if (_view_only)
            {
                CancelButton.Content = "✕ Cerrar";
                SaveOnlyButton.Visibility = Visibility.Collapsed;
                SaveExcelButton.Visibility = Visibility.Collapsed;
                SavePdfButton.Content = "📄 Guardar como PDF";
            }

            Loaded += OnWindowLoaded;
            Closed += OnWindowClosed;
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var doc = _pair_original != null
                    ? NotePdfGenerator.build_document(_pair_original, _note)
                    : NotePdfGenerator.build_document(_note);

                _tempPdfPath = TempFileManager.GetTempFilePath("preview_nota", ".pdf");

                await Task.Run(() =>
                {
                    QuestPDF.Settings.License = LicenseType.Community;
                    doc.GeneratePdf(_tempPdfPath);
                });

                if (string.IsNullOrWhiteSpace(_tempPdfPath) || !File.Exists(_tempPdfPath))
                {
                    throw new FileNotFoundException("No se pudo generar el archivo PDF temporal para la vista previa.");
                }

                string userDataFolder = Path.Combine(Path.GetTempPath(), "NinOS", "WebView2Data");
                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await PdfWebView.EnsureCoreWebView2Async(environment);

                PdfWebView.NavigationCompleted += (s, args) =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                };

                PdfWebView.CoreWebView2.Navigate(_tempPdfPath);
            }
            catch (Exception ex)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                FallbackOverlay.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"[NotePreviewWindow Error] {ex}");
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            PdfRequested = false;
            DialogResult = false;
            Close();
        }

        private void OnSaveOnly(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            PdfRequested = false;
            ExcelRequested = false;
            DialogResult = true;
            Close();
        }

        private void OnSaveExcel(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            PdfRequested = false;
            ExcelRequested = true;
            DialogResult = true;
            Close();
        }

        private void OnSavePdf(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            PdfRequested = true;
            ExcelRequested = false;
            DialogResult = true;
            Close();
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                PdfWebView?.Dispose();
            }
            catch
            {
                // Ignorar error de disposición
            }

            if (!string.IsNullOrWhiteSpace(_tempPdfPath) && File.Exists(_tempPdfPath))
            {
                _ = Task.Run(async () =>
                {
                    for (int i = 0; i < 6; i++)
                    {
                        try
                        {
                            await Task.Delay(1000);
                            if (File.Exists(_tempPdfPath)) File.Delete(_tempPdfPath);
                            break;
                        }
                        catch
                        {
                            // Reintentar si WebView2 aún tiene el handle abierto
                        }
                    }
                });
            }
        }
    }
}
