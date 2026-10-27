# Listas de precios múltiples (N listas con nombre, heredando el precio original)

## Concepto
- El usuario crea **cuantas listas quiera** (20, 50…), cada una con nombre libre ("Mayoreo", "Promo verano", "Cliente X"...).
- Un producto tiene su **precio original** (`product.unit_price_usd`, el de hoy) y opcionalmente un precio propio **por lista**. Si no tiene precio en una lista, **esa lista usa el precio original** (herencia). Para volver a heredar se limpia el campo.
- **En la nota, por renglón**: hoy el ciclo natural es buscar el producto y agregarlo; se agrega una **columna "Lista"** en el grid de ítems de la nota (solo visible en la ventana de creación — **nunca se imprime** en el PDF de la nota) donde se elige de qué lista toma referencia el precio de ese producto. Por defecto: "Original".
- Sin listas creadas, el sistema funciona exactamente igual que hoy.
- Las notas históricas quedan protegidas: `note_detail.unit_price_usd` ya guarda el snapshot por línea.

## 1. Base de datos (EF Core + SQL espejo)
- Tabla nueva `price_list`: `id_price_list`, `name` (único, obligatorio), `is_active`, `sort_order`. **Sin seed** — arranca vacía.
- Tabla nueva `product_price`: `id_product_price`, `id_product` (FK), `id_price_list` (FK), `unit_price_usd` (18,2), índice único `(id_product, id_price_list)`. **Ausencia de fila = hereda el original**.
- `note_detail.id_price_list` (nullable, FK): qué lista de referencia usó cada renglón (null = original). Trazabilidad sin cambiar la nota cabecera ni el flujo actual.
- Migración EF `AddPriceLists` + script espejo en `migrations/` (práctica existente del repo).

## 2. Dominio y servicios (`NinOS.Domain` / `NinOS.Infrastructure`)
- Entidades nuevas: `price_list.cs`, `product_price.cs`; mapeo en `NinOSDbContext.cs`.
- Resolución de herencia centralizada en un helper del dominio: precio efectivo = precio propio en la lista ?? precio original.
- `IPriceListService` + implementación: CRUD de listas, listar activas, guardar/quitar precio de un producto en una lista.
- `DeliveryNoteService`: persistir `id_price_list` por renglón al guardar la nota (los montos ya viajan snapshot; no cambia el cálculo).

## 3. Gestión de listas (Admin Panel)
- Ventana nueva `EditPriceListsWindow` siguiendo el patrón de `EditZonasWindow`: crear, renombrar, activar/desactivar.
- Botón "Gestionar listas de precios" en `AdminPanelWindow.xaml` junto a "Editar Zonas" / "Editar Líneas de Producto".
- Desactivar una lista no borra sus precios; deja de ofrecerse en notas y en el producto. Las notas viejas conservan su snapshot.

## 4. Precios por producto — ventana de producto (sin tocar el grid)
- `AddProductWindow.xaml`: sección **"Precios por lista"** (ItemsControl con scroll): una fila por lista activa con `TextBox` numérico; vacío con placeholder "(hereda el precio general)".
- `InventoryViewModel`: leer/guardar esos precios vía `IPriceListService`; en edición se cargan **solo los precios propios** (vacío sigue significando heredar).
- **Grid de inventario: sin cambios** (muestra el precio original como hoy) — no se corrompe con columnas dinámicas.

## 5. Nota — columna "Lista" por renglón (uso interno, no se imprime)
- `DeliveryNotesView.xaml`: columna nueva en el grid de ítems con un **ComboBox por renglón** ("Original" + listas activas), ubicada junto a "precio unitario".
- `billable_item`: trae el precio original + `Dictionary<int, decimal>` con el precio efectivo por lista (resuelto con herencia, cargado en `refresh_data`).
- `note_detail_row`: propiedad `selected_price_list_id` (null = Original) + colección de listas compartida; al cambiar la lista del renglón se re-toma el precio del producto en esa lista (acción explícita del usuario). Al seleccionar el producto: precio = precio en la lista vigente del renglón (por defecto Original).
- **Renglones de promoción**: el combo queda deshabilitado ("—") — las promociones conservan su propio precio, como hoy.
- El precio por renglón sigue siendo editable (comportamiento actual intacto).
- **No se imprime**: `NotePdfGenerator`, `NotePreviewWindow` y `note_print_dto` no cambian — la columna vive solo en la ventana de creación.

## 6. PDF de lista de precios
- El popup actual de marcas/categorías (`InventoryView.xaml`) gana una sección de **selección de listas** (checkboxes; por defecto solo la original).
- `PriceListPdfGenerator`: una columna por lista seleccionada, encabezado con el nombre de la lista, celda con el precio efectivo (heredado incluido).
- `CurrentInventoryPdfGenerator` y kardex: **sin cambios** (valorización de stock, base = precio original).

## 7. Verificación
- `dotnet build` de la solución.
- Migración aplicada en la BD de testing; prueba end-to-end: crear 2 listas → producto con precio propio en una y heredado en otra → nota con renglón en Original → cambiar la lista del renglón y verificar el precio → PDF de nota SIN la columna → PDF de lista de precios con 1 y varias listas → nota vieja sigue igual.
- Actualización breve de `docs/manual-usuario.md` (secciones Inventario y Notas).