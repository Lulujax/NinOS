-- Migracion: Agregar campos a payment y commission, cambiar porcentaje de comision
-- Ejecutar en la base de datos NinOS

-- 1. Campos nuevos en payment
ALTER TABLE payment ADD COLUMN IF NOT EXISTS payment_type VARCHAR(50) NOT NULL DEFAULT 'Transferencia';
ALTER TABLE payment ADD COLUMN IF NOT EXISTS reference_number VARCHAR(100) NOT NULL DEFAULT '';

-- 2. Hacer exchange_rate nullable en payment (para pagos en efectivo)
ALTER TABLE payment ALTER COLUMN exchange_rate DROP NOT NULL;

-- 3. Campos nuevos en commission
ALTER TABLE commission ADD COLUMN IF NOT EXISTS amount_bs DECIMAL(18,2) NOT NULL DEFAULT 0;
ALTER TABLE commission ADD COLUMN IF NOT EXISTS exchange_rate DECIMAL(18,2) NOT NULL DEFAULT 0;
ALTER TABLE commission ADD COLUMN IF NOT EXISTS reference_number VARCHAR(100) NOT NULL DEFAULT '';

-- 4. Si la migracion rota anterior no se aplico, aplicar customer_code_prefix
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'seller' AND column_name = 'customer_code_prefix') THEN
        ALTER TABLE seller ADD COLUMN customer_code_prefix VARCHAR(50) NOT NULL DEFAULT '';
    END IF;
END $$;
