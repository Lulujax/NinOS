# Tareas 006 - Promociones y anulaciones coherentes

## Tarea 1 (manual, solo lectura) - ¿Hay notas con promociones en producción?
Cubre: RF-1, RF-4 (preparación)
- Ejecutar en producción: `select count(*) from note_detail where id_promotion is not null;` y lo mismo en `credit_note_detail`.
- Hecho cuando: tienes los dos números. Si son 0, las notas viejas sin foto no son un problema.
- Cómo probarla: es la propia consulta.
- [x] Hecha y probada — 2026-11-15, corrida por Claude en solo lectura: producción tiene 0 renglones de nota con promoción y 0 de nota de crédito con promoción, así que las notas viejas sin foto no son un problema. Testing tiene 1 renglón con promoción (el de las pruebas).

## Tarea 2 - Migración: composición guardada por renglón
Cubre: RF-3
- Columna `promo_items_snapshot` (jsonb, nula) en `note_detail` y `credit_note_detail`. **Antes:** `NINOS_DB_CONNECTION` a testing y preguntarte.
- Hecho cuando: se aplica en testing sin tocar datos.
- Cómo probarla: abrir la app; las notas existentes se ven igual.
- [x] Hecha y probada

## Tarea 3 - Guardar la composición al emitir una nota
Cubre: RF-3
- `create_delivery_note_async` guarda en cada renglón de promoción la lista `{producto, cantidad por unidad}`.
- Hecho cuando: una nota con promoción tiene la composición guardada.
- Cómo probarla: crear una promoción de 3 productos y venderla; `select promo_items_snapshot from note_detail where id_promotion is not null order by 1 desc limit 1` → lista de 3.
- [x] Hecha y probada

## Tarea 4 - Rutina única para mover el stock de una línea con promoción
Cubre: RF-1, RF-2, RF-4
- Función que, dada una línea, carga todos los productos de su composición guardada (o la actual si no hay foto, con aviso en el log), y cancela la operación si falta alguno.
- Hecho cuando: compila (0 errores, 0 advertencias); se prueba en las tareas 5 y 6.
- Cómo probarla: ver tareas 5 y 6.
- [x] Hecha y probada

## Tarea 5 - Anular nota de entrega con promociones
Cubre: RF-1, RF-2, RF-4, RF-9
- `annul_delivery_note_async` usa la rutina de la tarea 4.
- Hecho cuando: anular una nota con 2 unidades de una promoción de 3 productos devuelve el stock de los 3 y deja 3 entradas en el kardex.
- Cómo probarla: anotar el stock de los 3 productos, vender 2 promociones, anular la nota; comparar; revisar kardex. Cambiar la promoción antes de anular otra nota vieja → el stock vuelve según la composición original.
- [x] Hecha y probada

## Tarea 6 - Crear y anular nota de crédito con promociones
Cubre: RF-1, RF-2, RF-3, RF-4, RF-9
- Crear (devolución copia la composición del renglón original; obsequio usa la actual) y `annul_credit_note_async` usan la rutina.
- Hecho cuando: devolver una promoción repone los 3 productos y anular esa nota de crédito los vuelve a sacar.
- Cómo probarla: devolver 1 promoción; anular la nota de crédito; revisar stock y kardex de los 3 productos.
- [x] Hecha y probada

## Tarea 7 - Notas de crédito anuladas no cuentan como devueltas
Cubre: RF-5, RF-6, RF-7
- Filtrar `status <> 'Anulada'` en el cálculo de lo ya devuelto (crear y fuente) y en el chequeo de "tiene notas de crédito" al anular una nota.
- Hecho cuando: tras anular una nota de crédito se puede devolver lo mismo otra vez, y una nota con todas sus notas de crédito anuladas se puede anular; con una vigente, no.
- Cómo probarla: devolver 1 unidad, anular la nota de crédito, devolver 1 unidad otra vez (ok); anular la nota de entrega (bloqueada por la vigente); anular la vigente y anular la nota (ok).
- [x] Hecha y probada

## Tarea 8 - Editar o eliminar una promoción no cambia el pasado
Cubre: RF-8
- Prueba sin código nuevo: confirmar con la foto que notas viejas no cambian.
- Hecho cuando: editar la promoción no altera la reimpresión ni la anulación de una nota vieja.
- Cómo probarla: vender, editar la promoción (quitar un producto), reimprimir y anular la nota → todo como al emitirla.
- [x] Hecha y probada — confirmada por Luis en pantalla (2026-11-15)
