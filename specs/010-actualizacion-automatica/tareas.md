# Tareas 010 - Actualización automática

**Requisito previo:** las tareas 1 a 7 de la spec 002 deben estar hechas (la conexión ya no vive en el código).

**Regla para todas:** nada se prueba en las PCs del cliente hasta la tarea 15.

---

## Tarea 1 (manual) - Respaldo antes de empezar
Cubre: preparación
- Respaldo de `ninos_db` y `ninos_db_testing`, y una copia del `publish\` de la versión 1.0.10 actual, guardados fuera del repo.
- Hecho cuando: existen los tres y el `.dump` se puede listar con `pg_restore --list`.
- Cómo probarla: listar el respaldo y abrir la carpeta copiada.
- [ ] Hecha y probada

## Tarea 2 (manual) - Averiguar qué puertos deja abiertos el proveedor
Cubre: RF-6 (preparación), riesgo 7 del plan
- nginx ya está instalado y el puerto 80 responde. Revisar el firewall del panel del proveedor y el del VPS (`ufw status`), hacer que nginx escuche temporalmente en el 443 y en el 8443, y probar desde una PC fuera del VPS cuáles llegan.
- Hecho cuando: queda anotado en este archivo qué puertos alcanzan a nginx desde internet y cuál se usará para HTTPS; o queda anotado que solo responde el 80.
- Cómo probarla: desde Windows, `Test-NetConnection <IP del VPS> -Port 443` y lo mismo con el 8443 → `TcpTestSucceeded : True` en el que sirva.
- **Si solo responde el 80, parar aquí:** la tarea 3 queda bloqueada hasta que Luis decida sobre el plan B (ver el `[NECESITA ACLARACIÓN]` del plan).
- [ ] Hecha y probada

## Tarea 3 (manual, depende de la tarea 2) - HTTPS con certificado propio en el VPS
Cubre: RF-6, RF-23
- Generar en el VPS un certificado autofirmado con vigencia larga (por ejemplo 10 años) y otro de respaldo guardado fuera del servidor; configurar nginx para servirlo en el puerto elegido en la tarea 2 y publicar una carpeta `/updates` de solo lectura. Anotar la fecha de vencimiento y las dos huellas (SHA-256) para la tarea 7. Sin dominio, sin certbot.
- Hecho cuando: `https://<IP del VPS>:<puerto>/updates/` responde (el navegador avisa que el certificado no es de una autoridad conocida; es lo esperado) y subir archivos por esa vía no es posible.
- Cómo probarla: poner un archivo de prueba y descargarlo con `curl -k`; comprobar con `curl --pinnedpubkey` que con la huella correcta responde y con una huella cualquiera lo rechaza; intentar un `PUT` con curl → rechazado.
- [ ] Hecha y probada

## Tarea 4 (manual) - Activar Ubuntu Pro (gratis)
Cubre: riesgo 6 del plan
- Registrar la cuenta gratuita de Ubuntu Pro (hasta 5 máquinas) y activarla en el VPS para recibir parches de seguridad sin reinstalar el sistema.
- Hecho cuando: `pro status` muestra los servicios de seguridad activos.
- Cómo probarla: ese comando y aplicar los parches pendientes.
- [ ] Hecha y probada

## Tarea 5 - Empaquetar la app con Velopack (local, sin servidor)
Cubre: RF-3
- Agregar Velopack al proyecto, generar el primer paquete de la versión 1.0.10 en una carpeta local y luego un paquete 1.0.11 con un cambio mínimo, para comprobar el tamaño del diferencial.
- Hecho cuando: el paquete completo ronda los 179 MB y el diferencial de la 1.0.11 pesa pocos MB; el build queda en 0 errores y 0 advertencias.
- Cómo probarla: mirar el tamaño de los archivos generados y anotarlo en este archivo.
- [ ] Hecha y probada

## Tarea 6 - La app busca y aplica la actualización al abrir
Cubre: RF-8, RF-9, RF-10, RF-7
- En el arranque, antes de terminar la pantalla de carga: buscar versión nueva, descargar mostrando el avance con `SetLoading`, verificar integridad, aplicar y reiniciar. Si algo falla, seguir con la versión actual y mostrar el motivo.
- Hecho cuando: una PC de prueba en 1.0.10 pasa sola a 1.0.11 y vuelve a abrir actualizada.
- Cómo probarla: instalar la 1.0.10 en una PC o máquina virtual de prueba, publicar la 1.0.11 en el servidor, abrir la app; luego apagar nginx y abrir otra vez → arranca igual y avisa que no pudo comprobar.
- [ ] Hecha y probada

## Tarea 7 - Fijar la huella del servidor en la app
Cubre: RF-7b, RF-7c, RF-22
- La app descarga con un cliente HTTP propio que solo acepta el certificado cuya huella SHA-256 coincide con una de las dos que lleva (la actual y la de respaldo), ignorando cualquier autoridad pública; sin ninguna llave de publicación dentro de la app. Las huellas no son secretas, pero la dirección del servidor y cualquier credencial no se escriben en el repositorio: salen de la configuración de la PC.
- Hecho cuando: un servidor distinto con otro certificado (incluso uno válido de una autoridad pública) es rechazado y queda en el log, y un servidor con el certificado de respaldo es aceptado.
- Cómo probarla: apuntar temporalmente la app a otro servidor de prueba con otro certificado → rechaza la actualización y lo deja en el log; cambiar el servidor de prueba al certificado de respaldo → la acepta.
- Pruebas unitarias: la comparación de huellas con caso feliz (huella actual), caso límite (huella de respaldo, mayúsculas/minúsculas y separadores) y caso de error (huella vacía o distinta).
- [ ] Hecha y probada

## Tarea 8 - Migración: tabla de política de actualización
Cubre: RF-16
- Tabla `app_update_policy` de una sola fila: versión mínima, versión recomendada, momento del aviso y mensaje. **Antes:** `NINOS_DB_CONNECTION` a testing y preguntarme.
- Hecho cuando: la migración se aplica en testing sin tocar datos y la fila inicial queda con la versión actual.
- Cómo probarla: abrir la app (funciona igual); `select * from app_update_policy`.
- [ ] Hecha y probada

## Tarea 9 - Versión mínima y recomendada
Cubre: RF-17, RF-18, RF-19
- Al abrir y cada 3 minutos: comparar la versión propia con la política. Por debajo de la mínima, no deja trabajar y obliga a actualizar; por debajo de la recomendada, deja trabajar con aviso.
- Hecho cuando: los dos casos se comportan distinto.
- Cómo probarla: poner la recomendada en 1.0.11 con la app en 1.0.10 → trabaja con aviso; poner la mínima en 1.0.11 → no deja trabajar.
- [ ] Hecha y probada

## Tarea 10 - Aviso con cuenta atrás mientras se trabaja
Cubre: RF-11, RF-12, RF-13, RF-15
- Recuadro visible con la cuenta atrás, revisado cada 3 minutos; al llegar a cero, pide guardar y actualizar; si Luis cancela el aviso, el recuadro desaparece.
- Hecho cuando: con la app abierta y trabajando, el aviso aparece solo, cuenta y al final pide actualizar.
- Cómo probarla: programar el aviso a 3 minutos con la app abierta; esperar sin tocar nada; cancelarlo a mitad y comprobar que desaparece.
- [ ] Hecha y probada

## Tarea 11 - Proteger el trabajo sin guardar
Cubre: RF-14
- Antes de cerrar por actualización, revisar `has_pending_data` y avisar; no cerrar sin que el usuario decida.
- Hecho cuando: con una nota a medio llenar, el aviso lo dice y no se pierde nada sin confirmación.
- Cómo probarla: empezar una nota con dos renglones, programar el aviso a 3 minutos y dejar que llegue a cero.
- [ ] Hecha y probada

## Tarea 12 (manual) - Script de publicación con contraseña
Cubre: RF-1, RF-2, RF-4, RF-5, RF-23
- `deploy.ps1`: pide la contraseña una vez, compila, empaqueta, sube por SSH, deja solo la versión actual y las 2 anteriores, y actualiza la política en la base. Si la subida se corta, no marca la versión nueva como disponible. La dirección del VPS y el usuario salen de la configuración local, no del repositorio.
- Hecho cuando: una publicación completa funciona con un solo comando y en el servidor quedan exactamente 3 versiones.
- Cómo probarla: publicar la 1.0.12, 1.0.13 y 1.0.14 seguidas y comprobar que la 1.0.11 ya no está; probar con contraseña incorrecta → no sube nada; cortar la red a mitad de la subida → las PCs siguen en la versión anterior.
- [ ] Hecha y probada

## Tarea 13 - Volver atrás
Cubre: RF-20, RF-21
- Opción del script que toma el código de una de las 2 versiones anteriores del servidor y lo publica como versión nueva; el mensaje recuerda que vuelve el código, no los datos.
- Hecho cuando: desde la 1.0.14 se vuelve al código de la 1.0.13 en un minuto y las PCs quedan en esa versión.
- Cómo probarla: publicar una versión con un cambio visible, volver atrás y comprobar en la PC de prueba que el cambio desapareció.
- [ ] Hecha y probada

## Tarea 14 (manual) - Prueba de punta a punta en PC real, con antivirus
Cubre: todos, y el riesgo 2 del plan
- En una PC real parecida a las del cliente (con su antivirus activo, no la de desarrollo): instalar, publicar una actualización y dejar que se aplique sola, sin intervenir.
- Hecho cuando: la actualización se aplica sin que nadie haga clic en ninguna advertencia de Windows.
- Cómo probarla: lo descrito. **Si Windows o el antivirus la bloquean, parar aquí** y decidir entre añadir la exclusión en cada PC durante la visita manual o comprar el certificado de firma. Anotar el resultado en este archivo.
- [ ] Hecha y probada

## Tarea 15 (manual, con el cliente) - La última instalación manual
Cubre: despliegue
- En cada PC del cliente, en la misma visita que la spec 002: desinstalar la versión vieja, instalar la nueva con actualizador, colocar el `appsettings.json` de producción, mover logs y configuración, y verificar que el sistema abre y trabaja.
- Hecho cuando: las 3 PCs trabajan con la versión nueva y una actualización de prueba les llega sola.
- Cómo probarla: publicar una actualización mínima al día siguiente y confirmar con la clienta que las 3 PCs la tomaron sin que nadie fuera.
- [ ] Hecha y probada
