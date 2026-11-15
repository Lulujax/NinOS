# Inventario 004 - Qué muestra cada documento y de dónde sale

Tarea 1 de [tareas.md](tareas.md). Cubre RF-1 y RF-6. Hecho solo leyendo el código; no se ejecutó nada ni se cambió código.

## Cómo leerlo

- **Fecha UTC / local:** UTC es la hora guardada en la base. Local es hora de Venezuela (UTC-4) después de pasar por `AppTimeZone.to_local`.
- **Dato vivo / congelado:** congelado es lo que se leyó de la foto (`print_snapshot`) o de columnas guardadas en la nota. Vivo es lo que se vuelve a consultar hoy en producto, cliente, vendedora o tipo de nota.
- La foto es el `note_print_dto` completo (`note_snapshot_store`, versión 1) y se guarda en `delivery_note.print_snapshot` y `credit_note.print_snapshot`.

## 1. De dónde sale el documento (la cadena)

| Momento | Quién arma el `note_print_dto` | Fecha | Dato |
|---|---|---|---|
| Vista previa antes de guardar una nota de entrega (`NotePreviewWindow`) | `DeliveryNotesViewModel.build_preview_dto()` con lo que hay en el formulario | local (`to_local(_creation_date)`, `to_local(_due_date)`) | vivo: es el formulario, aún no hay nota |
| Al guardar una nota de entrega | El mismo `build_preview_dto()` se entrega a `DeliveryNoteService.create_delivery_note_async`, que le pone el número definitivo y lo guarda como foto (`DeliveryNoteService.cs:195-197`) | local | **se congela aquí** (RF-1) |
| PDF/Excel inmediatamente después de guardar | `DeliveryNotesViewModel` vuelve a llamar `build_preview_dto()` después de guardar y antes de limpiar el formulario (`DeliveryNotesViewModel.cs:2168-2205`), con el número ya corregido | local | se arma otra vez desde el formulario, **no lee la foto guardada** (ver hallazgo H1) |
| Reimprimir o exportar una nota de entrega desde CxC, Ventas, Pro Venta o historial de cliente | `AccountsReceivableService.get_printable_note_async` (`:834`) | local (`to_local(note.creation_date)`) | **con foto: congelado.** Sin foto: vivo, con las excepciones de la sección 2 |
| Vista previa antes de guardar una nota de crédito (`AddCreditNoteWindow`, botón "Vista previa") | `CreditNoteService.get_draft_print_credit_note_async` con los datos de la pantalla | local | vivo: aún no hay nota guardada |
| Al guardar una nota de crédito | `CreditNoteService.create_credit_note_async` arma la foto con `build_credit_print_dto` (`CreditNoteService.cs:897-903`) | local (`to_local(new_note.creation_date)`) | **se congela aquí** (RF-7) |
| Reimprimir o exportar una nota de crédito | `CreditNoteService.get_printable_credit_note_async` (`:1243`) y, para la nota de entrega original, `get_printable_note_async` | local | **con foto: congelado.** Sin foto: vivo |
| Impresión desde el botón Imprimir | `PdfPreviewWindow.BtnPrint_Click` abre el diálogo de impresión de Windows sobre el mismo PDF que genera `NotePdfGenerator` | igual al PDF | igual al PDF |

La foto de la nota de entrega **no trae** `id_delivery_note`, `status`, `paid_amount_usd` ni `balance_due_usd` (quedan en 0 o vacío). Ni el PDF, ni el Excel, ni la vista previa los leen (verificado por búsqueda).

## 2. Nota de entrega: PDF (`NotePdfGenerator`) y Excel (`NoteExcelExporter`)

El PDF y el Excel reciben el mismo `note_print_dto`. Si la nota tiene foto, todo lo de esta tabla sale congelado. La columna "Sin foto" dice de dónde sale cada dato en las notas emitidas antes de la foto (RF-9).

| Campo | PDF | Excel | De dónde viene si hay foto | Sin foto (nota vieja) | Fecha |
|---|---|---|---|---|---|
| Encabezado (`header_title`, o `company_name`) | arriba izquierda (`:169`) | no | congelado | vivo: `note_type.header_title` | |
| Tipo de documento (`document_label`) | arriba derecha (`:175`) | columna DOCUMENTO | congelado | vivo: tipo de nota (`NOTA DE DESPACHO` si es Pro Venta) | |
| Número (`note_number`) | "Nro" (`:182`) y pie (`:554`) | NRO NOTA | congelado, con el número definitivo | columna `delivery_note.note_number` | |
| Vendedora (`seller_name`) | "Vendedor" (`:181`) | VENDEDOR | congelado | vivo: `seller.full_name` | |
| Nombre del cliente | `:197` | CLIENTE | congelado | vivo: `customer.business_name` | |
| Código del cliente | `:202` | no | congelado | vivo: `customer.customer_code` | |
| RIF | `:207` | RIF | congelado | vivo: `customer.rif` | |
| Dirección de entrega | `:223` | no | congelado | vivo: `customer.effective_delivery_address` | |
| Dirección fiscal | `:218` | no | congelado | vivo: `customer.fiscal_address` | |
| Teléfono | `:263` | no | congelado | vivo: `customer.phone_number` | |
| Contacto | `:277` | no | congelado | vivo: `customer.contact_name` | |
| Fecha de emisión (`creation_date`) | `:234`, `dd/MM/yyyy` | FECHA | congelado | `delivery_note.creation_date` convertida | local |
| Días de crédito (`credit_days_text`) | `:245`; no sale en promoción ni en nota de crédito | no | congelado | columna `delivery_note.credit_days` (congelada); si falta, 21 | |
| Vencimiento (`due_date`) | `:257`; mismo criterio que los días | no | congelado | columna `delivery_note.due_date` (congelada); si falta, emisión + días | local |
| Productos: código | tabla (`:316`) | CODIGO | congelado | `note_detail.product_code_snapshot` (congelado); si falta, `product.product_code` vivo. Promoción: `promotion.promotion_code` vivo | |
| Productos: nombre | `:317` | PRODUCTO | congelado | **vivo:** `product.name` o `promotion.name` | |
| Cantidad, precio unitario, subtotal | `:315,318,322` | CANTIDAD, PRECIO ($), TOTAL ($) | congelado | `note_detail` (cantidad, precio y subtotal guardados al emitir) | |
| Descuento y precio promocional por renglón | `:320-321` (solo promoción) | DESCUENTO ($) | congelado | calculado con los valores de `note_detail` | |
| Condiciones y condiciones del descuento | `:333`, `:428` | no | congelado | columnas `conditions_text` y `discount_conditions_text` (congeladas); si faltan, plantilla del tipo de nota (viva) | |
| Datos de pago (transferencia, cuenta, móvil) | `:365-399` | no | congelado | columnas `payment_*_text` (congeladas) | |
| Descuento %, descuento monto, bruto, total con descuento | `:435-456` | no (solo total) | congelado | `original_discount_percentage` (congelado), recalculado | |
| Descuento por volumen (solo tipo Volumen) | `:513-539` | no | congelado | `original_volume_discount_percentage` (congelado); si el tipo de nota es vivo, `es_volumen` se lee del tipo de nota hoy | |
| Total de la nota (`total_amount_usd`) | `:345`, `:539` | TOTAL NOTA ($) | congelado | recalculado desde las columnas originales (no desde el monto editado en CxC) | |
| Color del tipo de nota (`accent_color`) | `:135` | no | congelado | vivo: tipo de nota | |
| Pie "Impreso" | `:567` | no | **hora de imprimir, a propósito** | igual | local (`to_local(DateTime.UtcNow)`) |
| Tamaño de página | `PageSizes.Letter` (`:151`) | no aplica | carta | carta | |

No se imprimen ni se exportan: `promo_banner_text`, `payment_header_text`, `status`, `paid_amount_usd`, `balance_due_usd`, `id_delivery_note`, `total_con_descuento_volumen`, `total_credito_volumen`.

El Excel con dos notas (`export(original, copy)`) crea las hojas "Original" y "Copia" con las mismas columnas.

## 3. Nota de crédito: PDF y Excel

Mismo `NotePdfGenerator` y `NoteExcelExporter`, con `document_label = "NOTA DE CREDITO"`. Se imprime sola o junto con la nota de entrega original (dos páginas en el mismo PDF).

| Campo | De dónde viene si hay foto | Sin foto (nota vieja) | Fecha |
|---|---|---|---|
| Número y estado | congelado | columnas de `credit_note` | |
| Fecha (`creation_date`) | congelado | `credit_note.creation_date` convertida | local |
| Vendedora | congelado | vivo: `seller.full_name` | |
| Datos del cliente (nombre, código, RIF, teléfono, contacto, direcciones) | congelado | **vivo:** `customer` | |
| Encabezado (`header_title`) | congelado | vivo: tipo de la nota de entrega original | |
| Productos: código | congelado | `credit_note_detail.product_code_snapshot`; si falta, `product.product_code` vivo | |
| Productos: nombre | congelado | **vivo:** `product.name` / `promotion.name` | |
| Cantidad, precio, subtotal, total | congelado | guardados en `credit_note_detail` | |
| Texto de condiciones | congelado | texto fijo armado con el número de la nota original (`DEVOLUCION DE PRODUCTOS DE LA NOTA ...`) o texto de obsequio | |
| Datos de pago | congelado | **vivo:** los de la nota de entrega original | |
| Etiqueta de total | `TOTAL DEVUELTO` o `TOTAL OBSEQUIADO` según el texto de condiciones | igual | |
| Vencimiento y días de crédito | no se imprimen en nota de crédito | igual | |

La nota de entrega original que acompaña a la nota de crédito se lee con `get_printable_note_async`, así que sigue las reglas de la sección 2.

## 4. Pantallas y otros lugares que muestran datos o fechas de una nota

| Lugar | Qué muestra | Fuente | Fecha | Dato |
|---|---|---|---|---|
| Lista de Ventas (columna FECHA) | fecha de la nota | `accounts_receivable_dto.fecha_display` | **local** | fecha de emisión (no cambia); cliente, vendedora y tipo salen de consultas vivas |
| Lista de notas de crédito (columna FECHA NC) | fecha de la nota de crédito | `credit_note_dto.fecha_nc_display` (y `fecha_nc_month_label` para el mes) | **local** | igual |
| Lista de CxC | no tiene columna de fecha de emisión; solo el selector de fecha de despacho | `dispatch_date` | ver aclaración A1 | vivo |
| Lista de Pro Venta, columna FECHA PAGO CLIENTE | fecha de pago del cliente | `pro_venta_dto.payment_date_display` | no verificado | vivo; ver aclaración A4 |
| Ventana **historial de cliente**: pestaña Notas | fecha | `fecha_display` | **local** | vivo |
| Ventana historial de cliente: pestaña Notas, columna DESPACHO | fecha de despacho | `dispatch_date`, sin conversión | sin convertir | ver aclaración A1 |
| Ventana historial de cliente: pestaña Notas de crédito | fecha | `fecha_nc_display` | **local** | vivo |
| Ventana historial de cliente: pestaña Pagos | fecha del pago | `payment_date`, sin conversión | **UTC** | vivo |
| Ventana historial de cliente: Productos, "ÚLTIMA COMPRA" | fecha de la última compra | `LastPurchaseDate` = `creation_date` de la nota, sin conversión | **UTC** | vivo; el nombre del producto sale de `product.name` hoy |
| **PDF** del historial de cliente (`CustomerHistoryPdfGenerator`, líneas 319, 320, 392, 463, 537) | fechas de notas, despacho, notas de crédito, pagos y última compra | `creation_date.ToString("dd/MM/yyyy")` directo | **UTC** (a partir de las 8 pm sale el día siguiente) | vivo |
| **Excel** del historial de cliente (`CustomerHistoryExcelExporter`, líneas 31, 32, 85, 123, 188) | igual que el PDF | igual | **UTC** | vivo |
| **Detalle de nota de crédito** (`CreditNoteDetailWindow`) | fecha | `note.creation_date.ToString("dd/MM/yyyy")` (`CreditNoteDetailWindow.xaml.cs:27`) | **UTC** | vivo |
| Detalle de nota de crédito: renglones | código, nombre, cantidad, precio, subtotal | `get_credit_note_details_async`: código desde `product_code_snapshot`; nombre desde `product.name` hoy | no aplica | **mixto:** código congelado, nombre vivo |
| Detalle de nota de crédito: cliente y vendedora | nombre del cliente y de la vendedora | `credit_note_dto.customer_name` y `seller_name` (consulta viva) | no aplica | vivo |
| Comprobante de comisión: PDF (`CommissionPdfGenerator`) y Excel (`CommissionExcelExporter`) | fecha de pago de la comisión (`payment_date`, formato `dd-MMM` en PDF y `dd/MM/yyyy` en Excel) | `commission_receipt_dto.payment_date`, sin conversión | **UTC** | vivo |
| Reporte de comisiones (PDF y Excel) | fecha de último pago, fecha de pago de comisión | DTO sin conversión | **UTC** | vivo |
| Reporte de pagos (Excel) | fecha del pago | `payment_date.ToString("dd/MM/yyyy")` directo | **UTC** | vivo |
| Reportes de ventas, de CxC y de notas de crédito (PDF y Excel) | fecha de la nota, de despacho | `fecha_display`, `despacho_display`, `fecha_nc_display` | **local** | vivo (fuera de alcance de congelar) |
| Historial de ventas por producto y por promoción | fecha de la nota | `fecha_display` | **local** | vivo; fuera de alcance de congelar |
| Vista previa antes de guardar una nota de entrega | todo lo del PDF | formulario | **local** | vivo |
| Vista previa antes de guardar una nota de crédito | todo lo del PDF | pantalla | **local** | vivo |

Las fechas de pago se guardan en UTC porque `payment`, `commission_payment` y `credit_note` normalizan a UTC en el setter (`payment.cs:49`, `commission_payment.cs:99`, `credit_note.cs:43`). Por eso mostrarlas sin conversión da un día de más pasadas las 8 pm hora de Venezuela.

## 5. Hallazgos (lo que no cumple la spec hoy)

- **H1 (RF-4):** al guardar una nota de entrega, el PDF y el Excel inmediatos se arman otra vez desde el formulario (`build_preview_dto()`), no desde lo guardado. El número sí queda corregido, y el contenido coincide con la foto porque ambos salen del mismo formulario en el mismo momento, pero no se lee la foto guardada.
- **H2 (RF-6):** muestran la fecha en UTC sin convertir: el PDF y el Excel del historial de cliente, la ventana de historial en las pestañas de pagos y última compra, el detalle de nota de crédito, los comprobantes y reportes de comisiones y el Excel del reporte de pagos. Pasadas las 8 pm hora de Venezuela salen con el día siguiente.
- **H3 (RF-2, RF-7):** en notas sin foto el nombre del producto, los datos del cliente y de la vendedora son vivos (es lo decidido en RF-9). En las notas con foto, el detalle de nota de crédito mezcla el código congelado con el nombre vivo y el cliente vivo, porque esa ventana no lee la foto.
- **H4:** el pie "Impreso" del PDF usa la hora de impresión, no la de emisión. Es intencional, pero queda registrado para que nadie lo confunda con la fecha de la nota.

## 6. Aclaraciones

- **[NECESITA ACLARACIÓN A1: ¿`dispatch_date` (fecha de despacho) es una fecha de calendario sin hora, o lleva hora UTC? Se muestra sin conversión en la ventana de historial, en su PDF y en su Excel. Si lleva hora, también necesita pasar por `to_local`.]**
- **[NECESITA ACLARACIÓN A2: los comprobantes de pago y los de comisión están dentro de RF-6 ("pagos y comprobantes en hora de Venezuela"), pero la spec no dice si hay un comprobante de pago impreso aparte. El código solo tiene el comprobante de comisión. ¿Existe otro comprobante de pago que deba entrar en el inventario?]**
- **[NECESITA ACLARACIÓN A4: `pro_venta_dto.payment_date_display` (columna FECHA PAGO CLIENTE de Pro Venta) se arma en `ProVentaService`, y no se verificó si convierte a hora de Venezuela. ¿Se revisa en la tarea de fechas (RF-6)?]**
- **[NECESITA ACLARACIÓN A3: en el Excel de una nota de crédito con su nota de entrega las hojas se llaman "Original" y "Copia" (nota de entrega y nota de crédito). ¿Se dejan esos nombres?]**
