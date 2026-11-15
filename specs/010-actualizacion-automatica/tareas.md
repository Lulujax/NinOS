# Tareas 010 - Actualización automática

**Requisito previo:** las tareas 1 a 7 de la spec 002 deben estar hechas (la conexión ya no vive en el código).

**Regla para todas:** nada se prueba en las PCs del cliente hasta la tarea 12.

---

## Tarea 1 (manual) - Respaldo antes de empezar
Cubre: preparación
- Respaldo de `ninos_db` y `ninos_db_testing`, y una copia del `publish\` de la versión 1.0.10 actual, guardados fuera del repo.
- Hecho cuando: existen los tres y el `.dump` se puede listar con `pg_restore --list`.
- Cómo probarla: listar el respaldo y abrir la carpeta copiada.
- [ ] Hecha y probada

## Tarea 2 (manual, bloqueada: faltan puertos 80 y 443) - Subdominio y HTTPS en el VPS
Cubre: RF-6
- Crear el subdominio gratuito (DuckDNS) apuntando a la IP del VPS; instalar nginx; sacar el certificado con certbot; servir una carpeta `/updates` solo de lectura; dejar la renovación automática.
- Hecho cuando: `https://<subdominio>/updates/` responde desde un navegador con candado válido, y subir archivos por esa vía no es posible.
- Cómo probarla: abrir la URL en el navegador (candado); poner un archivo de prueba y descargarlo; intentar un `PUT` con curl → rechazado.
- [ ] Hecha y probada

## Tarea 3 (manual) - Activar Ubuntu Pro (gratis)
Cubre: riesgo 6 del plan
- Registrar la cuenta gratuita de Ubuntu Pro (hasta 5 máquinas) y activarla en el VPS para recibir parches de seguridad sin reinstalar el sistema.
- Hecho cuando: `pro status` muestra los servicios de seguridad activos.
- Cómo probarla: ese comando y aplicar los parches pendientes.
- [ ] Hecha y probada

## Tarea 4 - Empaquetar la app con Velopack (local, sin servidor)
Cubre: RF-3
- Agregar Velopack al proyecto, generar el primer paquete de la versión 1.0.10 en una carpeta local y luego un paquete 1.0.11 con un cambio mínimo, para comprobar el tamaño del diferencial.
- Hecho cuando: el paquete completo ronda los 179 MB y el diferencial de la 1.0.11 pesa pocos MB; el build queda en 0 errores y 0 advertencias.
- Cómo probarla: mirar el tamaño de los archivos generados y anotarlo en este archivo.
- [ ] Hecha y probada

## Tarea 5 - La app busca y aplica la actualización al abrir
Cubre: RF-8, RF-9, RF-10, RF-7
- En el arranque, antes de terminar la pantalla de carga: buscar versión nueva, descargar mostrando el avance con `SetLoading`, verificar integridad, aplicar y reiniciar. Si algo falla, seguir con la versión actual y mostrar el motivo.
- Hecho cuando: una PC de prueba en 1.0.10 pasa sola a 1.0.11 y vuelve a abrir actualizada.
- Cómo probarla: instalar la 1.0.10 en una PC o máquina virtual de prueba, publicar la 1.0.11 en el servidor, abrir la app; luego apagar nginx y abrir otra vez → arranca igual y avisa que no pudo comprobar.
- [ ] Hecha y probada

## Tarea 6 - Fijar la identidad del servidor en la app
Cubre: RF-7b, RF-22
- La app solo acepta actualizaciones del servidor de Luis, comprobando su identidad; sin ninguna llave de publicación dentro de la app.
- Hecho cuando: un servidor distinto con otro certificado es rechazado y queda en el log.
- Cómo probarla: apuntar temporalmente la app a otro servidor de prueba → rechaza la actualización; revisar el log.
- [ ] Hecha y probada

## Tarea 7 - Migración: tabla de política de actualización
Cubre: RF-16
- Tabla `app_update_policy` de una sola fila: versión mínima, versión recomendada, momento del aviso y mensaje. **Antes:** `NINOS_DB_CONNECTION` a testing y preguntarme.
- Hecho cuando: la migración se aplica en testing sin tocar datos y la fila inicial queda con la versión actual.
- Cómo probarla: abrir la app (funciona igual); `select * from app_update_policy`.
- [ ] Hecha y probada

## Tarea 8 - Versión mínima y recomendada
Cubre: RF-17, RF-18, RF-19
- Al abrir y cada 3 minutos: comparar la versión propia con la política. Por debajo de la mínima, no deja trabajar y obliga a actualizar; por debajo de la recomendada, deja trabajar con aviso.
- Hecho cuando: los dos casos se comportan distinto.
- Cómo probarla: poner la recomendada en 1.0.11 con la app en 1.0.10 → trabaja con aviso; poner la mínima en 1.0.11 → no deja trabajar.
- [ ] Hecha y probada

## Tarea 9 - Aviso con cuenta atrás mientras se trabaja
Cubre: RF-11, RF-12, RF-13, RF-15
- Recuadro visible con la cuenta atrás, revisado cada 3 minutos; al llegar a cero, pide guardar y actualizar; si Luis cancela el aviso, el recuadro desaparece.
- Hecho cuando: con la app abierta y trabajando, el aviso aparece solo, cuenta y al final pide actualizar.
- Cómo probarla: programar el aviso a 3 minutos con la app abierta; esperar sin tocar nada; cancelarlo a mitad y comprobar que desaparece.
- [ ] Hecha y probada

## Tarea 10 - Proteger el trabajo sin guardar
Cubre: RF-14
- Antes de cerrar por actualización, revisar `has_pending_data` y avisar; no cerrar sin que el usuario decida.
- Hecho cuando: con una nota a medio llenar, el aviso lo dice y no se pierde nada sin confirmación.
- Cómo probarla: empezar una nota con dos renglones, programar el aviso a 3 minutos y dejar que llegue a cero.
- [ ] Hecha y probada

## Tarea 11 (manual) - Script de publicación con contraseña
Cubre: RF-1, RF-2, RF-4, RF-5, RF-23
- `deploy.ps1`: pide la contraseña una vez, compila, empaqueta, sube por SSH, deja solo la versión actual y las 2 anteriores, y actualiza la política en la base. Si la subida se corta, no marca la versión nueva como disponible.
- Hecho cuando: una publicación completa funciona con un solo comando y en el servidor quedan exactamente 3 versiones.
- Cómo probarla: publicar la 1.0.12, 1.0.13 y 1.0.14 seguidas y comprobar que la 1.0.11 ya no está; probar con contraseña incorrecta → no sube nada; cortar la red a mitad de la subida → las PCs siguen en la versión anterior.
- [ ] Hecha y probada

## Tarea 12 - Volver atrás
Cubre: RF-20, RF-21
- Opción del script que toma el código de una de las 2 versiones anteriores del servidor y lo publica como versión nueva; el mensaje recuerda que vuelve el código, no los datos.
- Hecho cuando: desde la 1.0.14 se vuelve al código de la 1.0.13 en un minuto y las PCs quedan en esa versión.
- Cómo probarla: publicar una versión con un cambio visible, volver atrás y comprobar en la PC de prueba que el cambio desapareció.
- [ ] Hecha y probada

## Tarea 13 (manual) - Prueba de punta a punta en PC real, con antivirus
Cubre: todos, y el riesgo 2 del plan
- En una PC real parecida a las del cliente (con su antivirus activo, no la de desarrollo): instalar, publicar una actualización y dejar que se aplique sola, sin intervenir.
- Hecho cuando: la actualización se aplica sin que nadie haga clic en ninguna advertencia de Windows.
- Cómo probarla: lo descrito. **Si Windows o el antivirus la bloquean, parar aquí** y decidir entre añadir la exclusión en cada PC durante la visita manual o comprar el certificado de firma. Anotar el resultado en este archivo.
- [ ] Hecha y probada

## Tarea 14 (manual, con el cliente) - La última instalación manual
Cubre: despliegue
- En cada PC del cliente, en la misma visita que la spec 002: desinstalar la versión vieja, instalar la nueva con actualizador, colocar el `appsettings.json` de producción, mover logs y configuración, y verificar que el sistema abre y trabaja.
- Hecho cuando: las 3 PCs trabajan con la versión nueva y una actualización de prueba les llega sola.
- Cómo probarla: publicar una actualización mínima al día siguiente y confirmar con la clienta que las 3 PCs la tomaron sin que nadie fuera.
- [ ] Hecha y probada
