# Tareas 001 - Ventas sin notas Pro Venta

Sale del [plan.md](plan.md). Todo es puro código, sin migración.

## Tarea 1 - Regla pura en el dominio
Cubre: RF-1, RF-7
- Clase `SalesNoteScope` en `src/NinOS.Domain` con `is_visible_in_sales(string? note_type_code)`: devuelve `false` si `NoteTypeCodes.is_pro_venta` es verdadero, y `true` para las notas sin tipo.
- Hecho cuando: hay pruebas unitarias con caso feliz (GEN, PRM, VOL visibles), caso límite (nulo, vacío, minúsculas, espacios) y caso de error (MAR, PVP, VOLMAR ocultas), y pasan.
- Cómo probarla: `dotnet test NinOS.slnx`.
- [x] Hecha y probada

## Tarea 2 - Filtro de la pestaña Todos
Cubre: RF-1, RF-6
- Aplicar la regla en `apply_filters` de `SalesViewModel`, antes de llenar `all_notes` y las pestañas, para que la pestaña, la meta y los totales usen la misma lista.
- Hecho cuando: la pestaña Todos no muestra ninguna nota MAR, PVP ni VOLMAR.
- Cómo probarla: en el debug, con las 21 notas MAR y las 4 GEN de testing, abrir Ventas y contar; Cobranzas y Pro Venta siguen igual.
- [x] Hecha y probada

## Tarea 3 - Filtro del reporte de ventas
Cubre: RF-2, RF-3
- Aplicar la regla a `month_rows` en `build_sales_report_dto_async`, antes de los demás filtros, para que el reporte, el PDF y el Excel queden sin Pro Venta aunque el combo esté en "Todas".
- Hecho cuando: el reporte en "Todas" no trae notas Pro Venta y el total coincide con la suma de las generales del mes.
- Cómo probarla: generar el reporte del mes, el PDF y el Excel, y comparar el total con la suma de las notas generales.
- [x] Hecha y probada

## Tarea 4 - Lista de vendedores del reporte
Cubre: RF-4
- En `load_all_async`, construir la lista de vendedores solo con notas visibles en Ventas y no agregar vendedores con `is_pro_venta_only` aunque tengan notas.
- Hecho cuando: la lista no contiene "Juan Luis (inactivo)".
- Cómo probarla: abrir el filtro de vendedores del reporte de ventas y revisar la lista.
- [x] Hecha y probada

## Tarea 5 - Combo TIPO DE NOTA
Cubre: RF-5
- Quitar de `report_type_options` las opciones "Pro Venta", "Promocion Pro Venta" y "Volumen Pro Venta", y hacer que `matches_note_type` use `NoteTypeCodes` en vez de códigos escritos a mano.
- Hecho cuando: el combo no muestra esas tres opciones y el filtro por tipo sigue funcionando con las demás.
- Cómo probarla: abrir el combo en Ventas; probar el reporte filtrando por General, Promoción y Volumen.
- [x] Hecha y probada

## Tarea 6 - Limpieza de la colección sin uso
Cubre: RF-6
- Quitar `juan_luis_notes` de `SalesViewModel` y su llenado. Verificado en el plan: `SalesView.xaml` no la usa. Se cruza con la tarea 6 de la spec 009 (las demás colecciones con nombres fijos).
- Hecho cuando: compila en 0 errores y 0 advertencias y la pantalla Ventas funciona igual.
- Cómo probarla: abrir Ventas y recorrer todas las pestañas.
- [x] Hecha y probada

## Tarea 7 - Verificación final
Cubre: RF-1 a RF-8
- Sin código: recorrer la lista de "Cómo sé que está listo" de la spec.
- Hecho cuando: todos los puntos pasan y la base no cambió (el arranque ya no escribe, spec 007).
- Cómo probarla: Ventas, su reporte, PDF y Excel; después Cobranzas, Pagos, Comisiones y Pro Venta para confirmar que muestran lo mismo que antes.
- [ ] Hecha y probada
