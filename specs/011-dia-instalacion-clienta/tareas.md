# Tareas - Día de la instalación en la clienta

Todas son manuales y pendientes. No se hacen hasta que Luis diga que está en la visita.

## Tarea 1 - Respaldo de la base de producción
Cubre: RF-1
- Adelantada el 2026-11-26. Hecha por Claude: `respaldos\ninos_db_2026-11-26_2232_antes_de_instalar_1.0.10.dump` (copia también en `/root/respaldos` del VPS); `pg_restore --list` lee las 22 tablas con datos. Falta el visto bueno de Luis.
- Hacer un respaldo completo de `ninos_db` antes de cualquier cambio.
- Hecho cuando: el archivo `.dump` existe y `pg_restore --list` lo lee sin error.
- Cómo probarla: `pg_restore --list` sobre el archivo.
- [ ] Hecha y probada — pendiente: solo en la visita.

## Tarea 2 - Puerto nuevo, cifrado y 5432 cerrado
Cubre: RF-2
- Adelantada por decisión de Luis el 2026-11-26 (producción sin uso desde hace una semana). Hecha por Claude con `installer\private	area2_puerto_tls.sh`: puerto 54329, TLS 1.2 mínimo, `hostnossl ... reject` y 5432 cerrado en `ufw`. Comprobado: 54329 cifrado conecta, sin cifrar es rechazado, 5432 no responde. Vuelta atrás: `tarea2_volver_atras.sh`. Falta el visto bueno de Luis.
- Cambiar el puerto de PostgreSQL, exigir TLS en `pg_hba.conf` (`hostssl`), rechazar conexiones sin cifrar y cerrar el 5432 en el firewall. Viene de la tarea 9 de la spec 002.
- Hecho cuando: la app conecta por el puerto nuevo con TLS; una conexión al 5432 o sin TLS es rechazada.
- Cómo probarla: abrir la app; intentar conectar al 5432 y sin TLS → rechazado.
- [ ] Hecha y probada — pendiente: solo en la visita.

## Tarea 3 - Usuario de la app sin superusuario
Cubre: RF-3
- Adelantada el 2026-11-26. Hecha por Claude: tablas, secuencias y la base `ninos_db` son de `ninos_app`. Las 8 migraciones pendientes se aplicaron abriendo la 1.0.10 con `ninos_app` (log: "Migraciones aplicadas correctamente"); los conteos de notas, renglones y comisiones no cambiaron. Falta el visto bueno de Luis.
- Ejecutar `installer\private\rotacion_produccion.sql` con `ninos_admin`, para que las tablas pasen a `ninos_app`. Viene de la tarea 11 de la spec 002.
- Hecho cuando: la app arranca y migra con `ninos_app`, y `ninos_app` no puede crear bases ni roles.
- [ ] Hecha y probada — pendiente: solo en la visita.

## Tarea 4 - Configuración de producción en cada PC
Cubre: RF-4
- En cada PC: instalar la versión actual con el actualizador y colocar `installer\private\appsettings.client.json` como `appsettings.json` en `%LocalAppData%\NinOS\` (no dentro de `current\`, que la actualización borra; spec 010, RF-24). Antes de copiarlo, confirmar que tenga el puerto nuevo de la tarea 2. Si la PC tiene respaldos locales en la carpeta de la versión vieja, moverlos a `%LocalAppData%\NinOS\Backups\`. Viene de la tarea 10 de la spec 002 y de la tarea 15 de la spec 010.
- Hecho cuando: las 3 PCs abren y trabajan con la versión actual.
- [ ] Hecha y probada — pendiente: solo en la visita.

## Tarea 5 - Anular el usuario viejo
Cubre: RF-3
- Anular `ninos_admin` y verificar que ya no conecta. Solo después de la tarea 4.
- Hecho cuando: `ninos_admin` es rechazado en las 3 PCs y desde fuera.
- [ ] Hecha y probada — pendiente: solo en la visita.

## Tarea 6 - Actualización de prueba al día siguiente
Cubre: RF-5
- Publicar una actualización mínima y confirmar con la clienta que las 3 PCs la tomaron sin que nadie fuera.
- Hecho cuando: las 3 PCs quedan en la versión nueva sin intervención.
- [ ] Hecha y probada — pendiente: el día siguiente a la visita.
