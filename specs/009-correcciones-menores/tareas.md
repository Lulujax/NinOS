# Tareas 009 - Correcciones menores

## Tarea 1 - Doble clic al guardar notas de crédito y notas de entrega
Cubre: RF-1
- Indicador de "guardando" en `AddCreditNoteWindow.SaveAsync` y en `execute_save_note`; revisar y listar las demás ventanas que registran dinero o stock.
- Hecho cuando: pulsar dos veces rápido crea un solo registro.
- Cómo probarla: nota de crédito de 1 unidad con doble clic → una sola nota y el stock sube 1; lo mismo con una nota de entrega.
- [x] Hecha y probada

## Tarea 2 - Precisión de la comisión
Cubre: RF-2
- Migración que amplía `commission.commission_percentage` a (18,4). **Antes:** `NINOS_DB_CONNECTION` a testing y preguntarte.
- Hecho cuando: una comisión con 7,5% se guarda y se muestra 7,5%.
- Cómo probarla: poner 7,5% a una vendedora de prueba, pagar una nota y ver la comisión (7,50 sobre 100); restaurar el 10%.
- [x] Hecha y probada

## Tarea 3 - Descuento de volumen suma (función única)
Cubre: RF-3, RF-4
- `note_discount_calculator` en `NinOS.Domain`; la usan `recalculate_total`, la vista previa y el PDF; se comprueba que lo guardado sigue siendo condición + volumen.
- Hecho cuando: nota de 1.000 con 10% de condición y 5% de volumen da 850 en pantalla, PDF y Excel.
- Cómo probarla: crear una nota tipo Volumen con esos porcentajes y mirar vista previa, PDF y la fila en CxC (DCTO 15%).
- [x] Hecha y probada

## Tarea 4 - Estado vivo en el historial de producto
Cubre: RF-5
- La consulta del historial de producto muestra el estado actual de la nota o nota de crédito.
- Hecho cuando: una nota pagada ya no aparece como "Pendiente" en el historial.
- Cómo probarla: pagar una nota y abrir el historial de uno de sus productos.
- [x] Hecha y probada

## Tarea 5 - Textos de anulación
Cubre: RF-6
- Cambiar los tres mensajes que mencionan eliminar pagos.
- Hecho cuando: ya no se menciona "eliminar pagos" en ningún mensaje.
- Cómo probarla: intentar anular una nota con pagos y una nota Pro Venta con pagos MAR; buscar el texto en el código (`grep "Elimine"` sin resultados).
- [x] Hecha y probada

## Tarea 6 - Quitar colecciones con nombres fijos
Cubre: RF-7
- Confirmar con búsqueda que `sandra_notes`, `anais_notes`, `juan_luis_notes` (y equivalentes) no se usan en XAML y eliminarlas de los ViewModels.
- Hecho cuando: build en 0/0 y las pestañas por vendedora de Ventas, Cobranzas, Pagos, Comisiones y Notas de crédito siguen igual.
- Cómo probarla: abrir cada pantalla y comprobar que aparece la pestaña de cada vendedora activa, incluida la 005.
- [x] Hecha y probada

## Tarea 7 - Clientes sin vendedora (solo zona)
Cubre: RF-12, RF-13
- Quitar el "Anais" por defecto, dejar de guardar `seller_name` en clientes nuevos y quitar su uso para el correlativo de vendedora; revisar que ninguna pantalla agrupe clientes por ese campo.
- Hecho cuando: un cliente nuevo queda con `seller_name` vacío y la pantalla de clientes sigue funcionando igual.
- Cómo probarla: crear un cliente nuevo con una zona; `select seller_name from customer order by id_customer desc limit 1` → vacío; emitir una nota a ese cliente con una vendedora de esa zona; los 247 clientes antiguos siguen igual.
- [x] Hecha y probada

## Tarea 8 - Constantes en un solo lugar
Cubre: RF-8
- Centralizar ciudad, porcentajes de Pro Venta y vendedora administrativa de obsequios.
- Hecho cuando: buscar `"MARACAY"` y `0.25m` en el código solo encuentra las constantes.
- Cómo probarla: `grep` de esos valores; un obsequio sigue funcionando.
- [x] Hecha y probada

## Tarea 9 - Nivel de log configurable
Cubre: RF-9
- `AppLog` lee `NINOS_LOG_LEVEL` (Info por defecto).
- Hecho cuando: con `Warn` el log deja de mostrar mensajes de flujo normal.
- Cómo probarla: abrir con la variable en `Warn` y ver `ninos-ui.log`; sin variable vuelve a todo.
- [x] Hecha y probada

## Tarea 10 - Formato de los archivos de comisiones
Cubre: RF-10
- Normalizar saltos de línea de `CommissionService.cs` y `CommissionsViewModel.cs`.
- Hecho cuando: `git diff -w --ignore-blank-lines` no muestra cambios de contenido y el build queda en 0/0.
- Cómo probarla: ese comando; abrir Comisiones y pagar una comisión de prueba.
- [x] Hecha y probada

## Tarea 11 - Vista previa de la nota de crédito
Cubre: RF-14
- Botón "Vista previa" en `AddCreditNoteWindow` que arma el documento con los datos de la pantalla, sin guardar, usando el mismo generador del PDF final y mostrándolo con `PdfPreviewWindow`.
- Hecho cuando: la vista previa muestra la nota con la misma información que el PDF; no se crea ningún registro en la base.
- Cómo probarla: llenar una nota de crédito, pulsar "Vista previa", revisar que muestra los productos y el total; luego cerrar sin guardar y comprobar en Notas de crédito que no apareció nada nuevo.
- [x] Hecha y probada

## Tarea 12 - Excel de la nota de crédito
Cubre: RF-15
- Reutilizar `NoteExcelExporter.export` (ya genera la misma estructura y pone "NOTA DE CREDITO" en la columna DOCUMENTO), conectado al botón Excel de la vista previa. No se crea un exportador nuevo.
- Hecho cuando: el Excel abre con cliente, productos devueltos, cantidades, precio, subtotal y total, igual que el de una nota de entrega.
- Cómo probarla: vista previa de una nota de crédito con dos productos → Excel; comparar con una nota de entrega en Excel.
- [x] Hecha y probada

## Tarea 13 - Combobox del año en las cinco pantallas
Cubre: RF-14, RF-15
- Cambiar el año en Ventas, Cuentas por Cobrar, Pagos, Comisiones y Notas de Crédito no deja el combobox vacío.
- Hecho cuando: en las cinco pantallas el año se elige y se muestra bien.
- [x] Hecha y probada — 2026-11-16, confirmada por Luis en las cinco pantallas.
