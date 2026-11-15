using System;

namespace NinOS.Domain
{
    public enum CommissionAction
    {
        None,
        Create,
        Remove,
        UpdateAmount
    }

    public class NoteSettlementResult
    {
        public string status { get; }
        public CommissionAction commission_action { get; }
        public decimal? commission_amount_usd { get; }

        public NoteSettlementResult(string status, CommissionAction commission_action, decimal? commission_amount_usd)
        {
            this.status = status;
            this.commission_action = commission_action;
            this.commission_amount_usd = commission_amount_usd;
        }
    }

    public static class NoteSettlement
    {
        private const decimal TOLERANCIA_SALDO = 0.01m;

        public static NoteSettlementResult evaluate(
            decimal adjusted_total_usd,
            decimal total_paid_usd,
            bool? existing_commission_is_paid,
            bool eligible_for_commission,
            decimal commission_percentage,
            bool is_fully_returned = false)
        {
            if (adjusted_total_usd < 0)
                throw new ArgumentException("El monto ajustado no puede ser negativo.", nameof(adjusted_total_usd));
            if (commission_percentage < 0 || commission_percentage > 1)
                throw new ArgumentException("El porcentaje de comision debe estar entre 0 y 1.", nameof(commission_percentage));

            bool has_paid_commission = existing_commission_is_paid == true;
            bool has_unpaid_commission = existing_commission_is_paid == false;

            if (has_paid_commission)
            {
                string status_con_comision_pagada = is_fully_returned ? "Devuelta" : resolve_status(adjusted_total_usd, total_paid_usd);
                return new NoteSettlementResult(status_con_comision_pagada, CommissionAction.None, null);
            }

            if (is_fully_returned)
            {
                CommissionAction accion = has_unpaid_commission ? CommissionAction.Remove : CommissionAction.None;
                return new NoteSettlementResult("Devuelta", accion, null);
            }

            string status = resolve_status(adjusted_total_usd, total_paid_usd);

            if (status == "Pagada")
            {
                if (has_unpaid_commission)
                {
                    decimal amount = Money.round(adjusted_total_usd * commission_percentage);
                    return new NoteSettlementResult(status, CommissionAction.UpdateAmount, amount);
                }

                if (eligible_for_commission && commission_percentage > 0)
                {
                    decimal amount = Money.round(adjusted_total_usd * commission_percentage);
                    if (amount > 0)
                        return new NoteSettlementResult(status, CommissionAction.Create, amount);
                }

                return new NoteSettlementResult(status, CommissionAction.None, null);
            }

            if (has_unpaid_commission)
                return new NoteSettlementResult(status, CommissionAction.Remove, null);

            return new NoteSettlementResult(status, CommissionAction.None, null);
        }

        private static string resolve_status(decimal adjusted_total_usd, decimal total_paid_usd)
        {
            bool is_fully_paid = total_paid_usd > 0 && (adjusted_total_usd - total_paid_usd) <= TOLERANCIA_SALDO;
            return is_fully_paid ? "Pagada" : "Pendiente";
        }
    }
}
