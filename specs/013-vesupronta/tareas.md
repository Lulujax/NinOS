# Tareas 013 - Vesupronta

Orden recomendado. Las marcadas (manual) las haces tú; yo te dejo los pasos.

## Tarea 1 - Regla pura de empresa
Cubre: RF-1, RF-2, RF-13
- Agregar `business_unit` (General, ProVenta, Vesupronta), los códigos `VES`, `VESPRM` y `VESVOL`, las listas de códigos por empresa y `BusinessUnitRules.unit_of_note_type(code)` y `unit_of_zone(zona)`.
- Hecho cuando: cada código devuelve su empresa; vacío o desconocido devuelve General y nunca Vesupronta; una zona marcada como Pro Venta y Vesupronta a la vez lanza error.
- Cómo probarla: `dotnet test NinOS.slnx` (caso feliz, límite y error).
- [ ] Hecha y probada

## Tarea 2 - Migración: zona Vesupronta
Cubre: RF-1, RF-11
- Columna `zona.is_vesupronta` (por defecto no) y restricción que impide Pro Venta y Vesupronta a la vez. Sin actualizar filas existentes.
- Hecho cuando: la migración es aditiva y las zonas existentes quedan iguales.
- Cómo probarla: aplicar en testing y comparar las zonas antes y después.
- [ ] Hecha y probada

## Tarea 3 (manual) - Tipos de nota de Vesupronta en testing
Cubre: RF-4, RF-11
- Script SQL supervisado que crea `VES`, `VESPRM` y `VESVOL` copiando las reglas de GEN, PRM y VOL. Solo inserta; no modifica nada existente. Se corre en testing.
- Hecho cuando: los tres tipos existen en testing y los de General y Pro Venta no cambiaron.
- Cómo probarla: listar los tipos de nota antes y después.
- [ ] Hecha y probada

## Tarea 4 - Elegir la empresa de una zona
Cubre: RF-14, RF-15
- En la ventana de zona, cambiar la casilla Pro Venta por una selección de tres opciones (General, Pro Venta, Vesupronta). Se permite cambiar la empresa de una zona con notas: las notas emitidas conservan su tipo y, por lo tanto, su empresa.
- Hecho cuando: se puede crear una zona Vesupronta, editarla sin perder su empresa y pasar una zona con notas de General a Vesupronta sin que sus notas viejas cambien de empresa.
- Cómo probarla: en testing, crear una zona Vesupronta, cerrarla y reabrirla; pasar una zona con notas a Vesupronta y revisar que las notas viejas siguen en General.
- [ ] Hecha y probada

## Tarea 5 - Notas de entrega de las tres empresas
Cubre: RF-2, RF-6, RF-9, RF-15
- En la pestaña Notas de Entrega actual, al elegir un cliente se ofrecen los tipos de nota de la empresa de su zona: GEN, PRM y VOL para General; MAR, PVP y VOLMAR para Pro Venta; VES, VESPRM y VESVOL para Vesupronta.
- Hecho cuando: un cliente de cada empresa solo ofrece sus tipos y el número sale del correlativo global.
- Cómo probarla: pruebas de la regla que elige los tipos (feliz, límite y error); en testing, crear una nota para un cliente de cada empresa.
- [ ] Hecha y probada

## Tarea 6 - Ventas por empresa
Cubre: RF-5, RF-12
- Cambiar `SalesNoteScope` de "no es Pro Venta" a "es de esta empresa" y pasar el alcance a Ventas.
- Hecho cuando: Ventas de General no muestra Vesupronta y Ventas de Vesupronta solo muestra Vesupronta.
- Cómo probarla: prueba unitaria del filtro y prueba de integración contra testing.
- [ ] Hecha y probada

## Tarea 7 - Cuentas por Cobrar por empresa
Cubre: RF-4, RF-5
- `AccountsReceivableService` y su view model filtran por la lista de códigos de la empresa. La edición de montos y descuentos funciona igual que en General.
- Hecho cuando: cada pestaña muestra solo sus notas y la edición recalcula igual que General.
- Cómo probarla: prueba de integración contra testing con una nota de cada empresa.
- [ ] Hecha y probada

## Tarea 8 - Pagos por empresa y sin comisión en Vesupronta
Cubre: RF-5, RF-7
- `PaymentService` y su view model filtran por empresa; al registrar un pago de una nota de Vesupronta no se crea comisión.
- Hecho cuando: el pago de Vesupronta baja el saldo y no hay fila nueva en comisiones.
- Cómo probarla: prueba de integración contra testing.
- [ ] Hecha y probada

## Tarea 9 - Notas de crédito por empresa
Cubre: RF-5, RF-8, RF-9
- `CreditNoteService` y su view model filtran por empresa; la nota de crédito de Vesupronta mueve el mismo stock y usa el correlativo global de notas de crédito.
- Hecho cuando: cada pestaña solo muestra sus notas de crédito y el stock cuadra con el kardex.
- Cómo probarla: prueba de integración contra testing.
- [ ] Hecha y probada

## Tarea 10 - Comisiones sin Vesupronta
Cubre: RF-5, RF-7
- `CommissionService` y Comisiones excluyen las notas de Vesupronta en todas sus consultas. Revisar `seller.is_pro_venta_only` con zonas Vesupronta.
- Hecho cuando: Comisiones no cambia al agregar notas y pagos de Vesupronta.
- Cómo probarla: prueba de integración contra testing.
- [ ] Hecha y probada

## Tarea 11 - Pestaña Vesupronta con sub-pestañas
Cubre: RF-3, RF-12
- Reemplazar "Modulo en preparacion." por un control con tres sub-pestañas (Ventas, Cuentas por Cobrar, Pagos) que usan las mismas vistas con view models de alcance Vesupronta. Sin sub-pestaña de Notas de Entrega ni de Comisiones.
- Hecho cuando: la pestaña abre sin errores y vacía en una base sin notas de Vesupronta. Sin sub-pestaña de Notas de Crédito (decisión 2026-11-19).
- Cómo probarla: abrir la app en testing y recorrer las tres sub-pestañas.
- [ ] Hecha y probada

## Tarea 12 - Impresión igual a General
Cubre: RF-10
- PDF, Excel e impresión tratan a Vesupronta como General (título y colores). La nota queda congelada.
- Hecho cuando: una nota de Vesupronta se imprime igual que una General, en tamaño carta, y la reimpresión sale idéntica.
- Cómo probarla: emitir, exportar y reimprimir una nota de Vesupronta en testing.
- [ ] Hecha y probada

## Tarea 13 - Registro de notas sin empresa clara
Cubre: RF-13
- Las consultas que encuentren un código desconocido lo dejan en el log (sin datos del cliente) y no lo cuentan en Vesupronta.
- Hecho cuando: una nota con tipo desconocido no aparece en Vesupronta y queda registrada en el log.
- Cómo probarla: prueba unitaria.
- [ ] Hecha y probada

## Tarea 14 (manual) - Prueba completa en testing
Cubre: todos
- Comparar los totales de General y Pro Venta antes y después; crear zona y cliente Vesupronta, emitir la nota desde la pestaña Notas de Entrega, pagar, hacer nota de crédito y revisar stock, correlativo y que nada se mezcle; pasar una zona con notas a Vesupronta y confirmar que sus notas viejas no cambian de empresa.
- Hecho cuando: todo lo de "Cómo sé que está listo" pasa.
- [ ] Hecha y probada

## Tarea 15 (manual) - Salida a producción
Cubre: RF-11
- Con respaldo previo: publicar la versión, subir la versión mínima a esa versión, correr el script de tipos de nota en producción y recién después crear la primera zona Vesupronta.
- Hecho cuando: ninguna PC con versión vieja puede ver notas de Vesupronta.
- [ ] Hecha y probada
