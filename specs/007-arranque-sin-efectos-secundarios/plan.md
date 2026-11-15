# Plan 007 - Arranque que no cambia datos

Cómo cumplir [spec.md](spec.md). Todavía no se ha tocado código.

## Qué hace hoy el arranque (verificado en [DbInitializer.cs](../../src/NinOS.Infrastructure/data/DbInitializer.cs))

Cada vez que alguien abre la app, además de migrar, ejecuta en este orden:

| Bloque | Qué cambia |
|---|---|
| `cleanup_credit_note_payments_and_adjust_totals` | **Borra pagos** de tipo "NOTA DE CREDITO" y ajusta totales de notas |
| `migrate_legacy_series` | Renumera clientes, notas y notas de crédito de series viejas |
| `initialize_zonas_and_sellers` | Restaura zonas, renombra/marca Pro Venta la 06, mueve clientes sin zona a Zona 1, crea vendedoras base y reasigna Maracay a Juan Luis |
| `initialize_product_lines` | Crea líneas de producto (esto es dato inicial: ok si faltan) |
| `migrate_customer_correlatives_to_global5` | Si un cliente tiene código raro, **renumera todos** |
| Tipos de nota | Fuerza el encabezado, borra tipos obsoletos (PRO, CAN, HLS) y mueve sus notas a General |
| Relaciones Pro Venta | Crea relaciones faltantes, asigna notas a relaciones y renumera todas |
| Sembrado de ejemplo | Crea clientes/productos de ejemplo si todo está vacío |

El mensaje de error de [App.xaml.cs](../../src/NinOS.UI/App.xaml.cs) siempre dice "verifique su conexión a internet", cualquiera sea la causa, y la configuración ignora el aviso de cambios de modelo pendientes.

## Enfoque

1. **Primero mirar, sin tocar.** Un diagnóstico que al arrancar solo **escribe en el log** lo que hoy se "arregla" solo (clientes sin zona, códigos fuera de formato, notas Pro Venta sin relación, tipos obsoletos). Así se ve si algo sigue necesitando corrección antes de quitar los bloques.
2. **Quitar todos los bloques que escriben datos.** El arranque queda en: migrar (según se decida en la spec 002) + cargar en memoria los prefijos de las líneas de producto + diagnóstico en el log. Nada más. Los datos iniciales de una base nueva pasan a un comando explícito `--seed-initial-data` que se niega si la base tiene datos.
3. **El borrado de pagos de nota de crédito** se quita del arranque. Antes se confirma, en producción y solo lectura, que no queda ninguno por limpiar (en testing hay 0).
4. **Arranque simultáneo.** Se verifica si Npgsql 10 bloquea las migraciones entre procesos; si no, se envuelve `Migrate()` en `pg_advisory_lock`.
5. **Errores reales.** Se distinguen: sin configuración, sin red, credenciales, migración fallida, otro error; cada uno con su mensaje y el detalle en el log.
6. **Aviso de modelo pendiente** se vuelve a activar, después de comprobar que hoy no hay cambios pendientes (`dotnet ef migrations has-pending-model-changes` con una conexión de prueba, sin tocar producción).

## Opciones descartadas

- **Dejar los bloques pero con "banderas" de una sola vez:** deja código que nadie entiende y que sigue siendo un riesgo.
- **Convertir las correcciones en migraciones de EF:** las migraciones ya están aplicadas en producción; una nueva con datos es difícil de revisar. Cualquier corrección puntual se hace con un script supervisado.
- **Mover todo a un botón del Panel Admin:** útil después; no es necesario para quitar el riesgo.

## Decisión de Luis

Una base con datos nunca se toca al abrir. Zonas, vendedoras, tipos de nota y líneas de producto dejan de crearse o corregirse solas; si falta alguno, el log avisa. Los datos de una base nueva se cargan con `--seed-initial-data`.

## Pruebas (a mano)

Antes y después de abrir la app, comparar un respaldo de testing (conteo de filas por tabla y sumas de dinero): no debe haber diferencias. Eliminar una zona, quitar Maracay a una vendedora, dar a un cliente un código raro y reabrir. Abrir tres copias a la vez.

## Rama

`feature/arranque-limpio` saliendo de `TESTING`. Se prueba solo contra testing.
