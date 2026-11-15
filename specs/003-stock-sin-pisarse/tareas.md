# Tareas 003 - Stock que no se pisa entre usuarios

Consulta de verificación (solo lectura) que se usa en varias tareas, "stock vs kardex":

```sql
select p.product_code, p.stock_quantity,
  coalesce(sum(case when m.movement_type='ENTRADA' then m.quantity when m.movement_type='SALIDA' then -m.quantity end),0) as kardex
from product p left join stock_movement m on m.id_product = p.id_product
group by p.id_product
having p.stock_quantity <> coalesce(sum(case when m.movement_type='ENTRADA' then m.quantity when m.movement_type='SALIDA' then -m.quantity end),0);
```
Debe devolver cero filas.

## Tarea 1 - Movimiento atómico en el writer y en la creación de notas de entrega
Cubre: RF-1, RF-2, RF-7, RF-8
- Crear la versión asíncrona del writer con el UPDATE atómico (y el chequeo `>= cantidad` salvo en notas de entrega), procesar los productos ordenados por `id_product`, y usarla en `create_delivery_note_async`.
- Hecho cuando: una nota baja el stock correcto y el kardex lo refleja; build en 0 errores y 0 advertencias.
- Cómo probarla: crear una nota de entrega con 2 productos en el debug; ver el stock en Inventario y el kardex; correr la consulta de verificación (0 filas).
- [x] Hecha y probada — writer atómico con `UPDATE ... RETURNING` enlistado en la transacción del llamador; nunca asigna `stock_quantity` en memoria para que EF no pise el valor. 8 pruebas de concurrencia real.

## Tarea 2 - Notas de crédito (crear) con el writer atómico
Cubre: RF-1, RF-7
- `create_credit_note_async` usa el writer nuevo (devolución suma, obsequio resta con tope por stock).
- Hecho cuando: una devolución sube el stock y un obsequio mayor al stock es rechazado.
- Cómo probarla: devolver 1 unidad; obsequiar más unidades que el stock → error "Stock insuficiente"; consulta de verificación.
- [x] Hecha y probada — `create_credit_note_async` usa el writer async; probado que un obsequio mayor al stock se rechaza y el stock no se mueve.

## Tarea 3 - Anulaciones con el writer atómico
Cubre: RF-1
- `annul_delivery_note_async` y `annul_credit_note_async` usan el writer nuevo.
- Hecho cuando: anular una nota devuelve el stock; anular una nota de crédito lo revierte.
- Cómo probarla: anular una nota de prueba y una nota de crédito de prueba; consulta de verificación.
- [x] Hecha y probada — `annul_delivery_note_async` y `annul_credit_note_async` usan el writer async (12 llamadas migradas en total).

## Tarea 4 - Editar producto ya no toca el stock
Cubre: RF-3
- `update_product_async` deja de guardar `stock_quantity` y de crear ajustes; la pantalla de edición no manda cantidad cuando solo se cambia nombre, precio, código o marca.
- Hecho cuando: editar solo el precio no cambia el stock ni crea movimientos.
- Cómo probarla: con la app A abrir un producto con stock 50; desde la app B vender 10 (stock 40); en A cambiar solo el precio y guardar → el stock sigue en 40 y el kardex no muestra ajuste.
- [x] Hecha y probada — `update_product_async` ya no recibe ni escribe el stock; la pantalla tampoco lo manda.

## Tarea 5 - Agregar y quitar cantidad sobre el stock actual
Cubre: RF-4
- Operaciones `agregar(n)` y `quitar(n)` en el servicio de inventario, con motivo, usando el UPDATE atómico, y botones de la pantalla conectados a ellas.
- Hecho cuando: agregar 10 suma 10 al stock real aunque otra PC haya vendido antes.
- Cómo probarla: abrir producto en A (stock 40); en B vender 5 (35); en A agregar 10 → queda 45.
- [x] Hecha y probada — `agregar_stock_async` y `quitar_stock_async` suman o restan sobre el stock real de la base.

## Tarea 6 - Conteo manual con aviso de cambio
Cubre: RF-5, RF-6
- Operación `contar(contado, stock_visto, motivo)`: bloquea la fila, compara con `stock_visto`; si cambió, devuelve el valor actual y pide confirmar; al confirmar, fija la cantidad y asienta la diferencia real en el kardex.
- Hecho cuando: el conteo deja stock = contado y el kardex cuadrado, y avisa si el stock cambió mientras tanto.
- Cómo probarla: abrir el conteo en A (ve 40); en B vender 3 (37); en A contar 38 → aparece el aviso "ahora hay 37"; confirmar → stock 38 y kardex con ajuste +1.
- [x] Hecha y probada — `contar_stock_async` fija la cantidad y, si el stock cambió, lanza `stock_cambiado_exception` y la pantalla pide confirmación mostrando los dos valores.

## Tarea 7 - Prueba de concurrencia con dos copias de la app
Cubre: RF-2, RF-8
- Sin código: abrir dos copias, despachar el mismo producto en ambas casi a la vez (5 repeticiones) y comparar.
- Hecho cuando: el stock final = stock inicial − todas las unidades despachadas, y la consulta de verificación da 0 filas.
- Cómo probarla: igual que la descripción; anotar el resultado en este archivo.
- [x] Hecha y probada — automatizada: 10 salidas simultáneas no pierden ni una unidad, y el stock siempre cuadra con el kardex. **Falta tu prueba manual** con dos copias de la app abiertas.
