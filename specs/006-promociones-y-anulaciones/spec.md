# 006 - Promociones y anulaciones coherentes

## Qué y por qué

Al anular una nota o una nota de crédito que lleva promociones, el stock de los productos de la promoción no regresa y el sistema no avisa. Además, editar una promoción cambia lo que "contiene" para siempre: al anular o devolver una nota vieja se usan los productos que la promoción tiene hoy y no los que tenía cuando se vendió. Por último, una nota de crédito anulada sigue contando como "ya devuelto" y bloquea nuevas devoluciones y la anulación de la nota.

## Requisitos

- **RF-1** CUANDO se anula una nota de entrega o una nota de crédito con promociones, EL SISTEMA devolverá (o sacará) el stock de **todos** los productos que componían cada promoción al venderla, con las cantidades de entonces.
- **RF-2** SI no se puede revertir el stock de alguna línea (producto inexistente, datos incompletos), ENTONCES EL SISTEMA cancelará toda la operación con un mensaje que diga qué línea falló y no dejará anulaciones a medias.
- **RF-3** CUANDO se emite una nota con promociones, EL SISTEMA guardará en la nota la composición de cada promoción (producto y cantidad por unidad) tal como estaba.
- **RF-4** EL SISTEMA usará esa composición guardada al anular, devolver, regalar o reimprimir, sin importar cambios posteriores a la promoción.
- **RF-5** CUANDO una nota de crédito está anulada, EL SISTEMA no la contará como cantidad ya devuelta.
- **RF-6** CUANDO todas las notas de crédito de una nota están anuladas, EL SISTEMA permitirá anular la nota de entrega.
- **RF-7** EL SISTEMA mantendrá la regla de que una nota con notas de crédito vigentes no se puede anular.
- **RF-8** CUANDO se edita o elimina una promoción, EL SISTEMA no cambiará notas ni notas de crédito ya emitidas.
- **RF-9** EL SISTEMA dejará en el kardex un movimiento por cada producto afectado en cada anulación, con el documento y el estado correctos.

## Cómo sé que está listo

- Vender 2 unidades de una promoción de 3 productos, anular la nota: los 3 productos recuperan su cantidad y el kardex muestra 3 entradas.
- Cambiar la promoción y anular una nota vieja: el stock regresa según la composición original.
- Anular una nota de crédito y crear otra devolución de las mismas cantidades: se permite.
- Una nota cuyas notas de crédito están todas anuladas se puede anular.

## Fuera de alcance

- Cambiar precios o reglas de armado de promociones.
- Notas que hoy no tienen composición guardada (las ya emitidas): ver decisiones.

## Decisiones tomadas

- Hoy no hay notas con promociones en testing; el comportamiento de notas viejas sin composición guardada se decide en el plan.
