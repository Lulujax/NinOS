# Plan 009 - Correcciones menores

Cómo cumplir [spec.md](spec.md). Todavía no se ha tocado código.

## Qué hay hoy (verificado)

- [AddCreditNoteWindow.xaml.cs:1084](../../src/NinOS.UI/Views/AddCreditNoteWindow.xaml.cs#L1084) `SaveAsync` no desactiva el botón; `DeliveryNotesViewModel.execute_save_note` tampoco tiene protección (las ventanas de pagos y comisiones sí desactivan su botón).
- [NinOSDbContext.cs:316](../../src/NinOS.Infrastructure/data/NinOSDbContext.cs#L316): `commission.commission_percentage` con precisión (18,2); la de la vendedora ya es (18,4).
- [DeliveryNotesViewModel.cs `recalculate_total`](../../src/NinOS.UI/Common/ViewModels/DeliveryNotesViewModel.cs#L1894) aplica condición y volumen en cascada, y guarda `discount_percentage = cond + vol` (suma): los dos no coinciden. El cálculo de notas de crédito ya suma.
- `document_status` del kardex se escribe una vez al crear el movimiento y no se actualiza.
- Mensajes "Elimine primero los pagos" en [AccountsReceivableService.cs:751,775](../../src/NinOS.Infrastructure/Services/Implementations/AccountsReceivableService.cs#L751) y [AccountsReceivableView.xaml.cs:60](../../src/NinOS.UI/Views/AccountsReceivableView.xaml.cs#L60).
- Pestañas por vendedora: el `TabControl` ya usa `seller_tabs` (dinámico), pero hay colecciones `sandra_notes`, `anais_notes`, `juan_luis_notes` con nombres fijos en cinco ViewModels que parecen código muerto.
- `CustomerViewModel` trae `SellerOptions` y un valor por defecto `"Anais"`; `AddCustomerWindow.xaml` no muestra selector de vendedora, así que un cliente nuevo queda con vendedora "Anais" sin que nadie la elija.
- [CommissionService.cs](../../src/NinOS.Infrastructure/Services/Implementations/CommissionService.cs) y `CommissionsViewModel.cs` tienen 3 líneas en blanco entre cada línea de código.
- Constantes escritas en el código: `"MARACAY"`, 0,10 / 0,25 / 0,15 de Pro Venta, vendedora "001/Sandra" para obsequios.

## Enfoque

1. **Doble clic:** un indicador `_guardando` en cada acción de guardar; se activa al entrar y se libera en `finally`. Se revisan las demás ventanas con la misma lista.
2. **Precisión de comisión** a (18,4) por migración (solo ampliar; no pierde datos).
3. **Descuentos suman:** una función pura `note_discount_calculator` en `NinOS.Domain`: base = bruto − promoción; condición = base × cond%; volumen = base × vol%; total = base − condición − volumen. La usan la pantalla de la nota, la vista previa y el PDF. Lo guardado ya es `cond + vol` y se mantiene.
4. **Estado del kardex en vivo:** al consultar el historial de producto se muestra el estado actual de la nota o nota de crédito (join) y no el texto guardado al crear el movimiento. No se reescriben filas históricas.
5. **Textos:** "No se puede anular una nota con pagos registrados." (sin mencionar eliminar).
6. **Código muerto:** verificar con búsqueda que `sandra_notes` y similares no están en ningún XAML y borrarlos; las pestañas siguen saliendo de la lista de vendedoras.
7. **Cliente sin vendedora:** los clientes son un maestro general con zona. `seller_name` es un resto del sistema anterior (en testing: 130 "Anais", 117 "Juan Luis", 55 vacíos). Se quita el valor por defecto "Anais" de `CustomerViewModel`, se deja de guardar en clientes nuevos y se elimina el uso de `seller_name` en `CustomerService` para actualizar `last_customer_number` de la vendedora (ya no aplica: los códigos de cliente son globales de 5 dígitos). La columna no se borra ni se cambian datos viejos.
8. **Constantes** con nombre en un solo lugar (`ProVentaRates`, `AppDefaults`).
9. **Niveles de log:** `AppLog` lee un nivel mínimo de la variable `NINOS_LOG_LEVEL` (Info por defecto) para poder subirlo o bajarlo sin recompilar.
10. **Formato:** normalizar saltos de línea de los dos archivos de comisiones, comprobando que el diff sin espacios queda vacío.

## Opciones descartadas

- **Probar solo a mano:** descartado. El CLAUDE.md personal de Luis exige prueba unitaria por función con lógica relevante, y sin ellas no se puede avanzar sin pararse en cada tarea.
- **Guardar el estado en el kardex cada vez que cambia la nota:** reescribe historia y duplica lógica; el join no cambia datos.
- **Archivo de configuración para los porcentajes de Pro Venta:** son reglas del negocio estables y confirmadas; una constante es más segura que un archivo editable en cada PC.

## Pruebas (a mano)

Doble clic en cada botón de guardar; comisión 7,5%; nota con 10% + 5% sobre 1.000 → 850 en pantalla, PDF y Excel; nota pagada en el historial de producto; texto de anulación con pagos; crear cliente nuevo y mirar su vendedora; `dotnet build` en 0/0.

## Rama

`feature/correcciones-menores` saliendo de `TESTING`. Una migración (solo ampliar precisión), contra testing y preguntándote antes.
