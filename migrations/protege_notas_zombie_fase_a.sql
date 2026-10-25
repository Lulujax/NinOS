-- =====================================================================
-- FASE A: PROTEGER NOTAS ZOMBIE (SIN PRODUCTOS) CONTRA LA EDICION EN CxC
-- Generado: 2026-10-18
--
-- Que hace:
--   1. Crea UN producto fantasma interno e inactivo ('Z-9999-INT',
--      'SALDO CARTERA ANTERIOR', categoria 'AJUSTE INTERNO'). Inactivo =
--      no sale en las listas de Inventario ni se puede vender; su codigo
--      no sigue el patron PREFIJO+5digitos de get_next_product_code_async
--      y su categoria no tiene regla, asi que no desplaza correlativos.
--
--   2. Inserta UN renglon en note_detail por cada nota cuyo cliente es
--      zombie (customer.is_ghost = true): cantidad 1 y precio/subtotal =
--      el monto original de la nota (total_amount_usd, que ese dato nunca
--      lo toca el bug). Con detalle presente, la app YA INSTALADA calcula
--      el bruto desde note_detail (update_note_total_async), con lo que:
--        - editar monto o descuento (subir o bajar) ya no queda atrapado
--          en el valor anterior,
--        - el % de descuento se muestra y calcula correctamente.
--
--   3. Trigger SILENCIOSO en delivery_note (BEFORE UPDATE): si un guardado
--      deja el saldo en 0.00 (nota sin pagos directos ni por relacion, sin
--      promo, sin ser Anulada/Devuelta, y sin productos reales) recupera
--      el saldo correcto por descuento:
--        adjusted = total_amount_usd * (1 - dcto%/100)
--      y si el guardado la habia marcado 'Pagada' sin haber saldo, la
--      devuelve a 'Pendiente'. NUNCA lanza error: el guardado de la app
--      procede con normalidad y la nota queda como el usuario pretendia.
--
--   4. Trigger en commission: descarta comisiones de 0.00 pendientes
--      (residuo del bug: comision 10% de un saldo corrompido a 0).
--
-- Reversible con el bloque ROLLBACK del final (marcado).
-- =====================================================================

SET client_encoding = 'UTF8';

BEGIN;

-- ---------------------------------------------------------------------
-- 1. PRODUCTO FANTASMA (unico, inactivo, categoria sin regla de codigo)
-- ---------------------------------------------------------------------
INSERT INTO product (product_code, name, category, unit_price_usd, stock_quantity, is_active)
SELECT 'Z-9999-INT', 'SALDO CARTERA ANTERIOR', 'AJUSTE INTERNO', 0, 0, false
WHERE NOT EXISTS (SELECT 1 FROM product WHERE product_code = 'Z-9999-INT');

-- ---------------------------------------------------------------------
-- 2. RENGLON FANTASMA POR NOTA ZOMBIE (total_amount_usd queda intacto)
-- ---------------------------------------------------------------------
INSERT INTO note_detail (id_delivery_note, id_product, quantity,
                         unit_price_usd, subtotal_usd, product_code_snapshot)
SELECT dn.id_delivery_note, p.id_product, 1,
       dn.total_amount_usd, dn.total_amount_usd, p.product_code
FROM delivery_note dn
JOIN customer c ON c.id_customer = dn.id_customer
JOIN product p  ON p.product_code = 'Z-9999-INT'
WHERE c.is_ghost = true
  AND NOT EXISTS (SELECT 1 FROM note_detail nd
                  WHERE nd.id_delivery_note = dn.id_delivery_note);

-- ---------------------------------------------------------------------
-- 3. AUTO-CORRECCION SILENCIOSA (nunca error, nunca bloquea)
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION ninos_autocorrige_nota_en_cero()
RETURNS trigger LANGUAGE plpgsql AS
$$
BEGIN
    -- Solo interviene cuando un guardado deja el saldo exactamente en 0.00
    IF NEW.adjusted_total_usd <> 0 THEN RETURN NEW; END IF;
    IF OLD.adjusted_total_usd <= 0 THEN RETURN NEW; END IF;
    IF NEW.total_amount_usd <= 0 THEN RETURN NEW; END IF;
    IF COALESCE(NEW.promo_discount_percentage, 0) <> 0 THEN RETURN NEW; END IF;
    IF NEW.status IN ('Anulada', 'Devuelta') THEN RETURN NEW; END IF;

    -- Si la nota tiene productos reales o lineas de promocion, no intervenir
    IF EXISTS (
        SELECT 1
        FROM note_detail nd
        LEFT JOIN product pr ON pr.id_product = nd.id_product
        WHERE nd.id_delivery_note = NEW.id_delivery_note
          AND (nd.id_promotion IS NOT NULL
               OR pr.product_code IS NULL
               OR pr.product_code <> 'Z-9999-INT')
    ) THEN
        RETURN NEW;
    END IF;

    -- Si la nota tiene dinero cobrado (directo o por relacion), no intervenir
    IF EXISTS (SELECT 1 FROM payment p
               WHERE p.id_delivery_note = NEW.id_delivery_note
                 AND p.amount_usd > 0) THEN
        RETURN NEW;
    END IF;
    IF NEW.id_relacion IS NOT NULL AND EXISTS (
        SELECT 1 FROM payment pr
        WHERE pr.id_relacion = NEW.id_relacion
          AND pr.amount_usd > 0) THEN
        RETURN NEW;
    END IF;

    -- Recuperar el saldo correcto segun el descuento del propio guardado
    NEW.adjusted_total_usd := round(
        NEW.total_amount_usd
        * (1 - LEAST(100, GREATEST(0, COALESCE(NEW.discount_percentage, 0))) / 100.0),
        2);

    -- El guardado corrupto la marca 'Pagada' (saldo 0 => pagado 0 >= 0):
    -- si quedo saldo por cobrar, vuelve a 'Pendiente'
    IF NEW.status = 'Pagada' AND NEW.adjusted_total_usd > 0 THEN
        NEW.status := 'Pendiente';
    END IF;

    RETURN NEW;
END
$$;

DROP TRIGGER IF EXISTS trg_ninos_autocorrige_nota_en_cero ON delivery_note;
CREATE TRIGGER trg_ninos_autocorrige_nota_en_cero
BEFORE UPDATE ON delivery_note
FOR EACH ROW EXECUTE FUNCTION ninos_autocorrige_nota_en_cero();

-- ---------------------------------------------------------------------
-- 4. SIN COMISIONES FANTASMA (montos 0.00 pendientes)
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION ninos_descarta_comision_en_cero()
RETURNS trigger LANGUAGE plpgsql AS
$$
BEGIN
    IF NEW.amount_usd <= 0 AND NOT NEW.is_paid THEN
        RETURN NULL;
    END IF;
    RETURN NEW;
END
$$;

DROP TRIGGER IF EXISTS trg_ninos_descarta_comision_en_cero ON commission;
CREATE TRIGGER trg_ninos_descarta_comision_en_cero
BEFORE INSERT OR UPDATE ON commission
FOR EACH ROW EXECUTE FUNCTION ninos_descarta_comision_en_cero();

COMMIT;

-- =====================================================================
-- VERIFICACION (resultados esperados)
-- =====================================================================
-- -- Debe dar 86 (o las notas zombie que existan) con detalle y sin descuadre:
-- SELECT dn.note_number, dn.total_amount_usd,
--        (SELECT COALESCE(sum(nd.subtotal_usd),0) FROM note_detail nd
--         WHERE nd.id_delivery_note = dn.id_delivery_note) AS bruto_detalle,
--        dn.adjusted_total_usd, dn.status
-- FROM delivery_note dn JOIN customer c ON c.id_customer = dn.id_customer
-- WHERE c.is_ghost = true
--   AND (SELECT COALESCE(sum(nd.subtotal_usd),0) FROM note_detail nd
--        WHERE nd.id_delivery_note = dn.id_delivery_note) <> dn.total_amount_usd;
--
-- -- Producto fantasma (debe existir, inactivo):
-- SELECT product_code, name, is_active FROM product WHERE product_code = 'Z-9999-INT';
--
-- -- Triggers instalados:
-- SELECT tgname FROM pg_trigger
-- WHERE tgrelid = 'delivery_note'::regclass AND NOT tgisinternal;
-- SELECT tgname FROM pg_trigger
-- WHERE tgrelid = 'commission'::regclass AND NOT tgisinternal;

-- =====================================================================
-- PRUEBA DEL CANDADO (ejecutar SOLO dentro de BEGIN...ROLLBACK)
-- =====================================================================
-- BEGIN;
-- -- Simula el guardado corrupto (saldo a 0 + Pagada): debe quedar auto-corregido
-- UPDATE delivery_note SET adjusted_total_usd = 0, status = 'Pagada', discount_percentage = 10
-- WHERE id_delivery_note = 90 RETURNING note_number, adjusted_total_usd, status;
-- -- Esperado: 3300-343 | 554.98 | Pendiente
-- ROLLBACK;

-- =====================================================================
-- ROLLBACK (ejecutar solo si hay que deshacer)
-- =====================================================================
-- BEGIN;
-- DROP TRIGGER IF EXISTS trg_ninos_autocorrige_nota_en_cero ON delivery_note;
-- DROP FUNCTION IF EXISTS ninos_autocorrige_nota_en_cero();
-- DROP TRIGGER IF EXISTS trg_ninos_descarta_comision_en_cero ON commission;
-- DROP FUNCTION IF EXISTS ninos_descarta_comision_en_cero();
-- DELETE FROM note_detail nd USING product p
-- WHERE p.id_product = nd.id_product AND p.product_code = 'Z-9999-INT';
-- DELETE FROM product WHERE product_code = 'Z-9999-INT';
-- COMMIT;
