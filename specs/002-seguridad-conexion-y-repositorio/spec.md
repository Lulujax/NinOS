# 002 - Seguridad de la conexión y del repositorio

## Qué y por qué

Hoy la contraseña de la base de datos es de 4 caracteres, está escrita en el código y en el `appsettings.json` que se instala en cada PC, y es la misma para producción y testing. El servidor acepta conexiones desde internet. Además el repositorio de GitHub es **público** y contiene un respaldo de la base de producción y copias de datos de notas y comisiones. Cualquier persona puede leer esos datos o intentar entrar a la base. Esta spec cierra esas puertas.

## Requisitos

- **RF-1** EL SISTEMA no tendrá ninguna contraseña, usuario ni dirección de servidor escritos en el código fuente ni en archivos versionados en Git.
- **RF-2** EL SISTEMA leerá la cadena de conexión solo de la variable de entorno `NINOS_DB_CONNECTION` o de un `appsettings.json` local que Git ignora.
  - Decisión de Luis (2026-11-26): en la PC de Luis ya no se usa la variable, para separar producción de testing por archivo. La NinOS instalada lee `%LocalAppData%\NinOS\appsettings.json` (producción); el desarrollo, las pruebas y `dotnet ef` leen `src\NinOS.UI\appsettings.json` (testing), que se copia a la salida al compilar. La variable sigue funcionando como opción, pero si existe le gana a los dos archivos.
- **RF-3** SI no hay ninguna cadena de conexión configurada, ENTONCES EL SISTEMA se negará a arrancar con un mensaje claro y NO se conectará a producción por defecto.
- **RF-4** EL SISTEMA usará contraseñas distintas y fuertes para producción y testing, y usuarios de base distintos.
- **RF-5** EL SISTEMA tendrá el acceso a la base reforzado, no cerrado: las PCs se conectan desde cualquier internet, así que el puerto queda accesible pero con contraseña larga y aleatoria, cifrado TLS obligatorio, usuario sin superusuario y puerto distinto del 5432.
- **RF-6** EL SISTEMA no tendrá en el repositorio respaldos de base de datos ni copias de datos reales (`*.dump`, `scratch/`, JSON o SQL con datos).
- **RF-7** CUANDO se instale en una PC del cliente, EL SISTEMA obtendrá la conexión de un archivo de configuración creado por el instalador, no del código.
- **RF-8** EL SISTEMA no guardará la clave del Panel Admin en el código ni en archivos de la PC; se guardará solo como hash dentro de la base de datos y no volverá a una clave por defecto si falta.
- **RF-9** SI la clave del Panel Admin falta o está dañada en la base, ENTONCES EL SISTEMA bloqueará el acceso al panel en vez de restaurar una clave por defecto.
- **RF-14** EL SISTEMA guardará la clave del Panel Admin en una tabla de la base, como hash irreversible con sal, de modo que cualquier PC que se conecte a esa base pida la misma clave.
- **RF-15** SI la clave nueva del Panel Admin tiene menos de 12 caracteres, ENTONCES EL SISTEMA la rechazará y no guardará nada.- **RF-16** La combinación Ctrl+Alt+P puede mostrar la ventana de clave en cualquier PC; el panel sigue siendo secreto porque solo Luis conoce la combinación (decisión de Luis, 2026-11-16).
- **RF-10** EL SISTEMA tendrá el historial público de Git sin respaldos de base de datos, sin datos reales y sin contraseñas (se reescribe el historial), sabiendo que copias y clones anteriores pueden existir: por eso la rotación de contraseñas (RF-4) es obligatoria y va primero.
- **RF-11** EL SISTEMA no incluirá credenciales en el instalador: el `appsettings.json` se crea en cada PC al instalar, fuera del contenido de `publish\`.
- **RF-12** EL SISTEMA usará para la app un usuario de base que sea dueño únicamente de las tablas de NinOS: puede crear y modificar sus tablas (porque la app aplica las migraciones al arrancar), pero no es superusuario, no puede crear ni borrar bases, ni leer las de otros.
- **RF-13** EL SISTEMA exigirá TLS en la conexión a la base, de modo que lo que viaja entre la PC y el servidor vaya cifrado.

## Cómo sé que está listo

- Buscar `Password=` y la IP del servidor en el repositorio (todo el historial reciente) no devuelve nada.
- Sin configuración, la app muestra el error y no abre conexión.
- Desde una red cualquiera, conectarse al puerto 5432 del VPS falla.
- El `.gitignore` cubre respaldos, `scratch/`, `appsettings*.json` con secretos y `admin_security.json`.
- Producción y testing no comparten credenciales.
- `dotnet build NinOS.slnx` con 0 errores y 0 advertencias.

## Fuera de alcance

- Login de usuarios y auditoría por persona (decisión del cliente: no existen).
- Cifrado de datos dentro de la base.

## Decisiones tomadas

- El repositorio **se queda público** (decisión de Luis). Por eso todo lo que alguna vez estuvo en él se considera filtrado: la contraseña actual debe cambiarse (rotarse) y el historial se reescribe para quitar respaldos y datos.
- El respaldo `backup_ninos_db_pre_v1.0.9.dump` puede traer datos reales de clientes del cliente: se trata como filtración de datos y se le avisa a Luis para decidir si hay que informar al cliente.

- Las PCs del cliente se conectan desde **cualquier internet**, así que una lista de IPs permitidas no sirve. Decisión de Luis: **puerto reforzado**, no VPN. Se acepta que el puerto siga alcanzable desde internet; la protección es contraseña fuerte, TLS, usuario limitado y puerto no estándar.
- Las **migraciones las sigue aplicando la app al arrancar** (decisión de Luis). Por eso el usuario de la app necesita permisos para crear y modificar sus tablas, y ese riesgo se acepta.
- **Riesgo aceptado por el cliente:** la app se instala con permisos de administrador y todos los usuarios de la PC tienen acceso a su carpeta, así que cualquiera de ellos podría reemplazar el ejecutable. No se restringe.
