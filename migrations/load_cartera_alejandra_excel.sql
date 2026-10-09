-- =====================================================================
-- CARTERA ALEJANDRA - 22 NOTAS ZOMBIE (desde Excel)
-- Generado: 2026-10-09
--
-- Que es esto:
--   22 notas de entrega sin detalle de producto que representan saldos por
--   cobrar migrados del Excel de Alejandra. Sin note_detail, sin stock, sin
--   kardeex, sin productos: solo el saldo que el cliente debe.
--
-- Numeracion (decidido con el usuario, igual que en la carga de Anais):
--   El Excel usa '3500-011' (guion medio). ParseFullNumber de SeriesCalculator
--   exige guion bajo ('002_001') y trata '3500-011' como 0, lo que rompe el
--   orden de la lista y el calculo del siguiente correlativo. Por eso se
--   convierten al correlativo global de la app, arrancando en 000_109 (el
--   siguiente libre despues de 000_108, que ocupan las notas de Anais).
--   El numero original del Excel NO se conserva.
--
--   Nota del CSV: 3500-292 aparece con prefijo 3300 (de Anais). Es el mismo
--   formato, asi que la conversion es identica.
--
-- Correccion aplicada (acordada con el usuario):
--   El codigo 3500-048 venia duplicado en el CSV, en dos clientes distintos:
--   COMERCIALIZADORA CASA POPULAR 2068 (173,20) y JADE STUDIO DE BELLEZA
--   (178,08). Se le asigna 3500-055 a JADE STUDIO, que era el siguiente
--   libre; CASA POPULAR conserva el 048.
--
-- Nombres que se cargan tal cual, sin corregir (acordado con el usuario):
--   'SARAITH MARYELIS BERNAL'  (no SARITH)
--   'RORO'                     (no YORRO)
--   Se mantienenen tal cual para que el cliente decida al registrarlos.
--
-- Fechas:
--   creation_date = hoy (2026-10-09) al mediodia hora de Venezuela.
--   dispatch_date = la del CSV cuando viene; vacio (NULL) en las 11 que no
--   traen fecha, segun lo pedido. El CSV de Alejandra usa dd/mm/yyyy.
--
-- Clientes: 20 zombies con is_ghost = true, invisibles en Clientes y en la
--   Papelera (filtro is_ghost de CustomerService) pero presentes en los
--   respaldos, que usan GetAllCustomersAsync sin filtrar.
--
-- Tipo: General (note_type_id = NULL), nunca Pro Venta, segun AGENTS.md.
--
-- Reversible con el bloque DELETE del final (marcado).
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1. CLIENTES ZOMBIE (20)
-- ---------------------------------------------------------------------
INSERT INTO customer
    (customer_code, business_name, rif, contact_name, phone_number,
     fiscal_address, delivery_address, seller_name, is_active, is_ghost, id_zona)
VALUES
    ('ANT-L001', 'COMERCIAL EL FUTURO STYLE C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L002', 'COMERCIAL FONG E Y E C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L003', 'COMERCIAL PLAZA SUR C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L004', 'COMERCIAL PUEBLO TACARIGUA 888', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L005', 'COMERCIALIZADORA CASA POPULAR 2068', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L006', 'CONFITERIA TACARIGUA', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L007', 'COSMETICOS GLOBAL T.A. C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L008', 'DISTRIBUIDORA X-COLOR''S. C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L009', 'EMPRENDIMIENTO ANDREINA MACHADO 3', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L010', 'INVERSIONES QIANG 2024 C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L011', 'INVERSIONES YORKLEY TV C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L012', 'INVERSONES LA GRANDEZA LIANG C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L013', 'JADE STUDIO DE BELLEZA C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L014', 'LEDY SILOVE', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L015', 'MEGA SOL C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L016', 'MINILADS', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L017', 'RORO', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L018', 'SARAITH MARYELIS BERNAL', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L019', 'VARIEDADES ELIMAR C.A', '', '', '', '', '', '', true, true, NULL),
    ('ANT-L020', 'VICTORY TODAY', '', '', '', '', '', '', true, true, NULL)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------
-- 2. NOTAS (22) - id_seller = 3 (Alejandra, 003)
--    creation_date = 2026-10-09 12:00 Venezuela
--    dispatch_date = NULL cuando el CSV no trae fecha
-- ---------------------------------------------------------------------
INSERT INTO delivery_note
    (note_number, creation_date, dispatch_date, id_seller, id_customer,
     total_amount_usd, adjusted_total_usd, status, cxc_observations,
     note_type_id, discount_percentage, promo_discount_percentage,
     volume_discount_percentage, original_discount_percentage,
     original_volume_discount_percentage)
SELECT
    v.note_number, v.creation_date, v.dispatch_date, 3, cu.id_customer,
    v.amount, v.amount, 'Pendiente', 'Saldo anterior en el Excel',
    NULL, 0::numeric, 0::numeric, 0::numeric, 0::numeric, 0::numeric
FROM (VALUES
    ('000_109', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-L009', 106.24::numeric),
    ('000_110', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-L002', 124.90::numeric),
    ('000_111', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-07T12:00:00-04:00'::timestamptz,         'ANT-L014',  89.72::numeric),
    ('000_112', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-07T12:00:00-04:00'::timestamptz,         'ANT-L014',  14.40::numeric),
    ('000_113', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-L018',  84.80::numeric),
    ('000_114', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-L008',  99.36::numeric),
    ('000_115', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-L019',  48.53::numeric),
    ('000_116', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-18T12:00:00-04:00'::timestamptz,         'ANT-L010', 207.04::numeric),
    ('000_117', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-18T12:00:00-04:00'::timestamptz,         'ANT-L015', 177.28::numeric),
    ('000_118', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-18T12:00:00-04:00'::timestamptz,         'ANT-L001',  81.28::numeric),
    ('000_119', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-18T12:00:00-04:00'::timestamptz,         'ANT-L005', 173.20::numeric),
    ('000_120', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-18T12:00:00-04:00'::timestamptz,         'ANT-L003', 110.72::numeric),
    ('000_121', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-L016', 107.20::numeric),
    ('000_122', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-18T12:00:00-04:00'::timestamptz,         'ANT-L007', 626.35::numeric),
    ('000_123', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-18T12:00:00-04:00'::timestamptz,         'ANT-L011', 610.77::numeric),
    ('000_124', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-18T12:00:00-04:00'::timestamptz,         'ANT-L017', 173.56::numeric),
    ('000_125', '2026-10-09T12:00:00-04:00'::timestamptz, '2026-09-22T12:00:00-04:00'::timestamptz,         'ANT-L008', 255.28::numeric),
    ('000_126', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-L013', 178.08::numeric),
    ('000_127', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-L020', 173.76::numeric),
    ('000_128', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-L004',  99.84::numeric),
    ('000_129', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-L006', 739.20::numeric),
    ('000_130', '2026-10-09T12:00:00-04:00'::timestamptz, NULL::timestamptz,                              'ANT-L012', 284.48::numeric)
) AS v(note_number, creation_date, dispatch_date, customer_code, amount)
JOIN customer cu ON cu.customer_code = v.customer_code
WHERE NOT EXISTS (SELECT 1 FROM delivery_note dn WHERE dn.note_number = v.note_number);

COMMIT;

-- =====================================================================
-- ROLLBACK (ejecutar solo si hay que deshacer)
-- =====================================================================
-- DELETE FROM delivery_note WHERE note_number BETWEEN '000_109' AND '000_130';
-- DELETE FROM customer    WHERE customer_code LIKE 'ANT-L%';