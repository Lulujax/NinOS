-- =====================================================================
-- FIX: FECHAS DE LAS NOTAS ZOMBIE DE SANDRA
-- Generado y ejecutado: 2026-10-09
--
-- Que corrige:
--   La inyeccion inicial (load_cartera_sandra_zombie.sql) cargo las 11 notas
--   con creation_date = 2026-10-09 (el dia de la carga), asi que todas caian
--   en octubre y aparecian juntas en el mes en curso.
--
--   El Excel 'POR COBRAR SANDRA.xlsx' esta dividido en bloques por mes de
--   venta, y cada nota debe caer en el mes de su bloque. El dia exacto no
--   existe en el Excel: se usa el 01 a las 12:00 de Venezuela (16:00 UTC),
--   que garantiza que la nota quede dentro de su mes al filtrar por mes.
--
--     Bloque del Excel                          Mes        Notas
--     ----------------------------------------  ---------  ----------------
--     PENDIENTE POR COBRAR _ CLIENTES 2025      mar/2025   3200-462 (1)
--     PENDIENTE POR COBRAR _ CLIENTES 2025      nov/2025   3200-639 (1)
--     VENTAS  FEBRERO  2026-  SANDRA            feb/2026   3200-684
--     VENTAS  MAYO  2026-  SANDRA               may/2026   3200-691
--     VENTAS  JUNIO  2026-  SANDRA              jun/2026   3200-703, 3200-704,
--                                                          3200-706, 3200-712,
--                                                          3200-713, 3200-714,
--                                                          3200-715
--
--   (1) El bloque 'CLIENTES 2025' no tiene secciones por mes. Las fechas
--       08/03/2025 (COSMETICOS TINTES Y ALGO MAS) y 14/11/2025 (FARIÑA
--       DISTRIBUCIONES) las dio el usuario.
--
--   Sobre el bloque de junio: su encabezado dice JUNIO 2026 y su cierre dice
--   'TOTAL POR COBRAR SEPTIEMBRE 2026'. Son ventas de junio cobradas despues:
--   dentro del bloque hay abonos del 3/7/26, 6/8/26, 20-21/8/26, 3/9/2026,
--   15/9/2026, 23/9/2026 y 3/10/2026, todos posteriores a junio.
--
--   dispatch_date queda como esta (NULL): el Excel no trae fecha de despacho.
--
-- Idempotente: se puede volver a correr sin efecto adicional.
-- =====================================================================

BEGIN;

-- Bloque 'VENTAS FEBRERO 2026'
UPDATE delivery_note SET creation_date = '2026-02-01T12:00:00-04:00'::timestamptz
WHERE note_number = '3200-684';

-- Bloque 'VENTAS MAYO 2026'
UPDATE delivery_note SET creation_date = '2026-05-01T12:00:00-04:00'::timestamptz
WHERE note_number = '3200-691';

-- Bloque 'VENTAS JUNIO 2026'
UPDATE delivery_note SET creation_date = '2026-06-01T12:00:00-04:00'::timestamptz
WHERE note_number IN ('3200-703','3200-704','3200-706','3200-712','3200-713','3200-714','3200-715');

-- Bloque 'PENDIENTE POR COBRAR _ CLIENTES 2025' (fechas dadas por el usuario)
UPDATE delivery_note SET creation_date = '2025-03-08T12:00:00-04:00'::timestamptz
WHERE note_number = '3200-462';

UPDATE delivery_note SET creation_date = '2025-11-14T12:00:00-04:00'::timestamptz
WHERE note_number = '3200-639';

COMMIT;

-- =====================================================================
-- VERIFICACION (5 meses: 2025-03, 2025-11, 2026-02, 2026-05, 2026-06)
-- =====================================================================
-- SELECT to_char(creation_date + interval '-4 hours', 'YYYY-MM') AS mes,
--        count(*) AS notas, round(sum(adjusted_total_usd),2) AS saldo,
--        string_agg(note_number, ', ' ORDER BY note_number) AS numeros
-- FROM delivery_note WHERE note_number LIKE '3200-%' GROUP BY 1 ORDER BY 1;
