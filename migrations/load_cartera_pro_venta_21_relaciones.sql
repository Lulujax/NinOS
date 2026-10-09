-- =====================================================================
-- CARTERA ANTIGUA PRO VENTA - 21 RELACIONES HUECAS (Juan Luis)
-- Generado: 2026-10-09
--
-- Que es esto:
--   21 relaciones de la tabla 'relacion' marcadas es_hueca = true, cada una
--   con su propio saldo, representing saldos por cobrar migrados del Excel.
--   Mas una nota MAR por relacion, sin detalle de producto, para que la
--   relacion exista en el flujo de Pro Venta.
--
-- Por que relacion y no solo notas:
--   En Pro Venta el abono es a nivel de relacion, no de nota
--   (PaymentService.register_relation_payment_async deja id_delivery_note = null
--   y cuelga el pago de id_relacion). El boton de abono vive en la fila de la
--   relacion (ProVentaViewModel.execute_pay_relation), asi que con 21
--   relaciones se abona cada una por separado sin abrir el detalle.
--
--   Para las relaciones huecas el saldo se lee de relacion.saldo, no de la suma
--   de notas (ProVentaService.build_relation_rows_async). Las notas son de
--   apoyo: dan cuerpo a la relacion, pero el monto que se muestra y se cobra
--   es el de la relacion.
--
-- Fechas: cada relacion usa la fecha exacta del Excel de origen y la semana
--   (lunes a domingo) a la que pertenece. No se movio ninguna fecha.
--
-- Aislamiento del flujo normal de Pro Venta:
--   - get_or_create_week_relation_async busca solo relaciones NO huecas, asi que
--     la primera nota Pro Venta real de la semana crea su propia relacion y no
--     se engancha a una de estas.
--   - renumber_relations_chronologically_async ignora las huecas y numera las
--     normales despues de la 21.
--   Verificado en la prueba de arranque posterior a la carga.
--
-- Nota conocida: la relacion 21 va del 28/09 al 04/10, asi que cruza de mes y
--   aparece al filtrar septiembre y tambien octubre (ProVentaViewModel filtra
--   por week_start O week_end). Es el diseno de semanas del sistema.
--
-- Reversible con el bloque DELETE del final (marcado).
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1. CLIENTE ZOMBIE COMPARTIDO (1)
--    Necesario porque delivery_note.id_customer es NOT NULL con FK real.
--    is_ghost = true lo oculta en Clientes y en la Papelera, pero deja que
--    los nombres se resuelvan en Pro Venta, Pagos y Comisiones.
-- ---------------------------------------------------------------------
INSERT INTO customer
    (customer_code, business_name, rif, contact_name, phone_number,
     fiscal_address, delivery_address, seller_name, is_active, is_ghost, id_zona)
VALUES
    ('PV-ANT-001', 'SALDO ANTERIOR PRO VENTA', '', '', '', '', '', '', true, true, NULL);

-- ---------------------------------------------------------------------
-- 2. RELACIONES HUECAS (21) - numero 1..21 segun el Excel de origen
--    week_start = lunes de la semana de la fecha; week_end = domingo
-- ---------------------------------------------------------------------
INSERT INTO relacion (relation_number, week_start, week_end, es_hueca, saldo, observaciones)
VALUES
    ( 1, '2026-01-19', '2026-01-25', true,  1164.26, 'Saldo anterior en el Excel'),
    ( 2, '2026-01-19', '2026-01-25', true,   950.33, 'Saldo anterior en el Excel'),
    ( 3, '2026-01-19', '2026-01-25', true,   820.72, 'Saldo anterior en el Excel'),
    ( 4, '2026-01-19', '2026-01-25', true,  1718.80, 'Saldo anterior en el Excel'),
    ( 5, '2026-01-19', '2026-01-25', true,  1269.73, 'Saldo anterior en el Excel'),
    ( 6, '2026-01-19', '2026-01-25', true,   755.77, 'Saldo anterior en el Excel'),
    ( 7, '2026-01-19', '2026-01-25', true,  1586.38, 'Saldo anterior en el Excel'),
    ( 8, '2026-01-19', '2026-01-25', true,   889.12, 'Saldo anterior en el Excel'),
    ( 9, '2026-05-11', '2026-05-17', true,  1598.72, 'Saldo anterior en el Excel'),
    (10, '2026-05-11', '2026-05-17', true,   796.49, 'Saldo anterior en el Excel'),
    (11, '2026-05-11', '2026-05-17', true,  1038.63, 'Saldo anterior en el Excel'),
    (12, '2026-08-17', '2026-08-23', true,  1634.01, 'Saldo anterior en el Excel'),
    (13, '2026-08-17', '2026-08-23', true,   659.63, 'Saldo anterior en el Excel'),
    (14, '2026-08-17', '2026-08-23', true,   644.32, 'Saldo anterior en el Excel'),
    (15, '2026-09-28', '2026-10-04', true,  1882.99, 'Saldo anterior en el Excel'),
    (16, '2026-09-28', '2026-10-04', true,   690.26, 'Saldo anterior en el Excel'),
    (17, '2026-09-28', '2026-10-04', true,    44.16, 'Saldo anterior en el Excel'),
    (18, '2026-09-28', '2026-10-04', true,  1935.42, 'Saldo anterior en el Excel'),
    (19, '2026-09-28', '2026-10-04', true,   993.66, 'Saldo anterior en el Excel'),
    (20, '2026-09-28', '2026-10-04', true,    54.67, 'Saldo anterior en el Excel'),
    (21, '2026-09-28', '2026-10-04', true,   908.00, 'Saldo anterior en el Excel');

-- ---------------------------------------------------------------------
-- 3. NOTAS DE APOYO (21) - una por relacion, sin producto ni detalle
--    id_seller = 4 (Juan Luis, 004)   note_type_id = 2 (MAR)
--    id_relacion: liga cada nota con su relacion hueca
--    El saldo de la nota replica el de la relacion para que el detalle de la
--    semana no muestre 0,00.
-- ---------------------------------------------------------------------
INSERT INTO delivery_note
    (note_number, creation_date, dispatch_date, id_seller, id_customer,
     total_amount_usd, adjusted_total_usd, status, cxc_observations,
     note_type_id, discount_percentage, promo_discount_percentage,
     volume_discount_percentage, original_discount_percentage,
     original_volume_discount_percentage, id_relacion)
SELECT
    v.note_number, v.creation_date::timestamptz, v.creation_date::timestamptz,
    4, cu.id_customer,
    r.saldo, r.saldo, 'Pendiente', 'Saldo anterior en el Excel',
    2, 0::numeric, 0::numeric, 0::numeric, 0::numeric, 0::numeric,
    r.id_relacion
FROM (VALUES
    ('000_055', '2026-01-25', 1),
    ('000_056', '2026-01-25', 2),
    ('000_057', '2026-01-25', 3),
    ('000_058', '2026-01-25', 4),
    ('000_059', '2026-01-25', 5),
    ('000_060', '2026-01-25', 6),
    ('000_061', '2026-01-25', 7),
    ('000_062', '2026-01-25', 8),
    ('000_063', '2026-05-11', 9),
    ('000_064', '2026-05-11', 10),
    ('000_065', '2026-05-11', 11),
    ('000_066', '2026-08-19', 12),
    ('000_067', '2026-08-19', 13),
    ('000_068', '2026-08-19', 14),
    ('000_069', '2026-09-30', 15),
    ('000_070', '2026-09-30', 16),
    ('000_071', '2026-09-30', 17),
    ('000_072', '2026-09-30', 18),
    ('000_073', '2026-09-30', 19),
    ('000_074', '2026-09-30', 20),
    ('000_075', '2026-09-30', 21)
) AS v(note_number, creation_date, relation_number)
JOIN relacion r      ON r.relation_number = v.relation_number AND r.es_hueca
JOIN customer cu     ON cu.customer_code = 'PV-ANT-001'
WHERE NOT EXISTS (SELECT 1 FROM delivery_note dn WHERE dn.note_number = v.note_number);

COMMIT;

-- =====================================================================
-- ROLLBACK (ejecutar solo si hay que deshacer)
-- =====================================================================
-- DELETE FROM delivery_note WHERE note_number BETWEEN '000_055' AND '000_075';
-- DELETE FROM relacion    WHERE es_hueca AND relation_number BETWEEN 1 AND 21;
-- DELETE FROM customer    WHERE customer_code = 'PV-ANT-001';