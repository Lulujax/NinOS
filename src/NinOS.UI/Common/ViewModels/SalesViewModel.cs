using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class SalesViewModel : ViewModelBase
    {
        private readonly IAccountsReceivableService _receivable_service;
        private string _selected_month = string.Empty;
        private seller? _selected_seller;
        private decimal _total_sales_usd;
        private bool _is_loading;

        public ObservableCollection<string> pending_months { get; }
        public ObservableCollection<seller> sellers { get; }
        public ObservableCollection<accounts_receivable_dto> notes { get; }

        public string selected_month
        {
            get { return _selected_month; }
            set { if (_selected_month == value) return; _selected_month = value; on_property_changed(); if (!string.IsNullOrEmpty(_selected_month)) load_data_async(); }
        }

        public seller? selected_seller
        {
            get { return _selected_seller; }
            set { _selected_seller = value; on_property_changed(); load_data_async(); }
        }

        public decimal total_sales_usd
        {
            get { return _total_sales_usd; }
            private set { _total_sales_usd = value; on_property_changed(); }
        }

        public ICommand preview_note_command { get; }
        public ICommand print_pdf_command { get; }

        public Action<accounts_receivable_dto>? on_request_preview_window;

        public SalesViewModel(IAccountsReceivableService receivable_service)
        {
            _receivable_service = receivable_service ?? throw new ArgumentNullException(nameof(receivable_service));
            pending_months = new ObservableCollection<string>();
            sellers = new ObservableCollection<seller>();
            notes = new ObservableCollection<accounts_receivable_dto>();

            preview_note_command = new RelayCommand(execute_preview_note);
            print_pdf_command = new RelayCommand(execute_print_pdf);

            load_initial_data_async();
        }

        public void refresh_data() => load_initial_data_async();

        public async Task<note_print_dto> get_printable_note_async(int id_delivery_note)
        {
            return await _receivable_service.get_printable_note_async(id_delivery_note);
        }

        private async void load_initial_data_async()
        {
            try
            {
                _is_loading = true;
                var sl = await _receivable_service.get_sellers_async();
                sellers.Clear();
                sellers.Add(new seller("Todos", "-", "-") { id_seller = 0 });
                foreach (var s in sl) sellers.Add(s);

                var months = await _receivable_service.get_pending_months_async();
                pending_months.Clear();
                foreach (var m in months) pending_months.Add(m);

                _is_loading = false;

                if (pending_months.Any()) selected_month = pending_months.First();
                else load_data_async();
            }
            catch (Exception ex) { _is_loading = false; System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error"); }
        }

        private async void load_data_async()
        {
            if (string.IsNullOrEmpty(selected_month) || _is_loading) return;
            try
            {
                _is_loading = true;
                var all = selected_seller != null && selected_seller.id_seller != 0
                    ? await _receivable_service.get_all_by_month_and_seller_async(selected_month, selected_seller.id_seller)
                    : await _receivable_service.get_all_by_month_async(selected_month);

                notes.Clear();
                foreach (var n in all.Where(n => n.status != "Anulada")) notes.Add(n);
                total_sales_usd = notes.Sum(n => n.total_amount_usd);
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error"); }
            finally { _is_loading = false; }
        }

        private void execute_preview_note(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
                on_request_preview_window?.Invoke(note);
        }

        private async void execute_print_pdf(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
            {
                try
                {
                    note_print_dto printable = await _receivable_service.get_printable_note_async(note.id_delivery_note);
                    NotePdfGenerator.generate(printable);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }
}
