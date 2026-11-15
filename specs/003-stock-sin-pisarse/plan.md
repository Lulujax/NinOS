# Plan 003 - Stock que no se pisa entre usuarios

Cómo cumplir [spec.md](spec.md). Todavía no se ha tocado código.

## Qué hay hoy (verificado)

- [stock_movement_writer.cs:175](../../src/NinOS.Infrastructure/Services/stock_movement_writer.cs#L175) calcula `stock_quantity ± cantidad` sobre el producto cargado en memoria y EF guarda el número final. Con dos PCs, gana la última.
- [InventoryService.cs:197](../../src/NinOS.Infrastructure/Services/Implementations/InventoryService.cs#L197): `update_product_async` guarda `stock_quantity` tal como lo mandó la pantalla y calcula la "diferencia" contra lo que acaba de leer, por eso aparecen ajustes fantasma.
- Quienes llaman al writer: `DeliveryNoteService.create_delivery_note_async`, `CreditNoteService.create_credit_note_async` y `annul_credit_note_async`, `AccountsReceivableService.annul_delivery_note_async`.

## Enfoque

1. **Movimiento atómico en la base.** El writer deja de calcular en memoria y ejecuta `UPDATE product SET stock_quantity = stock_quantity + @delta WHERE id_product = @id` dentro de la transacción que ya existe. PostgreSQL bloquea la fila mientras dura la transacción, así que dos PCs se suman en vez de pisarse. Después recarga el producto para tener el valor real.
2. **Sin stock negativo salvo en notas de entrega.** Para los demás documentos el UPDATE lleva `AND stock_quantity >= @cantidad` y, si no actualizó ninguna fila, lanza "Stock insuficiente" (RF-7).
3. **Orden fijo para no trabarse.** Los productos de una nota se procesan ordenados por `id_product`. Así dos notas con los mismos productos no se bloquean entre sí (deadlock).
4. **Editar producto no toca el stock.** `update_product_async` solo cambia nombre, precio, código y marca. El stock cambia únicamente con tres operaciones explícitas: `agregar(n)`, `quitar(n)` y `contar(contado, stock_visto)` (RF-3, RF-4, RF-5).
5. **Conteo manual con aviso.** `contar` lee el stock real con la fila bloqueada, calcula la diferencia contra ese valor y la asienta en el kardex. Si el `stock_visto` que tenía la pantalla ya no coincide, devuelve un aviso con el valor actual y pide confirmar (RF-6).
6. **El writer pasa a ser asíncrono**, así que cada llamador agrega `await`. Es un cambio mecánico, una tarea por llamador.

## Opciones descartadas

- **Token de concurrencia optimista (xmin):** detecta el choque pero obliga a reintentar y a decirle al usuario "intente de nuevo" en la operación más común. El UPDATE atómico suma sin errores.
- **`SELECT ... FOR UPDATE` y calcular en C#:** funciona, pero son dos viajes a la base y más código; el UPDATE hace lo mismo en uno.
- **Triggers en la base que calculen el stock desde el kardex:** más robusto pero cambia el diseño completo y no se prueba bien a mano.
- **Reservar stock al abrir una nota:** fuera de alcance.

## Riesgo técnico a confirmar en la primera tarea

`ExecuteSqlInterpolated` más una recarga del producto dentro de la misma transacción debería devolver el valor nuevo. Si el seguimiento de EF molesta, plan B: un `SELECT stock_quantity` con `AsNoTracking` después del UPDATE. Se resuelve en la tarea 1.

## Pruebas (a mano)

Con **dos copias de la app** abiertas contra testing: despachar el mismo producto a la vez; abrir un producto en una y vender en la otra; hacer un conteo manual con el valor desactualizado. Al final, la consulta "stock vs kardex" (en las tareas) no debe devolver filas.

## Rama

`feature/stock-atomico` saliendo de `TESTING`.
