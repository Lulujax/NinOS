# 005 - Pagos y comisiones correctos con varias personas

## Qué y por qué

Si dos personas registran pagos de la misma nota a la vez, la nota puede quedar "Pendiente" con saldo cero y sin comisión, o aceptar más dinero del permitido. La base tampoco impide dos comisiones para una misma nota. Además, cuando una nota de crédito deja saldada una nota, no se crea la comisión de la vendedora. El reporte de Pagos puede incluir cobros de Pro Venta, el recibo de comisión mezcla pagos por número de referencia, y los abonos de relaciones Pro Venta no tienen tope.

## Requisitos

- **RF-1** CUANDO se registra o edita un pago, EL SISTEMA bloqueará la nota para que otra persona no pueda registrar otro pago de esa nota hasta terminar, y calculará saldo y estado con todos los pagos reales.
- **RF-2** EL SISTEMA garantizará a nivel de base de datos que una nota tenga como máximo una comisión.
- **RF-3** CUANDO una nota de crédito (o la anulación de una) deja la nota saldada por pagos reales y la vendedora tiene comisión, EL SISTEMA creará la comisión sobre el monto ajustado igual que cuando se salda por un pago.
- **RF-4** CUANDO una nota de crédito o su anulación cambia el monto de una nota con comisión no pagada, EL SISTEMA recalculará esa comisión; si ya fue pagada, no la tocará.
- **RF-5** EL SISTEMA no incluirá en el reporte de Pagos ni en sus PDF y Excel los abonos de relaciones Pro Venta (pagos sin nota).
- **RF-6** CUANDO se genera el recibo de una comisión, EL SISTEMA incluirá solo los pagos de comisión de esa vendedora y ese lote de pago, no todos los que compartan la misma referencia.
- **RF-7** SI al pagar comisiones la referencia ya fue usada por otra vendedora, ENTONCES EL SISTEMA avisará y pedirá confirmar.
- **RF-8** CUANDO se registra un abono a una relación Pro Venta, EL SISTEMA no permitirá superar el saldo de la relación más la misma tolerancia que usan las notas generales.
- **RF-9** SI un abono supera el tope, ENTONCES EL SISTEMA lo rechazará con un mensaje que indique saldo y máximo permitido.
- **RF-10** EL SISTEMA no agregará ninguna función para eliminar pagos.
- **RF-11** CUANDO se edita un pago, EL SISTEMA no modificará pagos de comisiones ya pagadas ni comisiones ya liquidadas.
- **RF-12** SI se intenta editar un pago de una nota cuya comisión ya fue pagada y el cambio dejaría la nota sin saldar, ENTONCES EL SISTEMA bloqueará la edición con un mensaje que explique el motivo.

## Cómo sé que está listo

- Dos PCs pagan a la vez el saldo de una nota: queda "Pagada", con una sola comisión.
- Nota de 100 con pago de 80 y devolución de 20: queda "Pagada" y aparece la comisión de 10% sobre 80.
- El reporte de Pagos con todo seleccionado no trae los 22 abonos de relaciones (≈ $4.234,69 en testing).
- Dos vendedoras con la misma referencia: el recibo de cada una solo muestra lo suyo.
- Un abono de relación mayor al tope es rechazado.
- La consulta de comisiones duplicadas sigue devolviendo vacío.

## Fuera de alcance

- Cambiar el porcentaje o las reglas de a quién se le paga comisión.
- Rediseñar los recibos.

## Decisiones tomadas

- El tope de abono de relaciones reutiliza la tolerancia actual de notas generales (3,00 USD).
- Cuando una nota de crédito salda la nota, la comisión se calcula sobre el monto ajustado (después de la devolución). Confirmado por Luis.

- SI se edita un pago de una nota cuya comisión ya fue pagada y el cambio la dejaría sin saldar, se **bloquea** con un mensaje (confirmado por Luis; ver RF-12).
