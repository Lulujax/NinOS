# Plan - Día de la instalación en la clienta

Orden de los pasos. Cada uno se hace con Luis presente en la visita.

1. **Respaldo de producción** (tarea 1 de `tareas.md`). Va primero: sin copia no se toca nada.
2. **Puerto y cifrado** (tarea 2). Se cambia el puerto de PostgreSQL, se exige TLS y se cierra el 5432. Va antes de cambiar usuarios, para que cualquier fallo no deje a la clienta sin sistema.
3. **Usuario de la app** (tarea 3). Se ejecuta el script de rotación con `ninos_admin`, que transfiere las tablas a `ninos_app`.
4. **Configuración en las PCs** (tarea 4). Se instala la versión actual y el archivo de conexión de producción en cada PC.
5. **Anular el usuario viejo** (tarea 5). Solo cuando las 3 PCs ya abren con el usuario nuevo.
6. **Actualización de prueba** (tarea 6). Al día siguiente, para confirmar que las actualizaciones llegan solas.

Si un paso falla, se detiene y no se avanza. El respaldo del paso 1 permite volver.
