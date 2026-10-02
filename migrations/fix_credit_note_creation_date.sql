-- Migracion: corregir la fecha de las notas de credito de devolucion
-- Ejecutar en la base de datos NinOS
--
-- MOTIVO
-- Al crear una nota de credito por devolucion, la aplicacion guardaba en credit_note.creation_date
-- la fecha de la NOTA DE ENTREGA que se estaba revirtiendo, en vez de la fecha en que se emitio la
-- propia nota de credito. Como ese error arrastra al resto del sistema, hay que corregir tres tablas:
--
--   1. credit_note.creation_date    -> es lo que muestra la columna FECHA NC del modulo y por lo que
--                                     se agrupa el filtro por mes.
--   2. payment.payment_date         -> el abono NEGATIVO de la devolucion. Con la fecha vieja, una
--                                     devolucion registrada hoy aparecia en el historial de pagos con
--                                     la fecha de una nota de meses anteriores.
--   3. stock_movement.movement_date  -> el kardex. Por lo mismo, la entrada por devolucion quedaba
--                                     fechada el dia de la entrega original.
--
-- Las notas de credito por OBSEQUIO no se tocan: nunca se anclaron a una nota de entrega, asi que ya
-- se guardaban con la fecha del sistema.
--
-- IMPORTANTE
-- Este script pone TODAS las devoluciones historicas en la misma fecha. Es la unica fecha que se
-- conoce con certeza (la de la correccion), porque el sistema no guardaba en ningun lado el momento
-- real en que se emitio cada devolucion. Si mas adelante se localiza el dato real de alguna NC, se
-- corrige fila por fila con un UPDATE sobre credit_note.id_credit_note.
--
-- ANTES de ejecutar, sacar un respaldo:
--   pg_dump -U postgres -d ninos_db -Fc -f backups_antes_fechas_nc_AAAAMMDD-HHMMSS.dump

BEGIN;

-- FECHA UNICA de correccion. Este es el unico valor que hay que cambiar si hay que rehacer el script.
-- Se usa un parametro de sesion (y no \set de psql) para que el bloque DO de abajo tambien lo vea, y
-- para que el script corra igual en psql, pgAdmin y DBeaver.
SET ninos.fecha_nc = '2026-10-01 00:00:00+00';

-- ---------------------------------------------------------------------------
-- Verificacion previa: que hay hoy y que se va a tocar.
-- ---------------------------------------------------------------------------

SELECT 'credit_note' AS tabla, COUNT(*) AS devoluciones
  FROM credit_note
 WHERE category = 'Devolucion'
UNION ALL
SELECT 'payment', COUNT(*)
  FROM payment
 WHERE payment_type = 'NOTA DE CREDITO'
UNION ALL
SELECT 'stock_movement', COUNT(*)
  FROM stock_movement
 WHERE document_type = 'NOTA DE CREDITO';

-- Las devoluciones que SI estan mal: cuya fecha de NC no coincide con la de la nota de entrega.
SELECT cn.id_credit_note,
       cn.note_number   AS nc,
       dn.note_number   AS nota_entrega,
       dn.creation_date AS fecha_entrega,
       cn.creation_date AS fecha_nc_actual
  FROM credit_note cn
  JOIN delivery_note dn ON dn.id_delivery_note = cn.id_delivery_note
 WHERE cn.category = 'Devolucion'
   AND cn.creation_date <> dn.creation_date
 ORDER BY cn.id_credit_note;

-- ---------------------------------------------------------------------------
-- Correccion. El filtro por category = 'Devolucion' es lo que evita tocar los obsequios.
-- ---------------------------------------------------------------------------

UPDATE credit_note
   SET creation_date = current_setting('ninos.fecha_nc')::timestamp
 WHERE category = 'Devolucion';

UPDATE payment
   SET payment_date = current_setting('ninos.fecha_nc')::timestamp
 WHERE payment_type = 'NOTA DE CREDITO';

UPDATE stock_movement
   SET movement_date = current_setting('ninos.fecha_nc')::timestamp
 WHERE document_type = 'NOTA DE CREDITO';

-- Si alguna fila no quedo como se esperaba, la transaccion se revierte sola.
DO $$
DECLARE
    pendientes integer;
BEGIN
    SELECT COUNT(*) INTO pendientes
      FROM credit_note
     WHERE category = 'Devolucion'
       AND creation_date <> current_setting('ninos.fecha_nc')::timestamp;

    IF pendientes > 0 THEN
        RAISE EXCEPTION 'Quedaron % notas de credito sin corregir; se revierte todo.', pendientes;
    END IF;
END
$$;

COMMIT;

-- ---------------------------------------------------------------------------
-- Comprobacion: ninguna devolucion debe seguir apuntando a la fecha de su entrega.
-- Debe devolver 0 filas.
-- ---------------------------------------------------------------------------

SELECT cn.id_credit_note,
       cn.note_number   AS nc,
       dn.note_number   AS nota_entrega,
       dn.creation_date AS fecha_entrega,
       cn.creation_date AS fecha_nc
  FROM credit_note cn
  JOIN delivery_note dn ON dn.id_delivery_note = cn.id_delivery_note
 WHERE cn.category = 'Devolucion'
   AND cn.creation_date = dn.creation_date;

SELECT 'credit_note' AS tabla, MIN(creation_date) AS desde, MAX(creation_date) AS hasta, COUNT(*) AS filas
  FROM credit_note
UNION ALL
SELECT 'payment', MIN(payment_date), MAX(payment_date), COUNT(*)
  FROM payment
 WHERE payment_type = 'NOTA DE CREDITO'
UNION ALL
SELECT 'stock_movement', MIN(movement_date), MAX(movement_date), COUNT(*)
  FROM stock_movement
 WHERE document_type = 'NOTA DE CREDITO';
