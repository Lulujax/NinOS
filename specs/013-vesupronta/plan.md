# Plan 013 - Vesupronta

Cómo cumplir [spec.md](spec.md). Todavía no se ha tocado código.

## Qué hay hoy (verificado en el código)

- La zona solo distingue dos empresas con `zona.is_pro_venta` (sí/no) en [zona.cs](../../src/NinOS.Domain/zona.cs).
- La nota no guarda la zona ni la empresa: guarda `note_type_id`. La empresa se deduce del **código del tipo de nota**: MAR, PVP y VOLMAR son Pro Venta ([NoteTypeCodes.cs](../../src/NinOS.Domain/NoteTypeCodes.cs)); todo lo demás cuenta como General.
- Las pantallas de General filtran por **"no es Pro Venta"**, por ejemplo [SalesNoteScope.cs](../../src/NinOS.Domain/SalesNoteScope.cs). Por eso una nota de Vesupronta aparecería sola dentro de General si no se cambia ese filtro. Es el punto más delicado del plan.
- Los filtros por familia Pro Venta están en: `AccountsReceivableService`, `PaymentService` (incluida la creación de comisión, línea ~586), `CreditNoteService`, `CommissionService`, `DeliveryNoteService`, `ProVentaService`, `DbDiagnostics`, `DbSeeder` y los view models de Notas de Entrega, Ventas, Cuentas por Cobrar, Pagos, Notas de Crédito, Comisiones, Vendedores y Zonas.
- La pestaña Vesupronta ya existe en `MainWindow.xaml` con el texto "Modulo en preparacion.".

## Enfoque

1. **La empresa sale del tipo de nota, como ya pasa con Pro Venta.** Se agregan tres tipos de nota de Vesupronta con códigos propios: `VES` (general), `VESPRM` (promoción) y `VESVOL` (volumen). Usan las mismas reglas de cálculo que GEN, PRM y VOL. Así una nota emitida nunca cambia de empresa, aunque el cliente cambie de zona después.
2. **Regla pura única en el dominio.** Un `business_unit` (General, ProVenta, Vesupronta) y una función `BusinessUnitRules.unit_of_note_type(code)` que diga a qué empresa pertenece un código. Un código vacío o desconocido cuenta como General (así se comportan hoy las notas viejas) y nunca como Vesupronta (RF-13). Todas las pantallas y servicios usan esa función o su lista de códigos, en lugar de "no es Pro Venta".
3. **La zona dice la empresa.** Columna nueva `zona.is_vesupronta` (sí/no, por defecto no) con una restricción que impide que una zona sea Pro Venta y Vesupronta a la vez. Es aditiva: no hay que actualizar ninguna fila existente, todas quedan en "no".
4. **Alcance por pantalla.** Los view models de Ventas, Cuentas por Cobrar, Pagos y Notas de Crédito reciben el alcance (`General` o `Vesupronta`) al crearse. Las pestañas actuales se crean con alcance General; la pestaña Vesupronta crea un segundo juego con alcance Vesupronta. Los servicios reciben el alcance como parámetro y filtran por la lista de códigos de esa empresa.
5. **Notas de Entrega sigue siendo una sola pestaña.** No recibe alcance: al elegir el cliente, ofrece los tipos de nota de la empresa de su zona (General, Pro Venta o Vesupronta), igual que hoy hace con Maracay. El tipo elegido queda guardado en la nota, así que si la zona cambia de empresa después, la nota conserva la suya (RF-15) sin ningún código extra.
6. **Sin comisiones.** `PaymentService` no crea comisión si la nota es de Vesupronta, y `CommissionService` excluye las notas de Vesupronta de todas sus consultas.
7. **Inventario y correlativo compartidos.** No se tocan: el stock y el correlativo global ya son únicos para todas las notas.
8. **Impresión igual a General.** El generador de PDF, Excel e impresión trata a Vesupronta como General (mismo título "NOTA DE ENTREGA" y mismos colores).

## Opciones descartadas

- **Reusar GEN, PRM y VOL y separar por la zona del cliente:** la nota no guarda la zona; si el cliente cambia de zona, sus notas viejas saltarían de empresa. Además, todos los filtros actuales son por código de tipo de nota.
- **Agregar una columna de empresa a cada nota:** habría que llenar esa columna en todas las notas existentes (un UPDATE masivo de datos de negocio), y CLAUDE.md lo prohíbe al arrancar. Además, duplicaría lo que ya dice el tipo de nota.
- **Cambiar `is_pro_venta` por un campo de texto con la empresa:** obliga a convertir los datos existentes y rompe a las versiones viejas que todavía lean `is_pro_venta`. La columna nueva `is_vesupronta` es aditiva.
- **Crear los tipos de nota de Vesupronta con la migración:** la migración corre al abrir la app, y CLAUDE.md prohíbe insertar tipos de nota al arrancar. Se crean con un script SQL supervisado, con respaldo previo, primero en testing y luego en producción.

## Riesgos

- **Versiones viejas abiertas en las PCs:** una versión anterior a esta mostraría las notas de Vesupronta dentro de General, porque para ella "no es Pro Venta" es General. Antes de crear la primera zona Vesupronta en producción hay que subir la **versión mínima** (spec 010) a la versión con Vesupronta.
- **Filtros que se escapen:** si alguna consulta de General sigue usando "no es Pro Venta", Vesupronta se mezcla. Por eso hay una prueba de integración por pantalla (Ventas, Cuentas por Cobrar, Pagos, Notas de Crédito, Comisiones) contra testing, con una nota de cada empresa.
- **Vendedoras:** `seller.is_pro_venta_only` decide cosas por "todas sus zonas son Pro Venta". Hay que revisar que una vendedora con zonas Vesupronta no quede tratada como Pro Venta ni como General por error.

## Decisiones de Luis (2026-11-16)

- Las notas de entrega de las tres empresas se crean desde la pestaña Notas de Entrega actual.
- Una zona puede cambiar de empresa; cada nota conserva la empresa con la que se emitió.
