# Plan 004 - La nota emitida queda congelada

Cómo cumplir [spec.md](spec.md). Todavía no se ha tocado código.

## Qué hay hoy (verificado)

- Ya se congelan `credit_days`, `due_date`, `conditions_text`, `discount_conditions_text` y los textos de pago en `delivery_note`.
- [AccountsReceivableService.cs `get_printable_note_async`](../../src/NinOS.Infrastructure/Services/Implementations/AccountsReceivableService.cs#L891) reconstruye el documento leyendo **datos vivos**: nombre del producto, datos del cliente, vendedora, tipo de nota.
- [NotePdfGenerator.cs:234](../../src/NinOS.UI/Common/NotePdfGenerator.cs#L234) imprime `creation_date` tal cual llega. La vista previa le pasa la fecha en UTC; la reimpresión la convierte a hora de Venezuela. Por eso a partir de las 8 pm salen días distintos.
- [DeliveryNotesViewModel.cs:2074-2196](../../src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs#L2074): el PDF se arma con la vista previa **antes** de guardar; si el correlativo cambia al guardar, el PDF queda con el número viejo.
- El PDF y el Excel de la nota **no** usan estado, abonado ni saldo (verificado), así que congelar el documento tal cual no los afecta.
- Las notas de crédito se imprimen con el mismo mecanismo ([CreditNoteService.cs:1272](../../src/NinOS.Infrastructure/Services/Implementations/CreditNoteService.cs#L1272)).

## Enfoque

1. **Foto completa del documento.** Al guardar, se guarda en la nota una columna `print_snapshot` (jsonb) con el `note_print_dto` completo tal como se emitió, con fechas ya en hora de Venezuela, más `print_snapshot_version`. Reimprimir = leer esa foto. Se queda en la nota y no depende de productos, clientes ni vendedoras. Las columnas ya congeladas se mantienen.
2. **Notas de crédito igual**, con su propia foto.
3. **Notas viejas sin foto** usan la ruta actual (datos vivos) y dejan una línea en el log (RF-9). No se rellenan solas.
4. **Orden correcto al guardar:** guardar → obtener el documento **desde lo guardado** → generar PDF/Excel/impresión con ese documento. Se acabó armar el PDF con la vista previa.
5. **Correlativo definitivo dentro de la transacción.** El servicio calcula el número con un bloqueo de PostgreSQL (`pg_advisory_xact_lock`) para que dos personas no tomen el mismo. Si el número difiere del que se veía en pantalla, el mensaje final lo dice.
6. **Fechas:** una sola regla: todo se muestra con `AppTimeZone.to_local`. Pantallas con un convertidor XAML, exportes con la conversión explícita.
7. **Carta:** ya lo cumplen todos los PDF (verificado en el código); solo se revisa.

## Opciones descartadas

- **Más columnas sueltas** (nombre del cliente, RIF, dirección, nombre del producto...): se olvida alguna y cada cambio del PDF exige otra migración. La foto cubre todo.
- **Guardar el PDF final como archivo en la base:** pesa mucho y no sirve para el Excel.
- **Congelar solo en la reimpresión desde CxC:** no resuelve el PDF original ni el Excel.
- **Rellenar las notas viejas con datos de hoy:** inventa un pasado que no existió.

## Riesgo

Si luego cambias el diseño del DTO, las fotos viejas deben seguir leyéndose: por eso `print_snapshot_version` y deserialización tolerante a campos nuevos.

## Pruebas (a mano)

Emitir una nota; cambiar nombre de producto y dirección del cliente; editar monto en CxC; reimprimir y comparar con el PDF original. Crear una nota pasadas las 8 pm hora de Venezuela y mirar la fecha en vista previa, PDF, Excel, lista e historial del cliente. Guardar desde dos copias con el mismo número.

## Rama

`feature/nota-congelada` saliendo de `TESTING`. Las migraciones se generan con `NINOS_DB_CONNECTION` apuntando a testing y me preguntas antes de aplicarlas.
