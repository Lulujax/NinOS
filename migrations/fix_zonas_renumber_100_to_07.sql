-- =====================================================================
-- RENUMERAR ZONAS ACTIVAS (100 -> 07, 101 -> 08, 102 -> 09, 103 -> 10)
-- MANTENIENDO LAS ZONAS ELIMINADAS EN LA PAPELERA
-- Fecha: 2026-10-24
-- =====================================================================

BEGIN;

-- 1. Renumerar Zona 6 (id_zona = 9) de '100' a '07'
UPDATE zona
SET code = '07', sort_order = 7
WHERE id_zona = 9 AND code = '100';

-- 2. Renumerar Zona 7 (id_zona = 10) de '101' a '08'
UPDATE zona
SET code = '08', sort_order = 8
WHERE id_zona = 10 AND code = '101';

-- 3. Renumerar Zona 9 VESU PRONTA (id_zona = 11) de '102' a '09'
UPDATE zona
SET code = '09', sort_order = 9
WHERE id_zona = 11 AND code = '102';

-- 4. Renumerar Zona 10 OFICINA VALENCIA (id_zona = 12) de '103' a '10'
UPDATE zona
SET code = '10', sort_order = 10
WHERE id_zona = 12 AND code = '103';

COMMIT;

-- =====================================================================
-- ROLLBACK (ejecutar solo en caso de revertir)
-- =====================================================================
-- BEGIN;
-- UPDATE zona SET code = '100', sort_order = 100 WHERE id_zona = 9 AND code = '07';
-- UPDATE zona SET code = '101', sort_order = 101 WHERE id_zona = 10 AND code = '08';
-- UPDATE zona SET code = '102', sort_order = 102 WHERE id_zona = 11 AND code = '09';
-- UPDATE zona SET code = '103', sort_order = 103 WHERE id_zona = 12 AND code = '10';
-- COMMIT;

