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
    public class CustomerHistoryViewModel : ViewModelBase
    {
        private readonly ICustomerService _customerService;
        private readonly IAccountsReceivableService _accountsReceivableService;
        private readonly ICreditNoteService _creditNoteService;
        private readonly IPaymentService _paymentService;

        private customer? _customer;
        private CustomerHistoryDataDto? _historyData;
        private bool _isLoading;
        private string _errorMessage = string.Empty;

        // Filtros
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private string _creditNoteCategoryFilter = "Todas";

        // Métricas
        private decimal _totalInvoicedUsd;
        private decimal _totalPaidUsd;
        private decimal _balanceDueUsd;
        private int _totalNotesCount;
        private int _totalCreditNotesCount;
        private decimal _totalCreditNotesUsd;

        // Colecciones observables para los DataGrids
        public ObservableCollection<accounts_receivable_dto> DeliveryNotes { get; } = new();
        public ObservableCollection<credit_note_dto> CreditNotes { get; } = new();
        public ObservableCollection<payment_dto> Payments { get; } = new();
        public ObservableCollection<CustomerHistoryProductDto> TopProducts { get; } = new();

        public ObservableCollection<string> CreditNoteCategoryOptions { get; } = new()
        {
            "Todas",
            "Devolución",
            "Obsequio"
        };

        // Callbacks para ventanas modales
        public Action<note_print_dto>? OnRequestPreviewNote { get; set; }
        public Action<accounts_receivable_dto, IEnumerable<payment_dto>>? OnRequestNotePayments { get; set; }
        public Action<credit_note_dto>? OnRequestCreditNoteDetail { get; set; }

        public customer? Customer
        {
            get => _customer;
            private set
            {
                _customer = value;
                on_property_changed();
                on_property_changed(nameof(CustomerCode));
                on_property_changed(nameof(BusinessName));
                on_property_changed(nameof(Rif));
                on_property_changed(nameof(ContactName));
                on_property_changed(nameof(PhoneNumber));
                on_property_changed(nameof(ZonaName));
                on_property_changed(nameof(FiscalAddress));
                on_property_changed(nameof(EffectiveDeliveryAddress));
            }
        }

        public string CustomerCode => _customer?.customer_code ?? string.Empty;
        public string BusinessName => _customer?.business_name ?? string.Empty;
        public string Rif => _customer?.rif ?? string.Empty;
        public string ContactName => _customer?.contact_name ?? string.Empty;
        public string PhoneNumber => _customer?.phone_number ?? string.Empty;
        public string ZonaName => _customer?.zona?.name ?? (_customer?.id_zona.HasValue == true ? $"Zona {_customer.id_zona}" : "-");
        public string FiscalAddress => _customer?.fiscal_address ?? string.Empty;
        public string EffectiveDeliveryAddress => string.IsNullOrWhiteSpace(_customer?.delivery_address) ? FiscalAddress : _customer!.delivery_address;

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; on_property_changed(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; on_property_changed(); }
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set { _fromDate = value; on_property_changed(); }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set { _toDate = value; on_property_changed(); }
        }

        public string CreditNoteCategoryFilter
        {
            get => _creditNoteCategoryFilter;
            set
            {
                if (_creditNoteCategoryFilter != value)
                {
                    _creditNoteCategoryFilter = value;
                    on_property_changed();
                    ApplyFilters();
                }
            }
        }

        public decimal TotalInvoicedUsd
        {
            get => _totalInvoicedUsd;
            private set { _totalInvoicedUsd = value; on_property_changed(); }
        }

        public decimal TotalPaidUsd
        {
            get => _totalPaidUsd;
            private set { _totalPaidUsd = value; on_property_changed(); }
        }

        public decimal BalanceDueUsd
        {
            get => _balanceDueUsd;
            private set { _balanceDueUsd = value; on_property_changed(); }
        }

        public int TotalNotesCount
        {
            get => _totalNotesCount;
            private set { _totalNotesCount = value; on_property_changed(); }
        }

        public int TotalCreditNotesCount
        {
            get => _totalCreditNotesCount;
            private set { _totalCreditNotesCount = value; on_property_changed(); }
        }

        public decimal TotalCreditNotesUsd
        {
            get => _totalCreditNotesUsd;
            private set { _totalCreditNotesUsd = value; on_property_changed(); }
        }

        // Commands
        public ICommand FilterCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand PreviewNoteCommand { get; }
        public ICommand PrintNotePdfCommand { get; }
        public ICommand ViewNotePaymentsCommand { get; }
        public ICommand ViewCreditNoteDetailCommand { get; }
        public ICommand PrintCreditNotePdfCommand { get; }
        public ICommand PrintCustomerHistoryPdfCommand { get; }

        public CustomerHistoryViewModel(
            ICustomerService customerService,
            IAccountsReceivableService accountsReceivableService,
            ICreditNoteService creditNoteService,
            IPaymentService paymentService)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _accountsReceivableService = accountsReceivableService ?? throw new ArgumentNullException(nameof(accountsReceivableService));
            _creditNoteService = creditNoteService ?? throw new ArgumentNullException(nameof(creditNoteService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));

            FilterCommand = new RelayCommand(_ => ApplyFilters());
            ClearFilterCommand = new RelayCommand(_ => ClearFilters());
            PreviewNoteCommand = new RelayCommand(ExecutePreviewNote);
            PrintNotePdfCommand = new RelayCommand(ExecutePrintNotePdf);
            ViewNotePaymentsCommand = new RelayCommand(ExecuteViewNotePayments);
            ViewCreditNoteDetailCommand = new RelayCommand(ExecuteViewCreditNoteDetail);
            PrintCreditNotePdfCommand = new RelayCommand(ExecutePrintCreditNotePdf);
            PrintCustomerHistoryPdfCommand = new RelayCommand(_ => ExecutePrintCustomerHistoryPdf());
        }

        public async Task LoadHistoryAsync(int customerId)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                _historyData = await _customerService.GetCustomerHistoryAsync(customerId);
                if (_historyData == null)
                {
                    ErrorMessage = "No se encontró la información del cliente.";
                    return;
                }

                Customer = _historyData.Customer;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar historial: {ErrorText.Get(ex)}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void ClearFilters()
        {
            _fromDate = null;
            _toDate = null;
            _creditNoteCategoryFilter = "Todas";
            on_property_changed(nameof(FromDate));
            on_property_changed(nameof(ToDate));
            on_property_changed(nameof(CreditNoteCategoryFilter));
            ApplyFilters();
        }

        public void ApplyFilters()
        {
            if (_historyData == null) return;

            DateTime? from = _fromDate?.Date;
            DateTime? to = _toDate?.Date.AddDays(1).AddTicks(-1);

            // 1. Delivery Notes
            var filteredNotes = _historyData.DeliveryNotes.AsEnumerable();
            if (from.HasValue)
                filteredNotes = filteredNotes.Where(n => n.creation_date >= from.Value);
            if (to.HasValue)
                filteredNotes = filteredNotes.Where(n => n.creation_date <= to.Value);

            var notesList = filteredNotes.ToList();
            DeliveryNotes.Clear();
            foreach (var n in notesList) DeliveryNotes.Add(n);

            // 2. Credit Notes
            var filteredCredit = _historyData.CreditNotes.AsEnumerable();
            if (from.HasValue)
                filteredCredit = filteredCredit.Where(c => c.creation_date >= from.Value);
            if (to.HasValue)
                filteredCredit = filteredCredit.Where(c => c.creation_date <= to.Value);

            if (!string.Equals(_creditNoteCategoryFilter, "Todas", StringComparison.OrdinalIgnoreCase))
            {
                filteredCredit = filteredCredit.Where(c => string.Equals(c.category?.Trim(), _creditNoteCategoryFilter.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            var creditList = filteredCredit.ToList();
            CreditNotes.Clear();
            foreach (var c in creditList) CreditNotes.Add(c);

            // 3. Payments
            var filteredPayments = _historyData.Payments.AsEnumerable();
            if (from.HasValue)
                filteredPayments = filteredPayments.Where(p => p.payment_date >= from.Value);
            if (to.HasValue)
                filteredPayments = filteredPayments.Where(p => p.payment_date <= to.Value);

            var paymentList = filteredPayments.ToList();
            Payments.Clear();
            foreach (var p in paymentList) Payments.Add(p);

            // 4. Top Products
            TopProducts.Clear();
            foreach (var prod in _historyData.TopProducts) TopProducts.Add(prod);

            // 5. Update KPI metrics based on filtered view
            var validNotes = notesList.Where(n => !string.Equals(n.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)).ToList();
            TotalInvoicedUsd = validNotes.Sum(n => n.total_amount_usd);
            TotalPaidUsd = paymentList.Where(p => p.amount_usd > 0).Sum(p => p.amount_usd);
            BalanceDueUsd = validNotes.Sum(n => n.balance_due_usd);
            TotalNotesCount = validNotes.Count;

            var validCredit = creditList.Where(c => !string.Equals(c.status?.Trim(), "Anulada", StringComparison.OrdinalIgnoreCase)).ToList();
            TotalCreditNotesCount = validCredit.Count;
            TotalCreditNotesUsd = validCredit.Sum(c => c.total_amount_usd);
        }

        private async void ExecutePreviewNote(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
            {
                try
                {
                    note_print_dto printable = await _accountsReceivableService.get_printable_note_async(note.id_delivery_note);
                    OnRequestPreviewNote?.Invoke(printable);
                }
                catch (Exception ex)
                {
                    AppDialog.Show($"Error al cargar vista previa: {ErrorText.Get(ex)}", "Error");
                }
            }
        }

        private async void ExecutePrintNotePdf(object? parameter)
        {
            if (parameter is accounts_receivable_dto note)
            {
                try
                {
                    note_print_dto printable = await _accountsReceivableService.get_printable_note_async(note.id_delivery_note);
                    NotePdfGenerator.generate(printable);
                }
                catch (Exception ex)
                {
                    AppDialog.Show($"Error al generar PDF: {ErrorText.Get(ex)}", "Error");
                }
            }
        }

        private void ExecuteViewNotePayments(object? parameter)
        {
            if (parameter is accounts_receivable_dto note && _historyData != null)
            {
                var notePayments = _historyData.Payments.Where(p => p.id_delivery_note == note.id_delivery_note).ToList();
                OnRequestNotePayments?.Invoke(note, notePayments);
            }
        }

        private void ExecuteViewCreditNoteDetail(object? parameter)
        {
            if (parameter is credit_note_dto note)
            {
                OnRequestCreditNoteDetail?.Invoke(note);
            }
        }

        private async void ExecutePrintCreditNotePdf(object? parameter)
        {
            if (parameter is credit_note_dto credit_note)
            {
                try
                {
                    note_print_dto? original = null;
                    if (!string.Equals(credit_note.category, "Obsequio", StringComparison.OrdinalIgnoreCase) && credit_note.id_delivery_note > 0)
                    {
                        original = await _accountsReceivableService.get_printable_note_async(credit_note.id_delivery_note);
                    }
                    var credit = await _creditNoteService.get_printable_credit_note_async(credit_note.id_credit_note);

                    if (original != null)
                        NotePdfGenerator.generate(original, credit);
                    else
                        NotePdfGenerator.generate(credit);
                }
                catch (Exception ex)
                {
                    AppDialog.Show($"Error al generar PDF: {ErrorText.Get(ex)}", "Error");
                }
            }
        }

        private void ExecutePrintCustomerHistoryPdf()
        {
            if (_customer == null)
            {
                AppDialog.Show("No hay datos de cliente disponibles para imprimir.", "Aviso");
                return;
            }

            try
            {
                string periodLabel = "Historial Completo";
                if (FromDate.HasValue && ToDate.HasValue)
                    periodLabel = $"Desde {FromDate.Value:dd/MM/yyyy} Hasta {ToDate.Value:dd/MM/yyyy}";
                else if (FromDate.HasValue)
                    periodLabel = $"Desde {FromDate.Value:dd/MM/yyyy}";
                else if (ToDate.HasValue)
                    periodLabel = $"Hasta {ToDate.Value:dd/MM/yyyy}";

                var model = new CustomerHistoryPdfModel
                {
                    CustomerCode = CustomerCode,
                    CustomerName = BusinessName,
                    Rif = Rif,
                    ContactName = ContactName,
                    Phone = PhoneNumber,
                    ZoneName = ZonaName,
                    FiscalAddress = FiscalAddress,
                    DeliveryAddress = EffectiveDeliveryAddress,
                    PeriodLabel = periodLabel,
                    TotalInvoicedUsd = TotalInvoicedUsd,
                    TotalNotesCount = TotalNotesCount,
                    TotalPaidUsd = TotalPaidUsd,
                    BalanceDueUsd = BalanceDueUsd,
                    TotalCreditNotesUsd = TotalCreditNotesUsd,
                    TotalCreditNotesCount = TotalCreditNotesCount,
                    DeliveryNotes = DeliveryNotes.ToList(),
                    CreditNotes = CreditNotes.ToList(),
                    Payments = Payments.ToList(),
                    TopProducts = TopProducts.ToList()
                };

                CustomerHistoryPdfGenerator.generate(model);
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Error al generar el PDF del historial: {ErrorText.Get(ex)}", "Error");
            }
        }
    }
}

