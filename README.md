# ⚡ NinOS — Enterprise Commercial & Distribution ERP

<div align="center">

| \ | ()     / _ / |
|  | | _ | | | _ \
| |\  | | ' \ || |) |
|| _||| ||___/|____/
### 🏛️ La Plataforma Definitiva de Gestión Comercial, Distribución, Cartera y Comisiones

*Diseñado, estructurado y forjado con pasión de ingeniería por*  
👉 **[Luis "Lulujax"](https://github.com/lulujax)** 👈

---

[![Author](https://img.shields.io/badge/Crafted%20by-Lulujax-FF6F00?style=for-the-badge&logo=github&logoColor=white)](https://github.com/lulujax)
[![.NET Version](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![WPF](https://img.shields.io/badge/UI-WPF%20%2F%20XAML-0078D7?style=for-the-badge&logo=windows&logoColor=white)](https://learn.microsoft.com/visualstudio/xaml-tools/)
[![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL%2016-336791?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![EF Core](https://img.shields.io/badge/ORM-EF%20Core%2010-68217A?style=for-the-badge&logo=nuget&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![Architecture](https://img.shields.io/badge/Design-Clean%20Architecture%20%7C%20MVVM-009688?style=for-the-badge)](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93viewmodel)
[![License](https://img.shields.io/badge/Status-Active%20Enterprise-blueviolet?style=for-the-badge)](https://github.com/lulujax/ninos)

[🌟 El Manifiesto](#-el-manifiesto-de-ninos) • [💎 Módulos de Élite](#-módulos-del-sistema) • [🏛️ Arquitectura & SOLID](#-arquitectura-y-patrones-de-diseño) • [📂 Radiografía del Código](#-estructura-del-repositorio) • [🚀 Puesta en Marcha](#-instalación-y-configuración) • [📊 Modelo de Datos](#-modelo-entidad-relación-erd) • [👨‍💻 Sobre el Autor](#-sobre-el-autor--lulujax)

</div>

---

## 🌟 El Manifiesto de NinOS

> *"El software de misión crítica no se escribe simplemente para funcionar: se diseña para perdurar, responder con precisión matemática y transformar el caos operativo en una sinfonía de datos claros."*

**NinOS** no es solo un sistema contable o un gestor de ventas más; es una suite ERP de escritorio de nivel empresarial forjada desde los cimientos para redefinir cómo operan las distribuidoras mayoristas, importadoras y empresas comerciales de alto volumen.

Creado por **Lulujax**, este ecosistema combina la potencia en tiempo real de **.NET 10** y la belleza reactiva de **WPF (Windows Presentation Foundation)** con el rigor transaccional de **PostgreSQL**. Cada línea de código responde a un propósito: erradicar los cuellos de botella, otorgar visibilidad financiera absoluta al segundo y blindar la integridad contable con transacciones atómicas e inmutables.

---

## 💎 Módulos del Sistema

╔═════════════════════════════════════════════════════════════════════════════════════╗║                                     NinOS CORE                                      ║╚═════════════════════════════════════════════════════════════════════════════════════╝│                  │                  │                  │                  │┌───────▼────────┐ ┌───────▼────────┐ ┌───────▼────────┐ ┌───────▼────────┐ ┌───────▼────────┐│ 📦 INVENTARIO  │ │ 📑 DESPACHOS   │ │ 💳 CARTERA &   │ │ 💰 TESORERÍA   │ │ 🤝 FUERZA DE   ││   & PROMOCIONES│ │   & FACTURACIÓN│ │    COBRANZAS   │ │   & AUDITORÍA  │ │    VENTAS      │└────────────────┘ └────────────────┘ └────────────────┘ └────────────────┘ └────────────────┘
### 1. 📦 Control Maestro de Inventario & Motor de Promociones
* 🏷️ **Catálogo de Alta Precisión**: Estructuración estricta de ítems mediante SKU, descripciones detalladas, costos directos, márgenes porcentuales dinámicos y precios calculados al mayor y al detal.
* 🎁 **Motor de Promociones Compuestas (`promotion` / `promotion_item`)**: Módulo avanzado para empaquetar múltiples productos en combos comerciales a precios promocionales, gestionando el decremento atómico del stock individual en cada venta.
* 🛡️ **Trazabilidad y Mínimos Críticos**: Monitoreo reactivo de existencias para anticipar quiebres de inventario y optimizar la cadena de reposición.

### 2. 📑 Facturación Rápida, Despachos & Ciclo de Notas
* ⚡ **Punto de Venta Agilizado (`SalesView`)**: Interfaz optimizada para teclear a alta velocidad. Búsqueda instantánea de clientes, predictivo de productos/promociones y cálculo en tiempo real de subtotales, recargos y montos netos.
* 📋 **Flujo Operativo de Entrega (`delivery_note` / `note_detail`)**: Seguimiento granular de estados de ciclo de vida de cada orden: *Emitida* ➔ *Despachada* ➔ *Cobrada* ➔ *Liquidada*.
* 🖨️ **Motor de Renderizado Vectorial PDF (`NotePdfGenerator`)**: Compilador de documentos PDF de nivel editorial. Genera comprobantes de entrega visualmente impecables, con códigos de control, desglose fiscal y previsualización modal fluida (`NotePreviewWindow`).

### 3. 💳 Cuentas por Cobrar (CxC) & Blindaje Financiero
* 📈 **Control de Cartera en Tiempo Real (`AccountsReceivableView`)**: Dashboard centralizado que agrupa la deuda global por cliente, límites crediticios autorizados y antigüedad de saldos.
* 🚦 **Semáforo Financiero Visual XAML (`BalanceColorConverter`)**: Marcado cromático reactivo e inteligente para clasificar cuentas solventes, cuentas en periodo de gracia y clientes en mora.
* 📜 **Kárdex Histórico Cruzado**: Conciliación transparente que contrasta notas emitidas contra pagos aplicados, evitando discrepancias en el saldo deudor.

### 4. 💰 Gestión de Tesorería, Abonos & Auditoría Bancaria
* 🏦 **Recepción de Cobros Multidivisa (`PaymentsView` / `AddPaymentWindow`)**: Procesamiento de pagos totales o abonos fraccionados distribuidos entre múltiples notas de entrega abiertas.
* 🛡️ **Auditoría Transaccional Estricta**: Registro obligatorio de entidades bancarias, números de referencia de transferencia, cotizaciones de cambio y marcas temporales de auditoría (`migrations/add_payment_audit_columns.sql`).
* 🔍 **Historial Transaccional Inmutable (`PaymentHistoryWindow` / `PaymentNoteHistoryWindow`)**: Registro cronológico no editable de cada entrada de dinero para auditorías fiscales y contables.

### 5. 🤝 Fuerza de Ventas & Liquidación Automática de Comisiones
* 🧑‍💼 **Estructura de Vendedores (`seller` / `comission`)**: Jerarquía de agentes de venta con prefijos identificadores asignados a carteras de clientes o regiones geográficas.
* 🧮 **Algoritmo de Liquidación Automatizado (`CommissionService`)**: Cálculo automatizado de comisiones porcentuales basadas únicamente en notas cobradas o despachadas.
* 💵 **Gestión de Egresos Comerciales (`CommissionsView` / `AddCommissionPaymentWindow`)**: Registro y control de comisiones acumuladas, anticipos entregados y balance neto por desembolsar.

### 6. 👥 Directorio Integral de Clientes
* 📇 **Ficha Maestra (`CustomerView` / `AddCustomerWindow`)**: Gestión de datos fiscales (RIF/NIT), teléfonos, direcciones de entrega, límites de crédito autorizados y vendedor asignado.

---

## 🏛️ Arquitectura y Patrones de Diseño

NinOS fue concebido bajo los principios de la **Clean Architecture (Arquitectura Limpia)** y los axiomas **SOLID**, desacoplando la lógica de negocio pura de la infraestructura y de los elementos visuales.

                              ┌───────────────────────────────┐
                              │           NinOS.UI            │
                              │     (XAML Views & ViewModels) │
                              └───────────────┬───────────────┘
                                              │ Consume
                                              ▼
                              ┌───────────────────────────────┐
                              │     NinOS.Infrastructure      │
                              │ (Services, Repositories, Data)│
                              └───────────────┬───────────────┘
                                              │ Implementa / Usa
                                              ▼
                              ┌───────────────────────────────┐
                              │         NinOS.Domain          │
                              │   (Entities, Enums & DTOs)    │
                              └───────────────────────────────┘

### 🔄 Flujo Reactivo de Ejecución

┌────────────────┐         Data Binding          ┌────────────────────┐│  Vista (XAML)  │ ◄───────────────────────────► │  ViewModel (MVVM)  │└────────────────┘   INotifyPropertyChanged /    └─────────┬──────────┘RelayCommand                    ││ Invocación de Casos de Uso▼┌────────────────┐         Persistencia          ┌────────────────────┐│  PostgreSQL DB │ ◄───────────────────────────► │   Service Layer    │└────────────────┘    NinOSDbContext / Npgsql    │ (Lógica Financiera)│└─────────┬──────────┘│▼┌────────────────────┐│  Repository Layer  ││  (IGenericRepo) │└────────────────────┘
### 🧩 Pilares de Ingeniería Aplicados

* **MVVM Puro (Model-View-ViewModel)**: Separación radical entre UI y lógica. Cero código espagueti en los archivos `code-behind` (`.xaml.cs`).
* **Repository Pattern (`IGenericRepository<T>`, `DeliveryNoteRepository`)**: Abstracción completa de las operaciones de lectura y escritura, aislando el motor de persistencia de la capa de servicios.
* **Service Layer (`ICustomerService`, `IDeliveryNoteService`, `ICommissionService`, etc.)**: Toda la inteligencia comercial reside en servicios orquestados, validando reglas de negocio antes de tocar la base de datos.
* **Factory Pattern (`NinOSDbContextFactory`, `DbConnectionFactory`)**: Gestión eficiente del ciclo de vida de los contextos tanto en tiempo de ejecución como para herramientas de migración por consola.
* **XAML Value Converters**: Transformación desacoplada de tipos de dominio en representaciones gráficas reactivas (`BalanceColorConverter`, `BooleanToVisibilityConverter`).

---

## 📂 Estructura del Repositorio

```text
NinOS/
├── migrations/                                     # 🗄️ Parches DDL y scripts SQL complementarios
│   ├── add_bank_and_observations.sql               # Soporte para entidades financieras y notas
│   ├── add_payment_and_commission_fields.sql       # Extensión para comisiones y liquidación
│   └── add_payment_audit_columns.sql               # Campos de auditoría contable en pagos
│
├── src/
│   ├── NinOS.Domain/                               # 🔷 CAPA DE DOMINIO (Núcleo Puro sin Dependencias)
│   │   ├── ViewModels/                             # DTOs optimizados para reportes y proyecciones
│   │   │   ├── accounts_receivable_dto.cs          # DTO de cuentas por cobrar y antigüedad
│   │   │   ├── commission_dto.cs                   # DTO para balance consolidado de comisiones
│   │   │   ├── note_print_dto.cs                   # DTO preparado para el generador PDF
│   │   │   └── payment_dto.cs                      # DTO para recibos y movimientos de caja
│   │   ├── comission.cs                            # Entidad de comisión devengada
│   │   ├── customer.cs                             # Entidad maestra de clientes
│   │   ├── delivery_note.cs                        # Entidad cabecera de nota de entrega
│   │   ├── note_detail.cs                          # Entidad línea de detalle de productos/combos
│   │   ├── payment.cs                              # Entidad transaccional de pagos y abonos
│   │   ├── product.cs                              # Entidad maestra de productos
│   │   ├── promotion.cs                            # Entidad de paquetes comerciales
│   │   ├── promotion_item.cs                       # Entidad de ítems asociados a promociones
│   │   ├── seller.cs                               # Entidad maestra de vendedores
│   │   └── NinOS.Domain.csproj                     # Proyecto .NET Class Library
│   │
│   ├── NinOS.Infrastructure/                       # 🔶 CAPA DE INFRAESTRUCTURA Y ACCESO A DATOS
│   │   ├── data/                                   # Contextos de datos y fábricas de conexión
│   │   │   ├── DbConnectionFactory.cs              # Fábrica de conexiones Npgsql
│   │   │   ├── DbInitializer.cs                    # Siembra inicial de datos (Seeding)
│   │   │   ├── NinOSDbContext.cs                   # DbContext central con mapeos Fluent API
│   │   │   └── NinOSDbContextFactory.cs            # Fábrica en tiempo de diseño para EF Tools
│   │   ├── Migrations/                             # Historial de migraciones Code-First versionadas
│   │   │   ├── 20260802172449_InitialCreate.cs     # Esquema relacional inicial
│   │   │   └── 20260813155508_FixNoteDetail...     # Refactorización de prefijos y detalles
│   │   ├── Repositories/                           # Implementación del Patrón Repositorio
│   │   │   ├── Interfaces/                         # IGenericRepository, IDeliveryNoteRepository
│   │   │   └── Implementations/                    # GenericRepository, DeliveryNoteRepository
│   │   ├── Services/                               # Implementación de Casos de Uso y Servicios
│   │   │   ├── Interfaces/                         # IAccountsReceivableService, ICommissionService...
│   │   │   └── Implementations/                    # AccountsReceivableService, CommissionService,
│   │   │                                           # CustomerService, DeliveryNoteService,
│   │   │                                           # InventoryService, PaymentService
│   │   └── NinOS.Infrastructure.csproj             # Dependencias: EF Core, Npgsql, LINQ
│   │
│   └── NinOS.UI/                                   # 🔴 CAPA DE PRESENTACIÓN (WPF / XAML / MVVM)
│       ├── Common/                                 # Clases base, infraestructura MVVM y utilitarios
│       │   ├── NotePdfGenerator.cs                 # Motor vectorial de renderizado PDF
│       │   ├── RelayCommand.cs                     # Implementación desacoplada de ICommand
│       │   ├── ViewModelBase.cs                    # Clase base reactiva con INotifyPropertyChanged
│       │   └── ViewModels/                         # ViewModels de interacción de usuario
│       │       ├── AccountsReceivableViewModel.cs  # Lógica de Cartera y Cuentas por Cobrar
│       │       ├── CommissionsViewModel.cs         # Lógica de Liquidación de Vendedores
│       │       ├── CustomerViewModel.cs            # Lógica de Directorio de Clientes
│       │       ├── DeliveryNotesViewModel.cs       # Lógica del Explorador de Despachos
│       │       ├── InventoryViewModel.cs           # Lógica de Inventario y Combos
│       │       ├── MainWindowViewModel.cs          # Lógica del Dashboard y Navegación
│       │       ├── PaymentsViewModel.cs            # Lógica de Tesorería y Caja
│       │       └── SalesViewModel.cs               # Lógica de Emisión de Ventas
│       ├── Converters/                             # Value Converters para enlace dinámico XAML
│       │   ├── BalanceColorConverter.cs            # Formateo condicional por saldo deudor
│       │   ├── BooleanToVisibilityConverter.cs     # Enlace Booleano a Visibility
│       │   ├── InverseBooleanToVisibility...       # Inversor Booleano a Visibility
│       │   └── StringToVisibilityConverter.cs      # Enlace String a Visibility
│       ├── Views/                                  # Vistas, Formularios y Modales XAML
│       │   ├── MainWindow.xaml                     # Contenedor principal de la aplicación
│       │   ├── SalesView.xaml                      # Pantalla de Facturación y Ventas
│       │   ├── InventoryView.xaml                  # Panel de Control de Inventario
│       │   ├── CustomerView.xaml                   # Directorio de Clientes
│       │   ├── DeliveryNotesView.xaml              # Historial y búsqueda de Notas de Entrega
│       │   ├── AccountsReceivableView.xaml         # Panel de Cuentas por Cobrar
│       │   ├── PaymentsView.xaml                   # Panel de Recepción de Pagos
│       │   ├── CommissionsView.xaml                # Panel de Comisiones
│       │   ├── NotePreviewWindow.xaml              # Ventana modal de vista previa de notas
│       │   ├── PaymentHistoryWindow.xaml           # Modal de historial de pagos por cliente
│       │   ├── PaymentNoteHistoryWindow.xaml       # Modal de pagos por nota de entrega
│       │   ├── AddCustomerWindow.xaml              # Formulario de alta de clientes
│       │   ├── AddProductWindow.xaml               # Formulario de alta de productos
│       │   ├── AddPromotionWindow.xaml             # Formulario de combos y promociones
│       │   ├── AddPaymentWindow.xaml               # Modal de registro de pago
│       │   └── AddCommissionPaymentWindow.xaml     # Modal de liquidación de comisiones
│       ├── App.xaml / App.xaml.cs                  # Configuración de inicio y contenedor
│       └── NinOS.UI.csproj                         # Ensamblado ejecutable WPF
│
├── NinOS.slnx                                      # Solución unificada .NET en formato XML moderno
└── README.md                                       # Documentación de ingeniería del proyecto
🛠️ Tecnologías y HerramientasComponenteTecnologíaRol en la SoluciónLenguaje CoreC# 13Tipado estricto, abstracciones POO, pattern matching y recordsPlataforma Runtime.NET 10 LTSMotor de ejecución optimizado de ultra alto desempeñoFramework UIWPF (XAML)Renderizado reactivo por GPU con enlaces de datos avanzadosBase de DatosPostgreSQL 16Persistencia relacional, concurrencia MVCC y transacciones ACIDMapeador ORMEntity Framework Core 10Migraciones Code-First y consultas optimizadas LINQ vía NpgsqlMotor de ReportesPDF Vectorial NativoGeneración sin dependencias pesadas de facturas y notas de entregaGestión de VersionesGit & GitHubArquitectura de ramas y control de cambios estructurado🚀 Instalación y Configuración📋 Prerrequisitos del Entorno.NET 10 SDK instalado globalmente.Instancia local o remota de PostgreSQL (v15 o superior).IDE recomendado: Visual Studio 2022/2026 con la carga Desarrollo de escritorio de .NET o VS Code con el paquete C# Dev Kit.1️⃣ Clonar el RepositorioBashgit clone [https://github.com/lulujax/ninos.git](https://github.com/lulujax/ninos.git)
cd ninos
2️⃣ Configurar la Cadena de ConexiónAjusta los parámetros de tu servidor PostgreSQL en el archivo NinOS.Infrastructure/data/DbConnectionFactory.cs o define la variable de entorno correspondiente:C#"Host=localhost;Port=5432;Database=ninos_db;Username=postgres;Password=tu_password_segura;"
3️⃣ Restaurar Dependencias y CompilarBashdotnet restore NinOS.slnx
dotnet build NinOS.slnx --configuration Release
4️⃣ Aplicar Migraciones de Base de DatosEjecuta la migración de Entity Framework Core para generar el esquema completo de base de datos de manera automática:Bashdotnet ef database update --project src/NinOS.Infrastructure --startup-project src/NinOS.UI
💡 Nota sobre scripts de auditoría: Si requieres aplicar manualmente los parches SQL de observaciones y auditoría contable, ejecuta en tu terminal:Bashpsql -U postgres -d ninos_db -f migrations/add_bank_and_observations.sql
psql -U postgres -d ninos_db -f migrations/add_payment_and_commission_fields.sql
psql -U postgres -d ninos_db -f migrations/add_payment_audit_columns.sql
5️⃣ Ejecutar la Suite NinOSBashdotnet run --project src/NinOS.UI
📊 Modelo Entidad-Relación (ERD)Fragmento de códigoerDiagram
    CUSTOMER ||--o{ DELIVERY_NOTE : "genera"
    CUSTOMER ||--o{ PAYMENT : "abona"
    SELLER ||--o{ CUSTOMER : "gestiona"
    SELLER ||--o{ COMISSION : "acumula"
    
    DELIVERY_NOTE ||--|{ NOTE_DETAIL : "contiene"
    DELIVERY_NOTE ||--o{ PAYMENT : "recibe"
    DELIVERY_NOTE ||--o{ COMISSION : "origina"
    
    PRODUCT ||--o{ NOTE_DETAIL : "despachado_en"
    PRODUCT ||--o{ PROMOTION_ITEM : "conforma"
    
    PROMOTION ||--|{ PROMOTION_ITEM : "agrupa"
    PROMOTION ||--o{ NOTE_DETAIL : "vendido_en"
🔮 Hoja de Ruta (Roadmap)[x] ✅ Módulo de Catálogo Maestro, Control de Existencias y Promociones Compuestas.[x] ✅ Motor de Emisión de Notas de Entrega y Generador PDF Vectorial.[x] ✅ Panel Integral de Cuentas por Cobrar (CxC) con Semáforo de Morosidad.[x] ✅ Auditoría de Pagos, Abonos Fraccionados y Conciliación Bancaria.[x] ✅ Algoritmo de Liquidación y Auditoría de Comisiones a Vendedores.[ ] 🔄 Arquitectura Multi-Sucursal con Sincronización en la Nube vía WebSockets.[ ] 📊 Tablero de Control Gerencial con Métricas de Rentabilidad y Proyección de Flujo de Caja.[ ] 🏷️ Integración con Impresoras Térmicas de Despacho y Lectores de Códigos de Barra 2D.👨‍💻 Sobre el Autor — LulujaxNinOS es el resultado de cientos de horas de diseño, arquitectura, refinamiento de código y pasión por crear software que marque la diferencia.Luis "Lulujax"Software Engineer & Creator of NinOS"El código limpio no es un lujo estético, es el compromiso ético
 del ingeniero con la calidad, la velocidad y la excelencia."
⭐ Si este proyecto te ha resultado inspirador, déjale una estrella en el repositorio. ⭐