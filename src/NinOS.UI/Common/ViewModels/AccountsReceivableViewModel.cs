using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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

        public ObservableCollection<string> pending_months { get; }
        public ObservableCollection<accounts_receivable_dto> current_month_notes { get; }

        public string selected_month
        {
            get { return _selected_month; }
            set
            {
                if (_selected_month == value) return;
                _selected_month = value;
                on_property_changed();
                if (!string.IsNullOrEmpty(_selected_month))
                {
                    load_month_data_async();
                }
            }
        }

        public accounts_receivable_dto? selected_note
        {
            get { return _selected_note; }
            set
            {
                _selected_note = value;
                on_property_changed();
            }
        }

        public decimal total_month_balance
        {
            get { return _total_month_balance; }
            private set
            {
                _total_month_balance = value;
                on_property_changed();
            }
        }

        public ICommand annul_note_command { get; }
        public ICommand open_payment_modal_command { get; }
        public ICommand print_pdf_command { get; }

        public Action? on_request_payment_window;
        public Action? on_request_confirmation_window;
        public Action? on_refresh_requested;

        public AccountsReceivableViewModel(IAccountsReceivableService receivable_service)
        {
            if (receivable_service == null) throw new ArgumentNullException(nameof(receivable_service));
            _receivable_service = receivable_service;

            pending_months = new ObservableCollection<string>();
            current_month_notes = new ObservableCollection<accounts_receivable_dto>();

            annul_note_command = new RelayCommand(execute_annul_note);
            open_payment_modal_command = new RelayCommand(execute_open_payment_modal);
            print_pdf_command = new RelayCommand(execute_print_pdf);

            load_pending_months_async();
        }

        public void refresh_data()
        {
            load_pending_months_async();
        }

        private async void load_pending_months_async()
        {
            try
            {
                var months = await _receivable_service.get_pending_months_async();
                pending_months.Clear();
                foreach (string month in months)
                {
                    pending_months.Add(month);
                }

                if (pending_months.Any())
                {
                    selected_month = pending_months.First();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al cargar meses: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void load_month_data_async()
        {
            if (string.IsNullOrEmpty(selected_month)) return;

            try
            {
                var notes = await _receivable_service.get_receivables_by_month_async(selected_month);
                current_month_notes.Clear();
                foreach (var note in notes)
                {
                    current_month_notes.Add(note);
                }

                update_month_balance();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al cargar notas: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void update_month_balance()
        {
            total_month_balance = current_month_notes.Sum(n => n.balance_due_usd);
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
                current_month_notes.Remove(selected_note);
                update_month_balance();
                selected_note = null;
                
                if (!current_month_notes.Any())
                {
                    load_pending_months_async();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al anular nota: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void execute_open_payment_modal(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
            {
                selected_note = note;
                on_request_payment_window?.Invoke();
            }
        }

        private void execute_print_pdf(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
            {
                selected_note = note;
            }
        }
    }
}