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
        private pro_venta_relation_option? _selected_relation;
        private bool _is_syncing_week_relation;
        private List<pro_venta_relation_option> _all_relations_cache = new();
        private pro_venta_weekly_dto? _report;
        private bool _is_loading;

        public ObservableCollection<pro_venta_month_option> available_months { get; } = new();
        public List<pro_venta_week_info> weeks { get; private set; } = new();
        public ObservableCollection<pro_venta_relation_option> available_relations { get; } = new();
        public ObservableCollection<pro_venta_weekly_row> report_rows { get; } = new();
        public ObservableCollection<pro_venta_relation_row> pending_rows { get; } = new();
        public ObservableCollection<pro_venta_relation_row> paid_rows { get; } = new();

        public ICommand print_command { get; }
        public ICommand refresh_command { get; }
        public ICommand relation_pdf_command { get; }
        public ICommand relation_detail_pdf_command { get; }
        public ICommand pay_relation_command { get; }
        public ICommand note_pdf_command { get; }
        public ICommand note_preview_command { get; }
        public ICommand annul_note_command { get; }
        public ICommand clear_month_filter_command { get; }

        public Action<pro_venta_relation_row>? on_request_relation_pdf;
        public Action<pro_venta_relation_row>? on_request_payment_window;
        public Action<pro_venta_weekly_row>? on_request_note_preview;
        public Action<pro_venta_weekly_row>? on_request_annul_note;

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
            relation_pdf_command = new RelayCommand(execute_relation_pdf);
            relation_detail_pdf_command = new RelayCommand(execute_relation_detail_pdf);
            pay_relation_command = new RelayCommand(execute_pay_relation);
            note_pdf_command = new RelayCommand(execute_note_pdf);
            note_preview_command = new RelayCommand(execute_note_preview);
            annul_note_command = new RelayCommand(execute_annul_note);
            clear_month_filter_command = new RelayCommand(_ => show_all_relations());
        }

        private bool _show_all_months;
        public bool show_all_months
        {
            get => _show_all_months;
            private set { _show_all_months = value; on_property_changed(); }
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

                if (is_pending)
                {
                    _ = load_pending_async();
                }
                else if (is_paid)
                {
                    _ = load_paid_async();
                }
                else if (is_weekly && _selected_month != null)
                {
                    refresh_weeks_async();
                }
            }
        }

        public bool is_weekly => _selected_report_index == 0;
        public bool is_pending => _selected_report_index == 1;
        public bool is_paid => _selected_report_index == 2;

        public bool month_not_selected => _selected_month == null && !_show_all_months;

        public bool pending_show_empty => (_selected_month != null || _show_all_months) && pending_empty;
        public bool paid_show_empty => (_selected_month != null || _show_all_months) && paid_empty;
        public bool pending_show_month_hint => _selected_month == null && !_show_all_months;
        public bool paid_show_month_hint => _selected_month == null && !_show_all_months;

        public pro_venta_month_option? selected_month
        {
            get { return _selected_month; }
            set
            {
                if (_selected_month == value) return;
                _selected_month = value;
                if (_selected_month != null)
                {
                    _show_all_months = false;
                    on_property_changed(nameof(show_all_months));
                }
                on_property_changed();
                on_property_changed(nameof(month_not_selected));

                if (_selected_month == null && !_show_all_months)
                {
                    weeks = new List<pro_venta_week_info>();
                    on_property_changed(nameof(weeks));
                    _selected_week = null;
                    on_property_changed(nameof(selected_week));
                    _report = null;
                    pending_rows.Clear();
                    paid_rows.Clear();
                    on_property_changed(nameof(has_pending));
                    on_property_changed(nameof(pending_empty));
                    on_property_changed(nameof(has_paid));
                    on_property_changed(nameof(paid_empty));
                    on_property_changed(nameof(pending_show_empty));
                    on_property_changed(nameof(pending_show_month_hint));
                    on_property_changed(nameof(paid_show_empty));
                    on_property_changed(nameof(paid_show_month_hint));
                    on_property_changed(nameof(pending_total_amount));
                    on_property_changed(nameof(pending_total_balance));
                    on_property_changed(nameof(paid_total_amount));
                    on_property_changed(nameof(available_relations));
                    on_property_changed(nameof(total_amount));
                    on_property_changed(nameof(total_commission_luis));
                    on_property_changed(nameof(total_gastos_25));
                    on_property_changed(nameof(total_gastos_15));
                    on_property_changed(nameof(total_cobrado));
                    on_property_changed(nameof(nota_por_pagar));
                    on_property_changed(nameof(diferencial));
                    return;
                }

                if (is_pending)
                {
                    _ = load_pending_async();
                }
                else if (is_paid)
                {
                    _ = load_paid_async();
                }
                else
                {
                    refresh_weeks_async();
                }
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
                sync_relation_with_week();
                if (!_is_loading)
                {
                    _ = load_report_async();
                }
            }
        }

        public pro_venta_relation_option? selected_relation
        {
            get { return _selected_relation; }
            set
            {
                if (_selected_relation == value) return;
                _selected_relation = value;
                on_property_changed();
                sync_week_with_relation();
            }
        }

        public string city => "MARACAY";

        public decimal nota_por_pagar => Money.round(total_amount - total_gastos_25 - total_gastos_15);

        public string relation_title => (_report == null || !has_rows) ? string.Empty : $"RELACION NRO {_report.relation_number}";

        public string relation_subtitle => (_report == null || !has_rows)
            ? string.Empty
            : $"{_report.week_start:dd/MM} AL {_report.week_end:dd/MM} _ FACTURAS POR COBRAR MARACAY";

        public bool has_rows => _report != null && _report.rows.Count > 0;
        public bool is_empty => !has_rows;

        /// <summary>
        /// Si la semana tiene alguna nota anulada. Las anuladas no se borran de la tabla: se ven en
        /// rojo para saber que existieron, pero quedan fuera de los totales y de la liquidacion.
        /// </summary>
        public bool has_annulled => _report?.has_annulled ?? false;

        public string annulled_legend_text => _report == null || !_report.has_annulled
            ? string.Empty
            : $"Las notas marcadas en rojo están ANULADAS y no se incluyen en los totales ni en la liquidación. " +
              $"Anuladas: {_report.annulled_count} nota(s) por {_report.annulled_amount:N2} USD.";

        public decimal total_amount => _report?.total_amount ?? 0;
        public decimal total_commission_luis => _report?.total_commission_luis ?? 0;
        public decimal total_gastos_25 => _report?.total_gastos_25 ?? 0;
        public decimal total_gastos_15 => _report?.total_gastos_15 ?? 0;
        public decimal total_cobrado => Money.round(total_amount - total_commission_luis);
        public decimal diferencial => Money.round(total_cobrado - nota_por_pagar);

        public bool has_pending => pending_rows.Count > 0;
        public bool pending_empty => !has_pending;
        public decimal pending_total_amount => pending_rows.Sum(r => r.amount);
        public decimal pending_total_balance => pending_rows.Sum(r => r.balance_due_usd);

        public bool has_paid => paid_rows.Count > 0;
        public bool paid_empty => !has_paid;
        public decimal paid_total_amount => paid_rows.Sum(r => r.amount);

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

                // El mes NO se preselecciona. Antes se elegia sola el mes actual (o el ultimo
                // disponible) y se cargaba todo de una, lo que hacia que al abrir el modulo ya
                // hubiera datos de un mes que el usuario no habia pedido. Ahora el combo arranca
                // vacio y no se carga nada hasta que elija un mes.
                _selected_month = null;
                on_property_changed(nameof(selected_month));

                // Estado vacio explicito: sin mes no hay reporte que mostrar.
                weeks = new List<pro_venta_week_info>();
                on_property_changed(nameof(weeks));
                _selected_week = null;
                on_property_changed(nameof(selected_week));
                _report = null;

                pending_rows.Clear();
                paid_rows.Clear();
                on_property_changed(nameof(has_pending));
                on_property_changed(nameof(pending_empty));
                on_property_changed(nameof(has_paid));
                on_property_changed(nameof(paid_empty));
                on_property_changed(nameof(pending_show_empty));
                on_property_changed(nameof(pending_show_month_hint));
                on_property_changed(nameof(paid_show_empty));
                on_property_changed(nameof(paid_show_month_hint));
                on_property_changed(nameof(pending_total_amount));
                on_property_changed(nameof(pending_total_balance));
                on_property_changed(nameof(paid_total_amount));
                on_property_changed(nameof(available_relations));
                on_property_changed(nameof(total_amount));
                on_property_changed(nameof(total_commission_luis));
                on_property_changed(nameof(total_gastos_25));
                on_property_changed(nameof(total_gastos_15));
                on_property_changed(nameof(total_cobrado));
                on_property_changed(nameof(nota_por_pagar));
                on_property_changed(nameof(diferencial));

                _all_relations_cache = await _pro_venta_service.get_all_relations_async();
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "error");
            }
        }

        public async Task load_relations_async()
        {
            try
            {
                _all_relations_cache = await _pro_venta_service.get_all_relations_async();
                refresh_month_relations();
                sync_relation_with_week();
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "error");
            }
        }

        private void refresh_month_relations()
        {
            var month_week_starts = weeks.Select(w => w.start.Date).ToHashSet();
            var filtered = _all_relations_cache
                .Where(r => month_week_starts.Contains(r.week_start.Date))
                .OrderBy(r => r.relation_number)
                .ToList();

            available_relations.Clear();
            foreach (var r in filtered) available_relations.Add(r);
        }

        private void show_all_relations()
        {
            _show_all_months = true;
            _selected_month = null;
            on_property_changed(nameof(selected_month));
            on_property_changed(nameof(show_all_months));
            on_property_changed(nameof(month_not_selected));
            on_property_changed(nameof(pending_show_month_hint));
            on_property_changed(nameof(paid_show_month_hint));
            if (is_pending)
            {
                _ = load_pending_async();
            }
            else if (is_paid)
            {
                _ = load_paid_async();
            }
        }

        public async Task load_pending_async()
        {
            try
            {
                if (_selected_month == null && !_show_all_months)
                {
                    pending_rows.Clear();
                    on_property_changed(nameof(has_pending));
                    on_property_changed(nameof(pending_empty));
                    on_property_changed(nameof(pending_show_empty));
                    on_property_changed(nameof(pending_show_month_hint));
                    on_property_changed(nameof(pending_total_amount));
                    on_property_changed(nameof(pending_total_balance));
                    return;
                }

                var all_rows = await _pro_venta_service.get_pending_relations_async();
                IEnumerable<pro_venta_relation_row> filtered = all_rows;
                if (_selected_month != null)
                {
                    int year = _selected_month.value.Year;
                    int month = _selected_month.value.Month;
                    filtered = all_rows.Where(r =>
                        (r.week_start.Year == year && r.week_start.Month == month) ||
                        (r.week_end.Year == year && r.week_end.Month == month));
                }

                pending_rows.Clear();
                foreach (var row in filtered) pending_rows.Add(row);

                on_property_changed(nameof(has_pending));
                on_property_changed(nameof(pending_empty));
                on_property_changed(nameof(pending_show_empty));
                on_property_changed(nameof(pending_show_month_hint));
                on_property_changed(nameof(pending_total_amount));
                on_property_changed(nameof(pending_total_balance));
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "error");
            }
        }

        public async Task load_paid_async()
        {
            try
            {
                if (_selected_month == null && !_show_all_months)
                {
                    paid_rows.Clear();
                    on_property_changed(nameof(has_paid));
                    on_property_changed(nameof(paid_empty));
                    on_property_changed(nameof(paid_show_empty));
                    on_property_changed(nameof(paid_show_month_hint));
                    on_property_changed(nameof(paid_total_amount));
                    return;
                }

                var all_rows = await _pro_venta_service.get_paid_relations_async();
                IEnumerable<pro_venta_relation_row> filtered = all_rows;
                if (_selected_month != null)
                {
                    int year = _selected_month.value.Year;
                    int month = _selected_month.value.Month;
                    filtered = all_rows.Where(r =>
                        (r.week_start.Year == year && r.week_start.Month == month) ||
                        (r.week_end.Year == year && r.week_end.Month == month));
                }

                paid_rows.Clear();
                foreach (var row in filtered) paid_rows.Add(row);

                on_property_changed(nameof(has_paid));
                on_property_changed(nameof(paid_empty));
                on_property_changed(nameof(paid_show_empty));
                on_property_changed(nameof(paid_show_month_hint));
                on_property_changed(nameof(paid_total_amount));
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "error");
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
                refresh_month_relations();
                sync_relation_with_week();
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

                if (_report.relation_number > 0)
                {
                    var existingRel = _all_relations_cache.FirstOrDefault(r => r.relation_number == _report.relation_number);
                    if (existingRel == null)
                    {
                        existingRel = new pro_venta_relation_option
                        {
                            id_relacion = _report.id_relacion,
                            relation_number = _report.relation_number,
                            week_start = _report.week_start,
                            week_end = _report.week_end,
                            label = $"Relacion nro {_report.relation_number}"
                        };
                        _all_relations_cache.Add(existingRel);
                        _all_relations_cache = _all_relations_cache.OrderBy(r => r.relation_number).ToList();
                        existingRel = _all_relations_cache.First(r => r.relation_number == _report.relation_number);
                    }

                    refresh_month_relations();

                    var currentRel = available_relations.FirstOrDefault(r => r.relation_number == _report.relation_number);
                    if (_selected_relation != currentRel && !_is_syncing_week_relation)
                    {
                        _is_syncing_week_relation = true;
                        try
                        {
                            _selected_relation = currentRel;
                            on_property_changed(nameof(selected_relation));
                        }
                        finally
                        {
                            _is_syncing_week_relation = false;
                        }
                    }
                }

                raise_report_changed();
            }
            catch (Exception ex)
            {
                AppDialog.Show(ErrorText.Get(ex), "error");
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
            on_property_changed(nameof(has_annulled));
            on_property_changed(nameof(annulled_legend_text));
        }

        private async void print_report()
        {
            if (_report == null || !has_rows) return;
            try
            {
                List<payment_dto>? payments = null;
                pro_venta_relation_row? relation_row = null;

                if (_report.id_relacion > 0)
                {
                    payments = (await _payment_service.get_payments_by_relation_async(_report.id_relacion))
                        .OrderBy(p => p.payment_date)
                        .ToList();
                    relation_row = pending_rows.FirstOrDefault(r => r.id_relacion == _report.id_relacion)
                                ?? paid_rows.FirstOrDefault(r => r.id_relacion == _report.id_relacion);
                }

                ProVentaPdfGenerator.generate(_report, nota_por_pagar, payments, relation_row);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al generar el PDF: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void execute_relation_pdf(object? parameter)
        {
            if (parameter is pro_venta_relation_row row)
                on_request_relation_pdf?.Invoke(row);
        }

        private async void execute_relation_detail_pdf(object? parameter)
        {
            if (parameter is not pro_venta_relation_row row) return;

            try
            {
                var week = new pro_venta_week_info
                {
                    week_index = row.relation_number,
                    start = row.week_start,
                    end = row.week_end,
                    label = $"{row.week_start:dd} AL {row.week_end:dd}"
                };

                var report = await _pro_venta_service.get_weekly_report_async(week);
                decimal nota_por_pagar = Money.round(report.total_amount - report.total_gastos_25 - report.total_gastos_15);
                var payments = (await _payment_service.get_payments_by_relation_async(row.id_relacion))
                    .OrderBy(p => p.payment_date)
                    .ToList();

                ProVentaPdfGenerator.generate(report, nota_por_pagar, payments, row);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al generar el PDF: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void execute_pay_relation(object? parameter)
        {
            if (parameter is pro_venta_relation_row row)
                on_request_payment_window?.Invoke(row);
        }

        private async void execute_note_pdf(object? parameter)
        {
            if (parameter is not pro_venta_weekly_row row) return;

            try
            {
                note_print_dto printable = await _accounts_receivable_service.get_printable_note_async(row.id_delivery_note);
                NotePdfGenerator.generate(printable);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al generar el PDF: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void execute_note_preview(object? parameter)
        {
            if (parameter is pro_venta_weekly_row row)
                on_request_note_preview?.Invoke(row);
        }

        private void execute_annul_note(object? parameter)
        {
            if (parameter is pro_venta_weekly_row row)
                on_request_annul_note?.Invoke(row);
        }

        public async Task confirm_annul_note_async(pro_venta_weekly_row row)
        {
            if (row == null) return;
            try
            {
                await _accounts_receivable_service.annul_delivery_note_async(row.id_delivery_note, registrar_asiento_pro_venta: true);
                refresh_data();
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<note_print_dto> get_printable_note_async(int id_delivery_note)
        {
            return await _accounts_receivable_service.get_printable_note_async(id_delivery_note);
        }

        public async Task print_relation_pdf_async(pro_venta_relation_row row)
        {
            try
            {
                var week = new pro_venta_week_info
                {
                    week_index = row.relation_number,
                    start = row.week_start,
                    end = row.week_end,
                    label = $"{row.week_start:dd} AL {row.week_end:dd}"
                };

                var report = await _pro_venta_service.get_weekly_report_async(week);
                decimal nota_por_pagar = Money.round(report.total_amount - report.total_gastos_25 - report.total_gastos_15);
                var payments = (await _payment_service.get_payments_by_relation_async(row.id_relacion))
                    .OrderBy(p => p.payment_date)
                    .ToList();

                ProVentaPdfGenerator.generate(report, nota_por_pagar, payments, row);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al generar el PDF: {ErrorText.Get(ex)}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task register_relation_payment_async(int id_relacion, decimal amount_usd, DateTime payment_date, string observations)
        {
            await _payment_service.register_relation_payment_async(id_relacion, amount_usd, payment_date, observations);
            AppDialog.Show("Pago registrado exitosamente.", "Exito");

            await load_pending_async();
            await load_paid_async();
            refresh_weeks_async();
        }

        public async Task<IEnumerable<payment_dto>> get_relation_payments_async(int id_relacion)
        {
            return await _payment_service.get_payments_by_relation_async(id_relacion);
        }

        public async Task<List<pro_venta_weekly_row>> get_relation_notes_async(int id_relacion)
        {
            return await _pro_venta_service.get_relation_notes_async(id_relacion);
        }

        public async Task update_relation_payment_async(int id_payment, decimal amount_usd, DateTime payment_date, string observations)
        {
            await _payment_service.update_relation_payment_async(id_payment, amount_usd, payment_date, observations);

            await load_pending_async();
            await load_paid_async();
        }

        private void sync_relation_with_week()
        {
            if (_is_syncing_week_relation) return;
            if (_selected_week == null)
            {
                if (_selected_relation != null)
                {
                    _selected_relation = null;
                    on_property_changed(nameof(selected_relation));
                }
                return;
            }

            _is_syncing_week_relation = true;
            try
            {
                var match = available_relations.FirstOrDefault(r => r.week_start.Date == _selected_week.start.Date);
                if (_selected_relation != match)
                {
                    _selected_relation = match;
                    on_property_changed(nameof(selected_relation));
                }
            }
            finally
            {
                _is_syncing_week_relation = false;
            }
        }

        private void sync_week_with_relation()
        {
            if (_is_syncing_week_relation) return;
            if (_selected_relation == null) return;

            _is_syncing_week_relation = true;
            try
            {
                var target_start = _selected_relation.week_start.Date;

                // 1. Check if the currently selected month's weeks contains target_start
                var current_week_match = weeks.FirstOrDefault(w => w.start.Date == target_start);
                if (current_week_match != null)
                {
                    if (_selected_week != current_week_match)
                    {
                        _selected_week = current_week_match;
                        on_property_changed(nameof(selected_week));
                        if (!_is_loading)
                        {
                            _ = load_report_async();
                        }
                    }
                    return;
                }

                // 2. Not in current month: find which month contains this week
                var target_month = available_months.FirstOrDefault(m =>
                    _pro_venta_service.build_weeks(m.value.Year, m.value.Month).Any(w => w.start.Date == target_start));

                if (target_month == null)
                {
                    var date_for_month = new DateTime(_selected_relation.week_end.Year, _selected_relation.week_end.Month, 1);
                    target_month = available_months.FirstOrDefault(m => m.value.Year == date_for_month.Year && m.value.Month == date_for_month.Month);
                    if (target_month == null)
                    {
                        var culture = new System.Globalization.CultureInfo("es-VE");
                        target_month = new pro_venta_month_option
                        {
                            value = date_for_month,
                            label = date_for_month.ToString("MMMM yyyy", culture)
                        };
                        available_months.Add(target_month);
                    }
                }

                _selected_month = target_month;
                on_property_changed(nameof(selected_month));

                var new_weeks = _pro_venta_service.build_weeks(_selected_month.value.Year, _selected_month.value.Month);
                weeks = new_weeks;
                on_property_changed(nameof(weeks));

                refresh_month_relations();

                var matching_week = weeks.FirstOrDefault(w => w.start.Date == target_start);
                _selected_week = matching_week ?? weeks.FirstOrDefault();
                on_property_changed(nameof(selected_week));

                _selected_relation = available_relations.FirstOrDefault(r => r.relation_number == _selected_relation.relation_number) ?? _selected_relation;
                on_property_changed(nameof(selected_relation));

                if (!_is_loading)
                {
                    _ = load_report_async();
                }
            }
            finally
            {
                _is_syncing_week_relation = false;
            }
        }

        public bool select_relation_by_number(int relation_number)
        {
            var match = available_relations.FirstOrDefault(r => r.relation_number == relation_number);
            if (match != null)
            {
                selected_relation = match;
                return true;
            }

            var globalMatch = _all_relations_cache.FirstOrDefault(r => r.relation_number == relation_number);
            if (globalMatch != null)
            {
                selected_relation = globalMatch;
                return true;
            }

            return false;
        }

        public bool select_relation_by_text(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            if (int.TryParse(text.Trim(), out int exactNum))
            {
                return select_relation_by_number(exactNum);
            }

            var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int num))
            {
                return select_relation_by_number(num);
            }

            var found = available_relations.FirstOrDefault(r => r.label.Contains(text.Trim(), StringComparison.OrdinalIgnoreCase))
                     ?? _all_relations_cache.FirstOrDefault(r => r.label.Contains(text.Trim(), StringComparison.OrdinalIgnoreCase));
            if (found != null)
            {
                selected_relation = found;
                return true;
            }

            return false;
        }
    }
}