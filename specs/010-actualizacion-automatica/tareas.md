# Tareas 010 - Actualización automática

**Requisito previo:** las tareas 1 a 7 de la spec 002 deben estar hechas (la conexión ya no vive en el código).

**Regla para todas:** nada se prueba en las PCs del cliente hasta la tarea 15.

---

## Tarea 1 (manual) - Respaldo antes de empezar
Cubre: preparación
- Respaldo de `ninos_db` y `ninos_db_testing`, y una copia del `publish\` de la versión 1.0.10 actual, guardados fuera del repo.
- Hecho cuando: existen los tres y el `.dump` se puede listar con `pg_restore --list`.
- Cómo probarla: listar el respaldo y abrir la carpeta copiada.
- [x] Hecha y probada — 2026-11-15: en `Programacion Trabajo\NinOS\respaldos\` (fuera del repo) están `ninos_db_2026-11-15_1549.dump` y `ninos_db_testing_2026-11-15_1549.dump` (ambos listados con `pg_restore --list`) y `publish-1.0.9-instalada` (444 archivos, 177,7 MB). Ojo: la versión instalada en producción y la de `publish\` es la **1.0.9**, aunque el `.csproj` dice 1.0.10; la versión de partida del actualizador es la 1.0.9.

## Tarea 2 (manual) - Averiguar qué puertos deja abiertos el proveedor
Cubre: RF-6 (preparación), riesgo 7 del plan
- nginx ya está instalado y el puerto 80 responde. Revisar el firewall del panel del proveedor y el del VPS (`ufw status`), hacer que nginx escuche temporalmente en el 443 y en el 8443, y probar desde una PC fuera del VPS cuáles llegan.
- Hecho cuando: queda anotado en este archivo qué puertos alcanzan a nginx desde internet y cuál se usará para HTTPS; o queda anotado que solo responde el 80.
- Cómo probarla: desde Windows, `Test-NetConnection <IP del VPS> -Port 443` y lo mismo con el 8443 → `TcpTestSucceeded : True` en el que sirva.
- **Si solo responde el 80, parar aquí:** la tarea 3 queda bloqueada hasta que Luis decida sobre el plan B (ver el `[NECESITA ACLARACIÓN]` del plan).
- [x] Hecha y probada — 2026-11-15: `ufw` activo con 22, 80, 443, 5432 y 54329 permitidos. nginx escuchó en el 443 con certificado autofirmado y desde la PC de Luis `Test-NetConnection` dio `True` y HTTPS respondió la página de prueba. **Se usará el 443 para HTTPS.** El proveedor no bloquea puertos; el plan B queda descartado. La página de prueba (`prueba-443` en nginx) se reemplaza en la tarea 3.

## Tarea 3 (manual, depende de la tarea 2) - HTTPS con certificado propio en el VPS
Cubre: RF-6, RF-23
- Generar en el VPS un certificado autofirmado con vigencia larga (por ejemplo 10 años) y otro de respaldo guardado fuera del servidor; configurar nginx para servirlo en el puerto elegido en la tarea 2 y publicar una carpeta `/updates` de solo lectura. Anotar la fecha de vencimiento y las dos huellas (SHA-256) para la tarea 7. Sin dominio, sin certbot.
- Hecho cuando: `https://<IP del VPS>:<puerto>/updates/` responde (el navegador avisa que el certificado no es de una autoridad conocida; es lo esperado) y subir archivos por esa vía no es posible.
- Cómo probarla: poner un archivo de prueba y descargarlo con `curl -k`; comprobar con `curl --pinnedpubkey` que con la huella correcta responde y con una huella cualquiera lo rechaza; intentar un `PUT` con curl → rechazado.
- [x] Hecha y probada — 2026-11-15, hecha por Claude por SSH y probada desde la PC de Luis. nginx sirve solo /updates/ por HTTPS en el 443 (TLS 1.2/1.3, sin listado de carpeta, sin versión en el encabezado); lo demás da 404. Certificado principal (en el VPS, vence 2036-11-12): huella de llave pública SHA-256 CaNxkh8HXtRJlcTxYcBEgRJVNkru0ZNZHZz9NikSz1c=. Certificado de respaldo (generado en la PC de Luis, vence 2036-11-12, llave privada fuera del repo y fuera del servidor): qKpO03hTCONttn2uu9wYrVOieVa1Q54acDbsFLMHOo4=. Pruebas con curl: con la huella correcta descarga; con la de respaldo o una inventada rechaza (exit 90); PUT y POST dan 403; listar la carpeta da 403; raíz y archivos inexistentes dan 404. Estas dos huellas son las que usa la tarea 7. La llave privada del certificado de respaldo está en Programacion Trabajo\NinOS\respaldos\certificado-respaldo: guardarla además en un lugar seguro y no subirla a ningún lado.

## Tarea 4 (manual) - Activar Ubuntu Pro (gratis)
Cubre: riesgo 6 del plan
- Registrar la cuenta gratuita de Ubuntu Pro (hasta 5 máquinas) y activarla en el VPS para recibir parches de seguridad sin reinstalar el sistema.
- Hecho cuando: `pro status` muestra los servicios de seguridad activos.
- Cómo probarla: ese comando y aplicar los parches pendientes.
- [x] Hecha y probada — Decisión de Luis (2026-11-15): no se activa Ubuntu Pro; el servidor se queda con la versión de Ubuntu que tiene (20.04, ya fuera del soporte estándar, sin parches extendidos). Riesgo aceptado: se mitiga con el firewall activo, lo mínimo expuesto y las actualizaciones normales de paquetes (apt).

## Tarea 5 - Empaquetar la app con Velopack (local, sin servidor)
Cubre: RF-3
- Agregar Velopack al proyecto, generar el primer paquete de la versión 1.0.10 en una carpeta local y luego un paquete 1.0.11 con un cambio mínimo, para comprobar el tamaño del diferencial.
- Hecho cuando: el paquete completo ronda los 179 MB y el diferencial de la 1.0.11 pesa pocos MB; el build queda en 0 errores y 0 advertencias.
- Cómo probarla: mirar el tamaño de los archivos generados y anotarlo en este archivo.
- [~] Hecha — 2026-11-15, medido por Claude en `respaldoselopack-pruebas\`: carpeta publicada 192 MB sin comprimir; paquete completo `NinOS-1.0.10-full.nupkg` 88,3 MB; instalador `NinOS-win-Setup.exe` 95,8 MB; diferencial 1.0.10 a 1.0.11 `NinOS-1.0.11-delta.nupkg` 0,18 MB (7 de 459 archivos cambiados). El 179 MB del plan coincide con la carpeta sin comprimir, no con el paquete: se confirma en la prueba de la tarea 14.

## Tarea 6 - La app busca y aplica la actualización al abrir
Cubre: RF-8, RF-9, RF-10, RF-7
- En el arranque, antes de terminar la pantalla de carga: buscar versión nueva, descargar mostrando el avance con `SetLoading`, verificar integridad, aplicar y reiniciar. Si algo falla, seguir con la versión actual y mostrar el motivo.
- Hecho cuando: una PC de prueba en 1.0.10 pasa sola a 1.0.11 y vuelve a abrir actualizada.
- Cómo probarla: instalar la 1.0.10 en una PC o máquina virtual de prueba, publicar la 1.0.11 en el servidor, abrir la app; luego apagar nginx y abrir otra vez → arranca igual y avisa que no pudo comprobar.
- [~] Implementado (`VelopackAppUpdater`, `DisabledAppUpdater`, integración en `start_startup_pipeline` y `create_app_updater`); compila y tiene pruebas de la configuración. Falta la prueba de punta a punta: una PC de prueba en 1.0.10 que pase sola a 1.0.11, lo que depende de la tarea 5.

## Tarea 7 - Fijar la huella del servidor en la app
Cubre: RF-7b, RF-7c, RF-22
- La app descarga con un cliente HTTP propio que solo acepta el certificado cuya huella SHA-256 coincide con una de las dos que lleva (la actual y la de respaldo), ignorando cualquier autoridad pública; sin ninguna llave de publicación dentro de la app. Las huellas no son secretas, pero la dirección del servidor y cualquier credencial no se escriben en el repositorio: salen de la configuración de la PC.
- Hecho cuando: un servidor distinto con otro certificado (incluso uno válido de una autoridad pública) es rechazado y queda en el log, y un servidor con el certificado de respaldo es aceptado.
- Cómo probarla: apuntar temporalmente la app a otro servidor de prueba con otro certificado → rechaza la actualización y lo deja en el log; cambiar el servidor de prueba al certificado de respaldo → la acepta.
- Pruebas unitarias: la comparación de huellas con caso feliz (huella actual), caso límite (huella de respaldo, mayúsculas/minúsculas y separadores) y caso de error (huella vacía o distinta).
- [x] Hecha y probada — 2026-11-15: `ServerPin` en `NinOS.Domain` (13 pruebas unitarias de la comparación: feliz, límite y error). Verificado con código de la app contra el VPS: la huella actual descarga `prueba-ninos`; la de respaldo sola y una inventada rechazan la conexión. El descargador de Velopack usa `PinnedFileDownloader` (`SimpleWebSource` con `IFileDownloader`).

## Tarea 8 - Migración: tabla de política de actualización
Cubre: RF-16
- Tabla `app_update_policy` de una sola fila: versión mínima, versión recomendada, momento del aviso y mensaje. **Antes:** `NINOS_DB_CONNECTION` a testing y preguntarme.
- Hecho cuando: la migración se aplica en testing sin tocar datos y la fila inicial queda con la versión actual.
- Cómo probarla: abrir la app (funciona igual); `select * from app_update_policy`.
- [x] Hecha y probada — 2026-11-15: migración `AddAppUpdatePolicy` aditiva (solo crea la tabla `app_update_policy` con restricción de una sola fila). Aplicada en `ninos_db_testing` con el mismo mecanismo de arranque de la app; no existe todavía en `ninos_db`. Fila inicial: mínima 1.0.0 (no bloquea a nadie) y recomendada 1.0.10 (la versión actual del código). Consecuencia a decidir: las PCs que hoy tienen 1.0.9 verán el aviso de versión recomendada al abrir la primera vez. Verificada con el usuario de solo lectura.

## Tarea 9 - Versión mínima y recomendada
Cubre: RF-17, RF-18, RF-19
- Al abrir y cada 3 minutos: comparar la versión propia con la política. Por debajo de la mínima, no deja trabajar y obliga a actualizar; por debajo de la recomendada, deja trabajar con aviso.
- Hecho cuando: los dos casos se comportan distinto.
- Cómo probarla: poner la recomendada en 1.0.11 con la app en 1.0.10 → trabaja con aviso; poner la mínima en 1.0.11 → no deja trabajar.
- [x] Hecha y probada — 2026-11-16, confirmada por Luis: con la mínima por encima de la versión instalada, la app bloquea; con la recomendada, avisa. Pruebas del evaluador (9) pasan. La revisión ahora es cada 10 segundos (decisión de Luis 2026-11-16).

## Tarea 10 - Aviso con cuenta atrás mientras se trabaja
Cubre: RF-11, RF-12, RF-13, RF-15
- Recuadro visible con la cuenta atrás, revisado cada 3 minutos; al llegar a cero, pide guardar y actualizar; si Luis cancela el aviso, el recuadro desaparece.
- Hecho cuando: con la app abierta y trabajando, el aviso aparece solo, cuenta y al final pide actualizar.
- Cómo probarla: programar el aviso a 3 minutos con la app abierta; esperar sin tocar nada; cancelarlo a mitad y comprobar que desaparece.
- [ ] Descartada por decisión de Luis (2026-11-16): ya no la considera necesaria. No se implementa la cuenta atrás; el aviso que queda es el recuadro «Actualizar ahora» de la tarea 9. El código de la cuenta atrás queda en el proyecto y se puede retirar después.

## Tarea 11 - Proteger el trabajo sin guardar
Cubre: RF-14
- Antes de cerrar por actualización, revisar `has_pending_data` y avisar; no cerrar sin que el usuario decida.
- Hecho cuando: con una nota a medio llenar, el aviso lo dice y no se pierde nada sin confirmación.
- Cómo probarla: empezar una nota con dos renglones, programar el aviso a 3 minutos y dejar que llegue a cero.
- [~] Código hecho: "Actualizar ahora" revisa `has_pending_data` y pregunta antes de reiniciar. Límite encontrado: `has_pending_data` solo cubre la nota de entrega. La ventana de nota de crédito y la de pagos a medias NO están cubiertas; si quieres, lo agrego en una tarea aparte. Prueba descartada por decisión de Luis (2026-11-16).

## Tarea 12 (manual) - Script de publicación con contraseña
Cubre: RF-1, RF-2, RF-4, RF-5, RF-23
- `deploy.ps1`: pide la contraseña una vez, compila, empaqueta, sube por SSH, deja solo la versión actual y las 3 anteriores, y actualiza la política en la base. Si la subida se corta, no marca la versión nueva como disponible. La dirección del VPS y el usuario salen de la configuración local, no del repositorio.
- Hecho cuando: una publicación completa funciona con un solo comando y en el servidor quedan exactamente 4 versiones.
- Cómo probarla: publicar la 1.0.12, 1.0.13 y 1.0.14 seguidas y comprobar que la 1.0.11 ya no está; probar con contraseña incorrecta → no sube nada; cortar la red a mitad de la subida → las PCs siguen en la versión anterior.
- [~] Hecha — 2026-11-15, `installer\deploy.ps1`. Probado por Claude: clave equivocada no sube nada; publicar 1.0.12, 1.0.13 y 1.0.14 deja exactamente 3 versiones completas en el servidor; una subida con llave SSH inexistente no cambia el servidor. Se usó una clave de prueba creada y borrada al terminar. Queda para Luis: escribir la clave real con `-crear_clave` (pide dos veces, guarda solo hash y sal en `installer\private\clave_publicacion.json`), publicar una versión con su contraseña y comprobar el resultado en PC. Las versiones de prueba 1.0.12 a 1.0.14 siguen en el servidor: pendiente de confirmación de Luis para borrarlas (la carpeta es la de actualizaciones).

## Tarea 13 - Volver atrás
Cubre: RF-20, RF-21
- Opción del script que toma el código de una de las 3 versiones anteriores del servidor y lo publica como versión nueva; el mensaje recuerda que vuelve el código, no los datos.
- Hecho cuando: desde la 1.0.14 se vuelve al código de la 1.0.13 en un minuto y las PCs quedan en esa versión.
- Cómo probarla: publicar una versión con un cambio visible, volver atrás y comprobar en la PC de prueba que el cambio desapareció.
- [x] Hecha y probada — 2026-11-16, confirmada por Luis. La prueba salió al publicar la 1.0.18 con `-volver_atras`: volvió el código de la 1.0.16 y el botón volvió a actualizar sin preguntar, como se esperaba de ese código. Esa publicación se hizo sin querer como prueba; queda registrada aquí.

## Tarea 14 (manual) - Prueba de punta a punta en PC real, con antivirus
Cubre: todos, y el riesgo 2 del plan
- En una PC real parecida a las del cliente (con su antivirus activo, no la de desarrollo): instalar, publicar una actualización y dejar que se aplique sola, sin intervenir.
- Hecho cuando: la actualización se aplica sin que nadie haga clic en ninguna advertencia de Windows.
- Cómo probarla: lo descrito. **Si Windows o el antivirus la bloquean, parar aquí** y decidir entre añadir la exclusión en cada PC durante la visita manual o comprar el certificado de firma. Anotar el resultado en este archivo.
- [ ] Hecha y probada

## Tarea 15 (manual, día de la visita, ver spec 011) - Instalación en la PC de la clienta
Cubre: despliegue
- En cada PC de la clienta, en la misma visita que la spec 002: desinstalar la versión vieja, instalar la nueva con actualizador, colocar el `appsettings.json` de producción, mover logs y configuración, y verificar que el sistema abre y trabaja.
- Hecho cuando: las 3 PCs trabajan con la versión nueva y una actualización de prueba les llega sola.
- Cómo probarla: publicar una actualización mínima al día siguiente y confirmar con la clienta que las 3 PCs la tomaron sin que nadie fuera.
- [ ] Hecha y probada

## Tarea 16 (pendiente) - Espacio del VPS: logs del sistema
Cubre: RF-4
- Revisar el espacio que muestra el panel del proveedor frente al del servidor (`df -h`). En el servidor: el disco está al 15 %, la carpeta de actualizaciones pesa 515 MB (4 versiones) y los registros del sistema (`/var/log/journal`) pesan 1,4 GB.
- Limitar el tamaño de los registros del sistema (journald) y verificar que la carpeta de actualizaciones siga con 4 versiones.
- Hecho cuando: el espacio de los registros queda por debajo de un límite fijado por Luis y el panel del proveedor coincide con el servidor.
- Cómo probarla: `df -h /` y `journalctl --disk-usage` antes y después.
- [ ] Hecha y probada — pendiente: se hace después de resolver la actualización, y solo con el visto bueno de Luis.

## Tarea 17 - Datos locales fuera de la carpeta que borra la actualización
Cubre: RF-24, RF-25, RF-26
- `appsettings.json`, `backup_config.json`, `Backups` y los logs pasan a la carpeta de datos (`%LocalAppData%\NinOS\` en Velopack; la del programa en otro caso). Al arrancar, lo que quede en la carpeta del programa se copia una vez sin pisar nada.
- Hecho cuando: las pruebas unitarias de la carpeta de datos y de la copia pasan, y una PC con `appsettings.json` sigue abriendo después de una actualización.
- Cómo probarla: en una PC o usuario de Windows sin `NINOS_DB_CONNECTION`, instalar con Velopack, poner `appsettings.json` (apuntando a testing) en `%LocalAppData%\NinOS\`, publicar una versión de prueba y dejar que se actualice: debe abrir, y el log debe seguir en `%LocalAppData%\NinOS\ninos-ui.log`.
- [ ] Hecha y probada
