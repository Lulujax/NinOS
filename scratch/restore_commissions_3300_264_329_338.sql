-- Script para restaurar las 3 comisiones a su estado previo pagado si se requiere
BEGIN;

UPDATE commission SET is_paid = True, payout_date = '2026-11-01 23:19:31.849579+00:00', amount_bs = 0.00, exchange_rate = 0.0000, reference_number = '' WHERE id_commission = 17;
UPDATE commission SET is_paid = True, payout_date = '2026-11-01 23:19:31.849579+00:00', amount_bs = 0.00, exchange_rate = 0.0000, reference_number = '' WHERE id_commission = 18;
UPDATE commission SET is_paid = True, payout_date = '2026-11-01 23:19:31.849579+00:00', amount_bs = 0.00, exchange_rate = 0.0000, reference_number = '' WHERE id_commission = 19;
INSERT INTO commission_payment (id_commission_payment, id_commission, amount_usd, amount_bs, exchange_rate, payment_type, reference_number, payment_date, bank_name, observations) VALUES (9, 17, 9.16, 8152.40, 890.0000, 'Pago Movil', '598987', '2026-11-01 04:00:00+00:00', '', '') ON CONFLICT (id_commission_payment) DO NOTHING;
INSERT INTO commission_payment (id_commission_payment, id_commission, amount_usd, amount_bs, exchange_rate, payment_type, reference_number, payment_date, bank_name, observations) VALUES (10, 18, 18.46, 16429.40, 890.0000, 'Pago Movil', '598987', '2026-11-01 04:00:00+00:00', '', '') ON CONFLICT (id_commission_payment) DO NOTHING;
INSERT INTO commission_payment (id_commission_payment, id_commission, amount_usd, amount_bs, exchange_rate, payment_type, reference_number, payment_date, bank_name, observations) VALUES (11, 19, 30.72, 27340.80, 890.0000, 'Pago Movil', '598987', '2026-11-01 04:00:00+00:00', '', '') ON CONFLICT (id_commission_payment) DO NOTHING;

COMMIT;
