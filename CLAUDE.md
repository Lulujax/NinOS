# NinOS

ERP de escritorio para una distribuidora: notas de entrega, cuentas por cobrar, pagos, Pro Venta, notas de crédito, comisiones de vendedoras, inventario, listas de precios y reportes PDF/Excel. Lo usa un cliente real a diario: cada error toca dinero de verdad.

## Stack

- .NET 10, WPF (MVVM), EF Core 10 con PostgreSQL (Npgsql), inyección de dependencias de Microsoft.
- PDF con QuestPDF, Excel con ClosedXML, instalador con Inno Setup (`installer/`).
- Capas: `src/NinOS.Domain` (entidades y DTOs) → `src/NinOS.Infrastructure` (datos, servicios, migraciones) → `src/NinOS.UI` (ventanas, vistas, reportes).
- Convención existente: nombres en snake_case para entidades, variables y campos (`delivery_note`, `settings_path`). Se respeta en código nuevo.
- Logs: usar `AppLog.Info / Warn / Error` (`src/NinOS.Infrastructure/Logging/AppLog.cs`), que escribe en `ninos-ui.log` en la carpeta de datos (`LocalDataFolder`): `%LocalAppData%\NinOS\` en una instalación de Velopack, junto al ejecutable en desarrollo. Ahí también viven `appsettings.json`, `backup_config.json` y `Backups` (spec 010, RF-24).

## Cómo se compila y se prueba

- Compilar: `dotnet build NinOS.slnx` (debe quedar con 0 errores y 0 advertencias).
- Ejecutar: `dotnet run --project src/NinOS.UI`
- Pruebas: `dotnet test NinOS.slnx` (proyecto `tests/NinOS.Tests`, xUnit). **Toda función con lógica relevante lleva prueba unitaria con caso feliz, caso límite y caso de error**, según el CLAUDE.md personal de Luis. Una tarea no está terminada si sus pruebas no pasan.
- Lo que una prueba automática no puede cubrir (ventanas WPF, PDF, instalador, servidor, antivirus) se prueba a mano contra la base de **testing**, y cada tarea dice cómo.
- Las pruebas que toquen la base **deben negarse a correr** si la cadena de conexión no apunta a una base de testing. Nunca una prueba contra producción.

## Ramas y bases de datos

- `main` = producción, base `ninos_db`. `TESTING` = pruebas, base `ninos_db_testing`.
- Se trabaja en `TESTING` (o en una rama que sale de ella). A `main` solo pasa lo que el cliente aprobó.
- `src/NinOS.UI/appsettings.json` **no va en Git** (spec 002). La base a la que apunta cada PC sale de la variable de entorno `NINOS_DB_CONNECTION` (tu PC → testing) o del archivo local que coloca el instalador (PCs del cliente → producción). Ya no depende de la rama, así que al unir ramas no hay riesgo de cruzar bases.

## Cuidado con la base de datos

- **Al arrancar, la app aplica sola las migraciones pendientes** (`Database.Migrate()`) a la base a la que se conecte. Es decisión de Luis. A cambio, el usuario de base de la app necesita permisos para crear y modificar tablas, y ese riesgo se acepta.
- Las migraciones deben ser **aditivas** siempre que se pueda: agregar columna o tabla nueva, con valor nulo o valor por defecto. Nunca borrar, renombrar ni cambiar el tipo de una columna que la versión anterior use. Una migración destructiva obliga a una ventana de mantenimiento con todas las PCs cerradas (ver spec 010).
- Los comandos `dotnet ef` no usan el appsettings de la rama: si no hay variable `NINOS_DB_CONNECTION`, se conectan a la base de **producción**. Nunca correr `dotnet ef database update` ni similares sin antes definir esa variable apuntando a testing, y preguntarme primero.
- Los cambios de estructura van como migración de EF Core en `src/NinOS.Infrastructure/Migrations`, nunca a mano.
- Nunca borrar ni modificar notas cobradas, pagos o comisiones ya pagadas sin que una spec lo diga.

## Decisiones del cliente que no se discuten

- **Una base con datos nunca se toca al abrir la app.** El arranque no ejecuta ningún INSERT, UPDATE ni DELETE de datos de negocio (zonas, clientes, vendedoras, notas, pagos, tipos de nota). Las correcciones de datos se hacen con scripts supervisados, con respaldo previo y spec.
- **Los clientes no se asignan a un vendedor:** son un maestro general y se les asigna una **zona**; el vendedor sale de la zona. El campo `customer.seller_name` es un resto del sistema anterior.
- **Sin usuarios ni registro de quién hizo qué.** El cliente lo decidió así: la app no tiene login ni auditoría por persona. No proponerlo ni agregarlo; si hace falta rastro, se deja en logs técnicos sin datos sensibles.
- **El Panel Admin es secreto.** Solo lo conoce Luis (Lulujax); el cliente no sabe que existe. Debe permanecer oculto: no se menciona en pantallas, mensajes, instalador, manuales ni documentación para el cliente.
- **El Panel Admin es exclusivo de Luis.** Sirve solo para decisiones de Luis y para recuperar datos, y es un servicio aparte del sistema que usa el cliente. El cliente no debe saber nada de él: ni su existencia, ni su clave, ni cómo entrar. Ningún cambio de la app que use el cliente puede depender del Panel Admin ni mostrar mensajes sobre él.
- **No existe "eliminar pago".** Un pago registrado no se borra desde el sistema. No agregar esa función.
- **Una nota emitida queda congelada.** Al exportar o imprimir (PDF, Excel o impresión directa) una nota, sale siempre igual a como se creó: montos, descuentos, días de crédito, condiciones, datos del cliente, códigos y nombres de productos y composición de promociones. Las ediciones posteriores (CxC, Pro Venta, notas de crédito) cambian los saldos del sistema, nunca la nota impresa.
- **Todo documento impreso o exportado a PDF es tamaño carta.**
- **Cada hoja de una nota de entrega o de crédito es 1 sola página tamaño carta, sin importar la empresa (General, Pro Venta o Vesupronta).** Una nota impresa se compone de una hoja ORIGINAL (1 página) y una hoja COPIA (1 página); en Vesupronta además una hoja ADMINISTRACION (1 página). Lo que nunca debe pasar es que una misma hoja (por ejemplo la ORIGINAL) ocupe 2 páginas, ni que haya 2 ORIGINAL o 2 COPIA. Si una hoja se pasa a una segunda página, es un error gravísimo: no se entrega ni se imprime, se corrige antes.
- **Una nota tiene como máximo 20 productos (renglones), en todas las empresas.** Es la regla que mantiene cada hoja en 1 página.
- **Descuentos de volumen:** se suman al descuento de condición (10% + 5% = 15%), no se aplican en cascada.
- **Numeración:** el sistema se reinició; las notas son generales (correlativo global `000_001`), ya no se identifican por vendedor. Los números viejos tipo `3200-715` son del sistema anterior.
- **Stock negativo permitido** en notas de entrega.
- **Pro Venta:** cada porcentaje se calcula **por nota** sobre el monto original (sin descuento aplicado): comisión = 10%; gastos operativos = 25%; gastos administrativos = 15% de (monto original − gastos operativos). En las relaciones debe verse el monto sin descuento aplicado. A futuro (pendiente, no hacerlo todavía) se podrá editar monto y descuento como en CxC general, por si el cliente pagó con o sin descuento. Pro Venta aún no está terminado.
- **El repositorio de GitHub es público.** Nada de contraseñas, respaldos, datos reales de clientes ni archivos de configuración con secretos puede entrar al repo. Todo lo que alguna vez estuvo en el historial se considera filtrado.
- **El instalador** copia lo que haya en `publish\`: ahí no debe ir ningún `appsettings.json` con credenciales. El archivo del cliente vive en `installer\private\appsettings.client.json` (fuera de Git) y se instala solo si no existe, para que una actualización no le pise la conexión a nadie.
- **El correlativo de una nota es único y, una vez guardada, nunca cambia.** El número que se ve en pantalla antes de guardar es solo una **previsualización**: si otra persona lo tomó primero, el sistema asigna el siguiente y avisa del número definitivo. No se reservan números al abrir la nota, para que la serie no quede con huecos. El PDF, el Excel y la impresión se generan **después** de guardar, con el número definitivo.
- **Las notas emitidas antes de la foto de impresión (spec 004) se reimprimen con datos vivos.** No se les inventa una foto con los datos de hoy. La reimpresión de esas notas avisa que es una copia con datos actuales.
- **Riesgos de congelar que la empresa acepta** (spec 004): un error del sistema queda congelado en la nota; una reimpresión muestra la dirección y el teléfono que el cliente tenía al emitirla; la nota impresa y el monto a cobrar pueden mostrar cifras distintas. Si un dato está mal, se anula la nota y se hace otra. Lo único que sí hay que cuidar es que al agregar un campo nuevo al diseño de la nota, las notas viejas no se rompan: ese espacio sale en blanco y la lectura de la foto tiene que tolerarlo (campo de versión).
- **Permisos de Windows:** la app se instala con permisos de administrador y todos los usuarios de la PC tienen acceso al sistema y a su carpeta. Es decisión del cliente y el riesgo se acepta (cualquier usuario de esa PC podría reemplazar el ejecutable). No proponer restringirlo.

## Infraestructura y despliegue

- **VPS:** Ubuntu Server 20.04 LTS Minimal (es lo que ofrece el proveedor, no se cambia), 1 vCore, 1,9 GB de RAM, 29,4 GB de disco. Ahí viven PostgreSQL y, a partir de la spec 010, **nginx** sirviendo los paquetes de actualización.
- **Acceso a la base:** puerto reforzado, no VPN (decisión de Luis). Las PCs del cliente se conectan desde cualquier internet, así que el puerto queda abierto pero con contraseña larga y aleatoria, TLS obligatorio, usuario sin superusuario y puerto no estándar.
- **Actualizaciones automáticas (spec 010):** Luis publica una versión al VPS y las PCs se actualizan solas. Reglas fijas:
  - Publicar **siempre pide contraseña** a Luis.
  - El servidor guarda la versión actual **más las 3 anteriores**, para poder volver atrás rápido y que, tras una vuelta atrás, queden 2 más atrás.
  - Volver atrás devuelve el **código**, nunca los datos: si una versión mala escribió datos mal, eso se arregla con el respaldo.
  - Los paquetes van **firmados** y se sirven por **HTTPS**; la app verifica la firma antes de aplicar nada. Sin eso, el actualizador sería una forma de instalar cualquier programa en las PCs del cliente.
  - El aviso de actualización se anuncia con antelación y cuenta atrás, estilo WoW o Albion: "en X minutos el sistema se reinicia para actualizar".
  - La base guarda la **versión mínima permitida**: una PC con versión menor no puede trabajar.
- **Tamaños reales:** el paquete completo pesa 179 MB, pero el código propio son solo 2,9 MB (`NinOS.UI.dll`, `NinOS.Infrastructure.dll`, `NinOS.Domain.dll`). Por eso las actualizaciones son **diferenciales**: una corrección de código son unos 3 MB, no 179 MB.
- **Última instalación manual:** la ronda PC por PC de la spec 002 (rotar la clave de producción y colocar el `appsettings.json`) es también la que instala el actualizador. Después de esa ronda, las correcciones llegan solas.
- **La versión que se ve en pantalla sale del ejecutable, nunca de un texto escrito a mano.** El título y la barra superior se llenan con el número de `<Version>` de `NinOS.UI.csproj`. Al subir de versión no hay que tocar ningún texto de la interfaz.
- **Los paquetes publicados nunca llevan `appsettings.json`.** La publicación lo quita, así que el archivo con la conexión solo vive en cada PC.

## Manual de usuario (estándar)

- El manual se arma desde la app real contra la base de testing, nunca desde el código.
- Cada botón, campo y mensaje de error importante tiene su propia captura de la ventana de NinOS, con flechas y círculos rojos sobre lo que se explica, y una leyenda numerada corta debajo.
- Las capturas son recortes de la zona relevante. Nunca pantallazos completos del escritorio ni de la barra de tareas.
- Cada flujo muestra el estado antes y después (formulario vacío, lleno, aviso y resultado).
- Ningún texto del manual se da por bueno sin comprobarlo en pantalla. Lo que no se pudo probar se marca como pendiente, no se describe de memoria.
- Los datos de prueba se crean solo en la base de testing y usan nombres claramente de prueba.

## Reglas de negocio

@AGENTS.md
