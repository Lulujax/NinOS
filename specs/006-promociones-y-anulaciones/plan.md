# Plan 006 - Promociones y anulaciones coherentes

Cómo cumplir [spec.md](spec.md). Todavía no se ha tocado código.

## Qué hay hoy (verificado)

- [AccountsReceivableService.cs:797](../../src/NinOS.Infrastructure/Services/Implementations/AccountsReceivableService.cs#L797) (anular nota) y [CreditNoteService.cs:1076](../../src/NinOS.Infrastructure/Services/Implementations/CreditNoteService.cs#L1076) (anular nota de crédito) cargan solo los productos que aparecen **directo** en los renglones. Los productos que forman una promoción no se cargan, y el código hace `if (products.TryGetValue(...))` sin `else`, así que **no devuelve stock y no avisa**.
- [InventoryService.cs:445](../../src/NinOS.Infrastructure/Services/Implementations/InventoryService.cs#L445): editar una promoción borra y recrea sus productos. Todas las notas viejas la ven con la composición nueva.
- [CreditNoteService.cs:746](../../src/NinOS.Infrastructure/Services/Implementations/CreditNoteService.cs#L746) y `:422` cuentan como "ya devuelto" las notas de crédito **anuladas**; [AccountsReceivableService.cs:778](../../src/NinOS.Infrastructure/Services/Implementations/AccountsReceivableService.cs#L778) bloquea anular una nota si tiene **cualquier** nota de crédito, anulada o no.
- En testing no hay notas con promociones; en producción hay que confirmarlo.

## Enfoque

1. **Guardar la composición al vender.** Columna `promo_items_snapshot` (jsonb) en `note_detail` con `[{id_product, quantity_required}]` de la promoción en ese momento. En `credit_note_detail`, una devolución copia la composición del renglón original; un obsequio usa la de la promoción actual.
2. **Una sola rutina para revertir/aplicar stock de una línea**, que lee la composición guardada, carga **todos** los productos necesarios y, si falta alguno, **cancela toda la operación** con un mensaje que nombra la línea. Se usa en crear, anular y devolver.
3. **Notas de crédito anuladas no cuentan:** los filtros de "ya devuelto" y de "tiene notas de crédito" agregan `status <> 'Anulada'`.
4. **Notas viejas sin composición guardada:** usan la composición actual de la promoción y dejan una línea en el log (no se puede saber la de entonces). Si la promoción ya no existe, la operación se cancela con mensaje.

## Opciones descartadas

- **Tabla nueva `note_detail_promo_item`:** más flexible pero más migración y más consultas; para revertir stock basta la lista guardada en el renglón.
- **No permitir editar promociones usadas en notas:** frena el trabajo diario; la foto resuelve el problema sin prohibir.
- **Versionar las promociones:** correcto pero sobredimensionado para este sistema.
- **Dejar que anular promociones "avise y siga":** deja el inventario mal sin que nadie lo note, justo lo que se quiere evitar.

## Riesgo

Notas con promoción emitidas antes de este cambio no tienen foto. Por eso la tarea 1 revisa producción (solo lectura) y decide si hace falta un tratamiento especial.

## Pruebas (a mano)

Crear una promoción de 3 productos, venderla, anular la nota y revisar los 3 productos y el kardex; editar la promoción y anular una nota vieja; devolver, anular la nota de crédito y devolver otra vez; anular una nota cuyas notas de crédito están todas anuladas.

## Rama

`feature/promociones-anulaciones` saliendo de `TESTING`. Migración solo contra testing y preguntándote antes.
