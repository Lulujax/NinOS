# 015 - Relación por cobrar/ventas de Vesupronta

## Qué y por qué

La clienta necesita, dentro de Vesupronta, una relación parecida a la del reporte semanal de Pro Venta, pero **por periodo**: mensual, anual o de todas las notas. Sirve para ver qué se vendió, cuánto se ha cobrado, cuánto falta por cobrar y la comisión del 10% sobre lo cobrado.

## Requisitos

- **RF-1** EL SISTEMA mostrará en la pestaña Vesupronta una sub-pestaña llamada **RELACION** con la relación de las notas de Vesupronta.
- **RF-2** EL SISTEMA permitirá elegir el periodo: **Mes** (con mes y año), **Año** o **Todos**.
- **RF-3** EL SISTEMA mostrará el título `RELACION POR COBRAR/VENTAS {PERIODO} VESU PRONTA`, donde {PERIODO} es el mes y año (ej. AGOSTO 2025), el año, o TODOS LOS PERIODOS.
- **RF-4** EL SISTEMA mostrará una fila por nota con las columnas: fecha de despacho, nro (solo el número de la nota), cliente (en varias líneas si el nombre es largo), monto facturado, descuento, fecha de pago, monto pagado, 10% comisión, saldo actualizado y fecha de comisión cancelada.
- **RF-5** CUANDO se calcule el monto facturado, EL SISTEMA usará el total congelado en la foto de impresión de la nota (el total con descuento que salió impreso). SI la nota no tiene foto, ENTONCES EL SISTEMA usará los datos vivos y lo dejará en el log.
- **RF-6** EL SISTEMA mostrará en la columna descuento **SI** cuando la nota tenga descuento de condición o de volumen mayor que cero, y **NO** en cualquier otro caso.
- **RF-7** EL SISTEMA usará como monto pagado el monto registrado en Cuentas por Cobrar (el que sale después de las ediciones de monto). Ejemplo: una nota de 100 editada a 80 muestra 80.
- **RF-8** EL SISTEMA calculará el saldo actualizado como monto pagado menos los abonos registrados, sin bajar de cero. Ejemplo: 100 con abono de 30 muestra 70.
- **RF-9** CUANDO el saldo de una nota llegue a cero, EL SISTEMA mostrará en fecha de pago la fecha del último abono. Mientras tenga saldo, la fecha de pago sale vacía.
- **RF-10** EL SISTEMA calculará el 10% comisión sobre el monto pagado, con dos decimales.
- **RF-11** EL SISTEMA dejará vacía la fecha de comisión cancelada mientras no exista un pago de comisión de la nota.
- **RF-12** EL SISTEMA no incluirá las notas anuladas en la relación.
- **RF-13** EL SISTEMA asignará cada nota a un mes o año según su fecha de emisión, en hora de Venezuela (igual que Cuentas por Cobrar).
- **RF-14** EL SISTEMA mostrará al pie dos totales uno al lado del otro: TOTAL VENTAS con el periodo (suma del monto pagado) y TOTAL POR COBRAR (suma del saldo actualizado).
- **RF-15** EL SISTEMA ofrecerá en la sub-pestaña los botones Vista Previa, Imprimir, PDF y Excel, con el mismo comportamiento que los reportes existentes. El PDF e impresión salen en tamaño carta.
- **RF-16** CUANDO no haya notas en el periodo, EL SISTEMA mostrará el aviso en la pantalla y en el documento, con totales en cero, sin errores.

## Cómo sé que está listo

- En testing, con una nota de 100 que tiene un abono de 30: monto pagado 100, saldo 70, comisión 10,00, fecha de pago vacía.
- Con una nota de 100 editada a 80 en Cuentas por Cobrar: la relación muestra 80 en monto pagado y 80 de saldo.
- Una nota pagada completa muestra fecha de pago y saldo 0.
- Una nota anulada no aparece.
- Los dos totales salen juntos al pie y cuadran con la suma de la tabla.
- La vista previa, la impresión, el PDF y el Excel muestran el mismo contenido y el título correcto.
- Pruebas unitarias del cálculo: caso feliz, límite y error (`dotnet test`).

## Fuera de alcance

- Pagar o registrar comisiones de Vesupronta (ver la aclaración abajo).
- Editar montos desde esta sub-pestaña (se edita en Cuentas por Cobrar).
- Cambiar la nota impresa de Vesupronta.

## Puntos abiertos

- [NECESITA ACLARACIÓN: La spec 013 (RF-7) dice que Vesupronta no tiene comisiones. Esta relación muestra un 10% calculado, pero la fecha de comisión cancelada sale vacía porque no hay pago de comisión. ¿Vesupronta debe pagar comisiones a las vendedoras? Si sí, hay que cambiar el RF-7 de la spec 013 antes de programar ese pago.]
- [NECESITA ACLARACIÓN: el periodo se mide por fecha de emisión (igual que Cuentas por Cobrar). ¿Debe medirse por fecha de despacho?]
