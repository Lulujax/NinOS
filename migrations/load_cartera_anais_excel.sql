-- =====================================================================
-- CARTERA ANAIS - 30 NOTAS ZOMBIE (desde Excel)
-- Generado: 2026-10-09
--
-- Que es esto:
--   30 notas de entrega sin detalle de producto que representan saldos por
--   cobrar migrados del Excel de Anais. Sin note_detail, sin stock, sin
--   kardeex, sin productos: solo el saldo que el cliente debe.
--
-- Numeracion (decidido con el usuario):
--   El Excel usa '3300-249' (guion medio). ParseFullNumber de SeriesCalculator
--   exige guion bajo ('002_001') y trata '3300-249' como 0, lo que rompe el
--   orden de la lista y el calculo del siguiente correlativo. Por eso se
--   convierten al correlativo global de la app, arrancando en 000_079 (el
--   siguiente libre despues de 000_075, que ocupan las notas Pro Venta).
--   El numero original del Excel NO se conserva.
--
--   3300-249 -> 000_079
--   3300-264 -> 000_085
--   ...      (la correspondencia es en el mismo orden del CSV)
--
-- Fechas:
--   creation_date = hoy (2026-10-09) al mediodia hora de Venezuela, para que
--   las notas caigan en el mes en curso y no se mezclen con meses anteriores.
--   dispatch_date = la del CSV cuando viene; vacio (NULL) en las 23 que no
--   traen fecha, segun lo pedido.
--
-- Clientes: 25 zombies con is_ghost = true, invisibles en Clientes y en la
--   Papelera (filtro is_ghost de CustomerService) pero presentes en los
--   respaldos, que usan GetAllCustomersAsync sin filtrar.
--
-- Tipo: General (note_type_id = NULL), nunca Pro Venta, segun AGENTS.md.
--
-- Reversible con el bloque DELETE del final (marcado).
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1. CLIENTES ZOMBIE (25)
-- ---------------------------------------------------------------------
INSERT INTO customer
    (customer_code, business_name, rif, contact_name, phone_number,
     fiscal_address, delivery_address, seller_name, is_active, is_ghost, id_zona)
VALUES
    ('ANT-A001', 'ALICIA COROMOTO VARGAS RODRIGUEZ', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A002', 'ALTA PELUQUERIA LORENZO Y NIKO C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A003', 'BELLGLAM C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A004', 'BELLISSIMA CORP CENTRO C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A005', 'BELLISSIMA LAS FERIAS C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A006', 'BELLEZA QUE INSPIRA', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A007', 'COMERCIAL DIFERENTE C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A008', 'COMERCIAL HAPPY PLUS C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A009', 'COMERCIAL METRO LAS FERIAS 333 C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A010', 'COMERCIALIZADORA CENTRO UNIVERSAL C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A011', 'COMERCIALIZADORA KIKEN 23 C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A012', 'DISTRIBUIDORA CINTHIA 2011 C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A013', 'DISTRIBUIDORA ESTETICA SALUD Y BELLEZA BLISS SPA', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A014', 'EMPRENDIMIENTO ANAHID CARRERA', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A015', 'EZEVICVARIADADES YANET STYLOS', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A016', 'HELIUS C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A017', 'ILUSION''S ASIA CENTER C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A018', 'INVERSIONES BULEVAR CENTER 88 C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A019', 'INVERSIONES SAM LYZ C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A020', 'JKOL C.A AV LARA', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A021', 'MUNDO MAYOR DOS C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A022', 'SUPER MERCADO MEGASUR C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A023', 'SUPER MERCADO PINO C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A024', 'TINTES Y MAQUILLAJE', '', '', '', '', '', '', true, true, NULL),
    ('ANT-A025', 'TODO BELLEZA EXPRESS C.A', '', '', '', '', '', '', true, true, NULL)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------
-- 2. NOTAS (30) - id_seller = 2 (Anais, 002)
--    creation_date = 2026-10-09 12:00 Venezuela (16:00 UTC)
--    dispatch_date = NULL cuando el CSV no trae fecha
-- ---------------------------------------------------------------------
INSERT INTO delivery_note
    (note_number, creation_date, dispatch_date, id_seller, id_customer,
     total_amount_usd, adjusted_total_usd, status, cxc_observations,
     note_type_id, discount_percentage, promo_discount_percentage,
     volume_discount_percentage, original_discount_percentage,
     original_volume_discount_percentage)
SELECT
    v.note_number, v.creation_date, v.dispatch_date, 2, cu.id_customer,
    v.amount, v.amount, 'Pendiente', 'Saldo anterior en el Excel',
    NULL, 0::numeric, 0::numeric, 0::numeric, 0::numeric, 0::numeric
FROM (VALUES
    ('000_079', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A014', 141.58::numeric),
    ('000_080', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A001',  91.63::numeric),
    ('000_081', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A001',  35.41::numeric),
    ('000_082', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A019', 106.21::numeric),
    ('000_083', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A020', 467.84::numeric),
    ('000_084', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A004', 357.77::numeric),
    ('000_085', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A004',  37.00::numeric),
    ('000_086', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A005', 208.04::numeric),
    ('000_087', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A004',  91.20::numeric),
    ('000_088', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A024', 101.00::numeric),
    ('000_089', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A012', 192.00::numeric),
    ('000_090', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-14T12:00:00-04:00'::timestamptz,         'ANT-A022', 227.02::numeric),
    ('000_091', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A025',  38.40::numeric),
    ('000_092', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-14T12:00:00-04:00'::timestamptz,         'ANT-A023', 325.12::numeric),
    ('000_093', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-18T12:00:00-04:00'::timestamptz,         'ANT-A008', 624.32::numeric),
    ('000_094', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-18T12:00:00-04:00'::timestamptz,         'ANT-A006', 222.00::numeric),
    ('000_095', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-17T12:00:00-04:00'::timestamptz,         'ANT-A007', 184.64::numeric),
    ('000_096', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-18T12:00:00-04:00'::timestamptz,         'ANT-A018', 424.96::numeric),
    ('000_097', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A016', 345.60::numeric),
    ('000_098', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A016', 353.60::numeric),
    ('000_099', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A002', 117.93::numeric),
    ('000_100', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-28T12:00:00-04:00'::timestamptz,         'ANT-A021', 374.32::numeric),
    ('000_101', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A013', 307.20::numeric),
    ('000_102', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A003', 245.12::numeric),
    ('000_103', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A004', 582.51::numeric),
    ('000_104', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A017',  77.44::numeric),
    ('000_105', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A015', 127.33::numeric),
    ('000_106', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A009', 616.64::numeric),
    ('000_107', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A011', 147.84::numeric),
    ('000_108', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-A010', 181.12::numeric)
) AS v(note_number, creation_date, dispatch_date, customer_code, amount)
JOIN customer cu ON cu.customer_code = v.customer_code
WHERE NOT EXISTS (SELECT 1 FROM delivery_note dn WHERE dn.note_number = v.note_number);

COMMIT;

-- =====================================================================
-- ROLLBACK (ejecutar solo si hay que deshacer)
-- =====================================================================
-- DELETE FROM delivery_note WHERE note_number BETWEEN '000_079' AND '000_108';
-- DELETE FROM customer    WHERE customer_code LIKE 'ANT-A%';