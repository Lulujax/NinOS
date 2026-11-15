# Plan 005 - Pagos y comisiones correctos con varias personas

Cómo cumplir [spec.md](spec.md). Todavía no se ha tocado código.

## Qué hay hoy (verificado)

- La lógica "¿quedó saldada? → estado → comisión" está **copiada cuatro veces**: [PaymentService.cs:123](../../src/NinOS.Infrastructure/Services/Implementations/PaymentService.cs#L123) (registrar), [:270](../../src/NinOS.Infrastructure/Services/Implementations/PaymentService.cs#L270) (editar), `AccountsReceivableService.update_note_total_async` y `CreditNoteService` ([:1006](../../src/NinOS.Infrastructure/Services/Implementations/CreditNoteService.cs#L1006)). Solo las tres primeras crean comisión; la de notas de crédito no.
- Ninguna toma bloqueo sobre la nota; dos transacciones simultáneas leen el mismo saldo.
- `commission` no tiene índice único por nota.
- [PaymentService.cs:452](../../src/NinOS.Infrastructure/Services/Implementations/PaymentService.cs#L452) deja pasar los pagos sin nota (abonos de relación).
- [CommissionService.cs:1508](../../src/NinOS.Infrastructure/Services/Implementations/CommissionService.cs#L1508) arma el recibo por `reference_number` solamente.
- [PaymentService.cs:175](../../src/NinOS.Infrastructure/Services/Implementations/PaymentService.cs#L175) `register_relation_payment_async` no valida tope.

## Enfoque

1. **Una sola regla, un solo lugar.** Clase interna `NoteSettlement` con `recalcular_async(db, note)`: suma pagos reales, decide Pagada/Pendiente (tolerancia 0,01), crea la comisión si corresponde, la borra si estaba sin pagar y la nota dejó de estar saldada, o actualiza su monto. La comisión se calcula sobre `min(cobrado, monto ajustado)`. Los cuatro lugares la llaman.
2. **Bloqueo de la nota.** Al registrar o editar un pago, la primera consulta es `SELECT ... FOR UPDATE` sobre la fila de la nota, dentro de la transacción. La segunda persona espera unos milisegundos y ve el saldo ya actualizado.
3. **Índice único** `commission(id_delivery_note)` por migración. Antes de aplicarla en producción se corre una consulta de solo lectura buscando duplicados.
4. **Reporte de Pagos** solo con pagos que tienen nota (`id_delivery_note IS NOT NULL`).
5. **Abonos de relación Pro Venta:** se extrae el cálculo de saldo de relación (hoy privado en `ProVentaService`) a un método compartido y se valida el tope en registrar **y** editar, bloqueando la fila de la relación.
6. **Recibo por lote.** Se agrega `id_batch` (uuid) a `commission_payment`, asignado al registrar un pago de varias comisiones. El recibo busca por lote; los pagos viejos (sin lote) usan "referencia + misma vendedora".
7. **Aviso de referencia repetida** antes de registrar: el servicio expone `referencia_usada_por_otra_vendedora_async`; la ventana pide confirmación.

## Opciones descartadas

- **Solo `SERIALIZABLE`:** exige reintentos en la pantalla; el `FOR UPDATE` es más simple y suficiente.
- **Bloqueo de aplicación en la PC (semáforo):** no sirve entre PCs distintas.
- **Corregir cada copia por separado:** es lo que causó el error (una copia quedó sin comisión).
- **Eliminar pagos para "arreglar" errores:** prohibido por decisión del cliente.

## Riesgo

La migración del índice único falla si en producción ya hay comisiones duplicadas. Por eso la tarea 1 es la consulta previa. Bloquear filas puede causar esperas largas si una pantalla deja una transacción abierta; las transacciones del sistema son cortas.

## Pruebas (a mano)

Dos copias registrando pagos a la misma nota; devolver parte de una nota casi pagada; reporte de Pagos con todo seleccionado; dos vendedoras con la misma referencia; abonos de relación mayores al saldo.

## Rama

`feature/pagos-comisiones-concurrentes` saliendo de `TESTING`. Migraciones solo contra testing y preguntándote antes.
