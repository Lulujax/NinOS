# Tareas — 016 Rango de fechas

Rama: `TESTING`. Cada tarea compila con 0 advertencias, sin comentarios en el código y con logs `AppLog` sin datos de clientes.

## 1. Pieza de dominio `rango_fechas`
- Cubre: RF-2, RF-3, RF-5, RF-6, RF-7, RF-9, RF-10, RF-22, RF-22e
- Hecho cuando: existe `src/NinOS.Domain/rango_fechas.cs` con `mes_actual`, `contiene`, `se_cruza_con`, `a_limites_utc`, `mes_unico`, `texto_titulo` y `texto_log`, y `tests/NinOS.Tests/Dominio/RangoFechasTests.cs` pasa (caso feliz, límite: extremos del día, cruce de año, hora VE contra UTC; error: Desde > Hasta).
- [ ] Hecha y probada

## 2. Control `RangoFechasPicker`
- Cubre: RF-1, RF-4, RF-5, RF-6, RF-6b
- Hecho cuando: `src/NinOS.UI/Controls/RangoFechasPicker` muestra Desde / Hasta con texto gris al estar vacío y una ✕ para vaciar. Avisa si Desde > Hasta o si la fecha es inválida, y solo publica rangos válidos.
- [ ] Hecha y probada

## 3. Ventas + Meta
- Cubre: RF-11, RF-22e, RF-9
- Hecho cuando: Ventas filtra por fecha de emisión con el control. La meta solo se ve con un solo mes, y el PDF y Excel llevan el título del rango.
- [ ] Hecha y probada

## 4. Cuentas por Cobrar (lista)
- Cubre: RF-12, RF-22c
- Hecho cuando: la lista de CxC filtra por fecha de emisión con el control.
- [ ] Hecha y probada

## 5. Reporte de Cuentas por Cobrar
- Cubre: RF-12, RF-22g, RF-9
- Hecho cuando: el panel del reporte tiene Normal / Por Zona con su propio Desde / Hasta, y los dos campos vacíos equivalen a «Todas a la fecha».
- [ ] Hecha y probada

## 6. Pagos (lista) + Agregar pago
- Cubre: RF-13, RF-22f
- Hecho cuando: Pagos filtra por fecha del pago y Agregar pago lista todas las notas por cobrar.
- [ ] Hecha y probada

## 7. Reporte de Pagos
- Cubre: RF-13, RF-9
- Hecho cuando: existe `PaymentService.get_payments_by_range_async` con límites UTC y su prueba, y el reporte usa Desde / Hasta con el título del rango.
- [ ] Hecha y probada

## 8. Comisiones
- Cubre: RF-14, RF-9
- Hecho cuando: Comisiones filtra por fecha de emisión de la nota cobrada, y sus reportes llevan el título del rango.
- [ ] Hecha y probada

## 9. Notas de Crédito (lista y reporte)
- Cubre: RF-15, RF-9
- Hecho cuando: la lista y el reporte usan Desde / Hasta por fecha de emisión de la nota de crédito, con el título del rango.
- [ ] Hecha y probada

## 10. Agregar nota de crédito (devolución)
- Cubre: RF-23, RF-24
- Hecho cuando: «Mes de la Nota» se reemplaza por Desde / Hasta (mes actual), que se habilita al elegir la vendedora y filtra sus notas.
- [ ] Hecha y probada

## 11. Relación Vesupronta
- Cubre: RF-16, RF-22d, RF-9
- Hecho cuando: el dominio usa `rango_fechas` en lugar de `relacion_periodo`, `RelacionVesuprontaCalculatorTests` pasa actualizada, y la vista, el PDF y el Excel usan el nuevo título.
- [ ] Hecha y probada

## 12. Pro Venta: opción de relación
- Cubre: RF-18
- Hecho cuando: `pro_venta_relation_option` trae `es_hueca` y su etiqueta es `Relación 149 (30/03 - 05/04)`.
- [ ] Hecha y probada

## 13. Pro Venta: Reporte Semanal
- Cubre: RF-17, RF-18, RF-19, RF-20, RF-22, RF-22b
- Hecho cuando: Mes y Semana desaparecen, queda Desde / Hasta y un combo Relación no editable con las relaciones guardadas no heredadas que tocan el rango, y sin relaciones sale el aviso.
- [ ] Hecha y probada

## 14. Pro Venta: Pendientes, Pagadas y títulos
- Cubre: RF-17, RF-21, RF-22, RF-22c, RF-9
- Hecho cuando: Pendientes y Pagadas filtran por cruce de semana con el rango, y todos los PDF y Excel de Pro Venta llevan el título del rango.
- [ ] Hecha y probada
