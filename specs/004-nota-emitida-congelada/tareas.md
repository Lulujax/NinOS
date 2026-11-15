# Tareas 004 - La nota emitida queda congelada

## Tarea 1 - Inventario de lo que muestran PDF, Excel y pantallas
Cubre: RF-1, RF-6
- Sin cambiar código, listar en `specs/004-nota-emitida-congelada/inventario.md` cada lugar que muestra datos de una nota o fechas (PDF nota, PDF nota de crédito, Excel, historial de cliente, detalle de nota de crédito, comprobantes) y qué fuente usa cada uno.
- Hecho cuando: el archivo existe y cada fila dice "fecha UTC / local" y "dato vivo / congelado".
- Cómo probarla: revisarlo contigo; no hay nada que ejecutar.
- [x] Hecha y probada — 2026-11-15, cerrada por Claude con autorización de Luis (ver `inventario.md`, secciones 6 y 7).

## Tarea 2 - Migración: columnas de foto en nota y nota de crédito
Cubre: RF-1, RF-7
- Agregar `print_snapshot` (jsonb, nulo) y `print_snapshot_version` (int, nulo) a `delivery_note` y `credit_note`. **Antes:** definir `NINOS_DB_CONNECTION` a testing y preguntarte.
- Hecho cuando: la migración se aplica en testing y los datos existentes quedan intactos.
- Cómo probarla: abrir la app; consulta `select count(*) from delivery_note where print_snapshot is not null` → 0; las notas se siguen viendo.
- [x] Hecha y probada

## Tarea 3 - Guardar y leer la foto (código común)
Cubre: RF-1, RF-11
- Clase en `NinOS.Domain` que convierte `note_print_dto` a JSON y de vuelta, con número de versión y tolerante a campos que falten o sobren (un campo nuevo sale en blanco, uno viejo desconocido se ignora, sin lanzar excepción).
- Hecho cuando: un DTO sale igual después de ir y volver, y un JSON al que le falta un campo se lee sin fallar.
- Cómo probarla: en el debug, reimprimir una nota con foto (tarea 5); además, editar a mano el JSON de una nota de prueba en testing quitándole un campo y reimprimir → sale en blanco, sin error.
- [x] Hecha y probada

## Tarea 4 - Al crear la nota: guardar la foto con número y fecha definitivos
Cubre: RF-1, RF-4, RF-6
- `create_delivery_note_async` arma el documento final dentro de la transacción (fechas en hora de Venezuela, número definitivo) y lo guarda en `print_snapshot`.
- Hecho cuando: una nota nueva tiene foto guardada.
- Cómo probarla: crear una nota; `select print_snapshot is not null from delivery_note order by id_delivery_note desc limit 1` → true.
- [x] Hecha y probada

## Tarea 5 - Reimprimir desde la foto
Cubre: RF-2, RF-3, RF-9
- `get_printable_note_async` usa la foto si existe; si no, usa la ruta actual y deja una línea en el log.
- Hecho cuando: reimprimir sale igual al original aunque cambien producto, cliente o monto en CxC.
- Cómo probarla: nota nueva; cambiar el nombre del producto y la dirección del cliente; editar el monto en CxC; reimprimir → igual que el original. Reimprimir una nota vieja → funciona y el log registra el aviso.
- [x] Hecha y probada

## Tarea 6 - Correlativo definitivo con bloqueo (opción A)
Cubre: RF-5, RF-5b, RF-5c
- El servicio asigna el número dentro de la transacción con `pg_advisory_xact_lock` y devuelve el definitivo; la pantalla sigue mostrando antes solo una previsualización; no se reservan números.
- Hecho cuando: dos copias guardando a la vez nunca producen el mismo número, ni huecos, ni error de duplicado.
- Cómo probarla: abrir dos copias con el mismo número en pantalla y guardar ambas casi juntas; las dos guardan con números consecutivos y sin saltos. Revisar que la serie no tenga huecos: `select note_number from delivery_note order by id_delivery_note desc limit 10`.
- [x] Hecha y probada

## Tarea 7 - Guardar y luego generar PDF/Excel/impresión, avisando el número
Cubre: RF-4, RF-5
- `execute_save_note` genera el PDF, Excel o impresión con el documento devuelto por el servicio; el mensaje final dice el número y, si cambió, explica por qué.
- Hecho cuando: el PDF lleva el mismo número que la lista y se avisa cuando cambió.
- Cómo probarla: repetir la prueba de la tarea 6 y revisar el PDF de la segunda copia.
- [x] Hecha y probada

## Tarea 8 - Fechas en hora de Venezuela en vista previa, PDF y Excel de la nota
Cubre: RF-6
- La vista previa y los exportes de nota muestran fecha y vencimiento convertidos.
- Hecho cuando: una nota creada después de las 8 pm muestra el mismo día en vista previa, PDF, Excel y lista.
- Cómo probarla: cambiar la hora de la PC a 9:00 pm, crear una nota y comparar el día en las cuatro vistas.
- [x] Hecha y probada

## Tarea 9 - Notas de crédito: foto, fecha local y número definitivo
Cubre: RF-7, RF-5
- Igual que 4, 5 y 6 para notas de crédito (el número se asigna siempre en el servicio).
- Hecho cuando: reimprimir una nota de crédito sale igual y su fecha es la de Venezuela.
- Cómo probarla: crear una nota de crédito después de las 8 pm; cambiar el nombre del producto; reimprimir.
- [x] Hecha y probada

## Tarea 10 - Fechas locales en historial de cliente y demás pantallas
Cubre: RF-6
- Convertidor XAML y conversión en PDF/Excel del historial de cliente, detalle de nota de crédito y comprobantes que listó la tarea 1.
- Hecho cuando: ninguna fecha de la lista cambia de día a partir de las 8 pm.
- Cómo probarla: con la nota de la tarea 8, abrir historial del cliente (ventana, PDF y Excel) y comparar.
- [x] Hecha y probada

## Tarea 11 - Revisión final: carta y consistencia
Cubre: RF-8, RF-2
- Generar un PDF de cada tipo y comprobar tamaño carta; recorrer la lista de la tarea 1.
- Hecho cuando: todos los PDF son carta y la tabla del inventario queda toda en "local / congelado" (salvo notas viejas).
- Cómo probarla: abrir cada PDF y ver propiedades del documento.
- [x] Hecha y probada — 2026-11-15, cerrada por Claude con autorización de Luis (ver `inventario.md`, secciones 6 y 7).
