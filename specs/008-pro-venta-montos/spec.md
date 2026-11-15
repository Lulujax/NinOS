# 008 - Montos de Pro Venta coherentes

## Qué y por qué

Pro Venta calcula distinto según la pantalla: el reporte semanal toma el total con el descuento de emisión ya aplicado (ej.: 292,90) y no el monto sin descuento (325,44); los gastos administrativos usan una fórmula equivocada (15% del total en vez de 15% de lo que queda tras los operativos); y la ventana "Editar montos" muestra una "Base" que ya trae el descuento, de modo que el descuento nuevo se aplica encima de otro. Esta spec fija una sola fórmula para todo. Pro Venta aún no está terminado, así que esta spec solo cubre los montos.

## Requisitos

- **RF-1** EL SISTEMA calculará, **por cada nota** Pro Venta vigente, todos los porcentajes sobre su **monto original**, que es el monto **sin descuento aplicado** (suma de los renglones a precio de lista).
- **RF-2** EL SISTEMA calculará la comisión como el 10% del monto original.
- **RF-3** EL SISTEMA calculará los gastos operativos como el 25% del monto original.
- **RF-4** EL SISTEMA calculará los gastos administrativos como el 15% de (monto original − gastos operativos).
- **RF-5** EL SISTEMA mostrará en las relaciones el monto original de cada nota (sin descuento aplicado) y sus porcentajes calculados sobre él.
- **RF-6** EL SISTEMA usará estas mismas fórmulas en pantalla, reporte semanal, detalle de relación, PDF y Excel, con una sola función de cálculo.
- **RF-7** EL SISTEMA calculará el saldo por cobrar de una relación con el monto **ajustado** (el que se cobra) de las notas vigentes menos los abonos positivos.
- **RF-8** CUANDO se edita el monto o descuento de una nota Pro Venta, EL SISTEMA tomará como base el monto original (sin descuento) y aplicará el descuento nuevo sobre esa base, sin acumular descuentos.
- **RF-9** CUANDO se edita una nota Pro Venta, EL SISTEMA no cambiará la nota impresa (spec 004), ni la comisión ni los gastos (siguen sobre el monto original); solo cambiará el saldo por cobrar.
- **RF-10** EL SISTEMA excluirá las notas anuladas de todos los totales y las mostrará aparte.
- **RF-11** EL SISTEMA usará la semana de Venezuela (UTC−4, lunes a domingo) para ubicar cada nota en su semana y su relación.
- **RF-12** EL SISTEMA redondeará cada monto a 2 decimales con una sola regla (`Money.round`).
- **RF-13** CUANDO una nota Pro Venta tiene notas de crédito vigentes, EL SISTEMA descontará del monto original las unidades devueltas a precio de lista, y calculará comisión y gastos sobre el monto original resultante.

## Cómo sé que está listo

- Nota con monto original 100: comisión 10,00; gastos operativos 25,00; gastos administrativos 11,25. Igual en pantalla, PDF y Excel.
- Una nota hoy guardada con total 292,90 y renglones por 325,44 muestra 325,44 como monto original y calcula sobre él.
- Editar esa nota a 80 no cambia comisión ni gastos, pero sí el saldo por cobrar.
- Editar dos veces el descuento no acumula descuento sobre descuento.
- Una nota hecha el domingo a las 9 pm cae en la semana de ese domingo.

## Fuera de alcance

- **Pendiente para después (no hacer ahora):** poder editar monto y descuento de la relación como en Cuentas por Cobrar general, por si el cliente pagó con o sin descuento. Esta spec solo deja la base correcta para eso.
- Terminar el resto de cambios de Pro Venta que Luis tiene en curso.
- Cambiar quién puede emitir notas Pro Venta.

## Decisiones tomadas

- Fórmulas y base confirmadas por Luis: cada % va por nota sobre el monto original sin descuento.
- Hay que comprobar con 2 o 3 notas reales que "monto original sin descuento" es la suma de renglones a precio de lista (en promociones, cantidad × precio unitario).

- Si hay una nota de crédito vigente sobre una nota Pro Venta, el monto original **baja** (y con él comisión y gastos). Confirmado por Luis. Baja por las unidades devueltas a precio de lista, no por el total de la nota de crédito (que ya trae descuento). Las notas de crédito anuladas no cuentan.
