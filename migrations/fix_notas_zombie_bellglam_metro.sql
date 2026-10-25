-- =====================================================================
-- REPARAR 2 NOTAS ZOMBIE QUEDADAS EN 0.00 (BELLGLAM y METRO LAS FERIAS)
-- Generado: 2026-10-18
--
-- Que corrige:
--   Al editarles el descuento en CxC, estas 2 notas zombie quedaron con
--   adjusted_total_usd = 0.00, status = Pagada y una comision de 0.00
--   (creada por update_note_total_async: pagado 0 >= ajustado 0).
--   No tienen abonos reales ni note_detail. Por eso desaparecieron de
--   "Por Cobrar" (solo muestra Pendientes) y quedaron en 0.
--
--   Reparacion (montos confirmados por el usuario):
--     3300-339  BELLGLAM C.A (Anais)            -> 245.12 * 0.90 = 220.61
--               (conserva el 10% de descuento que alcanzo a guardar)
--     3300-343  COMERCIAL METRO LAS FERIAS 333  -> 616.64 (sin descuento)
--
--   Ademas se eliminan las comisiones fantasma 11 y 12 (amount_usd = 0.00,
--   is_paid = false, sin commission_payment): son residuo de la corrupcion
--   y bloquearian el recalculo de la comision real al pagarse la nota.
--
-- Reversible con el bloque ROLLBACK del final (marcado).
-- =====================================================================

SET client_encoding = 'UTF8';

BEGIN;

-- ---------------------------------------------------------------------
-- 1. NOTAS AL VALOR CORRECTO Y PENDIENTES DE COBRO
--    Guardas: solo toca la nota si sigue exactamente en el estado corrupto
-- ---------------------------------------------------------------------
UPDATE delivery_note
SET adjusted_total_usd = 220.61,
    status = 'Pendiente'
WHERE id_delivery_note = 81
  AND note_number = '3300-339'
  AND adjusted_total_usd = 0
  AND status = 'Pagada';

UPDATE delivery_note
SET adjusted_total_usd = 616.64,
    status = 'Pendiente'
WHERE id_delivery_note = 90
  AND note_number = '3300-343'
  AND adjusted_total_usd = 0
  AND status = 'Pagada';

-- ---------------------------------------------------------------------
-- 2. COMISIONES FANTASMA (0.00) DE ESAS 2 NOTAS
-- ---------------------------------------------------------------------
DELETE FROM commission
WHERE id_delivery_note IN (81, 90)
  AND amount_usd = 0
  AND is_paid = false
  AND NOT EXISTS (
      SELECT 1 FROM commission_payment cp
      WHERE cp.id_commission = commission.id_commission
  );

COMMIT;

-- =====================================================================
-- VERIFICACION (debe mostrar 220.61 / 616.64, ambas Pendiente, sin comision)
-- =====================================================================
-- SELECT dn.id_delivery_note, dn.note_number, dn.total_amount_usd,
--        dn.adjusted_total_usd, dn.status, dn.discount_percentage,
--        (SELECT count(*) FROM commission c WHERE c.id_delivery_note = dn.id_delivery_note) AS comisiones
-- FROM delivery_note dn
-- WHERE dn.id_delivery_note IN (81, 90);

-- =====================================================================
-- ROLLBACK (ejecutar solo si hay que deshacer)
-- =====================================================================
-- BEGIN;
-- UPDATE delivery_note SET adjusted_total_usd = 0, status = 'Pagada' WHERE id_delivery_note = 81;
-- UPDATE delivery_note SET adjusted_total_usd = 0, status = 'Pagada' WHERE id_delivery_note = 90;
-- COMMIT;
-- (Las comisiones 11 y 12 tendrian que recrearse a mano si se deshace.)
