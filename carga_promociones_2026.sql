-- ============================================================
-- PROMOCIONES / COMBOS - LISTA 2026 OFICIAL
-- Opción B: recrear Oleos 3x2 con códigos limpios
-- ============================================================

BEGIN;

-- ============================================================
-- 1. ELIMINAR promos viejas de Oleos 3x2 (35-40)
--    No tienen ventas asociadas, se pueden borrar limpiamente
-- ============================================================
DELETE FROM promotion WHERE id_promotion IN (35, 36, 37, 38, 39, 40);

-- ============================================================
-- 2. RENOMBRAR promo 44 en vez de DELETE
--    (Tiene ventas históricas en note_detail y stock_movement,
--     si se borra viola la Foreign Key RESTRICT)
-- ============================================================
UPDATE promotion 
SET name = 'ANTERIOR - PROMO KID´S HAIR CLEAN ($1.00)' 
WHERE id_promotion = 44;

-- ============================================================
-- 3. ACTUALIZAR precios de ofertas existentes
-- ============================================================
UPDATE promotion SET unit_price_usd = 2.50 WHERE id_promotion = 42;  -- OFERTA KEDAM
UPDATE promotion SET unit_price_usd = 8.50 WHERE id_promotion = 43;  -- OFERTA POLVO

-- ============================================================
-- 4. TEMP TABLE con definición de las 16 promos nuevas
--    Un row por (promo, producto componente)
-- ============================================================
CREATE TEMP TABLE tmp_promos (
    promotion_code  varchar,
    promo_name      varchar,
    promo_price     numeric,
    product_code    varchar
) ON COMMIT PRESERVE ROWS;

INSERT INTO tmp_promos (promotion_code, promo_name, promo_price, product_code) VALUES

-- ---- Bloque A: Línea Blanca DEFILE 2x1 (1 promo, 2 items) ----
('C-PROMO-LB-TRICO-ACIDO', 'PROMO 2X1 LINEA BLANCA TRICOMPLEX CON ACIDO', 5.90, 'DEF30104'),
('C-PROMO-LB-TRICO-ACIDO', 'PROMO 2X1 LINEA BLANCA TRICOMPLEX CON ACIDO', 5.90, 'DEF30105'),

-- ---- Bloque B: Línea Rosa DEFILE 2x1 (6 promos, 12 items) ----
('C-PROMO-ROSA-MATIZADOR',  'PROMO 2X1 MATIZADOR',           5.90, 'DEF30100'),
('C-PROMO-ROSA-MATIZADOR',  'PROMO 2X1 MATIZADOR',           5.90, 'DEF30101'),
('C-PROMO-ROSA-REGULADOR',  'PROMO 2X1 REGULADOR',           5.90, 'DEF30110'),
('C-PROMO-ROSA-REGULADOR',  'PROMO 2X1 REGULADOR',           5.90, 'DEF30111'),
('C-PROMO-ROSA-ACIDO-HIAL', 'PROMO 2X1 ACIDO HIALURONICO',   5.90, 'DEF30104'),
('C-PROMO-ROSA-ACIDO-HIAL', 'PROMO 2X1 ACIDO HIALURONICO',   5.90, 'DEF30105'),
('C-PROMO-ROSA-ARGAN',      'PROMO 2X1 ARGAN',               5.90, 'DEF30112'),
('C-PROMO-ROSA-ARGAN',      'PROMO 2X1 ARGAN',               5.90, 'DEF30113'),
('C-PROMO-ROSA-VITAMINA-E', 'PROMO 2X1 TRICOMPLEX VITAMINA E', 5.90, 'DEF30102'),
('C-PROMO-ROSA-VITAMINA-E', 'PROMO 2X1 TRICOMPLEX VITAMINA E', 5.90, 'DEF30103'),
('C-PROMO-ROSA-KBOTROX',    'PROMO 2X1 K BOTROX',            5.90, 'DEF30108'),
('C-PROMO-ROSA-KBOTROX',    'PROMO 2X1 K BOTROX',            5.90, 'DEF30109'),

-- ---- Bloque C: Oleos 3x2 (6 promos, 18 items) ----
-- Frizz
('C-PROMO-OLEOS-FRIZZ',       'PROMO 3X2 OLEOS CONTROL FRIZZ',      12.80, 'OLE30305'),
('C-PROMO-OLEOS-FRIZZ',       'PROMO 3X2 OLEOS CONTROL FRIZZ',      12.80, 'OLE30306'),
('C-PROMO-OLEOS-FRIZZ',       'PROMO 3X2 OLEOS CONTROL FRIZZ',      12.80, 'OLE30317'),
-- Caspa
('C-PROMO-OLEOS-CASPA',       'PROMO 3X2 OLEOS CONTROL CASPA',      12.80, 'OLE30311'),
('C-PROMO-OLEOS-CASPA',       'PROMO 3X2 OLEOS CONTROL CASPA',      12.80, 'OLE30312'),
('C-PROMO-OLEOS-CASPA',       'PROMO 3X2 OLEOS CONTROL CASPA',      12.80, 'OLE30317'),
-- Caída
('C-PROMO-OLEOS-CAIDA',       'PROMO 3X2 OLEOS CONTROL CAIDA',      12.80, 'OLE30307'),
('C-PROMO-OLEOS-CAIDA',       'PROMO 3X2 OLEOS CONTROL CAIDA',      12.80, 'OLE30308'),
('C-PROMO-OLEOS-CAIDA',       'PROMO 3X2 OLEOS CONTROL CAIDA',      12.80, 'OLE30317'),
-- Restaurador
('C-PROMO-OLEOS-RESTAURADOR', 'PROMO 3X2 OLEOS RESTAURADOR',        12.80, 'OLE30309'),
('C-PROMO-OLEOS-RESTAURADOR', 'PROMO 3X2 OLEOS RESTAURADOR',        12.80, 'OLE30310'),
('C-PROMO-OLEOS-RESTAURADOR', 'PROMO 3X2 OLEOS RESTAURADOR',        12.80, 'OLE30317'),
-- Cuidado Diario
('C-PROMO-OLEOS-DIARIO',      'PROMO 3X2 OLEOS CUIDADO DIARIO',     12.80, 'OLE30313'),
('C-PROMO-OLEOS-DIARIO',      'PROMO 3X2 OLEOS CUIDADO DIARIO',     12.80, 'OLE30314'),
('C-PROMO-OLEOS-DIARIO',      'PROMO 3X2 OLEOS CUIDADO DIARIO',     12.80, 'OLE30317'),
-- Rizos
('C-PROMO-OLEOS-RIZOS',       'PROMO 3X2 OLEOS RIZOS DEFINIDOS',    12.80, 'OLE30315'),
('C-PROMO-OLEOS-RIZOS',       'PROMO 3X2 OLEOS RIZOS DEFINIDOS',    12.80, 'OLE30316'),
('C-PROMO-OLEOS-RIZOS',       'PROMO 3X2 OLEOS RIZOS DEFINIDOS',    12.80, 'OLE30317'),

-- ---- Bloque D: Ceras Italianas 400gr (3 promos, 3 items) ----
('C-PROMO-CERA-MANZANA', 'OFERTA CERA LATA MANZANA VERDE', 8.00, 'DEP30508'),
('C-PROMO-CERA-BANANA',  'OFERTA CERA LATA BANANA',        8.00, 'DEP30510'),
('C-PROMO-CERA-TALCO',   'OFERTA CERA LATA TALCO',         8.00, 'DEP30511');

-- ============================================================
-- 5. VALIDACIÓN: todos los product_code deben existir en BD
-- ============================================================
DO $$
DECLARE
    v_missing text;
BEGIN
    SELECT string_agg(DISTINCT t.product_code, ', ')
    INTO v_missing
    FROM tmp_promos t
    LEFT JOIN product p ON p.product_code = t.product_code
    WHERE p.id_product IS NULL;

    IF v_missing IS NOT NULL THEN
        RAISE EXCEPTION 'Productos no encontrados en BD: %', v_missing;
    END IF;

    RAISE NOTICE 'Validación OK: todos los product_code existen.';
END $$;

-- ============================================================
-- 6. VALIDACIÓN: los promotion_code nuevos no deben existir aún
-- ============================================================
DO $$
DECLARE
    v_existing text;
BEGIN
    SELECT string_agg(DISTINCT t.promotion_code, ', ')
    INTO v_existing
    FROM tmp_promos t
    JOIN promotion pr ON pr.promotion_code = t.promotion_code;

    IF v_existing IS NOT NULL THEN
        RAISE EXCEPTION 'Ya existen promos con estos códigos: %', v_existing;
    END IF;

    RAISE NOTICE 'Validación OK: promotion_code nuevos están disponibles.';
END $$;

-- ============================================================
-- 7. INSERTAR cabeceras (una fila por promo única)
-- ============================================================
INSERT INTO promotion (promotion_code, name, category, unit_price_usd)
SELECT DISTINCT ON (promotion_code)
       promotion_code,
       promo_name,
       'Promociones',
       promo_price
FROM tmp_promos
ORDER BY promotion_code;

-- ============================================================
-- 8. INSERTAR items (join por promotion_code + product_code)
-- ============================================================
INSERT INTO promotion_item (id_promotion, id_product, quantity_required)
SELECT
    pr.id_promotion,
    p.id_product,
    1
FROM tmp_promos t
JOIN promotion pr ON pr.promotion_code = t.promotion_code
JOIN product  p  ON p.product_code   = t.product_code;

-- ============================================================
-- 9. RESUMEN final
-- ============================================================
DO $$
DECLARE
    v_promos int;
    v_items  int;
BEGIN
    SELECT COUNT(DISTINCT promotion_code) INTO v_promos FROM tmp_promos;
    SELECT COUNT(*) INTO v_items FROM tmp_promos;

    RAISE NOTICE '============================================';
    RAISE NOTICE 'RESUMEN:';
    RAISE NOTICE '  Promos nuevas creadas: %', v_promos;
    RAISE NOTICE '  Items creados: %', v_items;
    RAISE NOTICE '  Oleos viejas eliminadas: 35, 36, 37, 38, 39, 40';
    RAISE NOTICE '  Promo 44 renombrada a histórica (para proteger ventas existentes)';
    RAISE NOTICE '  Precios actualizados: 42 -> $2.50, 43 -> $8.50';
    RAISE NOTICE '============================================';
END $$;

COMMIT;
