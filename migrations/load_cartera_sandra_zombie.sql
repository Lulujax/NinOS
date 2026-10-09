-- =====================================================================
-- CARTERA SANDRA - 11 NOTAS ZOMBIE (desde Excel)
-- Generado: 2026-10-09
--
-- Que es esto:
--   11 notas de entrega sin detalle de producto que representan saldos por
--   cobrar migrados del Excel 'POR COBRAR SANDRA.xlsx'. Sin note_detail, sin
--   stock, sin kardeex, sin productos: solo el saldo que el cliente debe.
--
-- Formato del numero (decidido con el usuario):
--   El Excel trae '3200_462' (guion bajo). Ese formato SI lo interpreta
--   SeriesCalculator.ParseFullNumber -> 3.200.462, y como la serie de notas es
--   GLOBAL (DeliveryNoteRepository.get_next_correlative_async usa
--   GetNextGlobal6 sobre TODOS los numeros de nota), la proxima nota que
--   emitiera la app saldria '3200_463' en vez de '000_006'.
--   Por eso se cargan con guion medio ('3200-462'), igual que las 73 notas de
--   cartera heredada que ya estan en la base (3300-249..., 3500-011...,
--   PV-001...): el numero se lee casi identico y no desplaza la serie de la app.
--
-- Fechas (cada nota en el mes en que se vendio):
--   El Excel esta dividido en bloques por mes de venta. Cada nota lleva el mes
--   de su bloque, dia 01 a las 12:00 de Venezuela (16:00 UTC), que es lo unico
--   que el Excel permite saber: no trae dia exacto.
--
--     Bloque del Excel                          Mes cargado     Notas
--     ----------------------------------------  --------------  -------------
--     PENDIENTE POR COBRAR _ CLIENTES 2025      mar/2025        3200-462 (1)
--     PENDIENTE POR COBRAR _ CLIENTES 2025      nov/2025        3200-639 (1)
--     VENTAS  FEBRERO  2026-  SANDRA            feb/2026        3200-684 (1)
--     VENTAS  MAYO  2026-  SANDRA               may/2026        3200-691 (1)
--     VENTAS  JUNIO  2026-  SANDRA              jun/2026        3200-703..715 (7)
--
--   (1) El bloque 'CLIENTES 2025' no tiene seccion por mes (cierra con
--       'TOTAL POR COBRAR AL 16 FEBRERO 2026'): las fechas 08/03/2025 para
--       COSMETICOS y 14/11/2025 para FARIÑA las dio el usuario.
--
--   Nota sobre el bloque de junio: su encabezado dice JUNIO 2026 pero su
--   cierre dice 'TOTAL POR COBRAR SEPTIEMBRE 2026'. Son ventas de JUNIO
--   cobradas despues: dentro del bloque hay abonos del 3/7/26, 6/8/26,
--   20-21/8/26, 3/9/2026, 15/9/2026, 23/9/2026 y 3/10/2026, todos POSTERIORES
--   a junio (y por lo tanto no puede tratarse de ventas de septiembre).
--
--   dispatch_date: el Excel no trae fecha de despacho, queda NULL.
--
-- Montos:
--   Se carga la columna 'SALDO POR COBRAR' (monto menos abonos), que es lo que
--   el cliente todavia debe. Los abonos ya recibidos NO se cargan como payment:
--   quedan escritos en cxc_observations, igual que en las cargas anteriores.
--
--   Caso 3200_462 (acordado con el usuario): el Excel dice monto 217,77,
--   abono 30,00 y saldo 30,00; los numeros no cierran (monto - abono = 187,77).
--   Se carga el saldo del Excel: 30,00.
--
-- Clientes: 8 zombies con is_ghost = true, codigos 00164..00171 (continuacion
--   de los 46 fantasmas que ya ocupan 00001..00046), en la zona '-' (code 99).
--   Invisibles en Clientes y en la Papelera (filtro is_ghost de
--   CustomerService) pero presentes en los respaldos, que usan
--   GetAllCustomersAsync sin filtrar. Efecto: el proximo cliente real que se
--   cree sera 00172.
--
-- Tipo: General (note_type_id = NULL), nunca Pro Venta, segun AGENTS.md.
--
-- Los nombres se cargan tal cual el Excel, sin corregir (mismo criterio que en
--   la carga de Alejandra): 'FARIÑA DISTRIBUCIONES, C.A.' y
--   'SUMMER 25,.  C.A' quedan como estan para que el cliente decida.
--
-- Reversible con el bloque DELETE del final (marcado).
-- =====================================================================

BEGIN;

-- El archivo esta en UTF-8 (el nombre FARIÑA lleva Ñ).
SET client_encoding = 'UTF8';

-- ---------------------------------------------------------------------
-- 1. CLIENTES ZOMBIE (8) - codigos 00164..00171, zona '-' (code 99)
-- ---------------------------------------------------------------------
INSERT INTO customer
    (customer_code, business_name, rif, contact_name, phone_number,
     fiscal_address, delivery_address, seller_name, is_active, is_ghost, id_zona)
SELECT v.customer_code, v.business_name, '', '', '', '', '', '',
       true, true, (SELECT id_zona FROM zona WHERE code = '99')
FROM (VALUES
    ('00164', 'COSMETICOS TINTES Y ALGO MAS'),
    ('00165', 'FARIÑA DISTRIBUCIONES, C.A.'),
    ('00166', 'SALON DE BELLEZA SILVIA'),
    ('00167', 'MAGIC C.A'),
    ('00168', 'SUPERCENTER 168, C.A.'),
    ('00169', 'MAXI CENTER 88 C.A'),
    ('00170', 'SUMMER 25,.  C.A'),
    ('00171', 'CENTRO BOTANICO NATURAL LA PRADERA, C.A.')
) AS v(customer_code, business_name)
WHERE NOT EXISTS (SELECT 1 FROM customer c WHERE c.customer_code = v.customer_code);

-- ---------------------------------------------------------------------
-- 2. NOTAS (11) - id_seller = 1 (Sandra, 001)
--    total = adjusted = saldo pendiente del Excel
--    creation_date = mes del bloque del Excel (dia 01, 12:00 Venezuela)
--    El abono ya recibido va descrito en cxc_observations, no como pago.
-- ---------------------------------------------------------------------
INSERT INTO delivery_note
    (note_number, creation_date, dispatch_date, id_seller, id_customer,
     total_amount_usd, adjusted_total_usd, status, cxc_observations,
     note_type_id, discount_percentage, promo_discount_percentage,
     volume_discount_percentage, original_discount_percentage,
     original_volume_discount_percentage)
SELECT
    v.note_number, v.creation_date, v.dispatch_date, 1, cu.id_customer,
    v.amount, v.amount, 'Pendiente', v.obs,
    NULL, 0::numeric, 0::numeric, 0::numeric, 0::numeric, 0::numeric
FROM (VALUES
    -- Bloque 'PENDIENTE POR COBRAR _ CLIENTES 2025'
    ('3200-462', '2025-03-08T12:00:00-04:00'::timestamptz, NULL::timestamptz, '00164'::text,  30.00::numeric,
     'Saldo anterior en el Excel | 29/8/26 BS 23.750 REF-45712'),
    ('3200-639', '2025-11-14T12:00:00-04:00'::timestamptz, NULL::timestamptz, '00165'::text, 300.00::numeric,
     'Saldo anterior en el Excel | ABONO EL 17/6/26 73.189,10BS=122,64 REF-17209 | ABONO 23/9/2026 85.350BS=100$ REF-63263'),
    -- Bloque 'VENTAS FEBRERO 2026'
    ('3200-684', '2026-02-01T12:00:00-04:00'::timestamptz, NULL::timestamptz, '00166'::text, 112.26::numeric,
     'Saldo anterior en el Excel'),
    -- Bloque 'VENTAS MAYO 2026'
    ('3200-691', '2026-05-01T12:00:00-04:00'::timestamptz, NULL::timestamptz, '00165'::text, 350.72::numeric,
     'Saldo anterior en el Excel'),
    -- Bloque 'VENTAS JUNIO 2026'
    ('3200-703', '2026-06-01T12:00:00-04:00'::timestamptz, NULL::timestamptz, '00164'::text, 189.78::numeric,
     'Saldo anterior en el Excel'),
    ('3200-704', '2026-06-01T12:00:00-04:00'::timestamptz, NULL::timestamptz, '00164'::text, 172.80::numeric,
     'Saldo anterior en el Excel'),
    ('3200-706', '2026-06-01T12:00:00-04:00'::timestamptz, NULL::timestamptz, '00167'::text,  33.28::numeric,
     'Saldo anterior en el Excel'),
    ('3200-712', '2026-06-01T12:00:00-04:00'::timestamptz, NULL::timestamptz, '00168'::text, 643.68::numeric,
     'Saldo anterior en el Excel'),
    ('3200-713', '2026-06-01T12:00:00-04:00'::timestamptz, NULL::timestamptz, '00169'::text, 320.76::numeric,
     'Saldo anterior en el Excel'),
    ('3200-714', '2026-06-01T12:00:00-04:00'::timestamptz, NULL::timestamptz, '00170'::text, 529.58::numeric,
     'Saldo anterior en el Excel'),
    ('3200-715', '2026-06-01T12:00:00-04:00'::timestamptz, NULL::timestamptz, '00171'::text, 309.00::numeric,
     'Saldo anterior en el Excel')
) AS v(note_number, creation_date, dispatch_date, customer_code, amount, obs)
JOIN customer cu ON cu.customer_code = v.customer_code
WHERE NOT EXISTS (SELECT 1 FROM delivery_note dn WHERE dn.note_number = v.note_number);

COMMIT;

-- =====================================================================
-- VERIFICACION (debe dar 8 clientes / 11 notas / 2991.86, repartidas en
-- mar-2025, nov-2025, feb-2026, may-2026 y jun-2026)
-- =====================================================================
-- SELECT to_char(creation_date + interval '-4 hours', 'YYYY-MM') AS mes,
--        count(*) AS notas, round(sum(adjusted_total_usd),2) AS saldo,
--        string_agg(note_number, ', ' ORDER BY note_number) AS numeros
-- FROM delivery_note WHERE note_number LIKE '3200-%' GROUP BY 1 ORDER BY 1;
--
-- SELECT c.customer_code, c.business_name, n.note_number, n.adjusted_total_usd, n.status
-- FROM delivery_note n JOIN customer c ON c.id_customer = n.id_customer
-- WHERE n.note_number LIKE '3200-%' ORDER BY n.note_number;

-- =====================================================================
-- ROLLBACK (ejecutar solo si hay que deshacer)
-- =====================================================================
-- DELETE FROM delivery_note WHERE note_number IN
--     ('3200-462','3200-639','3200-684','3200-691','3200-703','3200-704',
--      '3200-706','3200-712','3200-713','3200-714','3200-715');
-- DELETE FROM customer WHERE customer_code BETWEEN '00164' AND '00171';
