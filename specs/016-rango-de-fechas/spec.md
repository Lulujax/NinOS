# 016 - Filtro por rango de fechas (Desde / Hasta) en todo el sistema

## Qué y por qué

Hoy casi todos los módulos filtran con combos de **Año** y **Mes**. Eso obliga a ver meses completos y no deja consultar, por ejemplo, del 15 de enero al 3 de diciembre. El cliente necesita elegir el día exacto de inicio y el día exacto de fin en cualquier módulo, igual que ya se hace en el Historial de Clientes.

Se estandariza la búsqueda por fecha en todo el sistema: un mismo filtro **Desde / Hasta** con calendario, sin botones de atajo, que al entrar muestra el mes actual.

## Requisitos

### Filtro común

- **RF-1** EL SISTEMA mostrará en cada pantalla de la lista de alcance un filtro con dos campos de fecha con calendario, **Desde** y **Hasta**, en lugar de los combos de Año y Mes. No habrá botones de atajo. Cada campo tendrá una **✕** pequeña por dentro que solo lo vacía.
- **RF-2** CUANDO el usuario entre a un módulo, EL SISTEMA pondrá por defecto Desde = día 1 del mes actual y Hasta = último día del mes actual (hora de Venezuela).
- **RF-3** EL SISTEMA incluirá en el resultado los días Desde y Hasta completos (de 00:00 del día Desde hasta 23:59:59 del día Hasta, hora de Venezuela).
- **RF-4** CUANDO el usuario cambie Desde o Hasta, EL SISTEMA actualizará los datos en pantalla con el nuevo rango, sin otro botón.
- **RF-5** SI Desde queda después de Hasta, ENTONCES EL SISTEMA mostrará un aviso claro ("La fecha Desde no puede ser posterior a la fecha Hasta") y no filtrará hasta que se corrija.
- **RF-6** CUANDO un campo quede vacío, EL SISTEMA lo tomará como **sin límite** y mostrará dentro un texto gris: «Desde el inicio» en Desde y «Hasta hoy» en Hasta. Desde vacío = desde el primer registro; Hasta vacío = sin tope final; los dos vacíos = **Todos / Todas**, como la opción del combo anterior.
- **RF-6b** SI un campo tiene un texto que no es una fecha válida (ejemplo: 45/13/2026), ENTONCES EL SISTEMA mostrará un aviso y no filtrará hasta que se corrija.
- **RF-7** EL SISTEMA permitirá rangos que crucen años (ejemplo: del 15/11/2025 al 20/02/2026).
- **RF-8** EL SISTEMA mantendrá funcionando junto al rango los demás filtros que ya existen en cada pantalla (buscador, vendedor, zona, categoría, etc.).
- **RF-9** CUANDO se genere un PDF, Excel, vista previa o impresión desde una pantalla con este filtro, EL SISTEMA pondrá el rango en el título en lugar del mes o año, así:
  - Desde y Hasta llenos: **"Del dd/MM/aaaa al dd/MM/aaaa"**.
  - Solo Hasta: **"Desde el inicio hasta el dd/MM/aaaa"**.
  - Solo Desde: **"Desde el dd/MM/aaaa"**.
  - Los dos vacíos: **"TODOS LOS PERIODOS"**.
  El título de Relación Vesupronta (RF-22d) usa el mismo texto en mayúsculas en lugar de «DEL ... AL ...» cuando falte una fecha.
- **RF-10** EL SISTEMA dejará en el log (sin datos de clientes) el módulo y el rango aplicado cada vez que se filtre, y el aviso cuando el rango sea inválido.

### Qué fecha filtra cada módulo

- **RF-11** **Ventas**: fecha de emisión de la nota.
- **RF-12** **Cuentas por Cobrar** (lista y reporte de pendientes, incluido "Período por zona"): fecha de emisión de la nota.
- **RF-13** **Pagos** (lista y reporte): fecha del pago.
- **RF-14** **Comisiones**: fecha de emisión de la nota cobrada.
- **RF-15** **Notas de Crédito** (lista y reporte): fecha de emisión de la nota de crédito.
- **RF-16** **Relación Vesupronta**: fecha de emisión de la nota. Reemplaza el selector Mes / Año / Todos de la spec 015 (RF-2 y RF-3 de esa spec se actualizan).

### Pro Venta

- **RF-17** EL SISTEMA reemplazará el combo **Mes** de las tres vistas de Pro Venta (Reporte Semanal, Relaciones Pendientes y Relaciones Pagadas) por el filtro Desde / Hasta.
- **RF-18** En **Reporte Semanal**, EL SISTEMA unirá los combos **Semana** y **Relación** en un solo combo **Relación**, no editable (solo se elige de la lista), con un elemento por relación cuya semana cae dentro del rango, con el formato `Relación 149 (30/03 - 05/04)`. La lista sale de las relaciones guardadas (las que se crean solas al emitir la primera nota Pro Venta de la semana), no de las semanas del calendario: una semana sin notas no tiene relación y no aparece. Las relaciones de cartera heredada del sistema anterior no salen en este combo, porque no tienen reporte semanal; siguen viéndose en Pendientes y Pagadas.
- **RF-19** CUANDO el usuario elija una relación en ese combo, EL SISTEMA mostrará el reporte semanal de esa relación, igual que hoy.
- **RF-20** SI no hay relaciones en el rango, ENTONCES EL SISTEMA dejará el combo vacío y mostrará el aviso "No hay relaciones en este rango".
- **RF-21** En **Relaciones Pendientes** y **Relaciones Pagadas**, EL SISTEMA mostrará las relaciones cuya semana cae dentro del rango, sin combo de relación.
- **RF-22** Una relación "cae dentro del rango" CUANDO su semana (inicio a fin) se cruza con el rango Desde / Hasta en al menos un día. Ejemplo: con rango del 01/04 al 30/04, la relación del 30/03 al 05/04 sí aparece.
- **RF-22b** SI al cargar el Reporte Semanal hay notas Pro Venta en el rango que no tienen relación asignada (no debería pasar), ENTONCES EL SISTEMA lo dejará en el log como advertencia con la semana y la cantidad de notas, sin datos de clientes.

### Meta de Ventas, Agregar pago y reporte de CxC

- **RF-22e** CUANDO Desde y Hasta estén llenos y caigan en el mismo mes, EL SISTEMA mostrará y permitirá editar la meta de ventas de ese mes. En cualquier otro caso EL SISTEMA ocultará la meta y mostrará «Elija un solo mes para ver la meta».
- **RF-22f** EL SISTEMA mostrará en la ventana Agregar pago todas las notas por cobrar, sin importar el rango elegido en Pagos.
- **RF-22g** EL SISTEMA dejará en el reporte de Cuentas por Cobrar el criterio **Normal** o **Por Zona** y un filtro Desde / Hasta propio, en lugar de Mes, Año, «Todas a la fecha», «Mes en específico» y «Año completo». «Todas a la fecha» se obtiene dejando los dos campos vacíos.

### Lo pendiente fuera del rango

- **RF-22c** EL SISTEMA mostrará en Cuentas por Cobrar y en Relaciones Pendientes solo lo que cae dentro del rango, también lo pendiente. Una deuda de meses anteriores se ve ampliando el rango.

### Título de Relación Vesupronta

- **RF-22d** EL SISTEMA titulará la Relación Vesupronta `RELACION POR COBRAR/VENTAS DEL dd/MM/aaaa AL dd/MM/aaaa VESU PRONTA`. Ejemplo: `RELACION POR COBRAR/VENTAS DEL 01/08/2025 AL 31/08/2025 VESU PRONTA`.

### Ventana Agregar nota de crédito (devolución)

- **RF-23** EL SISTEMA reemplazará el combo **Mes de la Nota** por el filtro Desde / Hasta, por defecto el mes actual, para buscar la nota de entrega a la que se le hace la devolución.
- **RF-24** EL SISTEMA mantendrá la regla actual de esa ventana: el filtro se habilita después de elegir el vendedor.

## Cómo sé que está listo

- Al abrir cada módulo de la lista se ve el mes actual (del 1 al último día) en Desde y Hasta.
- Con Desde 15/01/2026 y Hasta 03/12/2026 en Ventas, aparecen las notas emitidas el 15/01 y el 03/12, y ninguna del 14/01 ni del 04/12.
- En Pagos, un pago hecho el 05/03 de una nota emitida en febrero aparece al filtrar marzo y no al filtrar febrero.
- En Comisiones, la nota cobrada aparece según su fecha de emisión.
- Poner Desde después de Hasta muestra el aviso y la tabla no cambia.
- Vaciar los dos campos con la ✕ muestra todo el historial, igual que la antigua opción «Todos»; los textos grises «Desde el inicio» y «Hasta hoy» se ven, y el PDF dice «TODOS LOS PERIODOS».
- Escribir 45/13/2026 muestra el aviso de fecha inválida.
- Un rango del 15/11/2025 al 20/02/2026 trae datos de los dos años.
- En Pro Venta, Reporte Semanal, con rango de abril, el combo Relación muestra la relación del 30/03 al 05/04 con el formato `Relación 149 (30/03 - 05/04)` y no se puede escribir en él.
- Los PDF, Excel, vistas previas e impresiones dicen "Del dd/MM/aaaa al dd/MM/aaaa" en el título, y siguen en tamaño carta.
- En Agregar nota de crédito, después de elegir el vendedor, el filtro Desde / Hasta lista las notas del rango.
- Pruebas unitarias del cálculo del rango (mes actual por defecto, días completos, Desde > Hasta, cruce de años, relación que cruza el borde del rango): caso feliz, límite y error (`dotnet test`).
- `dotnet build NinOS.slnx` con 0 errores y 0 advertencias.

## Fuera de alcance

- Botones de atajo (Hoy, Este mes, Este año, etc.).
- Librerías visuales externas: se usa el DatePicker propio de WPF.
- Cambiar los DatePicker que ya registran fechas (fecha del pago, fecha de despacho, fechas de la nota de entrega).
- Cambiar la nota impresa o congelada (spec 004).
- Cambios en la base de datos: no hay migración.
- El Historial de Clientes ya tiene Desde / Hasta. Solo se le aplica el mismo estilo si hace falta para que todo se vea igual.

## Decisiones tomadas

- Para ver todo no hay botón: se dejan Desde y Hasta vacíos (RF-6). Se descartaron un botón «Todos», una casilla que apaga el filtro, un enlace dentro del calendario y escribir «todos» en el campo.
