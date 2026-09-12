using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NinOS.Domain;
using NinOS.Domain.ViewModels;
using NinOS.Infrastructure.Services.Interfaces;
using NinOS.UI.Common;

namespace NinOS.UI.Common.ViewModels
{
    public class ProVentaViewModel : ViewModelBase
    {
        private readonly IProVentaService _pro_venta_service;
        private readonly IAccountsReceivableService _accounts_receivable_service;
        private readonly IPaymentService _payment_service;

        private int _selected_report_index;
        private pro_venta_month_option? _selected_month;
        private pro_venta_week_info? _selected_week;
        private pro_venta_weekly_dto? _report;
        private bool _is_loading;

        public ObservableCollection<pro_venta_month_option> available_months { get; } = new();
        public List<pro_venta_week_info> weeks { get; private set; } = new();
        public ObservableCollection<pro_venta_weekly_row> report_rows { get; } = new();
        public ObservableCollection<pro_venta_pending_row> pending_rows { get; } = new();

        public ICommand print_command { get; }
        public ICommand refresh_command { get; }
        public ICommand preview_note_command { get; }
        public ICommand print_pdf_command { get; }
        public ICommand pay_note_command { get; }

        public Action<pro_venta_pending_row>? on_request_preview_window;
        public Action<pro_venta_pending_row>? on_request_payment_window;

        public ProVentaViewModel(
            IProVentaService pro_venta_service,
            IAccountsReceivableService accounts_receivable_service,
            IPaymentService payment_service)
        {
            _pro_venta_service = pro_venta_service ?? throw new ArgumentNullException(nameof(pro_venta_service));
            _accounts_receivable_service = accounts_receivable_service ?? throw new ArgumentNullException(nameof(accounts_receivable_service));
            _payment_service = payment_service ?? throw new ArgumentNullException(nameof(payment_service));

            print_command = new RelayCommand(_ => print_report());
            refresh_command = new RelayCommand(_ => refresh_data());
            preview_note_command = new RelayCommand(execute_preview_note);
            print_pdf_command = new RelayCommand(execute_print_pdf);
            pay_note_command = new RelayCommand(execute_pay_note);
        }

        public int selected_report_index
        {
            get { return _selected_report_index; }
            set
            {
                if (_selected_report_index == value) return;
                _selected_report_index = value;
                on_property_changed();
                on_property_changed(nameof(is_weekly));
                on_property_changed(nameof(is_pending));
                on_property_changed(nameof(is_paid));
            }
        }

        public bool is_weekly => _selected_report_index == 0;
        public bool is_pending => _selected_report_index == 1;
        public bool is_paid => _selected_report_index == 2;

        public pro_venta_month_option? selected_month
        {
            get { return _selected_month; }
            set
            {
                if (_selected_month == value) return;
                _selected_month = value;
                on_property_changed();
                refresh_weeks_async();
            }
        }

        public pro_venta_week_info? selected_week
        {
            get { return _selected_week; }
            set
            {
                if (_selected_week == value) return;
                _selected_week = value;
                on_property_changed();
                if (!_is_loading)
                {
                    _ = load_report_async();
                }
            }
        }

        public string city => "MARACAY";

        public decimal nota_por_pagar => Math.Round(total_amount - total_gastos_25 - total_gastos_15, 2);

        public string relation_title => _report == null ? string.Empty : $"RELACION NRO {_report.relation_number}";

        public string relation_subtitle => _report == null
            ? string.Empty
            : $"{_report.week_start:dd/MM} AL {_report.week_end:dd/MM} _ FACTURAS POR COBRAR MARACAY";

        public bool has_rows => _report != null && _report.rows.Count > 0;
        public bool is_empty => !has_rows;

        public decimal total_amount => _report?.total_amount ?? 0;
        public decimal total_commission_luis => _report?.total_commission_luis ?? 0;
        public decimal total_gastos_25 => _report?.total_gastos_25 ?? 0;
        public decimal total_gastos_15 => _report?.total_gastos_15 ?? 0;
        public decimal total_cobrado => total_amount - total_commission_luis;
        public decimal diferencial => total_cobrado - nota_por_pagar;

        public bool has_pending => pending_rows.Count > 0;
        public bool pending_empty => !has_pending;
        public decimal pending_total_amount => pending_rows.Sum(r => r.amount);
        public decimal pending_total_balance => pending_rows.Sum(r => r.balance_due_usd);

        public void initialize()
        {
            refresh_data();
        }

        public async void refresh_data()
        {
            try
            {
                var months = await _pro_venta_service.get_available_months_async();

                var previous = _selected_month?.value;
                available_months.Clear();
                foreach (var month in months) available_months.Add(month);

                var next = months.FirstOrDefault(m => m.value == previous) ?? months.FirstOrDefault();
                _selected_month = null;
                on_property_changed(nameof(selected_month));
                _selected_month = next;
                on_property_changed(nameof(selected_month));

                if (_selected_month != null)
                {
                    refresh_weeks_async();
                }

                await load_pending_async();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "error");
            }
        }

        public async Task load_pending_async()
        {
            try
            {
                var rows = await _pro_venta_service.get_pending_relations_async();
                pending_rows.Clear();
                foreach (var row in rows) pending_rows.Add(row);

                on_property_changed(nameof(has_pending));
                on_property_changed(nameof(pending_empty));
                on_property_changed(nameof(pending_total_amount));
                on_property_changed(nameof(pending_total_balance));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "error");
            }
        }

        private async void refresh_weeks_async()
        {
            if (_selected_month == null) return;

            _is_loading = true;
            try
            {
                var new_weeks = _pro_venta_service.build_weeks(_selected_month.value.Year, _selected_month.value.Month);
                var previous_index = _selected_week?.week_index;
                weeks = new_weeks;
                on_property_changed(nameof(weeks));
                _selected_week = weeks.FirstOrDefault(w => w.week_index == previous_index) ?? weeks.FirstOrDefault();
                on_property_changed(nameof(selected_week));
            }
            finally
            {
                _is_loading = false;
            }

            await load_report_async();
        }

        private async Task load_report_async()
        {
            if (_selected_week == null)
            {
                _report = null;
                report_rows.Clear();
                raise_report_changed();
                return;
            }

            try
            {
                var report = await _pro_venta_service.get_weekly_report_async(_selected_week);
                report.city = "MARACAY";
                _report = report;

                report_rows.Clear();
                foreach (var row in report.rows) report_rows.Add(row);

                raise_report_changed();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "error");
            }
        }

        private void raise_report_changed()
        {
            on_property_changed(nameof(relation_title));
            on_property_changed(nameof(relation_subtitle));
            on_property_changed(nameof(has_rows));
            on_property_changed(nameof(is_empty));
            on_property_changed(nameof(total_amount));
            on_property_changed(nameof(total_commission_luis));
            on_property_changed(nameof(total_gastos_25));
            on_property_changed(nameof(total_gastos_15));
            on_property_changed(nameof(total_cobrado));
            on_property_changed(nameof(nota_por_pagar));
            on_property_changed(nameof(diferencial));
        }

        private void print_report()
        {
            if (_report == null) return;
            ProVentaPdfGenerator.generate(_report, nota_por_pagar);
        }

        private void execute_preview_note(object? parameter)
        {
            if (parameter is pro_venta_pending_row row)
                on_request_preview_window?.Invoke(row);
        }

        private void execute_pay_note(object? parameter)
        {
            if (parameter is pro_venta_pending_row row)
                on_request_payment_window?.Invoke(row);
        }

        private async void execute_print_pdf(object? parameter)
        {
            if (parameter is not pro_venta_pending_row row) return;
            try
            {
                note_print_dto printable = await get_printable_note_async(row.id_delivery_note);
                NotePdfGenerator.generate(printable);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<note_print_dto> get_printable_note_async(int id_delivery_note)
        {
            return await _accounts_receivable_service.get_printable_note_async(id_delivery_note);
        }

        public async Task register_payment_async(int id_delivery_note, string note_number, decimal amount_usd, DateTime payment_date)
        {
            payment new_payment = new payment(
                id_delivery_note, payment_date,
                amount_usd, 0, null, "Efectivo", $"EF-{note_number}", "", "Pago Pro Venta");

            await _payment_service.register_payment_async(new_payment, is_pro_venta: true);
            MessageBox.Show("Pago registrado exitosamente.", "Exito");

            await load_pending_async();
            refresh_weeks_async();
        }
    }
}