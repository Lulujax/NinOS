-- =====================================================================
-- CARTERA ANTIGUA PRO VENTA (NOTAS ZOMBIE CONSOLIDADAS) - Juan Luis
-- Generado: 2026-10-09
--
-- Que es esto:
--   4 notas de entrega Pro Venta (tipo MAR), una por semana, que SOLO
--   representan el saldo por cobrar heredado del Excel. Sin note_detail,
--   sin stock, sin kardeex, sin productos.
--
-- Por que consolidadas y no 21:
--   En Pro Venta el abono es a nivel de relacion, no de nota
--   (PaymentService.register_relation_payment_async deja
--   id_delivery_note = null y cuelga el pago de id_relacion). El saldo de
--   la relacion es sum(adjusted_total_usd de sus notas) - abonos
--   (ProVentaService.build_relation_rows_async). Una sola nota por semana
--   produce exactamente la misma fila y el mismo comportamiento de cobro
--   que 21 notas, sin el detalle por factura que se decide no conservar.
--
-- Por que hace falta un cliente:
--   delivery_note.id_customer es NOT NULL con FK real a customer. No se
--   puede apuntar a un cliente inexistente. Se crea UN cliente zombie
--   compartido (is_ghost = true), invisible en Clientes y en la Papelera
--   por el filtro is_ghost de CustomerService. Aparece en los respaldos
--   (DatabaseExporter usa GetAllCustomersAsync, sin filtro).
--
-- Por que el tipo es MAR y no null:
--   ProVentaService.build_relation_rows_async filtra por
--   note_type_id IN (tipos con code MAR o PVP). Con note_type_id nulo la
--   nota NO aparece en la tabla de Pro Venta.
--
-- Las relaciones se crean aqui porque hoy la tabla relacion esta vacia y
-- DeliveryNoteService solo las genera al crear una nota por la UI.
-- Sus week_start/week_end son lunes a domingo, igual que
-- get_or_create_week_relation_async.
--
-- Nota: la semana 2026-09-28 a 2026-10-04 cruza de mes, asi que su fila
-- aparece al filtrar septiembre y tambien octubre (ProVentaViewModel
-- filtra por week_start O week_end). Es el diseno de semanas, no un error.
--
-- Reversible con el bloque DELETE del final (marcado).
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1. CLIENTE ZOMBIE COMPARTIDO (1)
-- ---------------------------------------------------------------------
INSERT INTO customer
    (customer_code, business_name, rif, contact_name, phone_number,
     fiscal_address, delivery_address, seller_name, is_active, is_ghost, id_zona)
VALUES
    ('PV-ANT-001', 'SALDO ANTERIOR PRO VENTA', '', '', '', '', '', '', true, true, NULL);

-- ---------------------------------------------------------------------
-- 2. RELACIONES SEMANALES (4)
--    relation_number unico, correlativo global, orden cronologico
-- ---------------------------------------------------------------------
INSERT INTO relacion (relation_number, week_start, week_end)
SELECT v.rn, v.ws::date, v.we::date
FROM (VALUES
    (1, '2026-01-19', '2026-01-25'),
    (2, '2026-05-11', '2026-05-17'),
    (3, '2026-08-17', '2026-08-23'),
    (4, '2026-09-28', '2026-10-04')
) AS v(rn, ws, we)
WHERE NOT EXISTS (SELECT 1 FROM relacion r WHERE r.week_start = v.ws::date);

-- ---------------------------------------------------------------------
-- 3. NOTAS ZOMBIE PRO VENTA (4) - una por semana, saldo consolidado
--    id_seller = 4 (Juan Luis, 004)   note_type_id = 2 (MAR)
--    id_relacion: une cada nota con su semana, sin esto ProVentaService
--    no la muestra (filtra n.id_relacion != null)
-- ---------------------------------------------------------------------
INSERT INTO delivery_note
    (note_number, creation_date, dispatch_date, id_seller, id_customer,
     total_amount_usd, adjusted_total_usd, status, cxc_observations,
     note_type_id, discount_percentage, promo_discount_percentage,
     volume_discount_percentage, original_discount_percentage,
     original_volume_discount_percentage, id_relacion)
SELECT
    v.note_number, v.creation_date, v.dispatch_date, 4, cu.id_customer,
    v.amount, v.amount, 'Pendiente', 'Saldo anterior en el Excel',
    2, 0::numeric, 0::numeric, 0::numeric, 0::numeric, 0::numeric,
    r.id_relacion
FROM (VALUES
    ('000_055', '2026-01-25T12:00:00+00'::timestamptz, '2026-01-25T12:00:00+00'::timestamptz, 9155.11::numeric, '2026-01-19'),
    ('000_056', '2026-05-11T12:00:00+00'::timestamptz, '2026-05-11T12:00:00+00'::timestamptz, 3433.84::numeric, '2026-05-11'),
    ('000_057', '2026-08-19T12:00:00+00'::timestamptz, '2026-08-19T12:00:00+00'::timestamptz, 2937.96::numeric, '2026-08-17'),
    ('000_058', '2026-09-30T12:00:00+00'::timestamptz, '2026-09-30T12:00:00+00'::timestamptz, 6509.16::numeric, '2026-09-28')
) AS v(note_number, creation_date, dispatch_date, amount, week_start)
JOIN customer cu ON cu.customer_code = 'PV-ANT-001'
JOIN relacion r  ON r.week_start = v.week_start::date
WHERE NOT EXISTS (SELECT 1 FROM delivery_note dn WHERE dn.note_number = v.note_number);

COMMIT;

-- =====================================================================
-- ROLLBACK (ejecutar solo si hay que deshacer)
-- =====================================================================
-- DELETE FROM delivery_note WHERE note_number IN ('000_055','000_056','000_057','000_058');
-- DELETE FROM relacion    WHERE relation_number BETWEEN 1 AND 4
--                         AND NOT EXISTS (SELECT 1 FROM delivery_note d WHERE d.id_relacion = relacion.id_relacion);
-- DELETE FROM customer    WHERE customer_code = 'PV-ANT-001';