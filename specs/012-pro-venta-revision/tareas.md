# Tareas 012 - Revisión de Pro Venta

Las tareas marcadas **(bloqueada)** esperan una respuesta de Luis ([analisis.md](analisis.md), sección 5). Las pruebas que tocan la base usan `BaseDeTesting` y se niegan a correr fuera de testing.

## Tarea 1 - Cálculo de la edición (función pura)
Cubre: RF-1, RF-2, RF-4, RF-29
- Crear `pro_venta_note_edit` en `NinOS.Domain` con `monto_desde_descuento` y `descuento_desde_monto`. Valida la entrada, revisa la salida y redondea con `Money.round`.
- Pruebas: feliz (325,44 y 10% → 292,90; 325,44 y 0% → 325,44; 309,17 → 5%), límite (100% → 0; monto igual al original → 0%; monto 0), error (descuento −1 o 101, monto mayor al original, monto original 0 o negativo → excepción con mensaje).
- Hecho cuando: compila en 0/0 y las pruebas pasan.
- Cómo probarla: `dotnet test NinOS.slnx --filter ProVentaNoteEdit`.
- [ ] Hecha y probada

## Tarea 2 - Monto original como función pura, devoluciones por renglón
Cubre: RF-23, RF-29
- Sacar el cálculo de `ProVentaService.cargar_montos_originales_async` a `original_amount_calculator` en `NinOS.Domain`. La devolución de un producto que aparece en dos renglones se reparte entre esos renglones y no se resta dos veces. `ProVentaService` solo carga los datos y llama a la función.
- Pruebas: feliz (nota sin devoluciones; devolución de 1 unidad), límite (producto repetido en dos renglones con devolución de 1 → baja una sola vez; devolver más de lo vendido → 0), error (cantidad o precio negativo → excepción).
- Hecho cuando: las pruebas pasan y la relación de prueba de la spec 008 (nota 000_001) sigue mostrando 325,44.
- Cómo probarla: `dotnet test`. Abrir en debug la relación de la nota 000_001 y comparar el monto original.
- [ ] Hecha y probada

## Tarea 3 - Monto y saldo de la relación en una sola función
Cubre: RF-12, RF-15, RF-29
- Crear `relation_amounts` en `NinOS.Domain` (monto por pagar, abonado, saldo, saldo a favor, estado) a partir de las notas vigentes, los abonos positivos (sin los de notas anuladas) y si la relación es heredada.
- Pruebas: feliz (2 notas, 1 abono), límite (todas anuladas → Pagada con monto 0; abono igual al monto; heredada sin notas), error (monto de nota negativo → excepción).
- Hecho cuando: las pruebas pasan.
- Cómo probarla: `dotnet test --filter RelationAmounts`.
- [ ] Hecha y probada

## Tarea 4 - Usar la función única en servicio y tope de abono
Cubre: RF-12, RF-24
- `ProVentaService.build_relation_rows_async` y `PaymentService.get_relation_amount_and_paid_async` llaman a `relation_amounts`. La ventana de abono usa la misma constante de exceso (3,00) que el servicio.
- Hecho cuando: build 0/0, `dotnet test` pasa y la lista de relaciones muestra los mismos montos que antes del cambio.
- Cómo probarla (manual): anotar el monto y el saldo de 3 relaciones antes del cambio y compararlos después. Abonar el saldo + 2,50 → se acepta; saldo + 3,50 → se rechaza con el mensaje de tope.
- [ ] Hecha y probada

## Tarea 5 - Semana de Venezuela (función pura)
Cubre: RF-16, RF-29
- Crear `pro_venta_week.lunes_de(fecha_utc)` en `NinOS.Domain`, usando `AppTimeZone.to_local`.
- Pruebas: feliz (miércoles 12:00 VE → lunes de esa semana), límite (domingo 23:59 VE = lunes 03:59 UTC → lunes anterior; lunes 00:00 VE → ese lunes; cambio de mes y de año), error (fecha `default` → excepción).
- Hecho cuando: las pruebas pasan.
- Cómo probarla: `dotnet test --filter ProVentaWeek`.
- [ ] Hecha y probada

## Tarea 6 - Asignar la relación en hora de Venezuela y sin choque entre PCs
Cubre: RF-16, RF-19, RF-20, RF-30
- `DeliveryNoteService.get_or_create_week_relation_async` usa `pro_venta_week.lunes_de`. Toma `pg_advisory_xact_lock` antes de buscar o crear, y quita el `catch` que reintenta dentro de la transacción abortada. Sigue excluyendo las heredadas. Log con id de relación y semana.
- Hecho cuando: build 0/0 y una nota del domingo 9:00 pm queda en la relación de ese domingo.
- Cómo probarla (manual): poner la hora de la PC en un domingo 9:00 pm, crear una nota Pro Venta y ver que su relación es la de la semana que termina ese domingo, y que la nota sale en esa semana del reporte. Con dos copias abiertas, guardar a la vez la primera nota de una semana nueva: una sola relación, dos notas, ningún error.
- [ ] Hecha y probada

## Tarea 7 - Consultar no escribe y los números no se tocan
Cubre: RF-17, RF-18, RF-19
- En `ProVentaService.get_weekly_report_async`, quitar la llamada a `get_or_create_week_relation_async`. Borrar `get_or_create_week_relation_async` y `renumber_relations_chronologically_async` de `ProVentaService`. La búsqueda de la relación de la semana excluye las heredadas. Si una semana tiene notas sin relación, se muestra "sin relación" y queda un `AppLog.Warn`.
- Hecho cuando: build 0/0 y abrir semanas no cambia la tabla de relaciones.
- Cómo probarla (manual): en testing, `select id_relacion, relation_number, week_start from relacion order by id_relacion` antes y después de abrir 10 semanas y 5 relaciones: el resultado es idéntico.
- [ ] Hecha y probada

## Tarea 8 - Servicio de edición de Pro Venta
Cubre: RF-1, RF-4, RF-6, RF-9, RF-10, RF-11
- `ProVentaService.update_note_amount_async(id_nota, monto, dcto, monto_esperado)`: bloquea la relación y luego la nota, recalcula el monto original, compara con el monto esperado, valida con `pro_venta_note_edit` y guarda solo `adjusted_total_usd` y `discount_percentage`. Deja log de cada paso.
- Pruebas contra testing: feliz (sin descuento → monto original), límite (monto igual al original), error (monto esperado distinto → rechaza sin guardar; nota anulada → rechaza; monto mayor → rechaza).
- Hecho cuando: las pruebas pasan y `total_amount_usd`, `original_discount_percentage` y `print_snapshot` no cambian.
- Cómo probarla: `dotnet test --filter ProVentaEdicion` con `NINOS_DB_CONNECTION` a testing.
- [ ] Hecha y probada

## Tarea 9 - Bloqueo de edición en relación pagada
Cubre: RF-8
- Agregar al servicio de la tarea 8 el bloqueo: si la relación está pagada, no se edita ninguna de sus notas (impresa o liquidada no bloquean). Quitar el botón "Editar Notas" de la tabla Pagadas (`ProVentaView.xaml:473`).
- Hecho cuando: en una relación pagada no se puede editar, con un mensaje que dice que ya está pagada.
- Cómo probarla (manual): pagar por completo una relación de prueba e intentar editar una de sus notas.
- [ ] Hecha y probada

## Tarea 10 - Ventana "Editar Notas" con la base correcta
Cubre: RF-1, RF-2, RF-3, RF-5, RF-7
- `get_relation_notes_async` entrega `monto_original` y el descuento de emisión. La ventana muestra y usa `monto_original` como base, agrega los botones «Pagó con descuento» y «Pagó sin descuento», lee los montos con `parse_numeric` y llama a `update_note_amount_async` con el monto esperado.
- Hecho cuando: nota 325,44 emitida al 10%: «sin descuento» → 325,44; 5% → 309,17; «con descuento» → 292,90. Una nota de 1.250,00 se guarda sin reescribir el monto.
- Cómo probarla (manual): hacer esos tres cambios en una nota de prueba, ver que el saldo de la relación cambia y que la comisión y los gastos de la tabla semanal no cambian. Reimprimir la nota: sale igual que la original.
- [ ] Hecha y probada

## Tarea 11 - Antes y después en la edición
Cubre: RF-7
- La ventana muestra comisión, gastos, monto por pagar y saldo de la relación antes y después de guardar, usando `relation_amounts`.
- Hecho cuando: al cambiar el descuento se ve el saldo nuevo antes de guardar.
- Cómo probarla (manual): cambiar el descuento y comparar el saldo mostrado con el de la lista después de guardar.
- [ ] Hecha y probada

## Tarea 12 - Edición al mismo tiempo que un abono o que otra edición (manual)
Cubre: RF-9, RF-10
- Prueba con dos copias de la app contra testing.
- Hecho cuando: (a) si se edita la misma nota en las dos copias, la segunda recibe "la nota cambió" y no guarda; (b) si una copia abona el saldo completo mientras la otra sube el monto, el abono se valida contra el saldo que corresponde y la relación queda con el saldo correcto.
- Cómo probarla: los dos casos de arriba, comprobando la lista de relaciones al final.
- [ ] Hecha y probada

## Tarea 13 - Detalle de la relación con dos columnas (PDF y Excel)
Cubre: RF-12, RF-13
- `ProVentaPdfGenerator` (página 2) y `ProVentaWeeklyExcelExporter` (hoja 2) muestran "MONTO ORIG." y "MONTO A COBRAR". Los totales y el saldo salen de la fila de `relation_amounts`; se quitan las sumas propias.
- Hecho cuando: en una relación con una nota editada, el saldo es el mismo en la lista, el historial, el PDF y el Excel, y cada total es igual a su encabezado.
- Cómo probarla (manual): editar una nota y comparar las cuatro vistas.
- [ ] Hecha y probada

## Tarea 14 - Liquidación con una sola base (bloqueada: pregunta 2)
Cubre: RF-14
- Mover el cálculo de `total_cobrado`, `nota_por_pagar` y `diferencial` a una función de `NinOS.Domain` con la base que decida Luis. La usan `ProVentaViewModel`, `ProVentaPdfGenerator` y `ProVentaWeeklyExcelExporter`. Pruebas feliz, límite (relación sin notas vigentes) y error.
- Hecho cuando: la liquidación es la misma en pantalla, PDF y Excel, y coincide con 2 o 3 relaciones reales que dé Luis.
- Cómo probarla: `dotnet test` y comparación manual con las relaciones de Luis.
- [ ] Hecha y probada

## Tarea 15 - Nota de crédito sobre nota de relación pagada (bloqueada: pregunta 5)
Cubre: RF-15, RF-22
- En `CreditNoteService`, antes de guardar sobre una nota MAR, bloquear la relación y revisar su saldo con `relation_amounts`. Aplicar la decisión: rechazar, o dejar saldo a favor visible.
- Hecho cuando: con la relación pagada se cumple la decisión, con mensaje claro.
- Cómo probarla (manual): pagar por completo una relación de prueba e intentar una nota de crédito de 1 unidad.
- [ ] Hecha y probada

## Tarea 16 - Nota con fecha atrasada en relación pagada
Cubre: RF-21, RF-31
- Antes de guardar una nota MAR, si la relación de su semana está pagada, avisar y pedir confirmación. Si se confirma, guardar la nota y reabrir la relación como no pagada en la misma transacción, con log (ids y estados).
- Pruebas: feliz (relación abierta, sin aviso), límite (relación pagada y se cancela: no se guarda nada), error (relación pagada y se confirma: queda no pagada y el saldo sube por el monto de la nota).
- Hecho cuando: las pruebas pasan y la relación reabierta muestra el saldo nuevo en la lista, el PDF y el Excel.
- Cómo probarla (manual): crear una nota Pro Venta con la fecha de una semana cuya relación está pagada, cancelar una vez y confirmar la segunda.
- [ ] Hecha y probada

## Tarea 17 - Saldo a una fecha de corte (función pura)
Cubre: RF-25, RF-29
- Crear `relation_balance_at` en `NinOS.Domain`: solo cuenta notas creadas y abonos hechos hasta la fecha de corte, en hora de Venezuela.
- Pruebas: feliz (abono antes y otro después del corte), límite (abono el mismo día del corte a las 11:00 pm VE; relación heredada), error (corte `default` → excepción).
- Hecho cuando: las pruebas pasan.
- Cómo probarla: `dotnet test --filter RelationBalanceAt`.
- [ ] Hecha y probada

## Tarea 18 - Marcas de impresa y revisada (bloqueada: pregunta 7)
Cubre: RF-27, RF-28
- Migración aditiva en `relacion` (`printed_at`, `reviewed_at`, `review_note`, todas nulas). **Antes:** `NINOS_DB_CONNECTION` a testing y preguntar a Luis. Métodos del servicio para marcar y desmarcar, con log.
- Hecho cuando: la migración se aplica en testing sin tocar datos y una relación se puede marcar y desmarcar.
- Cómo probarla (manual): marcar una relación como impresa y otra como revisada; reiniciar la app y ver que las marcas siguen.
- [ ] Hecha y probada

## Tarea 19 - Reporte "Relación semanal por cobrar" (bloqueada: pregunta 8 y filtro por vendedor de RF-26)
Cubre: RF-25, RF-26, RF-27
- Pantalla con fecha de corte y vendedora; columnas RELACION NRO, MONTO POR PAGAR, SALDO, IMPRESA/POR IMPRIMIR; total. PDF tamaño carta y Excel con los mismos datos.
- Hecho cuando: con corte al 4 oct, el total coincide con la suma de saldos a esa fecha y las tres salidas muestran lo mismo.
- Cómo probarla (manual): sacar el reporte a dos fechas distintas con un abono entre ellas y comparar.
- [ ] Hecha y probada

## Tarea 20 - Logs y limpieza de código muerto
Cubre: RF-30
- `AppLog.Info/Warn/Error` en consulta de semana o relación, abono de relación y edición, con ids y estados. Borrar la edición en línea sin uso (`start/apply/cancel_edit_note` en `ProVentaViewModel`, `begin_edit` y sus campos en `pro_venta_weekly_row`) y `ProVentaDetailPdfGenerator`, comprobando antes con búsqueda que nada los usa.
- Hecho cuando: build 0/0, sin comentarios nuevos, y `ninos-ui.log` muestra el flujo sin nombres, RIF ni teléfonos.
- Cómo probarla (manual): abonar y editar en debug y leer `ninos-ui.log`.
- [ ] Hecha y probada

## Tarea 21 (manual, solo lectura) - Contar notas mal asignadas
Cubre: RF-16
- En testing (y en producción solo si Luis lo autoriza, y solo lectura): contar las notas MAR cuyo lunes en hora de Venezuela no coincide con el `week_start` de su relación.
- Hecho cuando: Luis tiene el número y decide si hace falta una spec de corrección.
- Cómo probarla: la consulta devuelve la lista; se revisan a mano 2 casos.
- [ ] Hecha y probada

## Tarea 23 - Monto por pagar sin descuento (bloqueada: RF-32, notas existentes)
Cubre: RF-32
- Al emitir una nota MAR, su monto a cobrar en la relación es el monto original sin descuento; la nota impresa no cambia (spec 004). La edición de las tareas 7 y 8 lo ajusta cuando el cliente paga con descuento. Aplicar a las notas existentes solo si Luis lo decide, y en ese caso con script supervisado y respaldo.
- Pruebas: feliz (nota al 10%: monto por pagar 325,44), límite (nota sin descuento: igual al original), error (nota sin renglones: excepción con mensaje).
- Hecho cuando: una nota nueva entra a su relación sin descuento y las relaciones existentes no cambian.
- Cómo probarla (manual): emitir una nota MAR al 10% en testing y ver el monto por pagar de su relación.
- [ ] Hecha y probada

## Tarea 22 (manual) - Prueba completa contra testing
Cubre: RF-6, RF-12, RF-29
- Recorrer "Cómo sé que está listo" de la spec completo y correr `dotnet build NinOS.slnx` y `dotnet test NinOS.slnx`.
- Hecho cuando: todo pasa.
- Cómo probarla: la lista de la spec, en orden.
- [ ] Hecha y probada
