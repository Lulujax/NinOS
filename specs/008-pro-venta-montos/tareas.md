# Tareas 008 - Montos de Pro Venta coherentes

## Tarea 1 - Función única de cálculo
Cubre: RF-2, RF-3, RF-4, RF-12
- Crear `pro_venta_calculator` en `NinOS.Domain` con comisión 10%, operativos 25% y administrativos 15% de (original − operativos), con redondeo `Money.round` y constantes con nombre.
- Hecho cuando: compila (0 errores, 0 advertencias) y se usa en la tarea 3.
- Cómo probarla: se comprueba con el ejemplo de la tarea 3 (100 → 10,00 / 25,00 / 11,25).
- [x] Hecha y probada

## Tarea 2 - Monto original por nota
Cubre: RF-1
- Calcular el monto original (renglones a precio de lista; en promociones, cantidad × precio) con una consulta agrupada y agregar el campo a la fila de Pro Venta.
- Hecho cuando: la nota hoy guardada con total 292,90 y renglones por 325,44 reporta 325,44 como monto original; y si se devuelve una unidad con nota de crédito, baja por el precio de lista de esa unidad.
- Cómo probarla: ver la fila de esa nota en el detalle de la relación (nota 000_001); crear una nota Pro Venta de prueba, devolver 1 unidad con nota de crédito y comparar; anular la nota de crédito y ver que el monto vuelve.
- [x] Hecha y probada

## Tarea 3 - Una sola construcción de filas y las fórmulas nuevas
Cubre: RF-2, RF-3, RF-4, RF-6, RF-10
- Los tres métodos de `ProVentaService` usan una función común que arma la fila con `pro_venta_calculator`; las notas anuladas se separan de los totales.
- Hecho cuando: nota con monto original 100 muestra 10,00 / 25,00 / 11,25 en el detalle y el reporte semanal.
- Cómo probarla: crear una nota Pro Venta de 100 en el debug; abrir su semana y su relación; anular otra nota y ver que no suma.
- [x] Hecha y probada

## Tarea 4 - Pantalla de relaciones muestra el monto original
Cubre: RF-5
- Las tablas de Pro Venta muestran el monto sin descuento aplicado y sus tres cálculos; el saldo sigue saliendo del monto ajustado.
- Hecho cuando: se ve el monto original y el saldo por cobrar sigue igual que antes.
- Cómo probarla: abrir una relación con notas editadas; el saldo coincide con el de antes de este cambio.
- [x] Hecha y probada

## Tarea 5 - PDF y Excel sin recalcular
Cubre: RF-6
- Revisar `ProVentaPdfGenerator`, los Excel y el resumen de relaciones para que usen los campos de la fila; quitar cualquier cálculo propio.
- Hecho cuando: pantalla, PDF y Excel muestran las mismas cifras.
- Cómo probarla: generar PDF y Excel de una semana y comparar con la pantalla.
- [x] Hecha y probada

## Tarea 6 (manual, con tus datos) - Verificar con notas reales
Cubre: RF-1
- Tomar 2 o 3 notas de tu Excel con su comisión y gastos conocidos y comparar con el sistema.
- Hecho cuando: coinciden. Si no, se ajusta la definición de "monto original" antes de seguir.
- Cómo probarla: comparar cifra por cifra.
- [ ] Hecha y probada (manual — tuya)

## Tarea 7 - Editar montos con base correcta
Cubre: RF-8, RF-9
- La ventana "Editar montos" usa `monto_original` como base; el descuento nuevo no se acumula; comisión y gastos no cambian al editar.
- Hecho cuando: editar dos veces el descuento da el mismo resultado que editarlo una vez.
- Cómo probarla: nota de 100: poner 10% → 90; cambiar a 5% → 95; comisión 10,00 en ambos casos; saldo cambia.
- [x] Hecha y probada

## Tarea 8 - Semana en hora de Venezuela
Cubre: RF-11
- El filtro de la semana usa la fecha convertida a hora de Venezuela.
- Hecho cuando: una nota del domingo 9:00 pm cae en esa semana.
- Cómo probarla: cambiar la hora de la PC a un domingo 9:00 pm, crear una nota Pro Venta y ver su semana y su relación.
- [x] Hecha y probada
