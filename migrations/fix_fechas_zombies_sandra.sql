-- =====================================================================
-- FIX: FECHAS DE LAS NOTAS ZOMBIE DE SANDRA
-- Generado y ejecutado: 2026-10-09
--
-- Que corrige:
--   La inyeccion inicial (load_cartera_sandra_zombie.sql) cargo las 11 notas
--   con creation_date = 2026-10-09 (el dia de la carga), asi que todas caian
--   en octubre. Este script deja cada nota con su fecha de venta real.
--
--   Todas las fechas son a las 12:00 de Venezuela (16:00 UTC). El dia 01 se usa
--   cuando la unica referencia es el mes; el dia exacto cuando el usuario lo
--   indico (3200-462, 3200-639, 3200-691, 3200-703, 3200-704 y 3200-706).
--
--     Nota        Fecha        Origen de la fecha
--     ----------  -----------  --------------------------------------------
--     3200-462    08/03/2025   usuario (bloque 'CLIENTES 2025', sin mes)
--     3200-639    14/11/2025   usuario (bloque 'CLIENTES 2025', sin mes)
--     3200-684    01/02/2026   bloque 'VENTAS FEBRERO 2026' (solo mes)
--     3200-691    05/05/2026   usuario (bloque 'VENTAS MAYO 2026')
--     3200-703    22/05/2026   usuario (bloque 'VENTAS JUNIO 2026')
--     3200-704    22/05/2026   usuario (bloque 'VENTAS JUNIO 2026')
--     3200-706    15/06/2026   usuario (bloque 'VENTAS JUNIO 2026')
--     3200-712    01/09/2026   bloque 'VENTAS JUNIO 2026' (solo mes, ver abajo)
--     3200-713    01/09/2026   idem
--     3200-714    01/09/2026   idem
--     3200-715    01/09/2026   idem
--
--   Sobre el bloque rotulado 'VENTAS JUNIO 2026': cierra con
--   'TOTAL POR COBRAR SEPTIEMBRE 2026' y adentro tiene abonos del 3/7/26, 6/8/26,
--   20-21/8/26, 3/9/2026, 15/9/2026, 23/9/2026 y 3/10/2026, o sea se le siguieron
--   agregando ventas sin renombrar el encabezado (19 notas contra 3-4 en marzo,
--   abril y mayo). Las 4 ultimas notas quedaron en septiembre por confirmacion
--   del usuario.
--
--   dispatch_date queda NULL: el Excel no trae fecha de despacho.
--
-- Idempotente: se puede volver a correr sin efecto adicional.
-- =====================================================================

BEGIN;

-- Bloque 'VENTAS FEBRERO 2026' (solo se conoce el mes)
UPDATE delivery_note SET creation_date = '2026-02-01T12:00:00-04:00'::timestamptz
WHERE note_number = '3200-684';

-- Bloque 'VENTAS MAYO 2026'
UPDATE delivery_note SET creation_date = '2026-05-05T12:00:00-04:00'::timestamptz
WHERE note_number = '3200-691';

-- Bloque 'VENTAS JUNIO 2026' -> 22/05/2026 (fecha exacta del usuario)
UPDATE delivery_note SET creation_date = '2026-05-22T12:00:00-04:00'::timestamptz
WHERE note_number IN ('3200-703','3200-704');

-- Bloque 'VENTAS JUNIO 2026' -> 15/06/2026 (fecha exacta del usuario)
UPDATE delivery_note SET creation_date = '2026-06-15T12:00:00-04:00'::timestamptz
WHERE note_number = '3200-706';

-- Bloque 'VENTAS JUNIO 2026' -> septiembre (solo se conoce el mes)
UPDATE delivery_note SET creation_date = '2026-09-01T12:00:00-04:00'::timestamptz
WHERE note_number IN ('3200-712','3200-713','3200-714','3200-715');

-- Bloque 'PENDIENTE POR COBRAR _ CLIENTES 2025' (fechas exactas del usuario)
UPDATE delivery_note SET creation_date = '2025-03-08T12:00:00-04:00'::timestamptz
WHERE note_number = '3200-462';

UPDATE delivery_note SET creation_date = '2025-11-14T12:00:00-04:00'::timestamptz
WHERE note_number = '3200-639';

COMMIT;

-- =====================================================================
-- VERIFICACION (11 notas / 2991.86)
-- =====================================================================
-- SELECT dn.note_number, to_char(dn.creation_date + interval '-4 hours','DD/MM/YYYY') AS fecha,
--        cu.business_name, dn.adjusted_total_usd
-- FROM delivery_note dn JOIN customer cu ON cu.id_customer = dn.id_customer
-- WHERE dn.id_seller = 1 ORDER BY dn.creation_date, dn.note_number;
