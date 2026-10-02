-- Migracion: normalizar los codigos de producto que no siguen el estandar
-- Ejecutar en la base de datos NinOS, DESPUES de la migracion del indice unico
-- IX_product_product_code.
--
-- MARIPOSA y PIE estan en la marca "Otros" pero sus codigos no son OTR + 5 digitos.
-- Se corrigen una vez para que toda la serie quede Homogenea y el correlativo de la
-- marca Otros no arranque con huecos raros.

-- Verificacion previa: ambos codigos destino deben estar libres. Si alguno ya existe,
-- NO seguir con los UPDATE.
SELECT product_code, name, category
FROM product
WHERE product_code IN ('MARIPOSA', 'PIE', 'OTR00001', 'OTR00002')
ORDER BY product_code;

UPDATE product SET product_code = 'OTR00001' WHERE product_code = 'MARIPOSA';
UPDATE product SET product_code = 'OTR00002' WHERE product_code = 'PIE';

-- Comprobacion: deben verse dos filas OTR.
SELECT product_code, name, category
FROM product
WHERE product_code LIKE 'OTR%'
ORDER BY product_code;
