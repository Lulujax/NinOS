# Plan — 017 Carga masiva de inventario y precios por Excel

Estado: **borrador para aprobar**. Nada de esto está programado.

## Idea general

El trabajo se divide en tres capas, igual que el resto de NinOS:

1. **Dominio (`NinOS.Domain`)**: reglas puras, sin base ni Excel. Reciben filas y valores actuales y dicen qué pasa con cada fila (cambia, igual, error, aviso). Es donde está casi toda la lógica, y se prueba sin base de datos.
2. **Infraestructura (`NinOS.Infrastructure`)**: un servicio nuevo que lee los valores actuales de la base para armar la vista previa y que aplica los cambios en una sola transacción.
3. **UI (`NinOS.UI`)**: el generador y el lector del Excel (ClosedXML ya está en este proyecto, como los exportadores actuales), los dos botones, la ventana de descarga y la ventana de vista previa.

Flujo: Excel → **lector** → filas → **servicio: vista previa** (usa el dominio) → ventana → usuario confirma → **servicio: aplicar** (transacción) → refrescar pantalla.

## 1. Dominio — `src/NinOS.Domain/CargaMasiva/`

- `plantilla_tipo` (enum): `cantidad`, `precio`.
- `tipo_carga_cantidad` (enum): `conteo`, `ajuste`.
- `plantilla_cabecera`: lo que va en la hoja oculta: tipo, `id_price_list` (null en cantidad o en la Principal), nombre de la lista, `version_formato`, `fecha_descarga`. Constructor que valida (precio de lista secundaria exige id de lista; versión mayor que 0).
- `fila_plantilla`: número de fila de Excel, `id_product`, `codigo`, `valor_archivo` (stock o precio de referencia que tenía la plantilla), y lo escrito: `decimal? valor_escrito` o `string? texto_invalido` (cuando la celda tiene letras).
- `resultado_fila`: estado (`cambia`, `igual`, `error`, `aviso`), valor actual, valor final, diferencia, porcentaje (precio), motivo del error o aviso. Inmutable.
- `evaluador_cantidad` (estático, puro): `evaluar(fila, stock_actual, tipo_carga)` aplica RF-19 (entero, negativo en conteo, ajuste 0, resultado negativo), RF-22 (conteo con stock cambiado → aviso), RF-23 (ajuste sin aviso) y RF-27 (igual).
- `evaluador_precio` (estático, puro): `evaluar(fila, precio_actual)` redondea con `Money.round` (RF-24), trata 0 y lo que redondea a 0 como `igual` (RF-18b), negativo o texto como error, cambio de más de 50% como aviso (RF-25) y precio cambiado desde la descarga como aviso (RF-22).
- `vista_previa_carga`: cabecera, tipo de carga, lista de `resultado_fila`, resumen (cambian, errores, avisos, iguales), `productos_afectados_en_otras_listas` (RF-26), `puede_aplicar` (sin errores y con al menos un cambio, RF-21/RF-28).
- `armador_vista_previa` (puro): recibe todas las filas y un diccionario `id_product → (código, valor actual, activo)`; marca duplicados, ID inexistente, eliminado y código que no coincide (RF-19), y llama al evaluador que corresponde.

Pruebas: `tests/NinOS.Tests/Dominio/CargaMasiva*Tests.cs`, con caso feliz, límites y errores de cada evaluador (tareas 1 y 2).

## 2. Excel — `src/NinOS.UI/Common/CargaMasiva/`

- `plantilla_excel_generator.generar(cabecera, filas, ruta)`:
  - Hoja visible "Plantilla": título con tipo y lista (RF-9), instrucción, encabezados y una fila por producto.
  - La columna editable con formato numérico (entero en cantidad, `0.00` en precio) y desbloqueada; el resto bloqueado. La hoja se protege **sin contraseña** permitiendo autofiltro y ordenar (RF-7). No es seguridad, es para evitar accidentes, así que no hace falta guardar una clave.
  - Hoja "_ninos" en estado **muy oculto** (no aparece en "Mostrar hojas" de Excel) con la cabecera en pares clave/valor (RF-8).
- `plantilla_excel_reader.leer(ruta)` → `(plantilla_cabecera, List<fila_plantilla>)`:
  - Abre el archivo en solo lectura compartida, para detectar el caso "abierto en Excel" con un mensaje claro (RF-17).
  - Sin hoja "_ninos", con clave distinta o versión desconocida → excepción con el texto de RF-15.
  - Celda numérica → `valor_escrito`. Celda de texto → `texto_invalido` (la valida el dominio). Celda vacía → la fila se descarta (RF-18).
  - Busca las columnas por el nombre del encabezado, no por la posición.
- Pruebas en `tests/NinOS.Tests/Documentos/PlantillaExcelTests.cs`: generar y volver a leer cada tipo, archivo sin marca, archivo dañado y celda con texto (tareas 3 y 4).

## 3. Servicio — `src/NinOS.Infrastructure/Services/`

Interfaz nueva `ICargaMasivaService` / `CargaMasivaService`, registrada como `AddScoped` en `App.xaml.cs`, junto a las demás:

- `obtener_filas_plantilla_async(plantilla_tipo, int? id_price_list)`: productos activos ordenados por marca y código, con stock, precio de la Principal y precio de la lista (propio o el de la Principal, con la misma regla que ya usa el sistema). Valida que la lista exista y esté activa.
- `generar_vista_previa_async(cabecera, filas, tipo_carga?)`: lee los valores actuales **sin bloquear** y llama a `armador_vista_previa`. Si la lista ya no está activa, lanza el error de RF-16. En la Principal calcula RF-26: cuántos de los productos que cambian no tienen precio propio en al menos una lista secundaria activa. No escribe nada.
- `aplicar_cantidad_async(vista_previa, aplicar_avisos, motivo, nota)` y `aplicar_precio_async(vista_previa, aplicar_avisos)`:
  1. Un scope, un `NinOSDbContext` y una transacción para todo (RF-30).
  2. `stock_movement_writer.reservar_productos_async` con todos los ids, en orden: bloquea esos productos para que nadie los mueva mientras se aplica, sin riesgo de que dos operaciones se queden esperando una a la otra (deadlock).
  3. Vuelve a leer los valores actuales y los compara con los de la vista previa. Si alguno cambió, lanza `carga_cambiada_exception` → rollback → "vuelva a cargar el archivo" (RF-33).
  4. **Cantidad, Conteo**: `stock_movement_writer.fijar_stock_async`, con `stock_visto` = el valor de la vista previa. **Cantidad, Ajuste**: `stock_movement_writer.ajustar_stock_async`. Documento `CONTEO-{código}` o `AJUSTE-{código}`, con el motivo y la nota `"Carga masiva"` más la nota del usuario (RF-31). Es exactamente el mismo camino que el ajuste manual, así que el kardex cuadra (RF-35).
  5. **Precio, Principal**: cambia `product.unit_price_usd` de la entidad cargada. **Precio, secundaria**: inserta o actualiza `product_price`. Ambos con `Money.round` (RF-32).
  6. Si `aplicar_avisos` es falso, salta las filas en amarillo (RF-22).
  7. `SaveChanges` y `Commit`. Ante cualquier excepción, rollback, log y se relanza con el código del producto que falló (RF-30).
- Los precios se escriben con un pequeño ayudante estático nuevo (`price_writer`) que recibe el `db_context`. No se tocan los métodos de `PriceListService`, porque cada uno abre su propio scope y guarda por su cuenta, y eso rompería el "todo o nada".
- Logs `AppLog` en cada paso (RF-37), sin montos ni nombres.
- Pruebas en `tests/NinOS.Tests/Inventario/CargaMasivaServiceTests.cs`, con el mismo guardián de `BaseDeTesting` que `StockConcurrenteTests` (se niega a correr fuera de testing) (tareas 5, 6 y 7).

## 4. UI — `src/NinOS.UI/`

- `InventoryView.xaml`: dos botones nuevos junto a los exportadores actuales: **Descargar plantilla** y **Cargar plantilla**.
- `Views/DescargarPlantillaWindow`: combo "¿Qué desea cambiar?" y combo "¿Qué lista?" (visible solo en Precio, Principal por defecto, solo listas activas). Luego el `SaveFileDialog` con el nombre de RF-10 (tarea 8).
- Cargar: `OpenFileDialog` → lector → en cantidad pregunta Conteo o Ajuste → servicio → `Views/VistaPreviaCargaWindow` + `VistaPreviaCargaViewModel` (tarea 9):
  - Resumen arriba, aviso de otras listas, `DataGrid` de solo lectura con colores por estado (rojo y amarillo), opción "Aplicar igual / Saltar estas filas" y botón Aplicar habilitado solo si `puede_aplicar`.
- Aplicar (tarea 10): en cantidad, se reutiliza el diálogo de motivo y nota del ajuste manual, si lo permite; si no, uno nuevo igual de simple. En precio, una confirmación. Al terminar, mensaje de RF-34 y recarga de productos y precios en `InventoryViewModel`.

## 5. Sin migración

No hace falta cambiar la estructura de la base: se usan `product`, `product_price`, `price_list` y el kardex tal como están. Nada se ejecuta al arrancar la app.

## Opciones descartadas

- **CSV en vez de Excel**: no permite bloquear columnas ni guardar la marca oculta, y en Venezuela Excel usa coma decimal y punto y coma como separador, así que se corrompería con facilidad.
- **Reconocer cada fila por el código en vez del ID**: el código se puede cambiar desde la app; el ID no. El código se usa solo para confirmar.
- **Aplicar fila por fila y dejar las que fallen**: viola "todo o nada" (RF-30) y deja inventarios a medias difíciles de revisar.
- **Usar los métodos actuales de `PriceListService` para guardar**: cada uno hace su propio guardado y no se pueden meter en una sola transacción.
- **Poner el Excel en Infraestructura**: obligaría a agregar ClosedXML a otro proyecto. Los exportadores actuales ya viven en `NinOS.UI/Common` y el proyecto de pruebas ya referencia la UI.
- **Proteger la hoja con contraseña**: sería una clave dentro del código (y el repo es público), y no aporta, porque cualquiera puede quitar la protección de Excel con herramientas comunes. El objetivo es evitar accidentes, no atacantes.
- **Aceptar textos que parecen números ("5,50")**: hay ambigüedad entre "1.000" y "1,000" según la configuración regional. La columna sale con formato numérico, así que Excel guarda números; un texto se marca como error con el número de fila.

## Riesgos y cómo se cubren

- **Error de dedo masivo** → vista previa obligatoria, rojo y amarillo, y aviso de cambio de más de 50%.
- **Ventas entre la descarga y la carga** → aviso amarillo (RF-22).
- **Ventas entre la vista previa y Aplicar** → bloqueo de productos y nueva comparación dentro de la transacción (RF-33).
- **Precio de una lista secundaria editado a mano justo en el mismo segundo de Aplicar**: el bloqueo es sobre el producto, y la edición manual de una lista secundaria no toma ese bloqueo. Gana el último en guardar, igual que hoy entre dos ventanas abiertas. Se acepta porque la ventana de tiempo es de milisegundos y la vista previa ya compara.
- **Muchos productos (cientos)**: una sola transacción con un bloqueo por producto. Para el tamaño del catálogo actual tarda segundos; se mide en la prueba a mano (tarea 11).
- **Nota emitida** → no se toca (spec 004); la tarea 7 lo prueba.
