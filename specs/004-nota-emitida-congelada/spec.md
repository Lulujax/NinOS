# 004 - La nota emitida queda congelada

## Qué y por qué

Una nota ya entregada al cliente debe salir siempre igual. Ya se corrigieron los días de crédito y las condiciones, pero siguen saliendo "vivos" otros datos: nombre del producto, datos del cliente, fecha (el PDF original usa UTC y a partir de las 8 pm sale con el día siguiente) y, al guardar con un correlativo que otra persona ya tomó, el PDF puede llevar un número distinto al de la nota guardada. El cliente debe recibir siempre la nota tal como se creó; las ediciones por sistema solo afectan saldos.

Esta spec es para **todas las notas** (generales y Pro Venta: son la misma tabla). Los **montos ya están bien resueltos**: cada nota guarda dos juegos de valores, los originales de emisión (para imprimir) y los de trabajo (los que se editan en Cuentas por Cobrar o Pro Venta). En testing las 91 notas tienen su valor original guardado y 4 ya fueron editadas en CxC sin cambiar lo impreso. Esta spec conserva esa regla y congela además lo que sigue "vivo": nombres, datos del cliente, fecha y número.

## Requisitos

- **RF-1** CUANDO se guarda una nota de entrega, EL SISTEMA congelará en ella: nombre y código de cada producto o promoción, nombre, RIF, teléfono, contacto y direcciones del cliente, nombre de la vendedora, tipo de nota y encabezado.
- **RF-2** CUANDO se reimprime o exporta una nota (PDF, Excel o impresión), EL SISTEMA usará los datos congelados y no los actuales de producto, cliente o vendedora.
- **RF-3** EL SISTEMA no alterará la nota impresa cuando se edite el monto o descuento en CxC o Pro Venta ni cuando se emita una nota de crédito: la impresión muestra los valores originales de emisión.
- **RF-4** EL SISTEMA generará el PDF/Excel/impresión **después** de guardar y usando el número definitivo de la nota guardada.
- **RF-5** EL SISTEMA mostrará el correlativo en pantalla, antes de guardar, solo como **previsualización**. No reservará números al abrir la nota, para que la serie no quede con huecos.
- **RF-5b** SI al guardar el correlativo previsualizado ya fue tomado por otra persona, ENTONCES EL SISTEMA asignará el siguiente libre, avisará al usuario del número definitivo y el documento saldrá con ese número.
- **RF-5c** CUANDO una nota ya está guardada, EL SISTEMA no cambiará nunca su correlativo, por ninguna vía: ni al arrancar, ni al editar, ni al migrar datos.
- **RF-6** EL SISTEMA mostrará todas las fechas de notas, notas de crédito, pagos y comprobantes en hora de Venezuela (UTC−4), en pantallas, PDF y Excel, incluida la vista previa original.
- **RF-7** EL SISTEMA aplicará el mismo criterio de congelado a las notas de crédito (cliente, producto, fecha) y a sus PDF.
- **RF-8** EL SISTEMA generará todo PDF en tamaño carta.
- **RF-9** SI una nota vieja no tiene datos congelados (emitida antes de este cambio), ENTONCES EL SISTEMA usará los datos actuales, avisará en pantalla que es una copia con datos actuales y lo dejará registrado en el log.
- **RF-10** EL SISTEMA no modificará las notas ya guardadas para "rellenar" los datos congelados: no se les inventa una foto con los valores de hoy.
- **RF-11** CUANDO se agregue más adelante un campo nuevo al diseño de la nota, EL SISTEMA leerá las fotos viejas sin fallar: el campo que no exista sale en blanco y la nota se imprime igual. La foto llevará un número de versión para permitirlo.

## Cómo sé que está listo

- Emitir nota, cambiar el nombre del producto y la dirección del cliente, reimprimir: sale igual que el original.
- Editar el monto en CxC y reimprimir: la nota impresa conserva montos y descuento originales.
- Una nota creada a las 9 pm del día 12 muestra 12 en el PDF original y en la reimpresión, en la lista y en el Excel.
- Dos PCs con el mismo correlativo: la segunda ve el aviso y su PDF lleva el número que quedó guardado.
- Todos los PDF abren en tamaño carta.

## Fuera de alcance

- Rediseñar el formato visual de la nota.
- Congelar datos en reportes (ventas, cobranzas), que son vistas del estado actual.

## Decisiones tomadas

- Congelar significa copiar el dato en la nota al emitirla (necesita columnas nuevas, por migración). El cómo va en el plan.
- **Correlativo: opción A** (previsualización). Una vez guardada la nota, el número no cambia nunca. No se reservan números, así que la serie no tiene huecos.
- **Notas viejas: se dejan con datos vivos** y la reimpresión avisa que es una copia con datos actuales.
- **Riesgos que la empresa acepta:** un error del sistema queda congelado en la nota; la reimpresión muestra la dirección y el teléfono que el cliente tenía al emitirla; la nota impresa y el monto a cobrar pueden mostrar cifras distintas. Si un dato está mal, se anula la nota y se emite otra.
- **El único riesgo que sí se cubre con código** es el de agregar campos nuevos al diseño en el futuro (RF-11): la lectura de las fotos viejas tiene que tolerarlo.
