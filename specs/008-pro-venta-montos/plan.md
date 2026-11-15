# Plan 008 - Montos de Pro Venta coherentes

Cómo cumplir [spec.md](spec.md). Todavía no se ha tocado código.

## Qué hay hoy (verificado en [ProVentaService.cs](../../src/NinOS.Infrastructure/Services/Implementations/ProVentaService.cs))

- Tres métodos construyen las filas con casi el mismo código copiado: `get_weekly_report_async` (:127), `get_relation_report_async` (:223) y `get_relation_notes_async`. Los dos primeros usan `total_amount_usd`, el tercero `adjusted_total_usd`.
- `total_amount_usd` ya trae el descuento de emisión (ej.: 292,90 mientras los renglones suman 325,44), así que comisión y gastos salen sobre un monto con descuento.
- `gastos_15 = amount * 0.15` en vez de `(amount − operativos) * 0.15`.
- `get_weekly_report_async` ubica las notas de la semana con `creation_date.Date` (UTC): una nota del domingo en la noche cae en la semana siguiente.
- La ventana [ProVentaEditNotesWindow](../../src/NinOS.UI/Views/ProVentaEditNotesWindow.xaml.cs) usa como "base" `gross_total_usd`, que viene de `total_amount_usd` (ya con descuento), y le aplica el descuento nuevo encima.
- El saldo de relaciones (`build_relation_rows_async`) usa `adjusted_total_usd` y abonos positivos: eso ya es lo que pide RF-7.

## Enfoque

1. **Una función pura** `pro_venta_calculator` en `NinOS.Domain` con constantes con nombre (10%, 25%, 15%): `comision(original)`, `gastos_operativos(original)`, `gastos_administrativos(original)`; cada resultado con `Money.round`. Todo el sistema la usa.
2. **Monto original por nota** = Σ (cantidad − unidades devueltas en notas de crédito vigentes) × precio unitario de lista del renglón (en promociones, el precio de la promoción). Se calcula en consultas agrupadas por nota (ya existe un patrón igual en CxC). Las notas de crédito anuladas no cuentan, igual que en la spec 006.
3. **Una sola función** que arma la fila de Pro Venta a partir de la nota; los tres métodos la llaman. La fila lleva `monto_original`, `amount` (ajustado, el que se cobra) y los tres cálculos.
4. **Semana en hora de Venezuela:** el filtro por semana convierte la fecha con `AppTimeZone.to_local` antes de comparar.
5. **Editar montos:** la base mostrada y usada es `monto_original`; el descuento nuevo se aplica sobre ella. Se guarda como hasta ahora (`adjusted_total_usd` y `discount_percentage`).
6. **PDF y Excel** consumen los campos ya calculados de la fila, sin recalcular.
7. **Lo futuro, no ahora:** editar monto y descuento de la relación "como en CxC". Este plan deja `monto_original` listo para eso.

## Opciones descartadas

- **Calcular en cada pantalla/exportador:** es lo que produjo las diferencias.
- **Guardar la comisión y los gastos en columnas:** se desactualizan al editar; se calculan siempre desde el monto original.
- **Usar `total_amount_usd` como monto original:** ya trae descuento; da 292,90 donde se espera 325,44.

## Riesgo y verificación con tus datos

La definición "monto original = suma de renglones a precio de lista" debe compararse con 2 o 3 notas reales de tu Excel (tarea 6). Con una nota de crédito el monto original baja por las unidades devueltas a precio de lista (no por el total de la nota de crédito, que ya trae descuento): se prueba en la tarea 2.

## Pruebas (a mano)

Nota con renglones por 100: comisión 10,00, operativos 25,00, administrativos 11,25 en pantalla, PDF y Excel. Editar el monto no cambia esas cifras pero sí el saldo. Crear una nota el domingo 9:00 pm (hora de la PC) y ver su semana.

## Rama

`feature/pro-venta-montos` saliendo de `TESTING`. Sin migraciones.
