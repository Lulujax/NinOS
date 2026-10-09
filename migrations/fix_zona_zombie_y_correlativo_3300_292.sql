-- =====================================================================
-- ZONA "-" PARA CARTERA ZOMBIE + RENUMERAR 3300-292 -> 3500-292
-- Generado: 2026-10-09
--
-- Que corrige:
--   1. Las 73 notas zombie tenian id_zona = 1 ("Zona 1"), valor puesto por
--      las migrations de carga, no por una decision real. Una deuda vieja
--      migrada de Excel no pertenece a una zona: no se emitio en ningun
--      recorrido de venta. Se crea la zona "-", que es la que pide el
--      cliente, y se pasan ahi todos los clientes zombie (is_ghost).
--
--      No se tocan las 3 notas de clientes NO zombies que si estan en
--      Zona 1 (1 de Sandra, 2 de Anais): esas son organicas y conservan
--      su zona.
--
--   2. 3300-292 es la UNICA nota de Alejandra en el prefijo 3300, que es
--      de Anais (30 notas, 3300-249..3300-345). Las otras 21 notas zombie
--      de Alejandra ya estan en 3500-011..3500-059. Se renumera a
--      3500-292, que esta libre, para que toda la cartera de Alejandra
--      quede en su prefijo.
--
-- Reversible con el bloque DELETE del final (marcado).
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1. ZONA "-" (codigo 99, el siguiente libre)
-- ---------------------------------------------------------------------
INSERT INTO zona (code, name, sort_order)
SELECT '99', '-', 99
WHERE NOT EXISTS (SELECT 1 FROM zona WHERE code = '99');

-- ---------------------------------------------------------------------
-- 2. CLIENTES ZOMBIE A LA ZONA "-"
--    Solo is_ghost = true. Las organicas no se mueven.
-- ---------------------------------------------------------------------
UPDATE customer
SET id_zona = (SELECT id_zona FROM zona WHERE code = '99')
WHERE is_ghost = true;

-- ---------------------------------------------------------------------
-- 3. RENUMERAR 3300-292 -> 3500-292
-- ---------------------------------------------------------------------
UPDATE delivery_note
SET note_number = '3500-292'
WHERE note_number = '3300-292'
  AND NOT EXISTS (SELECT 1 FROM delivery_note WHERE note_number = '3500-292');

COMMIT;

-- =====================================================================
-- ROLLBACK (ejecutar solo si hay que deshacer)
-- =====================================================================
-- BEGIN;
-- UPDATE delivery_note SET note_number = '3300-292' WHERE note_number = '3500-292';
-- UPDATE customer SET id_zona = 1 WHERE is_ghost = true;
-- DELETE FROM zona WHERE code = '99';
-- COMMIT;