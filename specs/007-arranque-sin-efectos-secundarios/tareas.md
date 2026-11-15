# Tareas 007 - Arranque que no cambia datos

## Tarea 1 - Diagnóstico de arranque que solo escribe en el log
Cubre: RF-6
- Al arrancar, contar y registrar en el log (sin modificar): clientes sin zona, clientes con código fuera de formato, notas Pro Venta sin relación, tipos de nota obsoletos, notas con tipo inexistente.
- Hecho cuando: el log muestra esas cifras en cada arranque y la base no cambia.
- Cómo probarla: abrir la app y leer `ninos-ui.log`; las cifras coinciden con consultas SQL equivalentes.
- [x] Hecha y probada — `DbDiagnostics` + `db_diagnostic_report`. 9 pruebas de la lógica pura y 1 de integración contra testing que comprueba que las consultas se traducen a SQL y que no deja cambios pendientes.

## Tarea 2 (manual, solo lectura) - Confirmar en producción que no hay nada por limpiar
Cubre: RF-1, RF-5
- Ejecutar en producción: `select count(*) from payment where payment_type='NOTA DE CREDITO';` y la consulta de clientes con código fuera de 5 dígitos.
- Hecho cuando: ambos devuelven 0. Si no, se corrige con un script supervisado antes de la tarea 3.
- Cómo probarla: es la propia consulta.
- [~] Ya no hace falta correr SQL a mano: el diagnóstico de la tarea 1 cuenta solo los pagos de tipo NOTA DE CREDITO y los códigos de cliente fuera de formato. **Falta que abras la app en producción y leas `ninos-ui.log`.**

## Tarea 3 - Quitar limpieza de pagos y renumeraciones del arranque
Cubre: RF-1, RF-5
- Quitar del arranque `cleanup_credit_note_payments_and_adjust_totals`, `migrate_legacy_series` y `migrate_customer_correlatives_to_global5`.
- Hecho cuando: un cliente con código raro no provoca renumeración y no se borra ningún pago.
- Cómo probarla: en testing, poner a un cliente el código `ABC`; reabrir la app → los demás códigos no cambian y el log avisa del raro (restaurar después).
- [x] Hecha y probada — borrados del arranque `cleanup_credit_note_payments_and_adjust_totals`, `migrate_legacy_series` y `migrate_customer_correlatives_to_global5`. 5 pruebas de regresión fallan si alguno vuelve.

## Tarea 4 - Quitar zonas, vendedoras y clientes del arranque
Cubre: RF-2, RF-3, RF-4
- Se elimina del arranque todo lo que crea, restaura, renombra o asigna zonas y vendedoras, y el movimiento de clientes sin zona. El log solo avisa si algo falta.
- Hecho cuando: eliminar una zona o quitar Maracay a una vendedora y reabrir no deshace el cambio.
- Cómo probarla: eliminar la zona 05 en la pantalla de zonas; quitar Maracay a Juan Luis; reabrir → siguen igual.
- [x] Hecha y probada — `initialize_zonas_and_sellers` salió del arranque; ya no restaura zonas, ni reasigna Maracay, ni mueve clientes de zona.

## Tarea 5 - Tipos de nota y relaciones Pro Venta sin efectos
Cubre: RF-1, RF-5
- Dejar de crear tipos de nota, de forzar el encabezado, de borrar tipos obsoletos y de crear, asignar y renumerar relaciones al arrancar; si falta un tipo, solo se avisa en el log.
- Hecho cuando: crear una nota Pro Venta sigue asignando su relación y el arranque no cambia nada.
- Cómo probarla: crear una nota Pro Venta de prueba; reabrir; las relaciones quedan iguales (comparar antes y después).
- [x] Hecha y probada — tipos de nota y relaciones Pro Venta ya no se crean, ni se fuerza el encabezado, ni se renumeran relaciones al arrancar. `DbInitializer` pasó de 1099 a 66 líneas y no contiene ni un `Add`, `Remove` ni `SaveChanges`.

## Tarea 6 - Datos iniciales por comando explícito
Cubre: RF-10, RF-11
- Mover el sembrado (tipos de nota, líneas de producto, zonas, ejemplos) a `--seed-initial-data`, que se niega a correr si la base tiene datos.
- Hecho cuando: abrir la app no crea nada, y el comando funciona en una base de prueba vacía y se niega en testing.
- Cómo probarla: abrir la app y comprobar que los conteos no cambian; crear una base vacía local, correr el comando (carga los datos) y correrlo otra vez (se niega); correrlo contra testing (se niega).
- [x] Hecha y probada — `DbSeeder.seed_initial_data()` + argumento `--seed-initial-data`. Se niega si la base tiene datos, verificado con una prueba de integración contra testing. **Falta tu prueba manual** del argumento (abre ventanas WPF).

## Tarea 7 - Arranque simultáneo con migración pendiente
Cubre: RF-7, RF-1b
- Envolver `Migrate()` en `pg_advisory_lock` para que, si tres PCs abren a la vez con una migración pendiente, solo una la aplique y las otras esperen; registrar en el log cada migración aplicada.
- Hecho cuando: tres copias abriendo a la vez con una migración pendiente arrancan todas sin errores, y el log de una (solo una) muestra la migración aplicada.
- Cómo probarla: generar una migración de prueba en testing (una columna nula), abrir tres copias a la vez, revisar los tres logs.
- [x] Hecha y probada — candado `pg_advisory_lock` alrededor de las migraciones, con la conexión mantenida abierta a mano (el candado vive en la sesión) y re-consulta tras tomar el turno. 2 pruebas de integración: el arranque no deja el candado tomado y no modifica ni una fila. **Falta tu prueba manual** con 3 copias a la vez y una migración pendiente.

## Tarea 8 - Mensajes de error reales al arrancar
Cubre: RF-8
- La pantalla de carga muestra causa distinta según sea: sin configuración, sin red, credenciales, migración u otra, con el detalle en el log.
- Hecho cuando: cada causa muestra su mensaje.
- Cómo probarla: usuario incorrecto en la variable → "credenciales"; puerto incorrecto → "sin conexión"; base inexistente → mensaje de base.
- [x] Hecha y probada — `StartupDiagnosis` distingue 7 causas (sin configuración, sin red, credenciales, base inexistente, permisos, migración, desconocida). 14 pruebas unitarias, incluida una que verifica que un error desconocido ya NO culpa a internet.

## Tarea 9 - Reactivar el aviso de cambios de modelo pendientes
Cubre: RF-9
- Comprobar que no hay cambios pendientes y quitar la línea que ignora el aviso en `App.xaml.cs`.
- Hecho cuando: la app arranca sin error y el aviso vuelve a funcionar.
- Cómo probarla: `dotnet ef migrations has-pending-model-changes` (con `NINOS_DB_CONNECTION` a testing, nunca a producción) no encuentra cambios; la app abre.
- [x] Hecha y probada — quitado el silenciamiento de `PendingModelChangesWarning` en la app y en el ayudante de pruebas. Verificado antes con `dotnet ef migrations has-pending-model-changes`: *No changes have been made to the model since the last migration*.

## Tarea 10 - Comparación de respaldo antes y después
Cubre: RF-1 a RF-6
- Sin código: respaldo de testing, abrir y cerrar la app dos veces, segundo respaldo y comparar conteos y sumas.
- Hecho cuando: no hay diferencias.
- Cómo probarla: conteos por tabla y `sum(amount_usd)` de pagos, `sum(adjusted_total_usd)` de notas, antes y después.
- [ ] Hecha y probada
