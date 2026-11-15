# 001 - Ventas sin notas Pro Venta

## Qué y por qué

En la pantalla **Ventas** aparecen las notas de la modalidad Pro Venta (MAR, PVP y VOLMAR) junto con las generales. Esto mezcla dos operaciones que el negocio lleva por separado: la pestaña **Todos** y el **reporte de ventas** muestran notas Pro Venta, y la lista de vendedores del reporte muestra a "Juan Luis (inactivo)", una vendedora que solo tiene zonas Pro Venta.

Las notas Pro Venta ya tienen su propia pantalla (Pro Venta), y Cobranzas y Pagos ya las excluyen. Esta spec deja Ventas en la misma línea: solo notas generales (GEN, PRM, VOL y las notas sin tipo).

El criterio de separación es el **tipo de nota** y no el vendedor. Según las reglas del negocio, cualquier vendedora con una zona Pro Venta puede emitir notas MAR, así que filtrar por nombre dejaría fuera notas que sí son Pro Venta.

## Alcance

Solo la pantalla Ventas: su pestaña Todos, su reporte y los PDF y Excel que salen de ese reporte.

## Requisitos

- **RF-1** EL SISTEMA no mostrará en la pestaña **Todos** de Ventas las notas de tipo MAR, PVP ni VOLMAR.
- **RF-2** EL SISTEMA no incluirá en el reporte de ventas las notas de tipo MAR, PVP ni VOLMAR, aunque el filtro de tipo de nota esté en "Todas".
- **RF-3** EL SISTEMA no incluirá en los PDF y Excel del reporte de ventas las notas de tipo MAR, PVP ni VOLMAR.
- **RF-4** EL SISTEMA no mostrará "Juan Luis (inactivo)" ni ningún otro vendedor con solo zonas Pro Venta en la lista de vendedores del reporte de ventas.
- **RF-5** CUANDO se abra la pantalla Ventas, EL SISTEMA no mostrará en el combo TIPO DE NOTA las opciones "Pro Venta", "Promocion Pro Venta" ni "Volumen Pro Venta", porque no tendrían notas que mostrar.
- **RF-6** EL SISTEMA no cambiará las notas de tipo General, Promoción ni Volumen, ni las notas que ya se ven en Cobranzas, Pagos, Comisiones o Pro Venta.
- **RF-7** SI una nota no tiene tipo asignado, ENTONCES EL SISTEMA la tratará como General, igual que hoy.
- **RF-8** EL SISTEMA no modificará la base de datos ni las notas guardadas.

## Cómo sé que está listo

- Con la pestaña Todos y el reporte en "Todas", no aparece ninguna nota MAR, PVP ni VOLMAR. Se verifica con notas de prueba de cada tipo en la base de testing.
- El total del reporte de ventas coincide con la suma de las notas generales del mes.
- El PDF y el Excel del reporte no contienen notas Pro Venta.
- La lista de vendedores del reporte no contiene "Juan Luis (inactivo)".
- Cobranzas, Pagos, Comisiones y Pro Venta muestran lo mismo que antes del cambio.
- La compilación queda con 0 errores y 0 advertencias (`dotnet build NinOS.slnx`).
- Hay pruebas unitarias del filtro por tipo de nota: caso feliz (GEN, PRM, VOL se muestran), caso límite (sin tipo, tipo vacío, minúsculas, espacios) y caso de error (MAR, PVP y VOLMAR se ocultan).
- Se probó además a mano en el debug (base de testing) con notas de cada tipo.

## Fuera de alcance

- Historiales de producto y de promoción en Inventario. No se han revisado; si muestran notas Pro Venta, va en una spec aparte.
- Cambiar la pantalla Pro Venta, el cálculo de comisiones o las reglas de zonas.
- Cambiar la base de datos, las migraciones o los datos de vendedoras y zonas.
- Renombrar la pestaña o el código de Juan Luis en otras pantallas.

## Decisiones tomadas

- La meta mensual de Ventas cuenta solo las notas generales. El avance de la meta se calcula con la misma lista filtrada de la pestaña Todos, así que las notas Pro Venta no suman.
- Se quitan del combo TIPO DE NOTA las tres opciones Pro Venta (RF-5). Ya no aparecen en la pantalla Ventas.
