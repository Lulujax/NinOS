using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NinOS.Domain;
using NinOS.Infrastructure.Logging;
using NinOS.UI.Common;
using NinOS.UI.Updates;

namespace NinOS.UI.Common.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private DeliveryNotesViewModel? _delivery_notes_vm;
        private AccountsReceivableViewModel? _accounts_receivable_vm;
        private SalesViewModel? _sales_vm;
        private PaymentsViewModel? _payments_vm;
        private CommissionsViewModel? _commissions_vm;
        private CustomerViewModel? _customer_vm;
        private InventoryViewModel? _inventory_vm;
        private ProVentaViewModel? _pro_venta_vm;
        private CreditNotesViewModel? _credit_notes_vm;

        public DeliveryNotesViewModel? delivery_notes_vm
        {
            get => _delivery_notes_vm;
            set { _delivery_notes_vm = value; on_property_changed(); }
        }

        public AccountsReceivableViewModel? accounts_receivable_vm
        {
            get => _accounts_receivable_vm;
            set { _accounts_receivable_vm = value; on_property_changed(); }
        }

        public SalesViewModel? sales_vm
        {
            get => _sales_vm;
            set { _sales_vm = value; on_property_changed(); }
        }

        public PaymentsViewModel? payments_vm
        {
            get => _payments_vm;
            set { _payments_vm = value; on_property_changed(); }
        }

        public CommissionsViewModel? commissions_vm
        {
            get => _commissions_vm;
            set { _commissions_vm = value; on_property_changed(); }
        }

        public CustomerViewModel? customer_vm
        {
            get => _customer_vm;
            set { _customer_vm = value; on_property_changed(); }
        }

        public InventoryViewModel? inventory_vm
        {
            get => _inventory_vm;
            set { _inventory_vm = value; on_property_changed(); }
        }

        public ProVentaViewModel? pro_venta_vm
        {
            get => _pro_venta_vm;
            set { _pro_venta_vm = value; on_property_changed(); }
        }

        public CreditNotesViewModel? credit_notes_vm
        {
            get => _credit_notes_vm;
            set { _credit_notes_vm = value; on_property_changed(); }
        }

        private SellersViewModel? _sellers_vm;
        public SellersViewModel? sellers_vm
        {
            get => _sellers_vm;
            set { _sellers_vm = value; on_property_changed(); }
        }

        private readonly IAppUpdater _updater;
        private bool _update_banner_visible;
        private string _update_banner_text = string.Empty;
        private bool _work_allowed = true;
        private bool _update_in_progress;
        private update_policy_snapshot? _policy_snapshot;

        public bool update_banner_visible
        {
            get => _update_banner_visible;
            private set { _update_banner_visible = value; on_property_changed(nameof(update_banner_visible)); }
        }

        public string update_banner_text
        {
            get => _update_banner_text;
            private set { _update_banner_text = value; on_property_changed(nameof(update_banner_text)); }
        }

        public bool work_allowed
        {
            get => _work_allowed;
            private set { _work_allowed = value; on_property_changed(nameof(work_allowed)); }
        }

        private void apply_policy_snapshot(update_policy_snapshot? snapshot)
        {
            _policy_snapshot = snapshot;

            if (snapshot == null)
            {
                work_allowed = true;
                update_banner_visible = false;
                return;
            }

            work_allowed = snapshot.status != update_status.obligatoria;

            if (snapshot.status == update_status.obligatoria)
            {
                show_banner($"Esta version ({snapshot.current_version}) ya no esta permitida. Debe actualizar a {snapshot.recommended_version} para seguir trabajando.");
            }
            else if (snapshot.countdown.state == notice_state.contando || snapshot.countdown.state == notice_state.vencido)
            {
                show_banner("Actualizacion programada: " + (snapshot.notice_message ?? snapshot.countdown.text));
            }
            else if (snapshot.status == update_status.recomendada)
            {
                show_banner($"Hay una version nueva recomendada ({snapshot.recommended_version}). Actualice cuando pueda.");
            }
            else
            {
                update_banner_visible = false;
            }
        }

        private void show_banner(string text)
        {
            update_banner_text = text;
            update_banner_visible = true;
        }

        public async Task update_now_async()
        {
            if (_update_in_progress) return;

            if (delivery_notes_vm?.has_pending_data == true)
            {
                MessageBoxResult answer = AppDialog.Show(
                    "Hay datos sin guardar en la Nota de Entrega. Si actualiza ahora se perderan.\n\n¿Desea actualizar de todas formas?",
                    "Datos sin guardar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (answer != MessageBoxResult.Yes) return;
            }

            _update_in_progress = true;
            try
            {
                AppLog.Info("Actualizacion iniciada por el usuario");
                update_check_result check = await _updater.check_for_update_async(CancellationToken.None);
                if (!check.is_available)
                {
                    _update_in_progress = false;
                    AppDialog.Show(
                        check.error_message ?? "Ya tienes la version mas reciente.",
                        "Actualizacion",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                IProgress<int> progress = new Progress<int>(percent => update_banner_text = $"Descargando actualizacion: {percent}%");
                await _updater.download_update_async(progress, CancellationToken.None);
                _updater.apply_update_and_restart();
            }
            catch (Exception exception)
            {
                AppLog.Error("La actualizacion fallo", exception);
                AppDialog.Show(
                    "No se pudo actualizar. Se sigue con la version actual.\n\n" + ErrorText.Get(exception),
                    "Actualizacion",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _update_in_progress = false;
                apply_policy_snapshot(_policy_snapshot);
            }
        }

        public MainWindowViewModel(
            DeliveryNotesViewModel deliveryNotesVm,
            AccountsReceivableViewModel accountsReceivableVm,
            SalesViewModel salesVm,
            PaymentsViewModel paymentsVm,
            CommissionsViewModel commissionsVm,
            CustomerViewModel customerVm,
            InventoryViewModel inventoryVm,
            ProVentaViewModel proVentaVm,
            CreditNotesViewModel creditNotesVm,
            SellersViewModel sellersVm,
            IAppUpdater updater,
            UpdatePolicyMonitor update_policy_monitor)
        {
            _updater = updater;
            delivery_notes_vm = deliveryNotesVm;
            accounts_receivable_vm = accountsReceivableVm;
            sales_vm = salesVm;
            payments_vm = paymentsVm;
            commissions_vm = commissionsVm;
            customer_vm = customerVm;
            inventory_vm = inventoryVm;
            pro_venta_vm = proVentaVm;
            credit_notes_vm = creditNotesVm;
            sellers_vm = sellersVm;

            update_policy_monitor.snapshot_changed += apply_policy_snapshot;
            update_policy_monitor.start();

            if (delivery_notes_vm != null)
            {
                delivery_notes_vm.OnNoteSaved += () =>
                {
                    accounts_receivable_vm?.refresh_data();
                    delivery_notes_vm?.refresh_data();
                    inventory_vm?.refresh_data();
                    pro_venta_vm?.refresh_data();
                };
            }

            if (credit_notes_vm != null)
            {
                credit_notes_vm.OnCreditNoteSaved += () =>
                {
                    accounts_receivable_vm?.refresh_data();
                    payments_vm?.refresh_data();
                    inventory_vm?.refresh_data();
                    pro_venta_vm?.refresh_data();
                    credit_notes_vm?.refresh_data();
                };
            }

            // Cuando el Panel Administrador restaura o elimina clientes/productos/vendedores,
            // todos los listados se recargan para que el cambio se vea de inmediato.
            AppDataEvents.CatalogsChanged += () =>
            {
                delivery_notes_vm?.refresh_data();
                accounts_receivable_vm?.refresh_data();
                sales_vm?.refresh_data();
                payments_vm?.refresh_data();
                commissions_vm?.refresh_data();
                customer_vm?.refresh_data();
                inventory_vm?.refresh_data();
                pro_venta_vm?.refresh_data();
                credit_notes_vm?.refresh_data();
                sellers_vm?.refresh_data();
            };
        }
    }
}
