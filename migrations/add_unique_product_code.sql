-- Migracion: indice unico en product.product_code
-- Ejecutar en la base de datos NinOS
--
-- NOTA: esta base ya tiene el indice uq_product_code, asi que probablemente NO necesites
-- este script. La migracion de EF 20261001195403_UniqueProductCode tambien es idempotente:
-- solo crea su indice si no existe ninguno. Se deja el script por si se reconstruye la base.

-- A partir de esta migracion el codigo de producto no se puede editar ni repetir: lo
-- asigna el sistema con el estandar PREFIJO DE MARCA + CORRELATIVO DE 5 DIGITOS.
-- El indice incluye los productos que estan en la papelera (is_active = false) para
-- que un codigo dado de baja no se pueda reutilizar en otro producto.

-- Verificacion previa: debe salir 0 filas. Si sale alguna, NO seguir con el CREATE INDEX,
-- porque el indice unico no se va a poder crear y hay que resolver los duplicados primero.
SELECT product_code, COUNT(*) AS repeticiones
FROM product
GROUP BY product_code
HAVING COUNT(*) > 1
ORDER BY product_code;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
         WHERE schemaname = 'public'
           AND tablename = 'product'
           AND indexdef ILIKE '%UNIQUE%'
           AND indexdef ILIKE '%product_code%'
    ) THEN
        CREATE UNIQUE INDEX IX_product_product_code ON product (product_code);
    END IF;
END $$;
