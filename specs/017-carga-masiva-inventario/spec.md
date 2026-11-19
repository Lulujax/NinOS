# 017 - Carga masiva de inventario y precios por Excel

## Qué y por qué

Hoy el stock y los precios se corrigen producto por producto. Con un conteo de fin de mes, un pedido grande del proveedor o un aumento de precios de una marca, eso son decenas o cientos de ventanas, y es fácil equivocarse o dejar productos sin actualizar.

El cliente necesita descargar una plantilla de Excel, escribir allí los valores nuevos (con filtros, copiar y pegar, fórmulas) y cargar el archivo de vuelta para aplicar todos los cambios de una vez. Antes de descargar elige **qué quiere cambiar**: **Cantidad** o **Precio** (y en Precio, **de qué lista**). Cada plantilla sirve para una sola cosa, para que no se toquen datos por accidente.

Como un error en el archivo puede mover muchos productos a la vez, el sistema muestra primero una **vista previa** de lo que va a cambiar y solo aplica cuando el usuario confirma.

Lo que ya funciona no cambia: el stock es uno solo por producto (no depende de la lista), todos los productos activos aparecen en todas las listas, y en las listas secundarias un producto sin precio propio sigue tomando el de la Principal. Las reglas de stock de la spec 003 siguen igual y las notas emitidas siguen congeladas (spec 004).

## Requisitos

### Descargar plantilla

- **RF-1** EL SISTEMA tendrá en la pestaña Inventario un botón **Descargar plantilla** que abre una ventana con el desplegable **¿Qué desea cambiar?**, con las opciones **Cantidad** y **Precio**.
- **RF-2** CUANDO se elija **Precio**, EL SISTEMA mostrará un segundo desplegable **¿Qué lista?** con la lista Principal y las listas secundarias activas, con la Principal seleccionada por defecto. CUANDO se elija **Cantidad**, EL SISTEMA no mostrará ese desplegable.
- **RF-3** EL SISTEMA generará la plantilla con todos los productos activos (no los eliminados), ordenados por marca y código, una fila por producto.
- **RF-4** La plantilla de **Cantidad** tendrá las columnas: **ID**, **CÓDIGO**, **MARCA**, **DESCRIPCIÓN**, **STOCK EN SISTEMA** (el stock al descargar) y **CANTIDAD** (vacía).
- **RF-5** La plantilla de **Precio de la lista Principal** tendrá las columnas: **ID**, **CÓDIGO**, **MARCA**, **DESCRIPCIÓN**, **PRECIO ACTUAL** y **PRECIO NUEVO** (vacía).
- **RF-6** La plantilla de **Precio de una lista secundaria** tendrá las columnas: **ID**, **CÓDIGO**, **MARCA**, **DESCRIPCIÓN**, **PRECIO PRINCIPAL**, **PRECIO EN ESTA LISTA** y **PRECIO NUEVO** (vacía). PRECIO EN ESTA LISTA es el precio con el que esa lista vende hoy el producto, sea propio o tomado de la Principal; la plantilla no indica cuál de los dos es.
- **RF-7** EL SISTEMA dejará bloqueadas todas las celdas salvo la columna editable (CANTIDAD o PRECIO NUEVO), para que el usuario no cambie por error el ID, el código ni los valores de referencia. Los filtros de Excel seguirán funcionando. El ordenamiento no funciona mientras la hoja esté protegida (Excel no ordena rangos con celdas bloqueadas): para ordenar, se quita la protección desde Revisar › Desproteger hoja, sin contraseña.
- **RF-8** EL SISTEMA guardará dentro del archivo, en una hoja oculta, una marca que lo identifique como plantilla de NinOS, su versión de formato, el tipo (Cantidad o Precio), la lista (si es de Precio) y la fecha y hora de descarga.
- **RF-9** EL SISTEMA pondrá en la parte superior de la plantilla el tipo y la lista (ejemplo: «PRECIOS — LISTA MAYORISTAS») y una instrucción corta: «Escriba solo en la columna [CANTIDAD / PRECIO NUEVO]. Las filas vacías no se modifican.»
- **RF-10** EL SISTEMA propondrá como nombre del archivo `Plantilla_Cantidad_aaaa-MM-dd_HHmm.xlsx` o `Plantilla_Precio_[LISTA]_aaaa-MM-dd_HHmm.xlsx` y dejará elegir dónde guardarlo.

### Cargar plantilla

- **RF-11** EL SISTEMA tendrá en la pestaña Inventario un botón **Cargar plantilla** que pide el archivo Excel y reconoce por la marca de RF-8 si es de Cantidad o de Precio y de qué lista. No se vuelve a preguntar la lista.
- **RF-12** CUANDO la plantilla sea de **Cantidad**, EL SISTEMA aplicará siempre **Conteo**: lo escrito en CANTIDAD es el stock que debe quedar (ejemplo: sistema 10, escribe 25 → queda 25). No se ofrece Ajuste: la función es un editar masivo del stock.
  - **Conteo**: lo escrito en CANTIDAD es el stock que debe quedar. Ejemplo: sistema 10, escribe 25 → queda 25 (+15 en el kardex).
- **RF-13** CUANDO la plantilla sea de **Precio**, lo escrito en PRECIO NUEVO es el precio que debe quedar en esa lista. Para subir un porcentaje el usuario usa fórmulas de Excel (ejemplo: `=E2*1.10`).
- **RF-14** CUANDO se carga un archivo, EL SISTEMA no modificará nada todavía: leerá el archivo, lo validará y mostrará la vista previa (RF-20).
- **RF-15** SI el archivo no es una plantilla de NinOS (no tiene la marca de RF-8 o su versión no es compatible), ENTONCES EL SISTEMA lo rechazará con el aviso «Este archivo no es una plantilla de NinOS. Descargue una plantilla nueva.» y no mostrará vista previa.
- **RF-16** SI la plantilla es de una lista que ya no existe o fue desactivada, ENTONCES EL SISTEMA la rechazará con el aviso «La lista de precios de este archivo ya no está activa. Descargue una plantilla nueva.».
- **RF-17** SI el archivo no se puede abrir (está abierto en Excel, está dañado o no existe), ENTONCES EL SISTEMA mostrará un aviso claro con la causa («Cierre el archivo en Excel e intente de nuevo», «El archivo está dañado») y no aplicará nada.
- **RF-18** EL SISTEMA ignorará las filas con la columna editable vacía: esos productos no se tocan. Los productos que no estén en el archivo tampoco se tocan.
- **RF-18b** En **Precio**, CUANDO PRECIO NUEVO sea 0, EL SISTEMA lo tratará igual que una celda vacía: el producto conserva el precio que ya tenía y nunca queda en 0. La vista previa lo cuenta en el resumen como «quedan igual».
- **RF-19** EL SISTEMA validará cada fila con valor escrito y la marcará como **error** cuando:
  - El ID no existe, el producto fue eliminado después de descargar, o el CÓDIGO no coincide con el del ID.
  - El mismo producto aparece más de una vez con valor.
  - **Cantidad**: no es un número entero (letras, decimales).
  - **Cantidad, Conteo**: es negativa.
  - **Precio**: no es un número, o es negativo.

### Vista previa

- **RF-20** EL SISTEMA mostrará una ventana de vista previa con una fila por producto a modificar: código, descripción, valor en el archivo, valor actual en la base, valor escrito, valor final y diferencia (en Precio, también en %). Arriba, el tipo y la lista, y un resumen: cuántos productos cambian, cuántos tienen error, cuántos tienen aviso y cuántos quedan igual.
- **RF-21** EL SISTEMA marcará en rojo las filas con error, con el motivo y el número de fila de Excel («Fila 18: la cantidad tiene letras»). Mientras exista al menos un error, EL SISTEMA no permitirá aplicar: se corrige el Excel y se vuelve a cargar.
- **RF-22** SI el valor actual en la base ya no es el del archivo (STOCK EN SISTEMA en Conteo, PRECIO ACTUAL o PRECIO EN ESTA LISTA en Precio) porque hubo cambios después de descargar, ENTONCES EL SISTEMA marcará la fila en amarillo con el aviso «Cambió desde que descargó: archivo 10, ahora 7», y el usuario elegirá, para todas las filas en amarillo a la vez, **Aplicar igual** o **Saltar estas filas**.
- **RF-24** En **Precio**, EL SISTEMA redondeará el precio escrito a 2 decimales (las fórmulas de Excel pueden dar 5.5000001) y mostrará en la vista previa el precio ya redondeado, que es el que se guardará.
- **RF-25** En **Precio**, SI el precio nuevo sube o baja más de 50% respecto al actual, ENTONCES EL SISTEMA marcará la fila en amarillo con el aviso «Cambio grande: +80%», por si fue un error de dedo. Estas filas siguen la misma elección de RF-22.
- **RF-26** En **Precio de la lista Principal**, EL SISTEMA avisará arriba de la vista previa cuántos de los productos modificados están tomando ese precio en otras listas: «Este cambio también cambia el precio de N productos en otras listas». Es el comportamiento que ya tiene el sistema al cambiar un precio de la Principal.
- **RF-27** EL SISTEMA no mostrará en la lista de cambios las filas cuyo valor final sea igual al actual (no hay nada que cambiar); solo las contará en el resumen como «quedan igual».
- **RF-28** SI el archivo no tiene ninguna fila con cambios, ENTONCES EL SISTEMA avisará «El archivo no tiene cambios para aplicar» y no mostrará el botón Aplicar.

### Aplicar

- **RF-29** CUANDO el usuario pulse **Aplicar** en una carga de **Cantidad**, EL SISTEMA pedirá un **motivo** obligatorio (ejemplo: «Conteo fin de mes», «Pedido proveedor») y una nota opcional, igual que en un ajuste manual. En una carga de **Precio**, EL SISTEMA pedirá solo confirmar («Se cambiará el precio de N productos en la lista X. ¿Continuar?»).
- **RF-30** EL SISTEMA aplicará todos los cambios en una sola operación: **o se aplican todos o ninguno**. SI falla cualquier producto, ENTONCES EL SISTEMA deshará todo, mostrará qué producto falló y por qué, y los datos quedarán como estaban antes de aplicar.
- **RF-31** En **Cantidad**, EL SISTEMA registrará cada cambio en el kardex igual que un conteo o ajuste manual (spec 003, RF-4 y RF-5), con el motivo y la nota indicados y con la marca «Carga masiva» en la nota, para poder distinguirlos en el historial.
- **RF-32** En **Precio de la lista Principal**, EL SISTEMA guardará el precio base del producto, igual que al editarlo a mano. En **Precio de una lista secundaria**, EL SISTEMA guardará el precio como precio propio del producto en esa lista, igual que al editarlo a mano en esa lista.
- **RF-33** CUANDO se aplica, EL SISTEMA volverá a leer los valores de la base en ese momento (no los de la vista previa). SI entre la vista previa y Aplicar otro cambio movió algún producto, ENTONCES EL SISTEMA no aplicará nada y pedirá volver a cargar el archivo para revisar la vista previa actualizada.
- **RF-34** CUANDO termine con éxito, EL SISTEMA mostrará «Se actualizaron N productos» y refrescará la lista de inventario y de precios en pantalla.
- **RF-35** EL SISTEMA mantendrá siempre: stock del producto = suma de entradas − suma de salidas del kardex (spec 003, RF-8).
- **RF-36** Los cambios de precio no modificarán ninguna nota ya emitida (spec 004): solo afectan a las notas nuevas y a las listas de precios que se generen después.

### Logs

- **RF-37** EL SISTEMA dejará en el log: descarga de plantilla (tipo, id de lista, cantidad de productos), carga (tipo, id de lista, tipo de carga, filas leídas, cambios, errores, avisos), aplicación (cantidad aplicada, saltada, resultado) y cada fallo con su causa técnica. Sin nombres de clientes, sin montos y sin la ruta completa del archivo.

## Cómo sé que está listo

- **Cantidad, Conteo**: descargo, escribo cantidades en 30 productos, cargo: la vista previa muestra los 30 con su diferencia, aplico y los 30 quedan con la cantidad escrita y su línea en el kardex.
- **Precio, Principal**: subo 10% una marca con fórmula, cargo: la vista previa muestra los precios redondeados y el aviso de cuántos productos cambian también en otras listas. Aplico y el precio cambia en la Principal y en las listas donde ese producto no tiene precio propio.
- **Precio, lista secundaria**: cambio 5 precios de la lista, aplico: cambian solo en esa lista y la Principal sigue igual.
- Un precio que sube más de 50% sale en amarillo; un precio negativo o con letras sale en rojo y no deja aplicar; un precio 0 no cambia nada y el producto conserva su precio.
- Escribo «abc» en una cantidad y un decimal en otra: la vista previa las marca en rojo con su número de fila y no deja aplicar.
- Descargo, emito una nota que vende 3 unidades de un producto, cargo un Conteo con ese producto: aparece en amarillo con «archivo 10, ahora 7» y puedo saltarlo o aplicarlo.
- Intento cambiar el CÓDIGO, el ID o una columna de referencia en Excel: está bloqueado.
- Cargo un Excel cualquiera (no la plantilla): se rechaza con el aviso de RF-15.
- Cargo el archivo mientras está abierto en Excel: aviso claro, nada se modifica.
- Fuerzo un fallo a mitad de la aplicación: ningún producto cambió.
- Una nota emitida antes del cambio de precios se reimprime igual que antes.
- La consulta «stock vs kardex» no devuelve diferencias después de una carga masiva.

## Fuera de alcance

- Cambiar nombres, marcas o códigos desde el Excel.
- Crear o eliminar productos desde el Excel.
- Productos que existan solo en una lista: hoy todos los productos están en todas las listas, y eso no cambia.
- Quitar el precio propio de un producto en una lista secundaria para que vuelva a tomar el de la Principal: se sigue haciendo uno por uno, como hoy.
- Cambiar varias listas en un mismo Excel: cada plantilla de Precio es de una sola lista.
- Promociones (su stock sale de los productos que las componen y su precio se edita aparte).
- Cargar archivos que no sean la plantilla (por ejemplo, el Excel de un proveedor con otro formato).
- Tabla editable dentro de la app y «nota de entrada de mercancía» por documento (opciones evaluadas; quedan para el futuro si hacen falta).
- Deshacer una carga masiva con un botón. Si una carga salió mal, se corrige con otra carga o con cambios manuales.

## Decisiones tomadas

- Se eligió el camino **Excel de ida y vuelta con vista previa** sobre la tabla editable y la nota de entrada, porque cubre conteos grandes, pedidos medianos y aumentos de precio, y reutiliza la lógica que ya existe.
- Antes de descargar se elige **qué cambiar** (Cantidad o Precio) y, en Precio, **qué lista**. Una plantilla por tipo y por lista, para que no se toquen datos por accidente.
- En la plantilla de una lista secundaria solo se muestra el precio con el que vende esa lista, sin indicar si es propio o tomado de la Principal (decisión de Luis).
- La carga de Cantidad es siempre **Conteo** (decisión de Luis: es un editar masivo, no un ajuste).
- Con valores que cambiaron desde la descarga se avisa y el usuario decide (igual que el conteo manual de la spec 003, RF-6).
- Cada fila se reconoce por su ID y se confirma con el CÓDIGO, para que reordenar o filtrar el Excel no cambie a qué producto se aplica.
- Precios: se redondean a 2 decimales, no se aceptan negativos, y un cambio de más de 50% se avisa en amarillo.
- Precio 0 = sin cambio, igual que la celda vacía: el producto conserva su precio y nunca queda en 0 (decisión de Luis).
- La función la usa el cliente desde la pestaña Inventario.
