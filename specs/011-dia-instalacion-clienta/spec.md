# Spec 011 - Día de la instalación en la clienta

## Qué y por qué

Agrupa en un solo lugar todo lo que solo se puede hacer estando con la clienta: proteger la base de producción, dejar sus PCs con la versión actual y comprobar que las actualizaciones les llegan solas. Antes estaba repartido entre las specs 002 y 010.

Estas tareas no se hacen hasta que Luis confirme que está en la visita.

## Requisitos

- **RF-1** CUANDO Luis confirma que está en la visita, EL SISTEMA hará una copia de respaldo de la base de producción antes de cualquier cambio.
- **RF-2** CUANDO se cambie la conexión, EL SISTEMA aceptará solo conexiones cifradas por el puerto nuevo y rechazará el 5432.
- **RF-3** EL SISTEMA dejará a la app con un usuario sin superusuario y anulará el usuario `ninos_admin`.
- **RF-4** CUANDO se instale en cada PC, EL SISTEMA quedará con la versión actual, el actualizador y la configuración de producción.
- **RF-5** CUANDO se publique una actualización de prueba, EL SISTEMA la recibirá sola en las 3 PCs sin que nadie haga nada.

## Cómo sé que está listo

- Las 3 PCs de la clienta abren y trabajan con la versión actual.
- Con `ninos_admin` la conexión es rechazada, y el 5432 no responde desde fuera.
- Al día siguiente de la visita, una actualización de prueba llega sola a las 3 PCs, confirmado por la clienta.

## Fuera de alcance

- Cambios de código del sistema.
- La prueba en una PC con antivirus (tarea 14 de la spec 010), que queda pendiente aparte.
- La firma del instalador.
