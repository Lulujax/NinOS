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
    public class AccountsReceivableViewModel : ViewModelBase
    {
        private readonly IAccountsReceivableService _receivable_service;

        private string _selected_month = string.Empty;
        private accounts_receivable_dto? _selected_note;
        private decimal _total_month_balance;
        private string _search_text = string.Empty;
        private seller? _selected_seller;
        private int _filter_mode = 0;
        private bool _is_loading;

        public ObservableCollection<string> pending_months { get; }
        public ObservableCollection<seller> sellers { get; }
        public ObservableCollection<accounts_receivable_dto> filtered_notes { get; }

        public string selected_month
        {
            get { return _selected_month; }
            set
            {
                if (_selected_month == value) return;
                _selected_month = value;
                on_property_changed();
                if (!string.IsNullOrEmpty(_selected_month))
                    load_month_data_async();
            }
        }

        public accounts_receivable_dto? selected_note
        {
            get { return _selected_note; }
            set { _selected_note = value; on_property_changed(); }
        }

        public decimal total_month_balance
        {
            get { return _total_month_balance; }
            private set { _total_month_balance = value; on_property_changed(); }
        }

        public string search_text
        {
            get { return _search_text; }
            set { _search_text = value; on_property_changed(); apply_filter(); }
        }

        public seller? selected_seller
        {
            get { return _selected_seller; }
            set { _selected_seller = value; on_property_changed(); load_month_data_async(); }
        }

        public int filter_mode
        {
            get { return _filter_mode; }
            set { _filter_mode = value; on_property_changed(); load_month_data_async(); }
        }

        public string filter_label => filter_mode switch
        {
            0 => "Por Cobrar",
            1 => "Anuladas",
            2 => "Todas",
            _ => "Por Cobrar"
        };

        public ICommand annul_note_command { get; }
        public ICommand preview_note_command { get; }
        public ICommand print_pdf_command { get; }
        public ICommand cycle_filter_command { get; }

        public Action<accounts_receivable_dto>? on_request_preview_window;
        public Action? on_request_confirmation_window;

        private readonly IPaymentService _payment_service;

        public AccountsReceivableViewModel(IAccountsReceivableService receivable_service, IPaymentService payment_service)
        {
            if (receivable_service == null) throw new ArgumentNullException(nameof(receivable_service));
            if (payment_service == null) throw new ArgumentNullException(nameof(payment_service));
            _receivable_service = receivable_service;
            _payment_service = payment_service;

            pending_months = new ObservableCollection<string>();
            sellers = new ObservableCollection<seller>();
            filtered_notes = new ObservableCollection<accounts_receivable_dto>();

            annul_note_command = new RelayCommand(execute_annul_note);
            preview_note_command = new RelayCommand(execute_preview_note);
            print_pdf_command = new RelayCommand(execute_print_pdf);
            cycle_filter_command = new RelayCommand(_ => { filter_mode = (filter_mode + 1) % 3; });

            load_initial_data_async();
        }

        public void refresh_data()
        {
            load_initial_data_async();
        }

        private async void load_initial_data_async()
        {
            try
            {
                _is_loading = true;
                var seller_list = await _receivable_service.get_sellers_async();
                sellers.Clear();
                sellers.Add(new seller("Todos", "-", "-") { id_seller = 0 });
                foreach (var s in seller_list) sellers.Add(s);

                var months = await _receivable_service.get_pending_months_async();
                pending_months.Clear();
                foreach (string m in months) pending_months.Add(m);

                _is_loading = false;

                if (pending_months.Any())
                    selected_month = pending_months.First();
                else
                    load_month_data_async();
            }
            catch (Exception ex)
            {
                _is_loading = false;
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void load_month_data_async()
        {
            if (string.IsNullOrEmpty(selected_month) || _is_loading) return;

            try
            {
                _is_loading = true;

                IEnumerable<accounts_receivable_dto> notes;
                if (filter_mode == 0)
                {
                    notes = selected_seller != null && selected_seller.id_seller != 0
                        ? await _receivable_service.get_receivables_by_month_and_seller_async(selected_month, selected_seller.id_seller)
                        : await _receivable_service.get_receivables_by_month_async(selected_month);
                }
                else
                {
                    notes = selected_seller != null && selected_seller.id_seller != 0
                        ? await _receivable_service.get_all_by_month_and_seller_async(selected_month, selected_seller.id_seller)
                        : await _receivable_service.get_all_by_month_async(selected_month);
                }

                var all_notes = notes.ToList();
                apply_filter_on(all_notes);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally { _is_loading = false; }
        }

        private void apply_filter()
        {
            if (_is_loading) return;
            load_month_data_async();
        }

        private void apply_filter_on(List<accounts_receivable_dto> source)
        {
            filtered_notes.Clear();

            IEnumerable<accounts_receivable_dto> result = source;

            if (filter_mode == 0)
                result = result.Where(n => n.status != "Anulada");
            else if (filter_mode == 1)
                result = result.Where(n => n.status == "Anulada");

            if (!string.IsNullOrWhiteSpace(search_text))
            {
                result = result.Where(n =>
                    n.note_number.Contains(search_text, StringComparison.OrdinalIgnoreCase) ||
                    n.customer_name.Contains(search_text, StringComparison.OrdinalIgnoreCase) ||
                    n.seller_name.Contains(search_text, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var note in result)
                filtered_notes.Add(note);

            total_month_balance = filtered_notes.Where(n => n.status != "Anulada").Sum(n => n.balance_due_usd);
        }

        private void execute_annul_note(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
            {
                selected_note = note;
                on_request_confirmation_window?.Invoke();
            }
        }

        public async Task confirm_annulation_async()
        {
            if (selected_note == null) return;
            try
            {
                await _receivable_service.annul_delivery_note_async(selected_note.id_delivery_note);
                selected_note.status = "Anulada";
                selected_note = null;
                load_month_data_async();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_preview_note(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
            {
                on_request_preview_window?.Invoke(note);
            }
        }

        public async Task<note_print_dto> get_printable_note_async(int id_delivery_note)
        {
            return await _receivable_service.get_printable_note_async(id_delivery_note);
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
