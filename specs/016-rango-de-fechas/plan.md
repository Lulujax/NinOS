# Plan — Spec 016: filtro Desde / Hasta en todo el sistema

## Contexto

Hoy cada módulo filtra con combos de Año y Mes (más la opción «Todos»). El cliente necesita elegir un día exacto de inicio y otro de fin (ej. 15/01/2026 al 03/12/2026), igual que en el Historial de Clientes. La spec [specs/016-rango-de-fechas/spec.md](specs/016-rango-de-fechas/spec.md) ya está cerrada: DatePicker de WPF, sin atajos, mes actual por defecto, campo vacío = sin límite (los dos vacíos = Todos), título de PDF/Excel con el rango.

Decisiones nuevas tomadas en el plan (se agregan a la spec antes de programar):
- **Meta de Ventas:** se ve y se edita solo si Desde y Hasta caen en el mismo mes; si no, se oculta con «Elija un solo mes para ver la meta».
- **Agregar pago:** la lista de notas muestra **todas las pendientes**, sin importar el rango de Pagos.
- **Reporte de CxC:** el criterio queda en **Normal / Por Zona** + Desde/Hasta propio. «Todas a la fecha» = los dos campos vacíos. Desaparecen los radios Mes y Año.

Hallazgo clave: casi todas las pantallas ya cargan todo en memoria (`_all_notes_source`, `_all_rows_source`...) y filtran con `filter_by_month_and_search(...)`. Por eso el cambio es sobre todo de **UI + filtro en memoria**, sin migraciones ni cambios de base.

## Enfoque

### 1. Pieza de dominio reutilizable (pura, con pruebas) — `src/NinOS.Domain/rango_fechas.cs`

Un `record` inmutable `rango_fechas(DateTime? desde, DateTime? hasta)` con fechas **locales de Venezuela** (solo el día). Se valida a sí mismo en el constructor (desde > hasta → `ArgumentException` con el mensaje de la spec RF-5).

- `static rango_fechas mes_actual(DateTime hoy_local)` → día 1 al último día del mes (RF-2).
- `bool contiene(DateTime utc)` → convierte con `AppTimeZone.to_local` (reuso [AppTimeZone.cs](src/NinOS.Domain/AppTimeZone.cs)) y compara por día completo, extremos incluidos (RF-3); `null` = sin límite (RF-6).
- `bool se_cruza_con(DateTime inicio, DateTime fin)` → relaciones Pro Venta (RF-22).
- `(DateTime? desde_utc, DateTime? hasta_utc_exclusivo) a_limites_utc()` → para las consultas a la base (Pagos).
- `DateTime? mes_unico()` → el mes si Desde y Hasta están llenos y caen en el mismo mes (Meta de Ventas).
- `string texto_titulo()` → «Del dd/MM/aaaa al dd/MM/aaaa», «Desde el inicio hasta el …», «Desde el …», «TODOS LOS PERIODOS» (RF-9).
- `string texto_log()` → para AppLog sin datos sensibles (RF-10).

Pruebas: `tests/NinOS.Tests/Dominio/RangoFechasTests.cs` (feliz, límite: día extremo 23:59 / cruce de año / nota a las 00:30 hora VE que en UTC es otro día, error: desde > hasta).

### 2. Control visual único — `src/NinOS.UI/Controls/RangoFechasPicker.xaml(.cs)`

UserControl con dos `DatePicker` de WPF (Desde / Hasta), cada uno con:
- texto gris por encima cuando está vacío: «Desde el inicio» / «Hasta hoy» (TextBlock superpuesto, visible si `SelectedDate == null`);
- una ✕ pequeña dentro que pone el campo en `null`;
- validación de texto: si el DatePicker no puede leer la fecha escrita (`DateValidationError`) muestra el aviso de RF-6b vía `AppDialog`.

Expone una sola `DependencyProperty` **`Rango`** (`rango_fechas`, TwoWay). Al cambiar cualquiera de los dos campos: si el rango es válido lo publica; si no (desde > hasta) muestra el aviso y **no** lo publica (RF-5). El ViewModel solo ve rangos válidos. El estilo se toma del Historial de Clientes ([CustomerHistoryWindow.xaml:213-218](src/NinOS.UI/Views/CustomerHistoryWindow.xaml#L213)) para que todo se vea igual.

### 3. Patrón por módulo (se repite igual en cada ViewModel)

1. Se agrega `public rango_fechas rango_filtro` (inicia en `rango_fechas.mes_actual(hoy)`), y al cambiar → `AppLog.Info` + reaplicar filtro.
2. `filter_by_month_and_search(source, _selected_month, query)` pasa a `filter_by_range_and_search(source, rango_filtro, query)` usando `rango.contiene(fecha)`. El buscador y las pestañas de vendedora/zona se quedan igual (RF-8).
3. Se eliminan `_selected_year`, `_selected_month`, `available_years`, `available_months`, `update_available_months()`, `parse_selected_month()`.
4. Los `period_label` / `month` que reciben los PDF/Excel pasan a `rango_filtro.texto_titulo()`.
5. En el XAML se cambian `cmb_year` + `cmb_month` por `<controls:RangoFechasPicker Rango="{Binding rango_filtro, Mode=TwoWay}"/>` y se borran sus manejadores en el `.xaml.cs`.

Módulos y la fecha que usa cada uno:

| Módulo | Archivos principales | Fecha |
|---|---|---|
| Ventas | [SalesViewModel.cs](src/NinOS.UI/Common/ViewModels/SalesViewModel.cs), SalesView.xaml | emisión de la nota. **Meta:** `rango.mes_unico()`; null → oculta meta |
| Cuentas por Cobrar | AccountsReceivableViewModel.cs, AccountsReceivableView.xaml | emisión. Reporte: radios Normal / Por Zona + su propio `RangoFechasPicker` |
| Pagos | PaymentsViewModel.cs, PaymentsView.xaml | fecha del pago. Reporte: nuevo `PaymentService.get_payments_by_range_async(rango)` con límites UTC (reemplaza el uso de `get_payments_by_period_async`) |
| Agregar pago | [AddPaymentWindow.xaml.cs:146](src/NinOS.UI/Views/AddPaymentWindow.xaml.cs#L146) | siempre todas las pendientes (`get_notes_by_month_async("")`, que ya devuelve todas) |
| Comisiones | CommissionsViewModel.cs, CommissionsView.xaml | emisión de la nota cobrada |
| Notas de Crédito | CreditNotesViewModel.cs, CreditNotesView.xaml | emisión de la NC. Reporte: el servicio ya trabaja con `from_date/to_date` ([CreditNoteService.cs:1556](src/NinOS.Infrastructure/Services/Implementations/CreditNoteService.cs#L1556)); solo cambia la UI y el título |
| Agregar NC (devolución) | AddCreditNoteWindow.xaml(.cs) | `CmbMonth` → `RangoFechasPicker` (habilitado tras elegir vendedora); filtra en memoria las notas de la vendedora |
| Relación Vesupronta | [relacion_vesupronta.cs](src/NinOS.Domain/relacion_vesupronta.cs), RelacionVesuprontaView.xaml, su VM, PDF y Excel | `relacion_periodo` / `relacion_modo_periodo` se reemplazan por `rango_fechas`; título RF-22d. Se actualizan sus pruebas `RelacionVesuprontaCalculatorTests.cs` |

### 4. Pro Venta — [ProVentaViewModel.cs](src/NinOS.UI/Common/ViewModels/ProVentaViewModel.cs), ProVentaView.xaml

- Se quita `selected_month`, `available_months`, `weeks`, `selected_week`, `refresh_weeks_async`, `sync_relation_with_week` / `sync_week_with_relation` y el combo editable `CmbRelation` con sus eventos de teclado.
- `pro_venta_relation_option` gana `es_hueca` (se llena en `ProVentaService.get_all_relations_async`, [ProVentaService.cs:1161](src/NinOS.Infrastructure/Services/Implementations/ProVentaService.cs#L1161)) y su `label` pasa a `Relación 149 (30/03 - 05/04)`.
- **Reporte Semanal:** `available_relations` = caché filtrada por `!es_hueca && rango.se_cruza_con(week_start, week_end)`, ordenada por número. Combo **no editable**. Al elegir una → `load_report_async` con la semana de esa relación. Sin relaciones → aviso «No hay relaciones en este rango» (RF-20).
- **Pendientes / Pagadas:** `load_pending_async` / `load_paid_async` filtran con `rango.se_cruza_con(...)` en lugar de año/mes (incluye heredadas, RF-18).
- Títulos de los PDF/Excel de Pro Venta (`monthLabel` en líneas ~722-881) → `rango.texto_titulo()`.
- El aviso existente de notas sin relación ([ProVentaService.cs:327](src/NinOS.Infrastructure/Services/Implementations/ProVentaService.cs#L327)) cubre el RF-22b.

### 5. Documentación de specs

- Agregar a la spec 016 las tres decisiones nuevas (Meta, Agregar pago, Reporte CxC).
- Actualizar la spec 015 (RF-2 y RF-3) para que apunten a la 016.
- Copiar este plan a `specs/016-rango-de-fechas/plan.md` y crear `tareas.md`.

## Opciones descartadas

- **Librerías externas** (MaterialDesign, WPF-UI, Syncfusion): suman peso a cada actualización y licencias; el cliente eligió el DatePicker de WPF.
- **Filtrar en la base en cada cambio de fecha:** las pantallas ya cargan todo en memoria; cambiarlo agrega riesgo sin necesidad. Solo el reporte de Pagos consulta a la base y ahí sí se usa el rango con límites UTC.
- **Mantener el combo Año y agregar el rango aparte:** dos filtros de fecha confunden; el rango con campos vacíos ya cubre «Todos».
- **Validar el rango en cada ViewModel:** se centraliza en `rango_fechas` + el control, para que todos los módulos se comporten igual.
- **Semanas del calendario en Pro Venta:** generaban «semanas sin relación»; se usan las relaciones guardadas.

## Tareas (orden, < 30 min cada una)

1. `rango_fechas` + pruebas (RF-2, 3, 5, 6, 7, 9, 22).
2. `RangoFechasPicker` (RF-1, 4, 5, 6, 6b).
3. Ventas + Meta (RF-11).
4. Cuentas por Cobrar lista (RF-12).
5. Reporte CxC Normal / Por Zona (RF-12, 9).
6. Pagos lista + Agregar pago con todas las pendientes (RF-13).
7. Reporte de Pagos + `get_payments_by_range_async` + prueba (RF-13, 9).
8. Comisiones (RF-14).
9. Notas de Crédito lista + reporte (RF-15).
10. Agregar NC (RF-23, 24).
11. Relación Vesupronta dominio + pruebas + UI (RF-16, 22d).
12. Pro Venta: relación con `es_hueca` y nuevo `label` (RF-18).
13. Pro Venta: Reporte Semanal con combo único (RF-17 a 20).
14. Pro Venta: Pendientes y Pagadas + títulos (RF-21, 9).

Cada tarea revisa que no queden comentarios, deja logs con `AppLog` y compila sin advertencias.

## Verificación

- `dotnet build NinOS.slnx` → 0 errores, 0 advertencias.
- `dotnet test NinOS.slnx` → pasan `RangoFechasTests`, `RelacionVesuprontaCalculatorTests` actualizadas y las existentes.
- A mano contra **testing** (`dotnet run --project src/NinOS.UI`), en cada módulo:
  1. Al abrir: Desde = 01/11/2026, Hasta = 30/11/2026.
  2. Rango 15/01–03/12: entran los extremos, no el 14/01 ni el 04/12.
  3. Desde > Hasta → aviso, la tabla no cambia.
  4. ✕ en los dos → se ve todo, textos grises visibles, PDF dice «TODOS LOS PERIODOS».
  5. Escribir 45/13/2026 → aviso de fecha inválida.
  6. PDF / Excel / vista previa con el título del rango y en tamaño carta.
- Casos especiales: Meta solo con un mes; Agregar pago lista todas las pendientes; Pro Venta con rango de abril muestra `Relación N (30/03 - 05/04)` sin poder escribir; Agregar NC filtra tras elegir vendedora.
