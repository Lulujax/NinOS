# Plan 012 - Revisión de Pro Venta

Cómo cumplir [spec.md](spec.md). Todavía no se ha tocado código. Los hallazgos con su evidencia están en [analisis.md](analisis.md).

## Qué hay hoy (verificado)

- [ProVentaEditNotesWindow.xaml.cs](../../src/NinOS.UI/Views/ProVentaEditNotesWindow.xaml.cs) usa `gross_total_usd` como base (:162, :177, :199). Ese campo se llena con `total_amount_usd`, que ya trae el descuento ([ProVentaService.cs:957](../../src/NinOS.Infrastructure/Services/Implementations/ProVentaService.cs#L957)). El monto se muestra con `N2` invariante y se lee cambiando coma por punto (:147, :227): falla con montos de 1.000 o más.
- Guardar la edición llama a [AccountsReceivableService.update_note_total_async](../../src/NinOS.Infrastructure/Services/Implementations/AccountsReceivableService.cs#L613), que bloquea la nota, topa al bruto de los renglones y recalcula con `NoteSettlement`. No bloquea la relación ni revisa si está pagada. Para una nota MAR lo "pagado" siempre es 0, porque los abonos de relación se guardan sin nota.
- [DeliveryNoteService.get_or_create_week_relation_async](../../src/NinOS.Infrastructure/Services/Implementations/DeliveryNoteService.cs#L220) calcula la semana con la fecha UTC. Si choca con otra PC, reintenta la lectura dentro de una transacción ya abortada.
- [ProVentaService.get_weekly_report_async](../../src/NinOS.Infrastructure/Services/Implementations/ProVentaService.cs#L177) crea la relación si falta (:327-333) y renumera todas (:779-857). Busca la relación de la semana sin excluir las heredadas (:323).
- El monto de la relación se calcula en tres lugares distintos: `build_relation_rows_async` (:1157), `PaymentService.get_relation_amount_and_paid_async` ([PaymentService.cs:74](../../src/NinOS.Infrastructure/Services/Implementations/PaymentService.cs#L74)) y los exportadores ([ProVentaPdfGenerator.cs:55](../../src/NinOS.UI/Common/ProVentaPdfGenerator.cs#L55), [ProVentaWeeklyExcelExporter.cs:197](../../src/NinOS.UI/Common/ProVentaWeeklyExcelExporter.cs#L197)), cada uno con su propia base.
- La liquidación (`nota_por_pagar`, `total_cobrado`, `diferencial`) se calcula en [ProVentaViewModel.cs:246,273-274](../../src/NinOS.UI/Common/ViewModels/ProVentaViewModel.cs#L246), [ProVentaPdfGenerator.cs:48](../../src/NinOS.UI/Common/ProVentaPdfGenerator.cs#L48) y [ProVentaWeeklyExcelExporter.cs:123](../../src/NinOS.UI/Common/ProVentaWeeklyExcelExporter.cs#L123), sobre `total_amount_usd`.
- Ya hay pruebas de `pro_venta_calculator` y `RelationBalance` en `tests/NinOS.Tests`, y `BaseDeTesting` para las pruebas que tocan la base: se niega a correr si la conexión no es de testing.

## Enfoque

1. **Funciones puras nuevas en `NinOS.Domain`** (sin base, fáciles de probar):
   - `pro_venta_note_edit`: `monto_desde_descuento(monto_original, dcto)` y `descuento_desde_monto(monto_original, monto)`. Valida al entrar (monto original > 0, dcto entre 0 y 100, monto entre 0 y el original) y revisa al salir que monto ≤ original. Redondea con `Money.round`.
   - `pro_venta_week`: `lunes_de(fecha_utc)`, que pasa la fecha a hora de Venezuela con `AppTimeZone.to_local` y devuelve el lunes. La usan `DeliveryNoteService` y `ProVentaService`.
   - `relation_amounts`: recibe las notas de la relación (estado y monto a cobrar), los abonos y si es heredada. Devuelve monto por pagar, abonado, saldo y estado. La usan la lista de relaciones, el tope de abono, la edición y los exportadores. Reemplaza las tres copias.
   - `original_amount_calculator`: el cálculo de monto original que hoy vive dentro de `ProVentaService.cargar_montos_originales_async`, sacado como función pura y con las devoluciones repartidas por renglón (RF-23).
   - `relation_balance_at`: saldo de una relación a una fecha de corte, contando solo notas y abonos hasta esa fecha (RF-25).
2. **Servicio de edición propio de Pro Venta** en `ProVentaService` (`update_note_amount_async(id_nota, monto_nuevo, dcto_nuevo, monto_esperado)`). Hace esto, en este orden y dentro de una transacción:
   1. `FOR UPDATE` sobre la relación y después sobre la nota, siempre en ese orden para no trabarse con un abono (que bloquea la relación).
   2. Recalcula el monto original con la función pura.
   3. Si `adjusted_total_usd` ya no es `monto_esperado`, rechaza (RF-9).
   4. Rechaza si la relación está pagada (RF-8, según la decisión).
   5. Valida con `pro_venta_note_edit`.
   6. Guarda `adjusted_total_usd` y `discount_percentage`.
   7. Deja log.

   No toca `total_amount_usd`, `original_discount_percentage`, `print_snapshot`, comisiones ni pagos. `update_note_total_async` de CxC no se cambia: CxC general sigue igual.
3. **Ventana "Editar Notas":** la base visible y usada es `monto_original`. Se agregan los botones «Pagó con descuento» y «Pagó sin descuento». El monto se lee con `pro_venta_weekly_row.parse_numeric`, que ya entiende las dos formas de miles. Se muestra el antes y el después del saldo. El botón no se ofrece en la tabla de relaciones Pagadas (o pide confirmación, según RF-8).
4. **Relación en hora de Venezuela y sin renumerar:** `DeliveryNoteService` usa `pro_venta_week.lunes_de`. Para que dos PCs no choquen, antes de buscar o crear la relación se toma un bloqueo de transacción con `pg_advisory_xact_lock` (el mismo patrón que ya protege el correlativo en `DeliveryNoteService.cs:63`). Con el bloqueo ya no hace falta el `catch` que reintenta dentro de la transacción abortada. `ProVentaService` deja de crear relaciones al consultar y se borra `renumber_relations_chronologically_async`. La búsqueda por semana excluye las heredadas.
5. **Una sola cifra en pantalla, PDF y Excel:** los exportadores reciben la fila de la relación ya calculada (`relation_amounts`) y no suman por su cuenta. El detalle lleva dos columnas: monto original y monto a cobrar. La liquidación se calcula en un solo método del dominio, con la base que decida Luis (RF-14).
6. **Notas de crédito y fechas atrasadas:** antes de guardar una nota de crédito sobre una nota MAR, o una nota MAR cuya semana ya tiene relación pagada, se consulta `relation_amounts` de esa relación y se aplica la decisión de RF-21 y RF-22.
7. **Reporte del cliente:** un método `get_relations_at_cutoff_async(fecha_corte, id_vendedora?)` y un generador PDF/Excel nuevo con las columnas del cliente.
## Migraciones

Esta spec no necesita migración de base de datos.

## Cómo no se rompe lo que ya está decidido

- **Nota congelada (spec 004):** la edición solo escribe `adjusted_total_usd` y `discount_percentage`. La reimpresión usa `print_snapshot` o `original_discount_percentage` (`AccountsReceivableService.cs:845-852,937`). Hay una prueba manual que reimprime después de editar.
- **Comisión y gastos (spec 008):** siguen saliendo de `monto_original` con `pro_venta_calculator`. La edición no los toca y una prueba lo comprueba.
- **Comisiones pagadas (spec 005):** las notas Pro Venta no generan comisión de vendedora (`PaymentService.cs:558`). La edición propia de Pro Venta no llama a `apply_settlement_async` para crear o borrar comisiones.
- **Sin eliminar pagos:** nada en esta spec borra o cambia abonos. Un abono que supere el tope se rechaza al registrarlo.
- **Arranque sin efectos (spec 007):** nada se ejecuta al abrir la app. Y con RF-18, abrir un reporte tampoco escribe.

## Opciones descartadas

- **Reutilizar `update_note_total_async` de CxC con otra base:** mezclaría reglas de Pro Venta (bloqueo por relación, base = monto original) en el flujo de CxC general, que funciona y usa otra base (`Σ subtotal`). Un cambio ahí arriesga CxC.
- **Guardar el monto original en una columna nueva de la nota:** se desactualiza con las notas de crédito. La spec 008 ya decidió calcularlo siempre.
- **Corregir la semana con `AddHours(-4)` en cada lugar:** es lo que dejó la asignación sin corregir en la 008. Una sola función evita que vuelva a pasar.
- **Índice único en `week_start`:** no se puede, porque varias relaciones heredadas comparten semana. Un índice único parcial (`WHERE es_hueca = false`) sería otra opción, pero es una migración y falla si en producción ya hay duplicados. Un bloqueo de transacción (`pg_advisory_xact_lock`) no cambia la base y ya se usa para el correlativo.
- **Seguir renumerando "solo cuando hace falta":** cualquier cambio de número rompe relaciones ya entregadas. Un número no se toca nunca.
- **Bloqueo optimista con columna de versión en la nota:** pide migración. Comparar el `adjusted_total_usd` esperado dentro del `FOR UPDATE` logra lo mismo para este caso.

## Riesgos

- **Bloqueos:** relación y nota se bloquean siempre en el mismo orden (relación primero) en edición y abono, para que no se traben entre sí. Las transacciones son cortas.
- **Datos ya mal asignados (domingo en la noche):** corregir el código no mueve notas viejas. Esta spec no las revisa; si Luis decide corregirlas, va en spec aparte con respaldo y script supervisado.
- **Cambiar la liquidación** cambia cifras que el cliente ya conoce. Por eso RF-14 espera la decisión de Luis y se compara con 2 o 3 relaciones reales antes de publicar.
- **Quitar la creación de relaciones al consultar:** una semana con notas sin relación mostraría "sin relación". No debería pasar, porque toda nota MAR recibe relación al guardarse. Si aparece, es un dato a revisar, no algo que se arregle solo.

## Pruebas (a mano, contra testing)

Editar con y sin descuento y reimprimir la nota. Editar una nota de 1.250. Intentar editar en una relación pagada. Abonar desde una PC mientras se edita desde otra. Comparar la misma relación en la lista, el historial, el PDF y el Excel. Crear una nota el domingo a las 9:00 pm (hora de la PC). Abrir semanas viejas y comprobar con una consulta de solo lectura que la tabla de relaciones no cambió. Dos PCs guardan a la vez la primera nota de una semana. Reporte con fecha de corte.

## Rama

`feature/pro-venta-revision` saliendo de `TESTING`. Sin migraciones en esta spec.
