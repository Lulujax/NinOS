# NinOS

ERP de escritorio para una distribuidora: notas de entrega, cuentas por cobrar, pagos, Pro Venta, notas de crédito, comisiones de vendedoras, inventario, listas de precios y reportes PDF/Excel. Lo usa un cliente real a diario: cada error toca dinero de verdad.

## Stack

- .NET 10, WPF (MVVM), EF Core 10 con PostgreSQL (Npgsql), inyección de dependencias de Microsoft.
- PDF con QuestPDF, Excel con ClosedXML, instalador con Inno Setup (`installer/`).
- Capas: `src/NinOS.Domain` (entidades y DTOs) → `src/NinOS.Infrastructure` (datos, servicios, migraciones) → `src/NinOS.UI` (ventanas, vistas, reportes).
- Convención existente: nombres en snake_case para entidades, variables y campos (`delivery_note`, `settings_path`). Se respeta en código nuevo.
- Logs: usar `AppLog.Info / Warn / Error` (`src/NinOS.Infrastructure/Logging/AppLog.cs`), que escribe en `ninos-ui.log` junto al ejecutable.

## Cómo se compila y se prueba

- Compilar: `dotnet build NinOS.slnx` (debe quedar con 0 errores y 0 advertencias).
- Ejecutar: `dotnet run --project src/NinOS.UI`
- Pruebas unitarias: todavía no existe proyecto de pruebas. Hasta que exista, cada tarea dice cómo probarla a mano.

## Ramas y bases de datos

- `main` = producción, base `ninos_db`. `TESTING` = pruebas, base `ninos_db_testing`.
- Se trabaja en `TESTING` (o en una rama que sale de ella). A `main` solo pasa lo que el cliente aprobó.
- `src/NinOS.UI/appsettings.json` es distinto en cada rama (apunta a una base u otra). Al unir ramas, nunca dejar que el de TESTING llegue a main ni al revés.

## Cuidado con la base de datos

- Al arrancar, la app aplica sola las migraciones pendientes (`DbInitializer` → `Database.Migrate()`) a la base a la que se conecte.
- Los comandos `dotnet ef` no usan el appsettings de la rama: si no hay variable `NINOS_DB_CONNECTION`, se conectan a la base de **producción**. Nunca correr `dotnet ef database update` ni similares sin antes definir esa variable apuntando a testing, y preguntarme primero.
- Los cambios de estructura van como migración de EF Core en `src/NinOS.Infrastructure/Migrations`, nunca a mano.
- Nunca borrar ni modificar notas cobradas, pagos o comisiones ya pagadas sin que una spec lo diga.

## Reglas de negocio

@AGENTS.md
