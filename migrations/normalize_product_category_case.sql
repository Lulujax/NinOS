-- Migracion: normalizar la categoria (marca) de los productos a MAYUSCULAS
-- Ejecutar en la base de datos NinOS
--
-- MOTIVO
-- product.category es la marca, y de ella dependen dos cosas:
--   1. el prefijo del codigo del producto (DEF, OLE, REM...);
--   2. el color y el agrupamiento en el PDF de la lista de precios.
-- El PDF busca el color con un diccionario EXACTO cuyas claves estan en mayusculas,
-- asi que un producto con categoria 'Defile' salia con el gris por defecto en vez del
-- color de su marca.
--
-- Ademas product.category ahora se guarda en mayusculas desde el dominio, de modo que
-- esta UPDATE deja lo que ya existe alineado con lo que se guarde de aqui en adelante.

-- Verificacion previa: estas son las categorias que hay hoy y cuantas hay de cada una.
-- Lo esperable es que solo 'Defile' y 'Otros' aparezcan con mezclas de mayusculas.
SELECT category, COUNT(*)
FROM product
GROUP BY category
ORDER BY category;

-- Productos cuya categoria NO esta en mayusculas (deben ser pocos).
SELECT product_code, name, category
FROM product
WHERE category <> UPPER(BTRIM(category))
ORDER BY product_code;

-- Normalizacion. BTRIM quita espacios sobrantes; UPPER deja la marca en mayusculas.
UPDATE product
   SET category = UPPER(BTRIM(category))
 WHERE category <> UPPER(BTRIM(category));

-- Comprobacion: no debe quedar ninguna fila, y las 10 marcas deben quedar completas.
SELECT COUNT(*) AS categorias_sin_normalizar
FROM product
WHERE category <> UPPER(BTRIM(category));

SELECT category, COUNT(*)
FROM product
GROUP BY category
ORDER BY category;
