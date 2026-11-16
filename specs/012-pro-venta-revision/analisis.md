# Análisis 012 - Revisión de Pro Venta

Revisión del código en la rama `TESTING` (2026-11-16) antes de escribir la [spec](spec.md). Solo se leyó código: no se tocó ninguna base ni se cambió código. Las rutas son relativas a la raíz del repo y las líneas son las del archivo actual.

## 1. ¿La relación guarda las notas "sin el precio del descuento"?

**No. La nota se guarda CON el descuento, y la relación no guarda montos.** Lo "sin descuento" se calcula al vuelo cada vez que se muestra.

| Qué | Dónde se guarda o calcula | Con o sin descuento |
|---|---|---|
| `delivery_note.total_amount_usd` | Se guarda al emitir con el total de la pantalla, ya descontado (`DeliveryNotesViewModel.cs:2085-2093`, total armado en `recalculate_total` `:1934-1940`). Las notas de crédito lo bajan después (`CreditNoteService.cs:987`) y su anulación lo sube (`:1156`). | **Con** descuento de emisión (ej.: 292,90) |
| `delivery_note.adjusted_total_usd` (monto a cobrar) | Igual a `total_amount_usd` al emitir (`DeliveryNotesViewModel.cs:2092`). Cambia con "Editar Notas" (`AccountsReceivableService.cs:649`) y con notas de crédito (`CreditNoteService.cs:986`). | **Con** descuento (el que se cobra) |
| `monto_original` | **No se guarda.** Se calcula: Σ (cantidad − unidades devueltas en NC vigentes) × precio de lista del renglón (`ProVentaService.cs:587-695`). | **Sin** descuento (ej.: 325,44) |
| Relación (`relacion`) | Solo guarda número, semana, `es_hueca`, `saldo` (solo para las heredadas) y `observaciones` (`relacion.cs`). | No guarda montos (salvo las heredadas) |

Qué se ve en cada lugar para **la misma relación**:

| Lugar | Columna o cifra | Fuente | Base |
|---|---|---|---|
| Tabla semanal en pantalla, PDF pág. 1, Excel hoja 1 | MONTO ORIG. / MONTO NOTA, comisión, gastos | `monto_original` (`ProVentaView.xaml:156`, `ProVentaPdfGenerator.cs:166`, `ProVentaWeeklyExcelExporter.cs:60`) | Sin descuento |
| Liquidación (pantalla, PDF pág. 1, Excel hoja 1) | TOTAL COBRADO, NOTA POR PAGAR A PRO VENTA, DIFERENCIAL | `total_amount` = Σ `total_amount_usd` (`ProVentaService.cs:262,360,463,518`; `ProVentaViewModel.cs:246,273-274`; `ProVentaPdfGenerator.cs:48-49`; `ProVentaWeeklyExcelExporter.cs:123-124`) | Con descuento de emisión, **sin** las ediciones |
| Lista de relaciones (Pendientes/Pagadas), historial, ventana de abono | MONTO POR PAGAR, SALDO COMPLETO | Σ `adjusted_total_usd` (`ProVentaService.cs:1157`, `ProVentaHistoryWindow.xaml.cs:38`, `PaymentService.cs:89-91`) | Con descuento, **con** las ediciones |
| PDF pág. 2 (detalle) | Columna MONTO ORIG. y su TOTAL; encabezado "MONTO:" | Columna: `monto_original` (`ProVentaPdfGenerator.cs:300,307`). Encabezado: Σ ajustado (`:238-240`) | Mezcla: la columna no suma lo que dice el encabezado |
| Excel hoja 2 (detalle) | MONTO ($), TOTAL, saldo | Σ `total_amount_usd` (`ProVentaWeeklyExcelExporter.cs:186,197,264`) | Con descuento de emisión, **sin** las ediciones: el saldo puede no coincidir con la pantalla |

Conclusión: en pantalla y en el PDF las **filas** de la relación muestran el monto sin descuento, como pide la spec 008. Pero el **monto por pagar y el saldo** de la relación salen del monto con descuento, y la liquidación y el Excel de detalle usan una tercera cifra que ignora las ediciones.

## 2. Cómo funciona hoy la edición en CxC general y en Pro Venta

- **CxC general:** edición en línea en la grilla (`AccountsReceivableView.xaml:110`). `begin_edit` arma la base como `monto / (1 − dcto)` (`AccountsReceivableViewModel.cs:111-128`) y el monto nuevo es `base × (1 − dcto)` (`:130-140`). Al guardar llama a `update_note_total_async` (`AccountsReceivableViewModel.cs:1075`), que bloquea la nota con `FOR UPDATE` (`AccountsReceivableService.cs:626`), rechaza anulada o devuelta (`:629-630`), topa el monto al bruto de los renglones (`:636-647`), guarda `adjusted_total_usd` y `discount_percentage` (`:649-654`) y recalcula estado y comisión con `NoteSettlement` (`:656-661`, `PaymentService.cs:548-602`). La pantalla no deja editar notas Pagadas (`AccountsReceivableViewModel.cs:1033`). La nota impresa no cambia porque la reimpresión usa la foto (`AccountsReceivableService.cs:845-852`) o los descuentos originales (`:937`).
- **Pro Venta:** botón "Editar Notas" en las tablas de relaciones **pendientes y pagadas** (`ProVentaView.xaml:358,473`) que abre `ProVentaEditNotesWindow` y llama al mismo `update_note_total_async` (`ProVentaViewModel.cs:1005-1011`). Con `is_pro_venta = true` no se crea comisión de vendedora (`PaymentService.cs:558-571`): la comisión de Luis y los gastos no se guardan, se calculan con `pro_venta_calculator` sobre el monto original.
- Diferencia de fondo: en Pro Venta los abonos son de la **relación** y se guardan sin nota (`PaymentService.cs:205-207`, `id_delivery_note = null`). Por eso el "pagado" que ve `update_note_total_async` para una nota MAR siempre es 0 (`AccountsReceivableService.cs:656-659`), la nota MAR nunca llega a "Pagada" y los bloqueos por "Pagada" de la ventana (`ProVentaEditNotesWindow.xaml.cs:221-225`) no se activan nunca. Lo que en Pro Venta equivale a "pagada" es que la **relación** esté pagada, y eso no se revisa en ningún lado al editar.

## 3. Hallazgos

| # | Riesgo | Hallazgo | Evidencia | ¿Cubierto antes? |
|---|---|---|---|---|
| H-01 | Alto | "Editar Notas" usa como base el total **con** descuento de emisión. Si se pone 0% sale 292,90 y no 325,44; poner 5% sobre una nota emitida al 10% descuenta sobre algo ya descontado. Justo lo que Luis quiere hacer ("pagó sin descuento") no se puede hacer con los botones. | `ProVentaEditNotesWindow.xaml.cs:140,162-163,177-180,199-200`; la base viene de `gross_total_usd = n.total_amount_usd` (`ProVentaService.cs:957`) | La spec 008 (RF-8, tarea 7) lo marca como hecho, pero el archivo no cambia desde el commit `07920d7`. **No está resuelto.** |
| H-02 | Alto | Una nota de 1.000 o más no se puede guardar desde "Editar Notas" sin reescribir el monto: la caja se llena con `"1,234.56"` (`N2` invariante) y al leerla se cambia la coma por punto: `"1.234.56"` no se puede convertir y sale "El monto USD debe ser un número válido". | `ProVentaEditNotesWindow.xaml.cs:147,166,175,201,227` | No |
| H-03 | Alto | Al guardar una nota la relación se elige con la fecha en **UTC** (`date.Date`). Una nota del domingo entre 8:00 pm y 11:59 pm (hora de Venezuela) entra en la relación de la semana **siguiente**, pero el reporte semanal (que ya usa hora de Venezuela) la muestra en la semana correcta. Resultado: la nota sale en la semana X y se cobra en la relación X+1. | `DeliveryNoteService.cs:167,220-223` frente a `ProVentaService.cs:213-215` | La spec 008 (RF-11, tarea 8) solo corrigió el filtro del reporte, no la asignación. |
| H-04 | Alto | Consultar el reporte semanal **escribe en la base**: si la semana tiene notas y no tiene relación, crea una y después **renumera todas** las relaciones normales por fecha. Si se crea la de una semana anterior a otras ya existentes, todas las posteriores cambian de número (ej.: 180, 181, 182 pasan a ser 181, 182, 183 y la nueva queda en 180). Los números ya impresos y entregados dejarían de coincidir. Con H-03 el caso es alcanzable: basta una semana cuya única nota sea de un domingo en la noche. | `ProVentaService.cs:319-333,709-775,779-857` | No |
| H-05 | Alto | La misma relación da cifras distintas según dónde se mire (ver tabla de la sección 1). Tras editar una nota, el Excel de detalle muestra otro saldo que la pantalla, y el PDF pág. 2 muestra un encabezado "MONTO" que no es la suma de su columna. | `ProVentaWeeklyExcelExporter.cs:186,197,264`; `ProVentaPdfGenerator.cs:238-240,300,307`; `ProVentaService.cs:463,518,1157` | La spec 008 RF-6 pedía una sola fuente para los porcentajes, no para el monto por pagar. |
| H-06 | Alto | La liquidación mezcla bases: "total cobrado" = Σ total con descuento de emisión − comisión calculada sin descuento; "nota por pagar a Pro Venta" = Σ total con descuento − gastos calculados sin descuento. Además ignora las ediciones. Ej.: nota de 100 emitida al 10% → total cobrado 80,00, nota por pagar 53,75, diferencial 26,25; si el cliente pagó sin descuento, la liquidación sigue igual. | `ProVentaViewModel.cs:246,273-274`; `ProVentaPdfGenerator.cs:48-49`; `ProVentaWeeklyExcelExporter.cs:123-124` | No. `[NECESITA ACLARACIÓN]` sobre la base correcta. |
| H-07 | Medio | Se pueden editar notas de una relación **ya pagada** (el botón está también en la tabla Pagadas). Bajar el monto deja un sobrepago que no se ve, porque el saldo se recorta a 0; subirlo hace que la relación vuelva a Pendientes sin aviso. | `ProVentaView.xaml:473`; `ProVentaService.cs:1191`; `ProVentaEditNotesWindow.xaml.cs:221` (revisa la nota, no la relación) | No |
| H-08 | Medio | Una nota de crédito sobre una nota MAR revisa el saldo **de la nota** (siempre completo, porque los abonos son de la relación), así que se permite aunque la relación ya esté pagada. Baja el monto y deja un sobrepago oculto. | `CreditNoteService.cs:666-674,986-987` | La spec 005 cubrió el saldo por nota en notas generales, no el caso Pro Venta. |
| H-09 | Medio | La fecha de la nota se puede cambiar en pantalla (`DatePicker`). Una nota con fecha atrasada entra sin aviso en una relación vieja, aunque ya esté impresa o pagada, y el monto de esa relación sube. Es la explicación más probable de las relaciones del Excel del cliente cuyo monto sube entre cortes (sección 4). | `DeliveryNotesView.xaml:156`; `DeliveryNotesViewModel.cs:1184-1191`; `DeliveryNoteService.cs:167` | No |
| H-10 | Medio | Concurrencia: editar una nota bloquea la nota pero no la relación, y abonar bloquea la relación pero no sus notas. Un abono puede validarse contra el saldo de antes de la edición. Dos personas editando la misma nota: gana la última sin aviso. | `AccountsReceivableService.cs:626`; `PaymentService.cs:197-203` | La spec 005 cubrió pagos contra pagos, no edición contra abono. |
| H-11 | Medio | Si dos PCs guardan a la vez la primera nota Pro Venta de una semana, las dos calculan el mismo número de relación; la segunda choca con el índice único y el reintento lee dentro de una transacción ya abortada de PostgreSQL, así que la nota no se guarda y el usuario ve un error. El índice de `week_start` dejó de ser único al cargar las relaciones heredadas. | `DeliveryNoteService.cs:229-248`; `20261009053450_AddLegacyRelations.cs:38-41` | No |
| H-12 | Medio | El reporte semanal busca la relación de la semana sin excluir las heredadas, así que puede mostrar el número de una relación heredada si comparten semana. | `ProVentaService.cs:319-323` (las otras búsquedas sí filtran: `:723`, `DeliveryNoteService.cs:226`) | No |
| H-13 | Medio | Sin logs en el flujo Pro Venta (reporte, relaciones, abonos de relación, edición). Si algo falla no hay rastro. | `ProVentaService.cs` sin `AppLog`; `PaymentService.cs:186-257` sin `AppLog` | No (regla de logs de Luis) |
| H-14 | Bajo | Al calcular el monto original, si un mismo producto aparece en dos renglones de la nota, la devolución se resta en **los dos** renglones. | `ProVentaService.cs:659-681` | No |
| H-15 | Bajo | El tope de abono es 2,00 en la ventana y 3,00 en el servicio. | `ProVentaPaymentWindow.xaml.cs:103`; `PaymentService.cs:24` | La spec 005 fijó 3,00 en el servicio. |
| H-16 | Bajo | Código muerto que calcula mal si alguien lo vuelve a conectar: edición en línea de Pro Venta (comandos sin botón en el XAML) con base `monto / (1 − dcto)` sobre el total con descuento, y `ProVentaDetailPdfGenerator` (no se usa y deja en 0 el total del monto original). | `ProVentaViewModel.cs:63-65,94-96,1013-1077`; `pro_venta_dto.cs:120-128`; `ProVentaDetailPdfGenerator.cs` | No |
| H-17 | Bajo | Cada consulta filtra el tipo de nota a su manera: detalle de relación sin filtro de tipo (`ProVentaService.cs:881-889`), reporte de relación acepta tipo nulo (`:416`), lista de relaciones exige MAR/PVP/VOLMAR (`:1027`). El tope de abono (`PaymentService.cs:84-99`) no descarta lo abonado a notas anuladas y la lista sí (`ProVentaService.cs:1079-1109`). | rutas indicadas | No |
| H-18 | Bajo | Si una nota quedara asignada a una relación heredada, su monto no se suma: la heredada usa solo su `saldo` fijo. | `ProVentaService.cs:1141,1209-1257` | No |
| H-19 | Bajo | Los abonos de relación se guardan siempre como "Efectivo", con referencia `REL-{id}` y sin banco. No hay forma de saber cómo pagó el cliente. | `PaymentService.cs:205-207` | No |
| H-20 | Bajo | La comisión de Luis y los gastos de Pro Venta nunca se congelan: si después de liquidar una relación se emite una nota de crédito, el PDF regenerado muestra otra liquidación. | `ProVentaService.cs:295-299` (siempre en vivo) | No. `[NECESITA ACLARACIÓN]` |
| H-21 | Bajo | El reporte semanal carga **todas** las notas Pro Venta de la historia y filtra en memoria; con años de datos se pondrá lento. | `ProVentaService.cs:197-219` | No |

Lo que sí está bien y hay que conservar: la comisión y los gastos se calculan por nota sobre el monto original con una sola función (`pro_venta_calculator.cs`); las anuladas no suman (`ProVentaService.cs:313-315,1151-1153`); el abono de relación tiene tope y bloqueo (`PaymentService.cs:186-257`); la anulación de una nota MAR con abonos en su relación se bloquea (`AccountsReceivableService.cs:716-732`); la nota impresa no cambia al editar (`AccountsReceivableService.cs:845-852,937`); no existe "eliminar pago".

## 4. El reporte del cliente ("RELACION SEMANAL POR COBRAR JUAN NAVAS")

### Qué lleva el cliente

- Una fila por relación con **número**, **monto por pagar** y **saldo** (completo o actualizado). El total de la hoja es la suma de esa columna en cada fecha de corte.
- El "monto por pagar" se **sobrescribe** con lo que falta: la 131 pasa de 735,00 a 100,00 y la 129 de 1.230,04 a 396,00. En la hoja con "SALDO ACTUALIZADO" ya separa las dos cifras (140: monto 2.424,05, saldo 680,00).
- Las relaciones cobradas salen con "-" o con "0,00". No está claro qué diferencia hay entre las dos marcas.
- Marca de **impresión** por relación: "POR IMPRIMIR" o "IMPRESA" (170-172 y 179 por imprimir; 177 y 178 impresas).
- Marca de **revisión**: columna "OK 30/07" y "REVISADAS POR SANDRA 28/9".
- Es un reporte **de una sola vendedora** (Juan Luis), no de toda la empresa.
- Comprobación de sumas: el corte del 25 ene suma exacto 12.068,83. El segundo corte suma **13.502,43** y la hoja dice 13.502,42: hay al menos un monto con más de 2 decimales escondidos en el Excel. El sistema redondea cada monto a 2 decimales (`Money.round`), así que puede haber diferencias de un céntimo al comparar.

### Por qué suben algunas relaciones (146 y 173)

- 146: 795,79 → 1.028,21 (+232,42) entre enero y mayo. 173: 1.046,83 → 1.882,99 (+836,16) entre el 30 sep y el 4 oct.
- **No es por quitar el descuento del 10%**: 795,79 / 0,9 = 884,21 y 1.046,83 / 0,9 = 1.163,14; ninguno da el monto nuevo.
- **No es por notas de crédito**: una nota de crédito baja el monto, nunca lo sube.
- Lo que encaja es que se **agregaron notas** a una relación ya emitida (una nota con fecha atrasada o despachada tarde que se anotó en esa semana), o una corrección a mano. En el sistema, eso pasa hoy sin aviso al guardar una nota con fecha atrasada (H-09), y también al editar una nota de una relación pagada (H-07).
- 174 (1.333,78 → 690,26) y 175 (488,94 → 44,16) bajan: son abonos.
- `[NECESITA ACLARACIÓN: ¿por qué subieron la 146 y la 173? ¿Se agregaron notas atrasadas, el cliente perdió el descuento o fue una corrección a mano?]`

### Cómo se relaciona con lo que hay en el sistema

- Según `ProVentaService.cs:803` y `DbSeeder.cs:315`, las relaciones 149 a 179 del Excel se cargaron como **relaciones heredadas** (`es_hueca`) con un saldo fijo, y la primera relación normal del sistema es la 180. Para las heredadas el sistema muestra "monto por pagar" = saldo cargado, y abonado = lo cobrado desde la carga.
- `[NECESITA ACLARACIÓN: ¿de qué corte salió el saldo de las heredadas (4 oct u otro)? Si el monto de la 173 subió después de la carga, el sistema no lo sabe.]`

| Lo que lleva el cliente | ¿Lo tiene el sistema hoy? |
|---|---|
| Número de relación | Sí (`relation_label`) |
| Monto por pagar | Sí: Σ monto a cobrar (con descuento y con ediciones) de las notas vigentes (`ProVentaView.xaml:330`) |
| Saldo completo/actualizado | Sí: monto − abonos positivos (`ProVentaView.xaml:332`) |
| Total al día de hoy | Sí, en los totales de Pendientes (`ProVentaView.xaml:397,400`) y en el PDF/Excel de resumen |
| Total **a una fecha de corte** ("al 4 oct") | **No**: siempre muestra el estado de hoy |
| Reporte por vendedora | **No**: las relaciones son por semana y mezclan vendedoras (`pro_venta_dto.cs` comentario de `seller_name`; relaciones 4 a 7 mezcladas según `AccountsReceivableService.cs:721-723`) |
| POR IMPRIMIR / IMPRESA | **No**: no se guarda si la relación se imprimió |
| Revisión (OK, fecha, quién revisó) | **No**. La tabla tiene `observaciones` pero ninguna pantalla la usa. Guardar quién revisó choca con la regla "sin registro de quién hizo qué" |
| "-" frente a "0,00" | **No**: el sistema solo tiene Pendiente/Pagada |

Lo que haría falta para reproducir el reporte (va en la spec, RF-25 a RF-28): un reporte con fecha de corte, filtro por vendedora si las relaciones son por vendedora, marca de impresa con fecha y marca de revisada con fecha y nota corta.

## 5. Preguntas que Luis debe responder

1. `[NECESITA ACLARACIÓN: El "MONTO POR PAGAR" de una relación, ¿debe ser con descuento (lo que hoy se cobra) o sin descuento? En el Excel del cliente, ¿esos montos tienen el 10% aplicado?]`
2. `[NECESITA ACLARACIÓN: En la liquidación, ¿sobre qué base van "total cobrado", "nota por pagar a Pro Venta" y "diferencial": monto original sin descuento, total emitido con descuento, o lo realmente cobrado después de editar?]`
3. `[NECESITA ACLARACIÓN: Cuando una relación ya está pagada (o impresa, o liquidada), ¿se puede seguir editando el monto o el descuento de sus notas? ¿Bloquear o solo avisar?]`
4. `[NECESITA ACLARACIÓN: Una nota con fecha atrasada que cae en una relación ya pagada o impresa: ¿se bloquea, se avisa o se manda a la relación de la semana actual?]`
5. `[NECESITA ACLARACIÓN: ¿Se permite una nota de crédito sobre una nota Pro Venta cuya relación ya está pagada? Si sí, ¿qué pasa con el dinero sobrante: saldo a favor del cliente, devolución o abono a otra relación?]`
6. `[NECESITA ACLARACIÓN: ¿Las relaciones son por semana para toda la empresa (como hoy) o por vendedora (como el Excel de Juan Navas)? ¿Anais tiene sus propias relaciones?]`
7. `[NECESITA ACLARACIÓN: ¿Se quiere guardar en el sistema la marca POR IMPRIMIR/IMPRESA y la marca de revisión? Si se guarda la revisión, ¿basta con fecha y una nota corta, sin nombre de persona, para respetar la regla de no registrar quién hizo qué?]`
8. `[NECESITA ACLARACIÓN: ¿Qué diferencia hay entre "-" y "0,00" en la columna de saldo?]`
9. `[NECESITA ACLARACIÓN: ¿La comisión de Luis y los gastos de una relación ya liquidada deben quedar congelados, o siempre se recalculan?]`
10. `[NECESITA ACLARACIÓN: ¿Por qué subieron la 146 y la 173 en el Excel? ¿De qué corte salió el saldo cargado de las relaciones heredadas 149-179?]`
11. `[NECESITA ACLARACIÓN: ¿Hay notas en producción mal asignadas por H-03 (domingo en la noche)? Se sabe con una consulta de solo lectura; corregirlas sería un script supervisado con respaldo y spec propia.]`
12. `[NECESITA ACLARACIÓN: ¿Hace falta un historial de cambios de monto en la base (qué nota, antes y después, cuándo, sin persona) o basta con el log técnico?]`
