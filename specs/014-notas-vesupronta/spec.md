# 014 - Notas de entrega Vesupronta: tres hojas y tabla de liquidación

## Qué y por qué

Las notas de entrega de Vesupronta (General, Volumen y Promoción) se imprimen igual que las de General, pero Vesupronta necesita un formato propio:

- Se imprimen **tres hojas**: ORIGINAL y COPIA del pedido, que van al cliente, y una hoja de ADMINISTRACIÓN, que se queda en la oficina.
- Las hojas del cliente **no llevan los datos de pago**.
- La hoja de ADMINISTRACIÓN lleva al final una **tabla de liquidación** que calcula cuánto se paga a Pronta y cuál es el diferencial. Esa tabla no sale en la copia del cliente.

Esta spec solo cubre la nota impresa. La relación por cobrar de Vesupronta se analiza aparte y no está en esta spec.

## Requisitos

### Hojas de la nota

- **RF-1** CUANDO se imprime o exporta a PDF una nota de Vesupronta, EL SISTEMA generará tres hojas tamaño carta: ORIGINAL, COPIA y ADMINISTRACIÓN, en ese orden.
- **RF-2** EL SISTEMA aplicará el RF-1 a los tres tipos de nota de Vesupronta: General, Volumen y Promoción. Los tres funcionan igual que sus equivalentes de General, salvo por lo que dice esta spec.
- **RF-3** Las hojas ORIGINAL y COPIA de Vesupronta NO llevan el bloque de datos de pago (transferencia, cuenta y pago móvil).
- **RF-4** La hoja ADMINISTRACIÓN lleva el mismo contenido que la hoja ORIGINAL, más la tabla de liquidación del RF-5 al RF-9 al final de la hoja.
- **RF-5** La tabla de liquidación tiene estas filas, en este orden:
  1. TOTAL GENERAL: el total de la nota **sin descuento** (suma de renglones a precio de lista).
  2. 25% DE LA LISTA DE PRECIOS VENTA: 25% del TOTAL GENERAL.
  3. TOTAL GENERAL MENOS 25%: TOTAL GENERAL menos la fila 2.
  4. 15% DE LA LISTA DE VESUVIANA: 15% de la fila 3.
  5. TOTAL A PAGAR A PRONTA: fila 3 menos fila 4.
  6. DIFERENCIAL: TOTAL GENERAL menos TOTAL A PAGAR A PRONTA.
- **RF-6** EL SISTEMA redondeará cada cifra de la tabla a 2 decimales (mitad hacia arriba) y hará cada resta sobre la cifra ya redondeada que se muestra.
- **RF-7** La tabla usa el TOTAL GENERAL sin descuento aunque la nota tenga descuento o promoción. El descuento no cambia la tabla.
- **RF-8** SI la nota no tiene renglones, ENTONCES EL SISTEMA no imprimirá la hoja ADMINISTRACIÓN y avisará que la nota está vacía.
- **RF-9** La tabla es solo informativa: no cambia saldos, comisiones ni relaciones de cobro.
- **RF-10** EL SISTEMA no cambia el número de la nota ni la forma de guardarla. Una nota de Vesupronta sigue siendo una sola nota con su correlativo global.

### Título de la nota

- **RF-11** EL SISTEMA pondrá el título de cada nota de Vesupronta como «V.P <título de la empresa> V.P». El título de la empresa es el que ya usa la app (BrandHeader.Title, hoy DEFILE_REMBRANT_OLEOS_TRICOMPLEX). Así queda «V.P DEFILE_REMBRANT_OLEOS_TRICOMPLEX V.P», como en el ejemplo de Luis.

### Congelado de notas emitidas

- **RF-12** En el sistema no hay notas de Vesupronta emitidas. Por eso no hay reimpresión de notas viejas: toda nota de Vesupronta sale con el formato de esta spec.
- **RF-13** Una nota impresa sale igual cada vez que se reimprime (spec 004). La tabla se calcula con los datos guardados de la nota, no con datos de hoy.

### Excel

- **RF-14** La exportación a Excel de una nota de Vesupronta tiene tres hojas: ORIGINAL, COPIA y ADMINISTRACIÓN. La hoja ADMINISTRACIÓN lleva la misma tabla de liquidación que el PDF.

### Límite de productos

- **RF-15** SI una nota de Vesupronta tiene más de 20 productos, ENTONCES EL SISTEMA no la guardará y dirá que el máximo es 20 productos.
- **RF-16** CUANDO una nota de Vesupronta tiene hasta 20 productos, EL SISTEMA imprimirá cada una de sus tres hojas en una sola página tamaño carta: no se corre a una segunda página y la tabla de liquidación no se corta.

### Notas de prueba

- **RF-17** EL SISTEMA tendrá tres notas de prueba de Vesupronta con fecha de octubre de 2026, en la base de testing, para revisar el formato. Una de ellas tendrá 20 productos, para comprobar RF-16. Estas notas se crean solo en testing, nunca en producción.

## Cómo sé que está listo

- Nota con TOTAL GENERAL 344,48 y descuento de 10%: la tabla muestra 344,48; 86,12; 258,36; 38,75; 219,61; 124,87. El descuento no cambia la tabla.
- La tabla de la nota de 344,48 cuadra exactamente con la hoja de cálculo que Luis usa hoy.
- Una nota de Vesupronta imprime tres hojas en el orden ORIGINAL, COPIA y ADMINISTRACIÓN.
- Las hojas ORIGINAL y COPIA no muestran ningún dato de pago. La hoja ADMINISTRACIÓN tampoco los muestra, pero sí tiene la tabla.
- Una nota de General sigue saliendo con dos hojas y datos de pago, sin cambios.
- Los tres tipos de nota de Vesupronta (General, Volumen y Promoción) salen con las tres hojas.
- Una nota de Vesupronta con 21 productos no se guarda y muestra el aviso del RF-15.
- Una nota de Vesupronta con 20 productos imprime cada hoja en una sola página, sin cortar la tabla (RF-16).
- El Excel de una nota de Vesupronta tiene las hojas ORIGINAL, COPIA y ADMINISTRACIÓN (RF-14).
- Las tres notas de prueba de octubre existen en la base de testing y salen con el formato nuevo (RF-17).
- Una nota sin renglones no genera la hoja ADMINISTRACIÓN y muestra el aviso del RF-8.
- `dotnet build NinOS.slnx` queda en 0 errores y 0 advertencias y `dotnet test NinOS.slnx` pasa.

## Fuera de alcance

- La relación por cobrar de Vesupronta (columnas, periodos, comisiones y fechas de pago). Se analiza en otra spec.
- Cambiar la nota de General o de Pro Venta.
- Cambiar las fórmulas de comisión de Pro Venta (spec 008).
- Guardar la tabla de liquidación en la base de datos. Se calcula al imprimir.
- Cambiar el correlativo o el formato del número de nota.

## Decisiones tomadas

- Luis, 2026-11-17: se imprimen tres hojas para Vesupronta. ORIGINAL y COPIA van al cliente sin datos de pago. ADMINISTRACIÓN se queda en la oficina y lleva la tabla.
- Luis, 2026-11-17: la base de la tabla es el total general de la nota sin descuento. El 25% se llama «de la lista de precios venta» y el 15% «de la lista de Vesuviana», según el título que Luis escribió en la tabla.
