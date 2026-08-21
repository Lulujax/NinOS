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
    public class PaymentsViewModel : ViewModelBase
    {
        private readonly IPaymentService _payment_service;
        private readonly IAccountsReceivableService _receivable_service;

        private string _selected_month = string.Empty;
        private seller? _selected_seller;
        private decimal _total_payments_usd;
        private bool _is_loading;

        public ObservableCollection<string> pending_months { get; }
        public ObservableCollection<seller> sellers { get; }
        public ObservableCollection<payment_dto> payments { get; }
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

        public decimal total_payments_usd
        {
            get { return _total_payments_usd; }
            private set { _total_payments_usd = value; on_property_changed(); }
        }

        public ICommand open_payment_command { get; }
        public ICommand open_note_history_command { get; }

        public Action<accounts_receivable_dto>? on_request_payment_window;
        public Action<int>? on_request_note_history_window;

        public PaymentsViewModel(IPaymentService payment_service, IAccountsReceivableService receivable_service)
        {
            _payment_service = payment_service ?? throw new ArgumentNullException(nameof(payment_service));
            _receivable_service = receivable_service ?? throw new ArgumentNullException(nameof(receivable_service));

            pending_months = new ObservableCollection<string>();
            sellers = new ObservableCollection<seller>();
            payments = new ObservableCollection<payment_dto>();
            notes = new ObservableCollection<accounts_receivable_dto>();

            open_payment_command = new RelayCommand(execute_open_payment);
            open_note_history_command = new RelayCommand(execute_open_note_history);

            load_initial_data_async();
        }

        public void refresh_data() => load_initial_data_async();

        public async Task confirm_payment_async(accounts_receivable_dto note, decimal amount_usd, decimal? exchange_rate, string payment_type, string reference_number, DateTime payment_date)
        {
            if (note == null) return;
            try
            {
                payment new_payment = new payment(
                    note.id_delivery_note, payment_date,
                    amount_usd, 0, exchange_rate, payment_type, reference_number);
                await _payment_service.register_payment_async(new_payment);
                load_data_async();
                System.Windows.MessageBox.Show("Pago registrado exitosamente.", "Exito");
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error"); }
        }

        public async Task<IEnumerable<payment_dto>> get_payments_by_note_async(int id_delivery_note)
        {
            return await _payment_service.get_payments_by_note_async(id_delivery_note);
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

                var all_notes = selected_seller != null && selected_seller.id_seller != 0
                    ? await _receivable_service.get_all_by_month_and_seller_async(selected_month, selected_seller.id_seller)
                    : await _receivable_service.get_all_by_month_async(selected_month);

                notes.Clear();
                foreach (var n in all_notes.Where(n => n.status != "Anulada")) notes.Add(n);

                var payment_list = selected_seller != null && selected_seller.id_seller != 0
                    ? await _payment_service.get_payments_by_month_and_seller_async(selected_month, selected_seller.id_seller)
                    : await _payment_service.get_payments_by_month_async(selected_month);

                payments.Clear();
                foreach (var p in payment_list) payments.Add(p);
                total_payments_usd = payments.Sum(p => p.amount_usd);
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error"); }
            finally { _is_loading = false; }
        }

        private void execute_open_payment(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
            {
                if (note.status == "Pagada")
                {
                    System.Windows.MessageBox.Show("Esta nota ya esta pagada.", "Info");
                    return;
                }
                on_request_payment_window?.Invoke(note);
            }
        }

        private void execute_open_note_history(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
                on_request_note_history_window?.Invoke(note.id_delivery_note);
        }
    }
}
