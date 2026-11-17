# 012 - Revisión de Pro Venta: editar montos como en CxC y cerrar huecos

## Qué y por qué

La spec 008 dejó una sola fórmula para la comisión y los gastos (por nota, sobre el monto original sin descuento). Quedó pendiente **editar el monto y el descuento de las notas Pro Venta como en Cuentas por Cobrar general**, por si el cliente pagó con o sin descuento. Hoy la ventana "Editar Notas" existe, pero toma como base el total que ya trae el descuento: "pagó sin descuento" da 292,90 y no 325,44. Además, una nota de 1.000 o más no se puede guardar.

La revisión ([analisis.md](analisis.md)) encontró otros huecos que tocan dinero:
- la misma relación muestra montos distintos en pantalla, PDF y Excel;
- una nota del domingo en la noche se cobra en la relación de la semana siguiente;
- consultar un reporte puede **renumerar** relaciones ya impresas;
- se pueden editar notas o hacer notas de crédito en relaciones ya pagadas, y el sobrepago queda oculto.

También falta lo que el cliente lleva a mano en su Excel: el saldo a una fecha de corte.

Esta spec no repite la 008: usa su fórmula y su "monto original" tal como están.

## Requisitos

### Editar monto y descuento (como en CxC general)

- **RF-1** CUANDO se edita el descuento de una nota Pro Venta, EL SISTEMA calculará el monto a cobrar como monto original × (1 − descuento), donde el monto original es el de la spec 008 (renglones a precio de lista menos devoluciones vigentes).
- **RF-2** CUANDO se escribe el monto a cobrar directamente, EL SISTEMA calculará y mostrará el descuento equivalente sobre el mismo monto original.
- **RF-3** EL SISTEMA ofrecerá dos accesos rápidos en la edición: «Pagó con descuento» (el descuento con el que se emitió la nota) y «Pagó sin descuento» (0%, el monto a cobrar es igual al monto original).
- **RF-4** SI el monto es negativo, es mayor que el monto original, o el descuento no está entre 0 y 100, ENTONCES EL SISTEMA rechazará el cambio con un mensaje que diga el valor permitido y no guardará nada.
- **RF-5** EL SISTEMA aceptará montos escritos con separador de miles en cualquiera de las dos formas (1.234,56 y 1,234.56) y montos de 1.000 o más.
- **RF-6** CUANDO se guarda la edición, EL SISTEMA cambiará solo el monto a cobrar de la nota y, con él, el monto por pagar y el saldo de su relación. No cambiará la comisión ni los gastos (siguen sobre el monto original, spec 008) ni la nota impresa (spec 004).
- **RF-7** EL SISTEMA mostrará en la edición, por nota: monto original, descuento de emisión, descuento actual, monto a cobrar, comisión y gastos, y el monto por pagar y el saldo de la relación antes y después del cambio.
- **RF-8** SI la relación de la nota ya está pagada, ENTONCES EL SISTEMA bloqueará la edición de sus notas y dirá que la relación ya está pagada. Que la relación esté impresa o liquidada no bloquea la edición.
- **RF-9** SI la nota cambió desde que se abrió la edición (otra PC la editó o le hicieron una nota de crédito), ENTONCES EL SISTEMA no guardará, avisará que la nota cambió y recargará los datos.
- **RF-10** CUANDO se guarda una edición, EL SISTEMA bloqueará también la relación mientras guarda, para que un abono hecho al mismo tiempo no se valide contra el saldo viejo.
- **RF-11** CUANDO se edita una nota Pro Venta, EL SISTEMA dejará en el log técnico el id de la nota, el id de la relación, el descuento de antes y el de después y el resultado (guardado o rechazado y por qué), sin datos del cliente.

### Una sola cifra por relación

- **RF-12** EL SISTEMA calculará el monto por pagar, lo abonado y el saldo de una relación con una sola función, y mostrará las mismas cifras en la lista de relaciones, el historial, la ventana de abono, la ventana de edición, el PDF (las dos páginas) y el Excel (las dos hojas).
- **RF-13** EL SISTEMA mostrará en el detalle de la relación (PDF página 2 y Excel hoja 2) dos columnas por nota, «monto original» y «monto a cobrar», y el total de cada columna coincidirá con la cifra del encabezado que lleva su nombre.
- **RF-14** EL SISTEMA calculará «total cobrado», «nota por pagar a Pro Venta» y «diferencial» sobre el monto original sin descuento, igual en pantalla, PDF y Excel.
- **RF-15** SI un abono supera lo que falta por pagar de la relación más el tope de exceso de la spec 005 (3,00), ENTONCES EL SISTEMA bloqueará el pago al registrarlo, con un mensaje que diga el máximo permitido, y no guardará nada. No existe saldo a favor.

### Relaciones, semanas y números

- **RF-16** CUANDO se guarda una nota Pro Venta, EL SISTEMA la asignará a la relación de su semana en hora de Venezuela (UTC−4, lunes a domingo), igual que el reporte semanal.
- **RF-17** EL SISTEMA no cambiará nunca el número de una relación ya creada, por ninguna vía.
- **RF-18** EL SISTEMA no creará ni modificará relaciones al consultar un reporte, una semana o una relación: solo al guardar una nota Pro Venta.
- **RF-19** EL SISTEMA no tomará una relación heredada (cartera cargada del Excel) como la relación de una semana.
- **RF-20** SI dos personas guardan al mismo tiempo la primera nota Pro Venta de una semana, ENTONCES EL SISTEMA creará una sola relación, guardará las dos notas en ella y ninguna de las dos personas verá un error.
- **RF-21** CUANDO una nota con fecha atrasada va a caer en una relación ya pagada, EL SISTEMA avisará antes de guardar que esa relación está pagada y pedirá confirmación, porque lo ideal es no agregar notas a relaciones pagadas.
- **RF-31** CUANDO se confirma y se guarda una nota en una relación ya pagada, EL SISTEMA reabrirá la relación como no pagada y su saldo incluirá la nota nueva.

### Notas de crédito y devoluciones

- **RF-22** SI se intenta hacer una nota de crédito sobre una nota Pro Venta cuya relación ya está pagada, ENTONCES EL SISTEMA lo impedirá con un mensaje que diga que la relación ya está pagada.
- **RF-23** EL SISTEMA descontará las devoluciones del monto original renglón por renglón, sin restar dos veces la misma devolución cuando un producto aparece en dos renglones de la nota.

### Abonos

- **RF-24** EL SISTEMA usará el mismo tope de exceso en la ventana de abono y al guardar el abono (3,00, el de la spec 005).

### El reporte que el cliente lleva a mano

- **RF-25** CUANDO se pide la «Relación semanal por cobrar» con una fecha de corte, EL SISTEMA mostrará por relación el número, el monto por pagar y el saldo a esa fecha (solo notas y abonos hasta la fecha de corte), y el total. Saldrá en pantalla, PDF tamaño carta y Excel.
- **RF-26** EL SISTEMA mantendrá las relaciones por semana para toda la empresa, como hoy. El reporte sale para toda la empresa, sin filtro por vendedor: Juan Navas es la única vendedora de la zona y la empresa confirmó que no planea sumar más.

### Comisión y gastos de la relación

- **RF-34** EL SISTEMA calculará la comisión de Luis y los gastos operativos siempre sobre el monto original de cada nota, y el gasto administrativo como 15% de (monto original − gastos operativos). Estos valores no cambian al editar el monto a cobrar ni el descuento. Como una relación pagada no admite notas de crédito (RF-22), tampoco cambian después de pagarse, así que no se guardan aparte ni hace falta migración.
- **RF-36** Las notas anuladas no suman en ningún total de la relación (monto por pagar, abonado, saldo, comisión ni gastos). Sí aparecen en la lista de la relación y en los reportes y descargas (PDF y Excel), marcadas como ANULADA y con valor cero, y los filtros de descarga las incluyen.
- **RF-35** CUANDO una relación tiene saldo 0, EL SISTEMA mostrará «0,00» en la columna de saldo, en pantalla, PDF y Excel. No usará guion.

### Pruebas y registro

- **RF-29** EL SISTEMA tendrá pruebas unitarias de: cálculo de la edición, semana de Venezuela, monto y saldo de la relación, saldo a una fecha de corte y devoluciones por renglón. Cada una con caso feliz, caso límite y caso de error.
- **RF-30** EL SISTEMA dejará logs del flujo Pro Venta (consulta de semana o relación, abono, edición, asignación de relación) con ids y estados, nunca con datos del cliente.

### Monto por pagar sin descuento

- **RF-32** EL SISTEMA tomará como monto por pagar de cada nota Pro Venta su monto original **sin descuento**. Quien usa el sistema lo ajusta con la edición (RF-1 a RF-3) cuando el cliente paga con descuento. Esto aplica a las notas nuevas. Las notas ya emitidas con descuento se corrigen en una spec aparte (ver Fuera de alcance), porque cambian datos de negocio.

## Cómo sé que está listo

- Nota con monto original 325,44 emitida al 10% (292,90). «Pagó sin descuento» la deja en 325,44 y el saldo de su relación sube 32,54. La comisión sigue en 32,54, los gastos operativos en 81,36 y los administrativos en 36,61. La nota reimpresa sale igual que la original.
- Con esa misma nota: 5% da 309,17; volver al 10% da 292,90. Editar dos veces no acumula descuento.
- Una nota de 1.250,00 se edita y se guarda sin reescribir el monto.
- Escribir 400 en una nota con monto original 325,44 da un mensaje y no guarda nada.
- En una relación pagada no se puede editar ninguna nota, y el mensaje dice que la relación ya está pagada.
- Una nota con fecha de una semana cuya relación está pagada pide confirmación; al guardarla, la relación vuelve a no pagada y su saldo sube por el monto de la nota.
- Una nota Pro Venta nueva entra a su relación con el monto original sin descuento.
- La misma relación muestra el mismo monto por pagar y el mismo saldo en la lista, el historial, el abono, el PDF y el Excel. En el detalle, el total de cada columna es igual a su encabezado.
- Una nota creada el domingo a las 9:00 pm queda en la relación de ese domingo y sale en esa misma semana del reporte.
- Abrir semanas viejas en el reporte no crea relaciones ni cambia ningún número. Con una consulta de solo lectura, antes y después, la tabla de relaciones queda igual.
- Dos PCs guardan a la vez la primera nota Pro Venta de una semana: queda una sola relación con las dos notas y ninguna PC ve error.
- El reporte con corte al 4 de octubre muestra los saldos a esa fecha, aunque después haya abonos.
- `dotnet build NinOS.slnx` queda en 0 errores y 0 advertencias y `dotnet test NinOS.slnx` pasa.

## Fuera de alcance

- Cambiar los porcentajes o la fórmula de comisión y gastos (spec 008).
- Login, usuarios o auditoría por persona (decisión del cliente).
- Eliminar pagos (no existe y no se agrega).
- Revisar o corregir notas que ya estén mal asignadas a una relación por el problema del domingo en la noche. Esta spec no las toca; Luis decide aparte.
- Editar el saldo de las relaciones heredadas (149-179).
- Cargar en el sistema el histórico del Excel del cliente.
- Corregir los montos de las notas Pro Venta ya emitidas con descuento (regla RF-32). Cambia datos de negocio: va en una spec aparte con script supervisado y respaldo, y esta spec no ejecuta ningún cambio sobre notas existentes.
- Registrar el método de pago, la referencia y el banco de los abonos de relación (hoy siempre "Efectivo").
- Historial de cambios de monto guardado en la base. Basta con el log técnico (RF-11).

## Decisiones tomadas

- La base de la edición es el monto original de la spec 008. No se inventa otra.
- Luis, 2026-11-16: el monto por pagar va sin descuento y quien usa el sistema lo modifica (RF-32); una relación pagada bloquea la edición (RF-8); no se deberían crear notas en relaciones pagadas, pero si pasa, la relación se reabre como no pagada (RF-21, RF-31); las relaciones son por semana para toda la empresa (RF-26); no se sabe por qué subieron la 146 y la 173, y como se trabaja en testing no bloquea nada.
- Las preguntas de [analisis.md](analisis.md) sección 5 se responden antes de empezar las tareas que dependen de ellas. Cada tarea dice si está bloqueada.
