-- =====================================================================
-- CARTERA ANTIGUA (NOTAS ZOMBIE) - Anais y Alejandra
-- Generado: 2026-10-09
--
-- Que es esto:
--   45 clientes "zombie" (customer.is_ghost = true) + 54 notas de entrega
--   que SOLO representan saldos por cobrar previos. Sin note_detail, sin
--   stock, sin kardeex, sin productos.
--
-- Por que clientes zombie:
--   delivery_note.id_customer es NOT NULL con FK real a customer. No se puede
--   apuntar a un cliente inexistente. Los zombies existen solo para que las
--   notas tengan un id valido; are invisibles en Clientes y en la Papelera
--   gracias al filtro is_ghost de CustomerService.
--
--   customer_code con prefijo ANT-C### a proposito: NO matchea la regex
--   (?:^|_)(\d+)$ de SeriesCalculator.GetNextCustomerCode, por lo que no
--   altera la numeracion real de clientes (00001, 00002, ...).
--
-- Notas:
--   note_type_id = NULL  -> General, nunca Pro Venta (regla de AGENTS.md)
--   status       = Pendiente -> habilitadas para registrar abonos
--   Comisiones: se generan solas (10%) cuando la nota queda pagada, ya que
--   nunca se pagaron. Comportamiento correcto.
--
-- Reversible con el bloque DELETE del final (marcado).
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1. CLIENTES ZOMBIE (44)
-- ---------------------------------------------------------------------
INSERT INTO customer
    (customer_code, business_name, rif, contact_name, phone_number,
     fiscal_address, delivery_address, seller_name, is_active, is_ghost, id_zona)
VALUES
    ('ANT-C001', 'ALICIA COROMOTO VARGAS RODRIGUEZ', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C002', 'ANABEL DESIRE DAVILA MATOS', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C003', 'ALTA PELUQUERIA LORENZO Y NIKO C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C004', 'BELLGLAM C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C005', 'BELLISSIMA CORP CENTRO C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C006', 'BELLISSIMA LAS FERIAS C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C007', 'BELLEZA QUE INSPIRA', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C008', 'COMERCIAL DIFERENTE C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C009', 'COMERCIAL EL FUTURO STYLE C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C010', 'COMERCIAL FONG E Y E C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C011', 'COMERCIAL HAPPY PLUS C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C012', 'COMERCIAL METRO LAS FERIAS 333 C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C013', 'COMERCIAL PLAZA SUR C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C014', 'COMERCIALIZADORA CASA POPULAR 2068', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C015', 'COMERCIALIZADORA CENTRO UNIVERSAL C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C016', 'COMERCIALIZADORA KIKEN 23 C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C017', 'COSMETICOS GLOBAL T.A C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C018', 'DISTRIBUIDORA CINTHIA 2011 C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C019', 'DISTRIBUIDORA ESTETICA SALUD Y BELLEZA BLISS SPA', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C020', 'DISTRIBUIDORA X-COLOR''S C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C021', 'EMPRENDIMIENTO ANDREINA MACHADO', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C022', 'EMPRENDIMIENTO ANAHID CARRERA', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C023', 'EZEVICVARIADADES YANET STYLOS', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C024', 'HELIUS C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C025', 'ILUSION''S ASIA CENTER C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C026', 'INVERSIONES BULEVAR CENTER 88 C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C027', 'INVERSIONES QIANG 2024 C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C028', 'INVERSIONES SAM LYZ C A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C029', 'INVERSIONES YORKLEY TV C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C030', 'JKOL C.A AV LARA', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C031', 'JADE STUDIO DE BELLEZA C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C032', 'LEDY SILOVE', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C033', 'MEGA SOL C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C034', 'MIRIAN STYLE INVERSIONES ORI & OSCAR C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C035', 'MUNDO MAYOR DOS C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C036', 'SARAH MARYELIS BERNAL', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C037', 'SEVEN COSMETICOS C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C038', 'SOMOS GLAMW', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C039', 'SUPER MERCADO MEGASUR C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C040', 'SUPER MERCADO PINO C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C041', 'TINTES Y MAQUILLAJE', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C042', 'TODO BELLEZA EXPRESS C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C043', 'VARIEDADES RUMAR C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-C044', 'YORRO', '', '', '', '', '', '', true, true, NULL)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------
-- 2. NOTAS ZOMBIE (54) - sin detalle, sin stock
--    id_seller: 2 = Anais (002), 3 = Alejandra (003)
-- ---------------------------------------------------------------------
INSERT INTO delivery_note
    (note_number, creation_date, dispatch_date, id_seller, id_customer,
     total_amount_usd, adjusted_total_usd, status, cxc_observations,
     note_type_id, discount_percentage, promo_discount_percentage,
     volume_discount_percentage, original_discount_percentage,
     original_volume_discount_percentage)
SELECT
    v.note_number, v.creation_date, v.dispatch_date, v.id_seller,
    cu.id_customer,
    v.total, v.adjusted, v.status, v.obs,
    NULL::integer,          -- note_type_id = General, nunca Pro Venta
    0::numeric, 0::numeric, 0::numeric, 0::numeric, 0::numeric
FROM (VALUES
    -- Anais (id_seller = 2)
    ('ANT-0001', '2025-04-01T12:00:00+00'::timestamptz, '2025-04-01T12:00:00+00'::timestamptz, 2, 'ANT-C028'::text, 16.97::numeric,   16.97::numeric,   'Pendiente', 'Saldo anterior a la migración'),
    ('ANT-0002', '2025-09-03T12:00:00+00'::timestamptz, '2025-09-03T12:00:00+00'::timestamptz, 2, 'ANT-C034'::text, 116.69::numeric,  116.69::numeric,  'Pendiente', 'Saldo anterior a la migración'),
    ('ANT-0003', '2026-03-26T12:00:00+00'::timestamptz, '2026-03-26T12:00:00+00'::timestamptz, 2, 'ANT-C022'::text, 141.58::numeric,  141.58::numeric,  'Pendiente', 'Abono parcial'),
    ('ANT-0004', '2026-04-13T12:00:00+00'::timestamptz, '2026-04-13T12:00:00+00'::timestamptz, 2, 'ANT-C037'::text, 648.00::numeric,  648.00::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0005', '2026-04-27T12:00:00+00'::timestamptz, '2026-04-27T12:00:00+00'::timestamptz, 2, 'ANT-C001'::text, 91.63::numeric,   91.63::numeric,   'Pendiente', 'Sin pago'),
    ('ANT-0006', '2026-05-05T12:00:00+00'::timestamptz, '2026-05-05T12:00:00+00'::timestamptz, 2, 'ANT-C001'::text, 35.41::numeric,   35.41::numeric,   'Pendiente', 'Sin pago'),
    ('ANT-0007', '2026-05-11T12:00:00+00'::timestamptz, '2026-05-11T12:00:00+00'::timestamptz, 2, 'ANT-C035'::text, 249.26::numeric,  249.26::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0008', '2026-05-18T12:00:00+00'::timestamptz, '2026-05-18T12:00:00+00'::timestamptz, 2, 'ANT-C002'::text, 145.16::numeric,  145.16::numeric,  'Pendiente', 'Abono parcial'),
    ('ANT-0009', '2026-05-20T12:00:00+00'::timestamptz, '2026-05-20T12:00:00+00'::timestamptz, 2, 'ANT-C008'::text, 106.20::numeric,  106.20::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0010', '2026-05-25T12:00:00+00'::timestamptz, '2026-05-25T12:00:00+00'::timestamptz, 2, 'ANT-C028'::text, 106.21::numeric,  106.21::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0011', '2026-06-15T12:00:00+00'::timestamptz, '2026-06-15T12:00:00+00'::timestamptz, 2, 'ANT-C030'::text, 467.84::numeric,  467.84::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0012', '2026-06-15T12:00:00+00'::timestamptz, '2026-06-15T12:00:00+00'::timestamptz, 2, 'ANT-C032'::text, 14.40::numeric,   14.40::numeric,   'Pendiente', 'Sin pago'),
    ('ANT-0013', '2026-07-26T12:00:00+00'::timestamptz, '2026-07-26T12:00:00+00'::timestamptz, 2, 'ANT-C038'::text, 158.72::numeric,  158.72::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0014', '2026-07-26T12:00:00+00'::timestamptz, '2026-07-26T12:00:00+00'::timestamptz, 2, 'ANT-C005'::text, 357.77::numeric,  357.77::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0015', '2026-07-26T12:00:00+00'::timestamptz, '2026-07-26T12:00:00+00'::timestamptz, 2, 'ANT-C005'::text, 37.00::numeric,   37.00::numeric,   'Pendiente', 'Sin pago'),
    ('ANT-0016', '2026-07-26T12:00:00+00'::timestamptz, '2026-07-26T12:00:00+00'::timestamptz, 2, 'ANT-C006'::text, 208.04::numeric,  208.04::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0017', '2026-07-26T12:00:00+00'::timestamptz, '2026-07-26T12:00:00+00'::timestamptz, 2, 'ANT-C005'::text, 91.20::numeric,   91.20::numeric,   'Pendiente', 'Sin pago'),
    ('ANT-0018', '2026-08-25T12:00:00+00'::timestamptz, '2026-08-25T12:00:00+00'::timestamptz, 2, 'ANT-C041'::text, 101.00::numeric,  101.00::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0019', '2026-08-24T12:00:00+00'::timestamptz, '2026-08-24T12:00:00+00'::timestamptz, 2, 'ANT-C018'::text, 192.00::numeric,  192.00::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0020', '2026-09-04T12:00:00+00'::timestamptz, '2026-09-04T12:00:00+00'::timestamptz, 2, 'ANT-C039'::text, 227.02::numeric,  227.02::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0021', '2026-09-11T12:00:00+00'::timestamptz, '2026-09-11T12:00:00+00'::timestamptz, 2, 'ANT-C042'::text, 38.40::numeric,   38.40::numeric,   'Pendiente', 'Sin pago'),
    ('ANT-0022', '2026-09-11T12:00:00+00'::timestamptz, '2026-09-11T12:00:00+00'::timestamptz, 2, 'ANT-C040'::text, 325.12::numeric,  325.12::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0023', '2026-09-16T12:00:00+00'::timestamptz, '2026-09-16T12:00:00+00'::timestamptz, 2, 'ANT-C011'::text, 624.32::numeric,  624.32::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0024', '2026-09-16T12:00:00+00'::timestamptz, '2026-09-16T12:00:00+00'::timestamptz, 2, 'ANT-C007'::text, 222.00::numeric,  222.00::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0025', '2026-09-16T12:00:00+00'::timestamptz, '2026-09-16T12:00:00+00'::timestamptz, 2, 'ANT-C008'::text, 184.64::numeric,  184.64::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0026', '2026-09-16T12:00:00+00'::timestamptz, '2026-09-16T12:00:00+00'::timestamptz, 2, 'ANT-C026'::text, 424.96::numeric,  424.96::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0027', '2026-09-25T12:00:00+00'::timestamptz, '2026-09-25T12:00:00+00'::timestamptz, 2, 'ANT-C024'::text, 345.60::numeric,  345.60::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0028', '2026-09-25T12:00:00+00'::timestamptz, '2026-09-25T12:00:00+00'::timestamptz, 2, 'ANT-C024'::text, 353.60::numeric,  353.60::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0029', '2026-09-25T12:00:00+00'::timestamptz, '2026-09-25T12:00:00+00'::timestamptz, 2, 'ANT-C003'::text, 117.93::numeric,  117.93::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0030', '2026-09-25T12:00:00+00'::timestamptz, '2026-09-25T12:00:00+00'::timestamptz, 2, 'ANT-C035'::text, 374.32::numeric,  374.32::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0031', '2026-09-25T12:00:00+00'::timestamptz, '2026-09-25T12:00:00+00'::timestamptz, 2, 'ANT-C019'::text, 307.20::numeric,  307.20::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0032', '2026-09-27T12:00:00+00'::timestamptz, '2026-09-27T12:00:00+00'::timestamptz, 2, 'ANT-C004'::text, 245.12::numeric,  245.12::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0033', '2026-09-27T12:00:00+00'::timestamptz, '2026-09-27T12:00:00+00'::timestamptz, 2, 'ANT-C005'::text, 582.51::numeric,  582.51::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0034', '2026-09-27T12:00:00+00'::timestamptz, '2026-09-27T12:00:00+00'::timestamptz, 2, 'ANT-C025'::text, 77.44::numeric,   77.44::numeric,   'Pendiente', 'Sin pago'),
    ('ANT-0035', '2026-09-28T12:00:00+00'::timestamptz, '2026-09-28T12:00:00+00'::timestamptz, 2, 'ANT-C023'::text, 127.33::numeric,  127.33::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0036', '2026-09-30T12:00:00+00'::timestamptz, '2026-09-30T12:00:00+00'::timestamptz, 2, 'ANT-C012'::text, 616.64::numeric,  616.64::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0037', '2026-09-30T12:00:00+00'::timestamptz, '2026-09-30T12:00:00+00'::timestamptz, 2, 'ANT-C016'::text, 147.84::numeric,  147.84::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0038', '2026-09-30T12:00:00+00'::timestamptz, '2026-09-30T12:00:00+00'::timestamptz, 2, 'ANT-C015'::text, 181.12::numeric,  181.12::numeric,  'Pendiente', 'Sin pago'),
    -- Alejandra (id_seller = 3)
    ('ANT-0039', '2026-03-01T12:00:00+00'::timestamptz, '2026-03-01T12:00:00+00'::timestamptz, 3, 'ANT-C021'::text, 106.24::numeric,  106.24::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0040', '2026-06-01T12:00:00+00'::timestamptz, '2026-06-01T12:00:00+00'::timestamptz, 3, 'ANT-C010'::text, 124.90::numeric,  124.90::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0041', '2026-07-09T12:00:00+00'::timestamptz, '2026-07-09T12:00:00+00'::timestamptz, 3, 'ANT-C032'::text, 89.72::numeric,   89.72::numeric,   'Pendiente', 'Abono parcial'),
    ('ANT-0042', '2026-07-01T12:00:00+00'::timestamptz, '2026-07-01T12:00:00+00'::timestamptz, 3, 'ANT-C036'::text, 84.80::numeric,   84.80::numeric,   'Pendiente', 'Sin pago'),
    ('ANT-0043', '2026-07-01T12:00:00+00'::timestamptz, '2026-07-01T12:00:00+00'::timestamptz, 3, 'ANT-C020'::text, 99.36::numeric,   99.36::numeric,   'Pendiente', 'Sin pago'),
    ('ANT-0044', '2026-07-01T12:00:00+00'::timestamptz, '2026-07-01T12:00:00+00'::timestamptz, 3, 'ANT-C043'::text, 48.53::numeric,   48.53::numeric,   'Pendiente', 'Sin pago'),
    ('ANT-0045', '2026-07-18T12:00:00+00'::timestamptz, '2026-07-18T12:00:00+00'::timestamptz, 3, 'ANT-C027'::text, 207.04::numeric,  207.04::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0046', '2026-07-18T12:00:00+00'::timestamptz, '2026-07-18T12:00:00+00'::timestamptz, 3, 'ANT-C033'::text, 177.28::numeric,  177.28::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0047', '2026-07-18T12:00:00+00'::timestamptz, '2026-07-18T12:00:00+00'::timestamptz, 3, 'ANT-C009'::text, 81.28::numeric,   81.28::numeric,   'Pendiente', 'Sin pago'),
    ('ANT-0048', '2026-09-18T12:00:00+00'::timestamptz, '2026-09-18T12:00:00+00'::timestamptz, 3, 'ANT-C031'::text, 178.08::numeric,  178.08::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0049', '2026-09-18T12:00:00+00'::timestamptz, '2026-09-18T12:00:00+00'::timestamptz, 3, 'ANT-C014'::text, 173.20::numeric,  173.20::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0050', '2026-09-18T12:00:00+00'::timestamptz, '2026-09-18T12:00:00+00'::timestamptz, 3, 'ANT-C013'::text, 110.72::numeric,  110.72::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0051', '2026-09-18T12:00:00+00'::timestamptz, '2026-09-18T12:00:00+00'::timestamptz, 3, 'ANT-C017'::text, 626.35::numeric,  626.35::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0052', '2026-09-18T12:00:00+00'::timestamptz, '2026-09-18T12:00:00+00'::timestamptz, 3, 'ANT-C029'::text, 610.77::numeric,  610.77::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0053', '2026-09-18T12:00:00+00'::timestamptz, '2026-09-18T12:00:00+00'::timestamptz, 3, 'ANT-C044'::text, 173.56::numeric,  173.56::numeric,  'Pendiente', 'Sin pago'),
    ('ANT-0054', '2026-09-22T12:00:00+00'::timestamptz, '2026-09-22T12:00:00+00'::timestamptz, 3, 'ANT-C020'::text, 255.28::numeric,  255.28::numeric,  'Pendiente', 'Sin pago')
) AS v(note_number, creation_date, dispatch_date, id_seller, customer_code, total, adjusted, status, obs)
JOIN customer cu ON cu.customer_code = v.customer_code
WHERE NOT EXISTS (SELECT 1 FROM delivery_note dn WHERE dn.note_number = v.note_number);

COMMIT;

-- =====================================================================
-- ROLLBACK (ejecutar solo si hay que deshacer)
-- =====================================================================
-- DELETE FROM delivery_note WHERE note_number LIKE 'ANT-%';
-- DELETE FROM customer    WHERE customer_code LIKE 'ANT-C%';