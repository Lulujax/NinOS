# Tareas 002 - Seguridad de la conexión y del repositorio

Orden recomendado. Una a la vez. Las marcadas (manual) las haces tú en el VPS o en tu PC; yo te dejo los comandos.

## Tarea 0 - Proyecto de pruebas
Cubre: regla del CLAUDE.md personal (toda función con lógica relevante lleva prueba unitaria)
- Crear `tests/NinOS.Tests` con xUnit, agregarlo a `NinOS.slnx` y referenciar Domain, Infrastructure y UI.
- Hecho cuando: `dotnet test NinOS.slnx` corre y pasa.
- Cómo probarla: `dotnet test NinOS.slnx`.
- [x] Hecha y probada — proyecto creado, 26 pruebas pasando.

## Tarea 1 (manual) - Respaldo antes de tocar nada
Cubre: RF-4 (preparación)
- Sacar un respaldo de `ninos_db` y de `ninos_db_testing` y guardarlos **fuera del repo** (por ejemplo `D:\respaldos\`).
- Hecho cuando: existen dos archivos y se pueden listar con `pg_restore --list`.
- Cómo probarla: abrir el respaldo con `pg_restore --list archivo.dump`.
- [x] Hecha y probada — 2026-11-15: respaldos en `Programacion Trabajo\NinOS\respaldos\` (fuera del repo, carpeta padre sin Git): `ninos_db_2026-11-15_1549.dump` y `ninos_db_testing_2026-11-15_1549.dump`, ambos listados con `pg_restore --list` (199 objetos) y con datos distintos y reales. Contienen datos reales de clientes: no subirlos a ningún lado.

## Tarea 2 (manual) - Rotar el usuario y la contraseña de testing
Cubre: RF-4
- Crear un usuario nuevo para testing con contraseña larga y aleatoria (guardada en un gestor de contraseñas) y poner la cadena en tu variable de entorno `NINOS_DB_CONNECTION`.
- Hecho cuando: la app en debug conecta a testing con el usuario nuevo.
- Cómo probarla: `dotnet run --project src/NinOS.UI` abre normal.
- [~] Parcialmente hecha — 2026-11-15: creado `ninos_test` (clave aleatoria de 32 caracteres, guardada solo en tu variable de entorno de usuario `NINOS_DB_CONNECTION` y en los `appsettings.json` locales, fuera de Git) y `ninos_app` para producción (su cadena quedó en `installer\private\appsettings.client.json`, fuera de Git). Las 145 pruebas pasan con `ninos_test`. **Falta que abras la app en debug con el usuario nuevo y copies las claves a tu gestor de contraseñas.** `ninos_admin` no se toca hasta el día de la rotación (tarea 10).

## Tarea 3 - La app no trae conexión escrita y falla claro sin configuración
Cubre: RF-1, RF-2, RF-3
- Quitar `DefaultConnectionString` de `DbConnectionFactory`; si no hay variable ni archivo, lanzar un error con mensaje claro y mostrarlo antes de abrir la pantalla de carga, sin conectarse.
- Hecho cuando: sin configuración sale el mensaje y la app se cierra; con la variable arranca.
- Cómo probarla: quitar la variable y renombrar `appsettings.json` junto al `.exe` de debug → mensaje claro. Poner la variable → abre.
- [x] Hecha y probada — 13 pruebas unitarias en `DbConnectionFactoryTests` (caso feliz, límite y error), más una de regresión que falla si alguien vuelve a escribir una conexión en el código. **Falta tu prueba manual:** abrir la app y abrirla sin `appsettings.json`.

## Tarea 4 - Sacar appsettings.json de Git y dejar un ejemplo
Cubre: RF-1, RF-2
- Agregar `appsettings.json` a `.gitignore`, crear `src/NinOS.UI/appsettings.example.json` sin credenciales, `git rm --cached` del archivo real y revisar el `.csproj` para que no se copie solo si no existe.
- Hecho cuando: `git status` no muestra el archivo y el build queda en 0 errores y 0 advertencias.
- Cómo probarla: `dotnet build NinOS.slnx`; `git ls-files | grep appsettings` solo muestra el ejemplo.
- [x] Hecha y probada — `appsettings.json` fuera de Git, `appsettings.example.json` versionado, `git ls-files` ya no lo lista y la compilación queda en 0/0.

## Tarea 5 - Sacar respaldo y datos reales del árbol de trabajo
Cubre: RF-6
- Mover `backup_ninos_db_pre_v1.0.9.dump` y `scratch/` a una carpeta fuera del repo; agregar `*.dump`, `scratch/`, `*.sql` con datos y `admin_security.json` al `.gitignore`.
- Hecho cuando: `git ls-files` no lista ninguno de esos archivos.
- Cómo probarla: `git ls-files | grep -E "dump|scratch"` no devuelve nada.
- [x] Hecha y probada — 2026-11-15: `scratch/` (69 archivos) movida a `Programacion Trabajo\NinOS\respaldos\scratch`; `git ls-files | grep -E "dump|scratch"` no devuelve nada. El `backup_ninos_db_pre_v1.0.9.dump` ya no estaba en el repo ni en el Escritorio.

## Tarea 6 - El instalador no lleva credenciales
Cubre: RF-7, RF-11
- En `NinOS_Setup.iss`: excluir `appsettings.json` de `publish\*`, agregar la línea del archivo privado `installer\private\appsettings.client.json` (ignorado por Git) con `DestName: appsettings.json` y `onlyifdoesntexist`.
- Hecho cuando: el instalador generado instala el archivo del cliente y una reinstalación no lo pisa.
- Cómo probarla: instalar en una PC o VM de prueba apuntando a testing; abrir la app; reinstalar y confirmar que el archivo conserva su contenido.
- [ ] Hecha y probada

## Tarea 7 - Panel Admin sin clave por defecto
Cubre: RF-8, RF-9
- Quitar `DefaultPassword`; si falta o está dañado `admin_security.json` el panel no abre y queda en el log; agregar el argumento oculto `--set-admin-password`.
- Hecho cuando: sin archivo no se puede entrar; creando la clave con el argumento sí.
- Cómo probarla: borrar `admin_security.json` → intentar entrar al panel (no entra); ejecutar con `--set-admin-password`, crear clave → entra.
- [x] Hecha y probada — 13 pruebas unitarias en `AdminSecurityTests`, incluida una de regresión que falla si alguien vuelve a poner una clave por defecto. **Falta tu prueba manual:** el argumento `--set-admin-password` abre ventanas WPF que una prueba no puede verificar.

## Tarea 8 (manual guiada) - Reescribir el historial de Git
Cubre: RF-10
- Clonar un espejo de respaldo; ejecutar `git filter-repo` quitando `.dump`, `scratch/`, `appsettings.json` y reemplazando el texto de las contraseñas; **antes del force-push te pido confirmación**.
- Hecho cuando: buscar el nombre del `.dump` y la contraseña vieja en todo el historial no devuelve nada.
- Cómo probarla: `git log --all -- backup_ninos_db_pre_v1.0.9.dump` vacío; `git grep "<contraseña vieja>" $(git rev-list --all)` vacío.
- [ ] Descartada por decisión de Luis (2026-11-16): el historial viejo se queda en GitHub y no se reescribe más.

## Tarea 9 (manual, día de la visita, ver spec 011) - Reforzar el acceso a la base
Cubre: RF-5, RF-13
- En el VPS: mover PostgreSQL a un puerto no estándar, exigir TLS en `pg_hba.conf` (`hostssl`), cerrar el 5432 en el firewall y rechazar conexiones sin cifrar. En la app, agregar `SSL Mode=Require` a la cadena de conexión.
- Hecho cuando: la app conecta por el puerto nuevo con TLS, y una conexión sin cifrar o al 5432 es rechazada.
- Cómo probarla: abrir la app (funciona); intentar conectar al 5432 con pgAdmin → rechazado; intentar sin TLS → rechazado.
- [ ] Hecha y probada

## Tarea 10 (manual, día de la visita, ver spec 011) - Rotar producción y actualizar las PCs
Cubre: RF-4, RF-7
- Crear usuario y contraseña nuevos de producción, instalar la versión nueva con su `appsettings.json` en cada PC del cliente y anular el usuario viejo.
- Hecho cuando: todas las PCs abren con el usuario nuevo y el viejo ya no conecta.
- Cómo probarla: abrir la app en cada PC; intentar conectar con `ninos_admin` / `1234` → rechazado.
- [ ] Hecha y probada

## Tarea 11 (manual, día de la visita, ver spec 011) - Usuario de la app limitado a sus tablas
Cubre: RF-12
- Crear el rol dueño de las tablas de NinOS: puede crear y modificar sus tablas (necesario porque la app migra al arrancar), pero sin `SUPERUSER`, sin `CREATEDB`, sin `CREATEROLE` y sin acceso a otras bases.
- Hecho cuando: la app arranca y aplica migraciones con ese usuario, y ese usuario no puede ver ni tocar otras bases.
- Cómo probarla: recorrer una nota, un pago, una nota de crédito y una comisión; con ese usuario intentar `CREATE DATABASE prueba` → rechazado; intentar listar otras bases → sin acceso.
- [~] Parcialmente hecha — 2026-11-15, en testing: `ninos_test` (sin superusuario, sin crear bases, sin crear roles) es dueño de `ninos_db_testing` y de sus tablas; `CREATE DATABASE` → rechazado; leer las tablas de producción → "permission denied"; las 145 pruebas pasan con ese usuario, incluidas las que migran la base. En producción falta aplicar `installer\private\rotacion_produccion.sql` con `ninos_admin` el día de la rotación (tarea 10), junto con instalar `appsettings.client.json` en las PCs. La lista de nombres de bases no se puede ocultar en PostgreSQL; sí se bloquea conectarse a ellas.

## Tarea 12 (manual) - Pedir a GitHub la purga de caché
Cubre: RF-10
- Escribir a GitHub Support para que eliminen los commits antiguos accesibles por SHA.
- Hecho cuando: GitHub confirma.
- [x] Hecha y probada — Decisión de Luis (2026-11-15): se da por cerrada sin enviar la solicitud porque llevaría tiempo; el repositorio sigue público y todo lo que estuvo en el historial se considera filtrado.
