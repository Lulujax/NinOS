-- Migracion: Agregar created_at y updated_at a payment
-- Ejecutar en la base de datos NinOS

ALTER TABLE payment ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ NOT NULL DEFAULT NOW();
ALTER TABLE payment ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ;

-- Backfill si alguna fila tiene created_at al minimo (no se pudo setear en inserciones viejas)
UPDATE payment SET created_at = payment_date WHERE created_at IS NULL OR created_at = '0001-01-01';