-- Migracion: Agregar campos bank_name y observations a payment
-- Ejecutar en la base de datos NinOS

ALTER TABLE payment ADD COLUMN IF NOT EXISTS bank_name VARCHAR(100) NOT NULL DEFAULT '';
ALTER TABLE payment ADD COLUMN IF NOT EXISTS observations TEXT NOT NULL DEFAULT '';
ALTER TABLE payment ALTER COLUMN exchange_rate DROP NOT NULL;
