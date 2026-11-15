# Plan 001 - Ventas sin notas Pro Venta

Este plan describe el cómo de la spec [spec.md](spec.md). Todavía no se ha tocado código.

## Enfoque

Hay un solo punto de verdad para decidir si una nota es Pro Venta: `NoteTypeCodes.is_pro_venta` en [NoteTypeCodes.cs](../../src/NinOS.Domain/NoteTypeCodes.cs). Ventas hoy usa una lista de códigos escrita a mano en [SalesViewModel.cs:842](../../src/NinOS.UI/Common/ViewModels/SalesViewModel.cs:842). El plan es usar el helper del dominio y quitar la lista duplicada.

La regla de visibilidad se extrae a una función pura y pequeña, para poder probarla sin WPF ni base de datos.

## Cambios

1. **Regla pura en el dominio.** Nueva clase estática en `src/NinOS.Domain` (por ejemplo `SalesNoteScope`) con `is_visible_in_sales(string? note_type_code)`. Devuelve `false` si `NoteTypeCodes.is_pro_venta` es verdadero. Las notas sin tipo siguen siendo visibles (RF-7).
2. **Filtro de la pestaña Todos.** En `apply_filters` ([SalesViewModel.cs:714](../../src/NinOS.UI/Common/ViewModels/SalesViewModel.cs:714)), aplicar la regla antes de llenar `all_notes` y las pestañas. Así la pestaña Todos, la meta y los totales usan la misma lista (RF-1, RF-6).
3. **Filtro del reporte.** En `build_sales_report_dto_async` ([SalesViewModel.cs:982](../../src/NinOS.UI/Common/ViewModels/SalesViewModel.cs:982)), aplicar la regla a `month_rows` antes de los demás filtros. Así el reporte y sus PDF y Excel quedan sin Pro Venta aunque el combo esté en "Todas" (RF-2, RF-3).
4. **Lista de vendedores del reporte.** En `load_all_async` ([SalesViewModel.cs:604-630](../../src/NinOS.UI/Common/ViewModels/SalesViewModel.cs:604)):
   - Construir la lista de vendedores de las notas solo con notas visibles en Ventas.
   - No agregar vendedores con `is_pro_venta_only` aunque tengan notas (RF-4).
5. **Combo TIPO DE NOTA.** Quitar de `report_type_options` ([SalesViewModel.cs:366-372](../../src/NinOS.UI/Common/ViewModels/SalesViewModel.cs:366)) las opciones "Pro Venta", "Promocion Pro Venta" y "Volumen Pro Venta" (RF-5). `matches_note_type` puede quedarse para compatibilidad, pero usando `NoteTypeCodes` en vez de códigos escritos a mano.
6. **Limpieza.** Quitar `juan_luis_notes` y su llenado ([SalesViewModel.cs:117](../../src/NinOS.UI/Common/ViewModels/SalesViewModel.cs:117), [355](../../src/NinOS.UI/Common/ViewModels/SalesViewModel.cs:355), [734](../../src/NinOS.UI/Common/ViewModels/SalesViewModel.cs:734)). Verificado: `SalesView.xaml` no la usa.
7. **Meta.** No hay cambio de código. `recalc_totals` ([SalesViewModel.cs:883](../../src/NinOS.UI/Common/ViewModels/SalesViewModel.cs:883)) toma la lista ya filtrada, así que la meta solo cuenta generales.

## Lo que no cambia

- `AccountsReceivableService` y la consulta `get_all_sales_notes_async`: siguen devolviendo todas las notas. El filtro se aplica en la pantalla, no en la base de datos. Así Ventas puede volver a mostrarlas si algún día se decide, sin tocar la capa de datos (RF-8).
- Ningún archivo de migraciones ni de datos.

## Opciones descartadas

- **Filtrar en la consulta SQL de `get_all_sales_notes_async`.** Descartada: afectaría a cualquier otro uso futuro de esa consulta y es más difícil de probar sin base de datos.
- **Filtrar por `is_pro_venta_only` del vendedor.** Descartada: no cumple la regla de negocio de AGENTS.md (cualquier vendedora con zona Pro Venta puede emitir MAR).
- **Dejar las opciones Pro Venta en el combo y solo ocultar notas.** Descartada: la decisión fue quitarlas.

## Pruebas

Las reglas de negocio del proyecto dicen que cada tarea debe tener pruebas unitarias. Hoy **no existe proyecto de pruebas** (CLAUDE.md del proyecto lo confirma).

Hay proyecto de pruebas (`tests/NinOS.Tests`, xUnit). `SalesNoteScope` es una función pura, así que va con pruebas unitarias: MAR, PVP y VOLMAR se ocultan; GEN, PRM, VOL y sin tipo se muestran; código en minúsculas o con espacios; código nulo o vacío.

Lo de la pantalla (pestaña Todos, reporte, PDF y Excel) se prueba a mano en el debug contra testing. Casos a recorrer:
- MAR, PVP y VOLMAR no aparecen; GEN, PRM, VOL y notas sin tipo sí aparecen; nota sin tipo cuenta como General.
- El combo TIPO DE NOTA no contiene las tres opciones Pro Venta.
- Pestaña Todos, reporte, PDF y Excel: sin notas Pro Venta y con el mismo total.

## Verificación final

- `dotnet build NinOS.slnx` con 0 errores y 0 advertencias.
- Prueba manual en la versión debug, conectada a la base de **testing** (`ninos_db_testing`).
- Ver la lista de criterios de "Cómo sé que está listo" en la spec.

## Rama

Se trabaja en una rama que sale de `TESTING` (por ejemplo `feature/ventas-sin-pro-venta`). No se toca `main`. No se cambia `appsettings.json`.
