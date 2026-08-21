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
    public class CommissionsViewModel : ViewModelBase
    {
        private readonly ICommissionService _commission_service;
        private readonly IAccountsReceivableService _receivable_service;

        private string _selected_month = string.Empty;
        private seller? _selected_seller;
        private decimal _total_pending_usd;
        private decimal _total_paid_usd;
        private decimal _exchange_rate;
        private string _payment_type = "Transferencia";
        private string _reference_number = string.Empty;
        private bool _is_loading;

        public ObservableCollection<string> pending_months { get; }
        public ObservableCollection<seller> sellers { get; }
        public ObservableCollection<commission_dto> commissions { get; }

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

        public decimal total_pending_usd
        {
            get { return _total_pending_usd; }
            private set { _total_pending_usd = value; on_property_changed(); }
        }

        public decimal total_paid_usd
        {
            get { return _total_paid_usd; }
            private set { _total_paid_usd = value; on_property_changed(); }
        }

        public decimal exchange_rate
        {
            get { return _exchange_rate; }
            set { _exchange_rate = value; on_property_changed(); }
        }

        public string payment_type
        {
            get { return _payment_type; }
            set { _payment_type = value; on_property_changed(); }
        }

        public string reference_number
        {
            get { return _reference_number; }
            set { _reference_number = value; on_property_changed(); }
        }

        public ICommand pay_commission_command { get; }

        public Action? on_request_history_window;

        public CommissionsViewModel(ICommissionService commission_service, IAccountsReceivableService receivable_service)
        {
            _commission_service = commission_service ?? throw new ArgumentNullException(nameof(commission_service));
            _receivable_service = receivable_service ?? throw new ArgumentNullException(nameof(receivable_service));

            pending_months = new ObservableCollection<string>();
            sellers = new ObservableCollection<seller>();
            commissions = new ObservableCollection<commission_dto>();

            pay_commission_command = new RelayCommand(execute_pay_commission, _ => commissions.Any(c => c.is_selected && !c.is_paid));

            load_initial_data_async();
        }

        public void refresh_data() => load_initial_data_async();

        private async void load_initial_data_async()
        {
            try
            {
                _is_loading = true;
                var sl = await _commission_service.get_sellers_with_commissions_async();
                sellers.Clear();
                sellers.Add(new seller("Todos", "-", "-") { id_seller = 0 });
                foreach (var s in sl) sellers.Add(s);

                var months = await _receivable_service.get_pending_months_async();
                pending_months.Clear();
                foreach (var m in months) pending_months.Add(m);
                if (pending_months.Any()) selected_month = pending_months.First();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error"); }
            finally { _is_loading = false; }
        }

        private async void load_data_async()
        {
            if (string.IsNullOrEmpty(selected_month) || _is_loading) return;
            try
            {
                _is_loading = true;
                List<commission_dto> all;

                if (selected_seller != null && selected_seller.id_seller != 0)
                {
                    all = (await _commission_service.get_commissions_by_seller_and_month_async(selected_seller.id_seller, selected_month)).ToList();
                }
                else
                {
                    all = new List<commission_dto>();
                    foreach (var s in sellers.Where(x => x.id_seller != 0))
                    {
                        var sc = await _commission_service.get_commissions_by_seller_and_month_async(s.id_seller, selected_month);
                        all.AddRange(sc);
                    }
                }

                commissions.Clear();
                foreach (var c in all.OrderByDescending(x => x.creation_date)) commissions.Add(c);
                total_pending_usd = commissions.Where(c => !c.is_paid).Sum(c => c.amount_usd);
                total_paid_usd = commissions.Where(c => c.is_paid).Sum(c => c.amount_usd);
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error"); }
            finally { _is_loading = false; }
        }

        private async void execute_pay_commission(object? parameter)
        {
            var selected = commissions.Where(c => c.is_selected && !c.is_paid).ToList();
            if (!selected.Any()) return;

            if (exchange_rate <= 0)
            {
                System.Windows.MessageBox.Show("Ingrese una tasa de cambio valida.", "Validacion");
                return;
            }

            try
            {
                int[] ids = selected.Select(c => c.id_commission).ToArray();
                await _commission_service.register_commission_payment_async(ids, exchange_rate, payment_type, reference_number);
                System.Windows.MessageBox.Show("Liquidacion registrada exitosamente.", "Exito");
                load_data_async();
            }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error"); }
        }
    }
}
