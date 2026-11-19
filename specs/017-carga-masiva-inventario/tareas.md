# Tareas — 017 Carga masiva de inventario y precios por Excel

Rama: `TESTING`. Cada tarea compila con 0 advertencias, sin comentarios en el código y con logs `AppLog` sin datos de clientes. Las pruebas que toquen la base solo corren contra testing.

Antes de empezar: revisar y aprobar `plan.md` (pendiente, modo plan).

## 1. Validación por fila de Cantidad (dominio, sin base)
- Cubre: RF-12, RF-18, RF-19, RF-22, RF-23, RF-27
- Hecho cuando: existen las piezas puras que, dada una fila (ID, código, stock del archivo, cantidad) y el stock actual, calculan stock final, diferencia y estado (cambia, igual, error con motivo, aviso) para Conteo y Ajuste. Pruebas: caso feliz de cada tipo, límites (cantidad 0, stock que queda exactamente en 0, fila vacía) y errores (negativo en Conteo, resultado negativo en Ajuste, duplicados, código que no coincide).
- [ ] Hecha y probada

## 2. Validación por fila de Precio (dominio, sin base)
- Cubre: RF-13, RF-18, RF-18b, RF-19, RF-22, RF-24, RF-25, RF-27
- Hecho cuando: existen las piezas puras que, dada una fila (ID, código, precio del archivo, precio nuevo) y el precio actual, calculan precio final redondeado, diferencia, % y estado. Pruebas: caso feliz, límites (5.5000001 → 5.50, exactamente +50% sin aviso, 50.01% con aviso, mismo precio = igual, 0 = igual y conserva el precio, 0.004 redondea a 0 = igual) y errores (negativo, letras, duplicados).
- [ ] Hecha y probada

## 3. Generar las plantillas Excel
- Cubre: RF-3, RF-4, RF-5, RF-6, RF-7, RF-8, RF-9
- Hecho cuando: un generador con ClosedXML produce las tres plantillas (Cantidad, Precio Principal, Precio lista secundaria) con sus columnas, la hoja oculta con marca, versión, tipo, lista y fecha, el título y la instrucción, y solo la columna editable desbloqueada. Pruebas: genera y relee cada tipo (con productos y sin productos), verifica bloqueo y marca.
- [ ] Hecha y probada

## 4. Leer las plantillas Excel
- Cubre: RF-11, RF-14, RF-15, RF-17, RF-18
- Hecho cuando: un lector reconoce el tipo y la lista por la marca, devuelve las filas con su número de fila de Excel y rechaza con mensaje claro los archivos sin marca o de versión incompatible, dañados o bloqueados por Excel. Pruebas: cada tipo válido, plantilla sin cambios, Excel ajeno, archivo dañado, celda con letras.
- [ ] Hecha y probada

## 5. Servicio: vista previa contra la base
- Cubre: RF-16, RF-19, RF-20, RF-22, RF-26, RF-28
- Hecho cuando: un servicio recibe las filas leídas y devuelve la vista previa con resumen, errores, avisos y, en la Principal, cuántos productos cambian también en otras listas, sin modificar nada. Pruebas contra testing: producto eliminado, ID inexistente, lista desactivada, valor cambiado desde la descarga, producto con precio propio y heredado en otra lista.
- [ ] Hecha y probada

## 6. Servicio: aplicar Cantidad en una sola operación
- Cubre: RF-29, RF-30, RF-31, RF-33, RF-35
- Hecho cuando: aplica todos los cambios de stock en una sola transacción usando `stock_movement_writer` (fijar o ajustar), con motivo, nota y marca «Carga masiva». Si el stock cambió desde la vista previa o falla un producto, deshace todo. Pruebas contra testing: aplica N productos y el kardex cuadra; un fallo a mitad no deja cambios; un cambio entre vista previa y aplicar bloquea.
- [ ] Hecha y probada

## 7. Servicio: aplicar Precio en una sola operación
- Cubre: RF-30, RF-32, RF-33, RF-36
- Hecho cuando: aplica todos los precios en una sola transacción, en el precio base (Principal) o como precio propio en la lista secundaria, reutilizando la lógica de `PriceListService`. Si un precio cambió desde la vista previa o falla un producto, deshace todo. Pruebas contra testing: Principal (cambia en listas sin precio propio), secundaria (la Principal no cambia), fallo a mitad sin cambios, nota emitida antes sigue igual.
- [ ] Hecha y probada

## 8. Ventana Descargar plantilla
- Cubre: RF-1, RF-2, RF-10, RF-37
- Hecho cuando: el botón en Inventario abre la ventana con «¿Qué desea cambiar?», muestra «¿Qué lista?» solo en Precio (Principal por defecto, solo listas activas), propone el nombre del archivo, guarda y deja el log.
- [ ] Hecha y probada

## 9. Botón Cargar plantilla y ventana de vista previa
- Cubre: RF-11, RF-12, RF-20, RF-21, RF-22, RF-25, RF-26, RF-27, RF-28, RF-37
- Hecho cuando: el botón pide el archivo, reconoce el tipo, pide Conteo o Ajuste solo en Cantidad, y la vista previa muestra el resumen, las filas en rojo y amarillo con su motivo, el aviso de otras listas, la elección Aplicar igual / Saltar y el botón Aplicar deshabilitado mientras haya errores.
- [ ] Hecha y probada

## 10. Aplicar desde la vista previa
- Cubre: RF-29, RF-30, RF-33, RF-34, RF-37
- Hecho cuando: en Cantidad pide motivo obligatorio y nota opcional; en Precio pide confirmar. Ejecuta la carga, muestra «Se actualizaron N productos» o el error y refresca inventario y precios en pantalla.
- [ ] Hecha y probada

## 11. Prueba completa a mano contra testing
- Cubre: «Cómo sé que está listo»
- Hecho cuando: se recorren todos los puntos de «Cómo sé que está listo» de `spec.md` en la base de testing y la consulta stock vs kardex no da diferencias.
- [ ] Hecha y probada
