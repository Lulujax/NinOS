# Tareas 005 - Pagos y comisiones correctos con varias personas

## Tarea 1 (manual, solo lectura) - Buscar comisiones duplicadas en producción
Cubre: RF-2 (preparación)
- Ejecutar en producción: `select id_delivery_note, count(*) from commission group by 1 having count(*) > 1;`
- Hecho cuando: devuelve 0 filas (en testing ya devuelve 0). Si devuelve filas, se resuelven con una spec aparte antes de seguir.
- Cómo probarla: es la propia consulta.
- [x] Hecha y probada — 2026-11-15, corrida por Claude con la conexión en modo solo lectura: `ninos_db` devuelve 0 filas (117 notas, 61 pagos); `ninos_db_testing` también 0.

## Tarea 2 - Migración: índice único de comisión por nota y columna de lote
Cubre: RF-2, RF-6
- Índice único en `commission(id_delivery_note)` y columna `id_batch` (uuid, nula) en `commission_payment`. **Antes:** `NINOS_DB_CONNECTION` a testing y preguntarte.
- Hecho cuando: la migración se aplica en testing sin tocar datos.
- Cómo probarla: abrir la app; intentar insertar una segunda comisión para una nota con SQL de prueba → la base lo rechaza.
- [x] Hecha y probada

## Tarea 3 - Regla única de saldo, estado y comisión (`NoteSettlement`)
Cubre: RF-1, RF-3, RF-4
- Crear la clase y usarla desde `PaymentService.register_payment_async` y `update_payment_async`, sin cambiar el comportamiento visible.
- Hecho cuando: pagar una nota completa deja "Pagada" con una comisión de 10%; editar un pago para dejarla incompleta la devuelve a "Pendiente" y quita la comisión no pagada.
- Cómo probarla: nota de 100: pagar 100 → Pagada + comisión 10,00; editar el pago a 60 → Pendiente, sin comisión.
- [x] Hecha y probada

## Tarea 4 - Bloquear la nota al registrar o editar pagos
Cubre: RF-1
- `SELECT ... FOR UPDATE` de la nota como primer paso dentro de la transacción de pago.
- Hecho cuando: dos pagos simultáneos a la misma nota dan un resultado correcto.
- Cómo probarla: con dos copias, nota de 100: pagar 60 en una y 40 en otra a la vez, 5 veces con notas distintas → todas "Pagada", una comisión cada una.
- [x] Hecha y probada

## Tarea 5 - Notas de crédito y edición de monto usan la misma regla
Cubre: RF-3, RF-4
- `CreditNoteService` (crear y anular) y `update_note_total_async` llaman a `NoteSettlement`.
- Hecho cuando: una devolución que salda la nota crea la comisión sobre el monto ajustado; anular esa nota de crédito la recalcula.
- Cómo probarla: nota de 100 con pago de 80; devolver 20 → Pagada, comisión 8,00; anular la nota de crédito → Pendiente, sin comisión. Comisión ya pagada no se toca.
- [x] Hecha y probada

## Tarea 6 - Reporte de Pagos sin abonos de relaciones
Cubre: RF-5
- Filtrar solo pagos con nota en `get_payments_by_period_async`; revisar que PDF y Excel no dependan de las filas de anulación de Pro Venta.
- Hecho cuando: el reporte del mes con todo seleccionado no incluye los abonos de relaciones.
- Cómo probarla: generar el reporte con todos los vendedores y zonas; confirmar que no aparece "SIN VENDEDOR" ni los $4.234,69 de relaciones.
- [x] Hecha y probada

## Tarea 7 - Saldo de relación compartido y tope en abonos
Cubre: RF-8, RF-9
- Extraer el cálculo de saldo de relación a un método común (también lo usa Pro Venta); validar el tope al registrar y editar abonos, con la fila de la relación bloqueada.
- Hecho cuando: un abono mayor al saldo + 3,00 es rechazado con saldo y máximo en el mensaje.
- Cómo probarla: relación con saldo 100: abonar 50 (ok), abonar 60 (rechazado), abonar 53 (ok, tolerancia); la pantalla Pro Venta muestra el mismo saldo.
- [x] Hecha y probada

## Tarea 8 - Recibo de comisión por lote
Cubre: RF-6
- Asignar `id_batch` al pagar comisiones; el recibo busca por lote y, sin lote, por referencia + vendedora.
- Hecho cuando: dos vendedoras con la misma referencia reciben cada una su propio recibo.
- Cómo probarla: pagar comisiones de dos vendedoras con referencia "TEST1"; abrir el recibo de cada una → solo sus notas. Abrir el recibo de un pago viejo → sigue funcionando.
- [x] Hecha y probada

## Tarea 9 - Aviso de referencia repetida
Cubre: RF-7
- Método del servicio y confirmación en la ventana de pago de comisiones.
- Hecho cuando: usar una referencia de otra vendedora pide confirmación.
- Cómo probarla: pagar comisión con referencia "TEST2" a Ana; intentar la misma con Sandra → aparece el aviso.
- [x] Hecha y probada

## Tarea 10 - Prueba final de concurrencia y consultas de control
Cubre: RF-1, RF-2, RF-3
- Sin código: repetir las pruebas de concurrencia con dos copias y correr las consultas de control de la auditoría (pendientes con saldo cero, pagadas con saldo, sobrepagos, comisiones duplicadas).
- Hecho cuando: todas las consultas devuelven 0 filas.
- Cómo probarla: lo indicado; guardar el resultado en este archivo.
- [x] Hecha y probada — 2026-11-15, corrida por Claude. Concurrencia: dos abonos simultáneos (60 y 40) sobre una nota de 100 en testing dejaron la nota Pagada, 2 pagos que suman 100 y una sola comisión de 10,00. Consultas de control en solo lectura, con saldo = `adjusted_total_usd` menos la suma de pagos de la nota: pendientes con saldo cero, pagadas con saldo, sobrepagos y comisiones duplicadas devuelven 0 filas tanto en `ninos_db` como en `ninos_db_testing`. Nota: la fórmula del saldo de esas consultas es una aproximación mía a la que usa la app; si quieres una auditoría exacta, se compara contra la pantalla de Cuentas por cobrar.

## Tarea 11 - Bloquear la edición de un pago con comisión ya pagada
Cubre: RF-11, RF-12
- `update_payment_async` rechaza el cambio si dejaría sin saldar una nota cuya comisión ya está pagada, con un mensaje claro; no toca la comisión.
- Hecho cuando: la edición se bloquea y el pago, la nota y la comisión quedan como estaban.
- Cómo probarla: nota de 100 pagada con comisión pagada; intentar bajar el pago a 60 → mensaje de bloqueo; subirlo a 100 (sin cambio) o editar solo la referencia → permitido.
- [x] Hecha y probada
