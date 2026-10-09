<div align="center">

<img src="src/NinOS.UI/Assets/ninOS_logo.png" alt="NinOS ERP Logo" width="140" style="margin-bottom: 15px;" />

# ⚡ NinOS ERP — Commercial & Distribution Suite
### *Suite de escritorio para facturación, distribución, cobranzas y liquidación de comisiones*

[![Versión](https://img.shields.io/badge/version-1.0.6-FF6F00?style=for-the-badge&logo=github&logoColor=white)](https://github.com/lulujax/ninos)
[![.NET Version](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![WPF](https://img.shields.io/badge/UI-WPF%20%2F%20Modern%20XAML-0078D7?style=for-the-badge&logo=windows&logoColor=white)](https://learn.microsoft.com/visualstudio/xaml-tools/)
[![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL%2016-336791?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![EF Core](https://img.shields.io/badge/ORM-EF%20Core%2010.0-68217A?style=for-the-badge&logo=nuget&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![QuestPDF](https://img.shields.io/badge/Vector%20PDF-QuestPDF%202024-1F6FEB?style=for-the-badge&logo=adobeacrobatreader&logoColor=white)](https://www.questpdf.com/)
[![Arquitectura](https://img.shields.io/badge/Design-Clean%20Architecture%20%7C%20MVVM-00897B?style=for-the-badge)](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93viewmodel)
[![Instalador](https://img.shields.io/badge/Setup-Inno%20Setup%20%E2%80%93%20Espa%C3%B1ol-2E6FBF?style=for-the-badge)](installer/NinOS_Setup.iss)

<br/>

**[🌟 Filosofía](#-filosofía-y-manifiesto) • [💎 Módulos](#-los-módulos-del-sistema) • [🖨️ Suite PDF](#-suite-de-reportes-vectoriales-questpdf) • [📜 Reglas de Negocio](#-reglas-de-negocio-invariantes) • [🏛️ Arquitectura](#-arquitectura-y-patrones-de-diseño) • [📂 Estructura](#-estructura-del-proyecto) • [📊 Modelo de Datos](#-modelo-entidad-relación-erd) • [🚀 Puesta en Marcha](#-instalación-y-puesta-en-marcha) • [📚 Documentación](#-documentación) • [🔮 Roadmap](#-hoja-de-ruta-y-evolución)**

---

</div>

## 🌟 Filosofía y Manifiesto

> *"El software empresarial de alto rendimiento no se concibe únicamente para digitalizar procesos: se diseña para transformar la incertidumbre operativa en certidumbre matemática, ejecutando transacciones atómicas con cero latencia y elegancia visual."*

**NinOS** es una suite ERP de escritorio de nivel corporativo concebida específicamente para distribuidoras de alimentos, importadoras, salones, belleza y empresas de logística comercial con operaciones de alto volumen de rotación.

Frente a sistemas legados lentos y propensos a la dispersión de datos, **NinOS** unifica en una sola consola reactiva todo el ciclo operativo: desde la recepción de pedidos y control granular de stock hasta la emisión de notas, trazabilidad de cobranzas multidivisa, gestión de relaciones semanales (**ProVenta**), procesamiento de devoluciones (**Notas de Crédito**) y liquidación matemática de comisiones para la fuerza de ventas.

**En resumen, qué resuelve NinOS:**

| Necesidad | Cómo la resuelve |
|---|---|
| Facturar rápido sin errores de precio o stock | Punto de venta con búsqueda predictiva, validación de stock en tiempo real y precios netos |
| Saber quién debe y cuánto | Cuentas por cobrar con semáforo financiero, segmentación por vendedora y reportes por zona |
| Cobrar sin descuadres | Tesorería sin sobrepago, historial no destructivo y doble footer `FACTURADO / COBRADO` |
| Devolver mercancía sin perder la trazabilidad | Notas de crédito al precio neto, con anulación reversible y auditoría completa |
| Liquidar vendedoras con números exactos | Comisiones sobre recaudo real, con kárdex histórico devengado vs. pagado |
| Imprimir documentos listos para el cliente | 10 generadores PDF vectoriales con branding, totales sanos y formato editorial |
| Nunca perder la información | PostgreSQL ACID + papelera lógica + panel de administrador con backups y bitácora |

---

## 💎 Los módulos del sistema

```
                               ┌───────────────────────────────────────────────────────────┐
                               │                    NinOS CORE ENGINE                      │
                               └───────────────────────────────────────────────────────────┘
                                   │            │             │            │          │
         ┌─────────────────────────┴────┐       │             │            │          └────────────────────────┐
         ▼                              ▼       ▼             ▼            ▼                                   ▼
 ┌───────────────┐ ┌───────────────┐ ┌───────────────┐ ┌──────────────┐ ┌──────────────┐ ┌───────────────┐ ┌───────────────┐
 │   DESPACHOS   │ │   COBRANZAS   │ │     VENTAS    │ │   TESORERÍA  │ │  COMISIONES  │ │   PRO VENTA   │ │ NOTAS CRÉDITO │
 │ (Notas Entrega│ │     (CxC)     │ │   (Punto POS) │ │   (Abonos)   │ │  (Vendedores)│ │  (Semanales)  │ │ (Devoluciones)│
 └───────────────┘ └───────────────┘ └───────────────┘ └──────────────┘ └──────────────┘ └───────────────┘ └───────────────┘
                                   ▲                                                  ▲
                                   │        ┌───────────────────────────────┐        │
                                   └────────┤  MAESTROS TRANSVERSALES     ├────────┘
                                            │ Vendedores · Zonas · Clientes │
                                            │ Líneas de producto · Catálogo │
                                            └───────────────────────────────┘
```

Las pestañas del `MainWindow` están en este orden: **Notas de Entrega · Vendedores · Clientes · Ventas · Cuentas por Cobrar · Pagos · Comisiones · Inventario · Notas de Crédito · Pro Venta**.

### 1. 🚚 Despachos & Notas de Entrega (`DeliveryNotesView`)
*Gestión integral del ciclo de despacho con numeración correlativa inmutable.*
- **Control de Ciclo de Vida:** Estados formales `Pendiente` ➔ `Pagada` ➔ `Devuelta` ➔ `Anulada`.
- **Validación Automática de Stock:** Reserva y decremento en tiempo real al emitir; reincorporación atómica al stock en caso de anulación.
- **Vendedor visible en el encabezado:** El nombre de la vendedora se imprime debajo de *Caracas Venezuela* en la nota, para identificar de quién es la venta.
- **Tipos de nota:** `General` (GEN), `Pro Venta` (MAR), `Promoción` (PRM), `Promoción Pro Venta` (PVP) y las variantes por volumen `VOL` / `VOLMAR`, con banner comercial opcional.
- **Descuento por volumen:** solo los tipos `VOL` y `VOLMAR` muestran el campo de descuento, que se aplica en cascada sobre el total.
- **Previsualizador Editorial Vectorial:** Modal interactivo (`NotePreviewWindow`) con vista previa, desglose fiscal, impresión y **Guardar y PDF**.
- **Edición inline** de observaciones de cobranza, crédito (`DIAS` / fecha de vencimiento) y fecha de despacho.

### 2. 👩 Fuerza de Ventas (`SellersView`)
*Maestro de vendedoras con zonas asignadas y series de código propias.*
- **Códigos de 3 dígitos** (001 Sandra, 002 Anais, 003 Alejandra, 004 Juan Luis, 005+ para nuevas), usados como prefijo del código de cliente.
- **Zonas asignadas por vendedora** (`seller_zones`): delimitan la cartera y habilitan el agrupamiento Pro Venta por zona.
- **Serie de clientes por vendedora:** el último correlativo usado se persiste para que un código borrado nunca se reutilice.
- **Borrado lógico:** `deleted_at` / `deleted_reason`; nunca se elimina de forma física para no romper el historial.

### 3. 💳 Cuentas por Cobrar (CxC) (`AccountsReceivableView`)
*Monitoreo analítico de la cartera con segmentación reactiva.*
- **Segmentación por vendedora:** pestañas dedicadas con filtrado instantáneo (*Todos*, *Sandra*, *Anais*, *Alejandra*, *Juan Luis*).
- **Filtro dual:** mes + estado (*Por Cobrar*, *Anuladas*, *Devueltas*, *Todas*).
- **Acciones directas:** `Registrar pago`, `Editar`, `Anular`, `Vista Previa`, `PDF` y `Descargar PDF` por nota.
- **Reporte configurable:** botón **Reporte** que abre un *popup* con mes, **Agrupar en** (Por Vendedor / Por Zona) y checklists de vendedores y zonas. Las listas **son** el filtro: lo que está desmarcado no entra.
- **Regla del reporte:** solo entran notas **Pendientes**. Las pagadas, anuladas y devueltas no salen ni suman.
- **Semáforo Financiero Dinámico:** código de colores reactivo para identificar solventes, en margen o en mora.

### 4. 🛒 Punto de Venta & Emisión Rápida (`SalesView`)
*Terminal de captura veloz diseñada para operaciones de alto tráfico comercial.*
- **Búsqueda Predictiva de Clientes y SKU:** al vuelo por código, descripción, RIF o razón social sin bloqueos de interfaz.
- **Reporte mensual configurable** (mismo *popup* que CxC): mes, agrupado por vendedor/zona, tipo de nota, filtro de cobranza, bloque de **Meta de Ventas**, observaciones del mes y opción de incluir anuladas y devueltas (que se muestran pero **no** suman).

### 5. 💰 Tesorería, Abonos & Auditoría Bancaria (`PaymentsView`)
*Gestión transaccional estricta de ingresos de efectivo y transferencias.*
- **Formas de pago:** efectivo, transferencia, pago móvil y bolívares con **tasa** de conversión registrada en el asiento.
- **Pago de monto exacto:** no se permite sobrepagar. Un abono **menor** al saldo sí se acepta y queda saldo pendiente.
- **Sin sobrepago ni descuadre:** no existe margen de tolerancia.
- **Footer de dos totales:** `TOTAL FACTURADO` y `TOTAL COBRADO`. El "total por cobrar" **no** se muestra aquí: es información de cobranza, no de tesorería.
- **Reporte de pagos del mes** por vendedor/zona y **Historial No Destructivo** (`PaymentHistoryWindow` / `PaymentNoteHistoryWindow`).

### 6. 🤝 Comisiones (`CommissionsView`)
*Liquidación matemática automatizada de comisiones y control de egresos a vendedoras.*
- **Cálculo por porcentaje sobre recaudo real:** las comisiones se devengan solo sobre lo efectivamente facturado y cobrado.
- **Selección obligatoria de vendedora:** el `ComboBox` de *Vendedora* arranca vacío; solo después de elegirlo se buscan las notas pendientes, y la lista muestra únicamente las de esa vendedora. Así no se mezclan notas de dos liquidaciones.
- **Totales en vivo:** `TOTAL VENTA`, `TOTAL COMISION` y `COMISION PENDIENTE`.
- **Kárdex Histórico** (`CommissionHistoryWindow`): devengado vs. pagado por vendedora, con edición y anulación de pagos.

### 7. 👥 Directorio Maestro de Clientes (`CustomerView`)
*Ficha 360° del cliente con blindaje referencial.*
- **Expediente Comercial:** razón social, identificación/RIF, teléfono, contacto, dirección fiscal, dirección de entrega y **zona obligatoria**.
- **Código autogestionado:** se sugiere el correlativo de la serie de la vendedora (prefijo de 3 dígitos).
- **Asignación de Zona:** cada cliente pertenece a una zona, que es la que agrupa los reportes.
- **Historial de Cliente** (`CustomerHistoryWindow` / `CustomerHistoryPdfGenerator`): notas de entrega, notas de crédito y pagos.

### 8. 📦 Inventario, Líneas de Producto & Kárdex (`InventoryView`)
*Almacén central, promociones compuestas y seguimiento físico de existencias.*
- **Líneas de producto (`product_line`):** cada marca tiene un **prefijo de código editable** y su propio estándar.
- **Migración de códigos al cambiar el prefijo:** al editar el prefijo se **reescriben todos los productos** de la línea conservando su número (`DEF30508` → `DEFX30508`), y también el historial: *snapshots* de notas de entrega y de notas de crédito, y los documentos de ajuste del kárdex. Todo en una sola transacción, con pantalla de confirmación previa que muestra cuántos registros se van a tocar.
- **Códigos siempre en mayúsculas** y validados contra duplicados: el índice único de `product.product_code` es la red de seguridad final.
- **Promociones Compuestas:** combos que agrupan productos y descuentan el stock de cada componente de forma atómica.
- **Kárdex Reactivo:** doble clic en un producto para ver entradas y salidas con acumulado, exportable a PDF (`InventoryHistoryPdfGenerator`).
- **Lista de precios** con selección de marcas/categorías para el PDF.
- **Papelera lógica** para productos, promociones, clientes, vendedoras y zonas.

### 9. 📊 Pro Venta & Relaciones Semanales (`ProVentaView`)
*Orquestación de relaciones de entrega semanales.*
- **Arranque en estado vacío:** el módulo **no** preselecciona mes ni carga datos. Hasta que elijas un mes, muestra *"Seleccione un mes para ver el reporte"* y las tablas quedan vacías a propósito.
- **Agrupación Semanal:** desglose del mes en semanas operativas.
- **Anuladas visibles en rojo:** se muestran marcadas pero **no** suman al monto, ni al conteo de notas, ni a la liquidación de la relación.
- **Liquidación con desglose:** columnas de `GASTOS ADM. 15%`, `GASTOS OPER. 25%`, `COM. LUIS 10%`, `MONTO POR PAGAR` y `SALDO COMPLETO`.
- **Tres reportes:** relaciones *pendientes*, *pagadas* y *reporte semanal*, más `Abonar / Pagar`, `Anular` e impresión por detalle.
- **Metas por vendedor** (`sales_goals`).

### 10. 📑 Notas de Crédito (`CreditNotesView`)
*Gestión formal de devoluciones y reversiones de saldo.*
- **Fecha propia de NC:** la columna `FECHA NC` y el filtro por mes siguen la fecha de la nota de crédito, no la de la nota de entrega que se está revirtiendo.
- **Columna TIPO NOTA:** hereda el tipo de la nota de entrega original (`General`, `Pro Venta`, `PVP`, `Promoción`). Los obsequios muestran `Obsequio`. Si la nota original no tiene tipo, cae a `General` en vez de quedar en blanco.
- **Precio NETO en las devoluciones:** se devuelve el precio que el cliente realmente pagó, prorrateando el descuento.
- **Recorte proporcional:** si la devolución supera lo disponible, el excedente se recorta proporcionalmente sin bloquear el guardado; el servidor es la autoridad final y deja rastro en el log.
- **Las notas pagadas no se pueden devolver:** el buscador de notas de entrega solo ofrece notas con saldo pendiente. Hay validación en el servicio, no solo en la interfaz.
- **Anulación de NC:** disponible para **obsequio** y **devolución**. Revierte exactamente lo que hizo al crearse: la devolución saca el stock que ingresó y devuelve el abono a la nota de entrega; el obsequio devuelve al inventario el stock que salió. La nota no se borra, queda como `Anulada`.
- **Notas anuladas bien distinguidas:** la columna CATEGORIA muestra **"Anulada"** y toda la fila va en rojo y semi negrita.
- **Reporte configurable:** período (general o mensual), categoría y checkbox **"Incluir notas anuladas"** (apagado por defecto).

### 🛡️ Panel de Administrador (`AdminPanelWindow`)
*Área restringida para el dueño del sistema — no es parte del flujo diario de las vendedoras.*
- **Acceso:** `Ctrl+F12` o 5 clics sobre el título de la ventana; pide contraseña de administrador.
- **Backups:** generar backup de la base de datos ahora, abrir la carpeta de backups, restaurar desde archivo y consultar el estado de la base.
- **Papelera lógica:** clientes, productos, promociones, vendedoras y zonas, con fecha de borrado, motivo y opción de restaurar.
- **Catálogos:** gestionar líneas de producto y zonas, exportar clientes y productos a CSV.
- **Bitácora de actividad** y cambio de la contraseña de administrador.

---

## 🖨️ Suite de Reportes Vectoriales (QuestPDF)

| Generador | Archivo Fuente | Descripción |
|---|---|---|
| **Nota de Entrega** | `NotePdfGenerator.cs` | Comprobante editorial con desglose fiscal, ítems, totales y coordenadas de pago. |
| **Reporte de Mes** | `MonthlyReportPdfGenerator.cs` | Compartido por **Ventas** y **CxC**. Agrupa por vendedor o por zona, con desglose por grupo. |
| **Relación Semanal ProVenta** | `ProVentaPdfGenerator.cs` | Cuadro de distribución semanal, con las notas anuladas en rojo y aparte del total. |
| **Detalle de Relación ProVenta** | `ProVentaDetailPdfGenerator.cs` | Desglose ítem por ítem de cada relación comercial. |
| **Reporte de Pagos** | `PaymentsReportPdfGenerator.cs` | Recaudaciones del mes. Los asientos de anulación van en bloque aparte y **no** suman. |
| **Comprobante de Comisión** | `CommissionPdfGenerator.cs` | Recibo de liquidación y desglose de comisiones acumuladas y pagadas. |
| **Reporte de Notas de Crédito** | `CreditNotesReportPdfGenerator.cs` | Auditoría de devoluciones del período, con y sin anuladas. |
| **Catálogo de Precios** | `PriceListPdfGenerator.cs` | Lista de precios con branding corporativo y selección de categorías. |
| **Historial de Cliente** | `CustomerHistoryPdfGenerator.cs` | Consolidado de notas, notas de crédito y pagos por cliente. |
| **Historial de Movimientos** | `InventoryHistoryPdfGenerator.cs` | Kárdex de inventario: entradas, salidas y saldo acumulado por producto. |

> **Muestras:** [muestra_nota_entrega.pdf](muestra_nota_entrega.pdf) · [muestra_historial_movimientos.pdf](muestra_historial_movimientos.pdf)

![Muestra del historial de movimientos](muestra_historial_movimientos.png)

### Regla de totales en todos los PDF
Las notas **anuladas** y las **devueltas** **nunca** suman. Aparecen marcadas en su color (rojo y morado) y en su propio bloque al pie, con la leyenda *"Excluidas del subtotal y total"*. Se aplica igual en subtotales, total general, promedio, comparativo contra el periodo anterior, barras de gráficos y en el cumplimiento de metas. Si un filtro deja un grupo sin ninguna nota vigente, se avisa en vez de imprimir una página con total 0.

---

## 📜 Reglas de Negocio (invariantes)

Estas reglas dan por sentadas varias pantallas. Si cambias una, revisa las demás.

| Regla | Dónde vive |
|---|---|
| Las notas **anuladas** y **devueltas nunca suman** en totales, footers, subtotales, promedios, comparativos, cumplimiento de metas ni gráficos de PDF. Salen a la vista en color y en bloque aparte. | Generadores PDF + ViewModels |
| Una nota de entrega **pagada no admite nota de crédito**: no hay saldo que devolver. | `CreditNoteService` |
| Las devoluciones se calculan al **precio neto** (con descuento prorrateado), con recorte proporcional si exceden lo disponible. | `CreditNoteService` |
| Los **códigos de producto** se guardan en mayúsculas y son únicos; cambiar el prefijo de una línea **reescribe** productos e historial. | `product_code_rules`, `ProductLineService` |
| La **fecha de la NC** es la fecha de emisión del crédito, no la de la nota de entrega. El filtro por mes sigue a la NC. | `CreditNoteService` |
| Las fechas se guardan en **UTC** y se muestran en hora local (`ToLocalTime()`), o se ve el día anterior por la noche. | `AppTimeZone`, DTOs de reporte |
| Los **pagos no pueden exceder** el saldo pendiente. Un abono menor sí se acepta. | `AddPaymentWindow` |
| La **zona** es la que agrupa reportes; el **vendedor** es el que liquida. | `AccountsReceivable`, `ProVenta` |
| Los catálogos (`product_line`, `zona`, `seller`, `customer`, `promotion`) se **borran lógicamente**, nunca en cascada física. | Migraciones *SoftDelete* |
| **Zonas Pro Venta (MAR / PVP / VOLMAR):** cualquier vendedora con una zona en modo Pro Venta puede emitir notas Pro Venta para los clientes de esa zona. Maracay **no** es exclusiva de un vendedor en particular. | `DeliveryNoteService`, `ZonaService` |
| **Zonas General:** trabajan con notas `General` (GEN) y `Promoción` (PRM). | `DeliveryNoteService` |
| **Códigos de vendedoras de 3 dígitos:** Sandra `001`, Anais `002`, Alejandra `003`, Juan Luis `004` (005+ para nuevas). El código es el prefijo de la serie de clientes de esa vendedora. | `SellersView`, `CustomerService` |
| El **descuento por volumen** solo existe en los tipos `VOL` y `VOLMAR`; el resto de tipos ni siquiera muestran el campo. | `NoteTypeCodes.es_volumen` |

---

## 🏛️ Arquitectura y Patrones de Diseño

```
       ┌────────────────────────────────────────────────────────┐
       │                       NinOS.UI                         │
       │       WPF • Modern XAML • MVVM • Reactive Event Mesh   │
       │       Value Converters • Custom Dialogs • QuestPDF     │
       └───────────────────────────┬────────────────────────────┘
                                   │  Consume abstracciones (DI)
                                   ▼
       ┌────────────────────────────────────────────────────────┐
       │                 NinOS.Infrastructure                   │
       │      EF Core 10 • Npgsql • Migrations • Repositories   │
       │      Business Services (Delivery, AR, Payment, etc.)   │
       └───────────────────────────┬────────────────────────────┘
                                   │  Implementa interfaces y referencia
                                   ▼
       ┌────────────────────────────────────────────────────────┐
       │                      NinOS.Domain                      │
       │       Entidades Puras • DTOs de Reporte • Enums         │
       │       Zero Dependencias Externas (POCOs puros)         │
       └───────────────────────────┬────────────────────────────┘
                                   │  Persiste / Consulta
                                   ▼
       ┌────────────────────────────────────────────────────────┐
       │                     PostgreSQL 16                      │
       │       ACID • Índices Únicos • Integridad Referencial   │
       └────────────────────────────────────────────────────────┘
```

### 🧩 Pilares Técnicos Destacados

- **MVVM Radical:** la lógica vive en los ViewModels; las vistas usan `DataBinding`, `RelayCommand` e `ICommand`.
- **Malla de Eventos Reactiva Inter-ViewModel:** `AppDataEvents.CatalogsChanged` + callbacks por ViewModel (`OnNoteSaved`, `OnCreditNoteSaved`) para que un cambio en un módulo refresque inventario, cobranzas, pagos y Pro Venta sin recargar la aplicación.
- **Inyección de Dependencias:** `Microsoft.Extensions.DependencyInjection` configurado en `App.xaml.cs`.
- **Estrategia Híbrida de Conexión:** variable de entorno (`NINOS_DB_CONNECTION`) ➔ `appsettings.json` ➔ servidor por defecto en `DbConnectionFactory.cs`.
- **Reintento de arranque:** si la base no está disponible al iniciar, el diálogo ofrece **Reintentar** hasta 5 veces antes de permitir salir. Los botones llevan texto propio (`Reintentar` / `Salir`) en vez de *Sí* / *No*.
- **Sistema de Diálogos Unificado (`AppDialogWindow` / `AppDialog`):** modales con el diseño de la aplicación, sin recurrir a los diálogos nativos del SO.
- **Reglas de código en dominio:** `product_code_rules` y `stock_movement_writer` concentran el estándar de códigos y la escritura del kárdex, de modo que el formulario, el servicio y la validación hablen del mismo formato.
- **Backups y auditoría:** `DatabaseBackup` (pg_dump/restauración), `DatabaseExporter` (CSV) y `AppLog` (bitácora con rotación) viven detrás del panel de administrador.
- **Accesibilidad DPI:** manifiesto `PerMonitorV2` + `UiScale.cs`, que **respeta la escala de Windows y nunca la achica**. Detalles y deuda técnica en [docs/accesibilidad.md](docs/accesibilidad.md).

---

## 📂 Estructura del Proyecto

```text
NinOS/
├── installer/
│   └── NinOS_Setup.iss                         # 📦 Script de Inno Setup (español) → NinOS_Setup_vX.Y.Z.exe
│
├── migrations/                                 # 🗄️ Scripts DDL y parches de auditoría SQL
│   ├── add_bank_and_observations.sql
│   ├── add_payment_and_commission_fields.sql
│   ├── add_payment_audit_columns.sql
│   ├── add_unique_product_code.sql
│   ├── fix_credit_note_creation_date.sql
│   ├── normalize_legacy_product_codes.sql
│   ├── normalize_product_category_case.sql
│   └── load_*.sql                              # Cargas de cartera y catálogo puntuales
│
├── docs/
│   ├── manual-usuario.md                       # 📚 Manual de usuario para clientes finales
│   └── accesibilidad.md                        # 🔍 Escala DPI, tipografía y deuda técnica
│
├── src/
│   ├── NinOS.Domain/                           # 🔷 CAPA DE DOMINIO (Núcleo Puro)
│   │   ├── ViewModels/                         # DTOs de proyección y reportes
│   │   ├── customer.cs / seller.cs / seller_zone.cs
│   │   ├── zona.cs                             # Zonas (marca de territorio, modo General/Pro Venta)
│   │   ├── delivery_note.cs / note_detail.cs / note_type.cs
│   │   ├── credit_note.cs / credit_note_detail.cs
│   │   ├── payment.cs                          # Pagos (amount_usd admite negativos: NC y asientos)
│   │   ├── comission.cs / commission_payment.cs
│   │   ├── product.cs / product_line.cs        # Producto y línea con prefijo de código
│   │   ├── promotion.cs / relacion.cs / sales_goal.cs
│   │   ├── stock_movement.cs                   # Movimientos de almacén
│   │   ├── NoteTypeCodes.cs                    # GEN · MAR · PRM · PVP · VOL · VOLMAR
│   │   ├── product_code_rules.cs               # Estándar PREFIJO + CORRELATIVO
│   │   ├── AppTimeZone.cs / Money.cs / BrandHeader.cs
│   │   └── product_code_migration_dto.cs       # Preview y resultado de migración de prefijos
│   │
│   ├── NinOS.Infrastructure/                   # 🔶 CAPA DE INFRAESTRUCTURA
│   │   ├── data/                               # DbContext, ConnectionFactory, DbInitializer (seeding)
│   │   ├── Logging/AppLog.cs
│   │   ├── Migrations/                         # Migraciones Code-First versionadas
│   │   └── Services/
│   │       ├── Interfaces/
│   │       ├── Implementations/                # Delivery, AR, Payment, CreditNote, ProVenta, etc.
│   │       └── stock_movement_writer.cs        # Escritor transaccional de kárdex
│   │
│   └── NinOS.UI/                               # 🔴 CAPA DE PRESENTACIÓN (WPF / XAML)
│       ├── Assets/                              # Logo .png (512²) + .ico multi-resolución
│       ├── Common/                              # 10 generadores PDF, ViewModels base, Comandos,
│       │   └── ViewModels/                      # un ViewModel por módulo + reportes (19)
│       ├── Converters/                          # ValueConverters XAML
│       ├── Views/                               # Vistas, modales y ventanas (36 .xaml)
│       ├── app.manifest                         # DPI PerMonitorV2
│       └── App.xaml(.cs)                        # Arranque, reintento y contenedor DI
│
├── AGENTS.md                                    # Reglas de negocio del repositorio
├── NinOS.slnx                                   # Solución moderna .NET XML
└── README.md
```

---

## 📊 Modelo Entidad-Relación (ERD)

```mermaid
erDiagram
    SELLER ||--o{ CUSTOMER : "gestiona"
    SELLER ||--o{ COMISSION : "acumula"
    SELLER ||--o{ COMMISSION_PAYMENT : "recibe"
    SELLER ||--o{ SALES_GOAL : "tiene_asignada"
    SELLER ||--o{ SELLER_ZONE : "cubre"

    ZONA ||--o{ SELLER_ZONE : "asignada"
    ZONA ||--o{ CUSTOMER : "agrupa"

    CUSTOMER ||--o{ DELIVERY_NOTE : "solicita"
    CUSTOMER ||--o{ PAYMENT : "abona"
    CUSTOMER ||--o{ CREDIT_NOTE : "beneficiario"

    DELIVERY_NOTE ||--|{ NOTE_DETAIL : "contiene"
    DELIVERY_NOTE ||--o{ PAYMENT : "recibe_abonos"
    DELIVERY_NOTE ||--o{ COMISSION : "liquida"
    DELIVERY_NOTE ||--o{ CREDIT_NOTE : "origen_de"
    DELIVERY_NOTE }o--o| RELACION : "agrupada_en"
    DELIVERY_NOTE }o--o| NOTE_TYPE : "tipificada"

    PRODUCT ||--o{ NOTE_DETAIL : "facturado_en"
    PRODUCT ||--o{ CREDIT_NOTE_DETAIL : "devuelto_en"
    PRODUCT ||--o{ STOCK_MOVEMENT : "registra"
    PRODUCT }o--|| PRODUCT_LINE : "pertenece_a"

    PROMOTION ||--|{ PROMOTION_ITEM : "empaqueta"
    PROMOTION ||--o{ NOTE_DETAIL : "vendido_en"
    PROMOTION }o--o{ PRODUCT : "compuesta_de"

    CREDIT_NOTE ||--|{ CREDIT_NOTE_DETAIL : "detalla"
    COMISSION ||--|{ COMMISSION_PAYMENT : "amortiza"
```

**Notas de modelado relevantes:**
- `note_detail.product_code_snapshot` y `credit_note_detail.product_code_snapshot` guardan el código del producto **tal como estaba** al emitir el documento. Las notas se muestran con el snapshot, no con el código actual.
- `payment.amount_usd` admite valores **negativos**: una nota de crédito se registra como abono negativo para restar en el saldo, y la anulación genera el asiento positivo que lo revierte.
- `note_type` es una tabla, no un enum: los tipos (`GEN`, `MAR`, `PRM`, `PVP`, `VOL`, `VOLMAR`) se seedean en `DbInitializer` y pueden crecer sin tocar código.

---

## 🚀 Instalación y Puesta en Marcha

### 📋 Prerrequisitos
1. **.NET 10 SDK** (v10.0.100 o superior).
2. **PostgreSQL** (v15 o v16) local o accesible por red/VPS.
3. **IDE Recomendado:** Visual Studio 2022/2026 (carga *"Desarrollo de escritorio de .NET"*) o VS Code con *C# Dev Kit*.

### 1️⃣ Clonar el Repositorio
```bash
git clone https://github.com/lulujax/ninos.git
cd ninos
```

### 2️⃣ Configurar la Base de Datos
- **Opción A (Variable de Entorno - Recomendada en Producción):**
  ```powershell
  $env:NINOS_DB_CONNECTION="Host=localhost;Port=5432;Database=ninos_db;Username=postgres;Password=tu_password;"
  ```
- **Opción B (`appsettings.json` junto al binario):**
  ```json
  {
    "ConnectionStrings": {
      "NinOSDb": "Host=localhost;Port=5432;Database=ninos_db;Username=postgres;Password=tu_password;"
    }
  }
  ```

### 3️⃣ Restaurar, Compilar y Migrar
```bash
dotnet restore NinOS.slnx
dotnet build NinOS.slnx -c Release
dotnet ef database update --project src/NinOS.Infrastructure --startup-project src/NinOS.UI
```

> [!TIP]
> Parches SQL adicionales aplicables con `psql`:
> ```bash
> psql -U postgres -d ninos_db -f migrations/add_bank_and_observations.sql
> psql -U postgres -d ninos_db -f migrations/add_payment_and_commission_fields.sql
> psql -U postgres -d ninos_db -f migrations/add_payment_audit_columns.sql
> psql -U postgres -d ninos_db -f migrations/normalize_legacy_product_codes.sql
> psql -U postgres -d ninos_db -f migrations/normalize_product_category_case.sql
> ```

### 4️⃣ Ejecutar
```bash
dotnet run --project src/NinOS.UI
```

> [!NOTE]
> Si la base no está disponible, el arranque ofrece **Reintentar** hasta 5 veces en lugar de cerrarse.

### 5️⃣ Empaquetar el instalador para clientes
El repositorio incluye un script de **Inno Setup** en español que empaqueta la carpeta `publish/`:

```bash
# 1. Publicar la app autocontenida
dotnet publish src/NinOS.UI -c Release -r win-x64 --self-contained true -o publish

# 2. Compilar el instalador (requiere Inno Setup 6 en Windows)
iscc installer/NinOS_Setup.iss
# → genera NinOS_Setup_v1.0.6.exe en la carpeta configurada
```

El instalador crea acceso directo en el escritorio, entrada en el menú Inicio y opción de desinstalar.

---

## 📚 Documentación

| Documento | Para quién es |
|---|---|
| **[Manual de Usuario](docs/manual-usuario.md)** | Clientes y usuarios finales: guías paso a paso, sin jerga técnica. |
| **[Accesibilidad y escala](docs/accesibilidad.md)** | Equipo técnico: DPI, tipografía y deuda de layout. |
| **[Informe QA](informe_qa_ninos.md)** | Registro de pruebas y hallazgos de calidad. |
| **[Reglas de negocio](AGENTS.md)** | Invariantes que todo cambio debe respetar. |

---

## 🔮 Hoja de Ruta y Evolución

| Estado | Característica | Descripción |
|:---:|---|---|
| ✅ | **Core de Facturación & Despacho** | Notas de entrega, cálculo preciso, vendedor visible y previsualización. |
| ✅ | **Maestros de Vendedoras y Zonas** | Códigos de 3 dígitos, zonas asignadas y zonas de cliente. |
| ✅ | **Cuentas por Cobrar** | Tabs por vendedora, filtro de mes/estado y reporte configurable. |
| ✅ | **Notas de Crédito** | Fecha propia, tipo de nota, precio neto, anulación y exclusiones. |
| ✅ | **Pro Venta** | Relaciones semanales, arranque en vacío, anuladas visibles sin sumar. |
| ✅ | **Líneas de Producto** | Prefijo editable con migración de códigos e historial. |
| ✅ | **Motor Vectorial QuestPDF** | 10 generadores, con reglas de exclusión de anuladas. |
| ✅ | **Descuentos por Volumen** | Tipos `VOL` / `VOLMAR` con descuento en cascada. |
| ✅ | **Panel de Administrador** | Backups, restauración, papelera lógica, bitácora y exportación CSV. |
| ✅ | **Arranque resiliente** | Reintento automático ante base de datos no disponible. |
| ✅ | **Instalador de cliente** | Inno Setup en español con acceso directo y desinstalador. |
| 🔄 | **Sincronización Cloud Multisede** | Conexión bidireccional en tiempo real para sucursales remotas. |
| 🔄 | **Impresión Térmica ESC/POS** | Driver nativo para ticketeras de 58mm y 80mm. |
| 🔄 | **Preferencia de tamaño de interfaz** | Selector Normal/Grande/Muy grande en Ajustes (ver [accesibilidad](docs/accesibilidad.md)). |
| 💡 | **Dashboard Ejecutivo de BI** | Rotación de SKU, margen operativo y flujo proyectado. |
| 💡 | **App Móvil de Preventa (MAUI)** | Toma de pedidos offline con sincronización en ruta. |

---

## 👨‍💻 Autor y Créditos

**NinOS** ha sido concebido, estructurado y programado con los más altos estándares de ingeniería de software por:

<div align="center">

### **Luis "Lulujax"**
*Software Engineer & Systems Architect*

[![GitHub](https://img.shields.io/badge/GitHub-Profile-181717?style=flat-square&logo=github)](https://github.com/lulujax)

> *"El código elegante no es un capricho estético: es el compromiso innegociable del desarrollador con la robustez, la estabilidad y la productividad de quienes dependen de él a diario."*

---

⭐ **¿Encontraste útil o inspirador este proyecto?** Déjale una estrella en el repositorio. ⭐

</div>
