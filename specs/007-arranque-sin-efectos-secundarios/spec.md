# 007 - Arranque que no cambia datos

## Qué y por qué

Cada vez que alguien abre la app, el inicializador de la base corre contra la base en uso y cambia datos solo: restaura zonas eliminadas, fuerza el nombre y modalidad de la zona 06, reasigna Maracay a Juan Luis (contra la regla de negocio), mueve clientes sin zona a la Zona 1 y puede renumerar a **todos** los clientes si uno solo tiene un código raro. Con varias PCs abriendo a la vez, el arranque puede fallar y mostrar "verifique su internet" aunque no sea ese el problema.

## Requisitos

- **RF-1** CUANDO se abre la app, EL SISTEMA no ejecutará ningún INSERT, UPDATE ni DELETE de **datos de negocio** (zonas, clientes, vendedoras, notas, pagos, comisiones, tipos de nota, relaciones). Lo único que el arranque puede cambiar es la **estructura**, aplicando las migraciones pendientes de EF Core, que es la decisión tomada por Luis. Cargar en memoria los prefijos de las líneas de producto sí está permitido porque no escribe nada.
- **RF-1b** EL SISTEMA dejará registrado en el log cada migración que aplique al arrancar, con su nombre y el resultado.
- **RF-2** EL SISTEMA no restaurará zonas eliminadas, no renombrará zonas ni cambiará su modalidad al arrancar.
- **RF-3** EL SISTEMA no asignará ni reasignará zonas a ninguna vendedora al arrancar; las zonas Pro Venta las puede tener cualquier vendedora (ver AGENTS.md).
- **RF-4** EL SISTEMA no cambiará la zona de ningún cliente al arrancar.
- **RF-5** EL SISTEMA no renumerará ni reescribirá códigos de clientes, notas ni vendedoras al arrancar.
- **RF-6** SI el arranque detecta datos fuera de lo esperado (cliente sin zona, código fuera de formato), ENTONCES EL SISTEMA lo dejará en el log sin modificar nada.
- **RF-7** SI dos o más PCs arrancan a la vez, ENTONCES EL SISTEMA aplicará las migraciones una sola vez, sin errores para las demás.
- **RF-8** SI el arranque falla, ENTONCES EL SISTEMA mostrará la causa real (conexión, migración, permisos) y no un mensaje genérico de internet.
- **RF-9** EL SISTEMA volverá a avisar si hay cambios de modelo sin migración, en vez de silenciar el aviso.
- **RF-10** EL SISTEMA no sembrará datos al arrancar. Los datos iniciales de una base nueva (tipos de nota, líneas de producto, zonas) se cargarán con un comando explícito (`--seed-initial-data`) que ejecuta Luis y que se **niega** a correr si la base ya tiene datos.
- **RF-11** SI al arrancar falta un dato esperado (por ejemplo un tipo de nota), ENTONCES EL SISTEMA lo avisa en el log y no lo crea.

## Cómo sé que está listo

- Eliminar una zona y reabrir la app: sigue eliminada.
- Quitar a Juan Luis la zona Maracay y reabrir: no vuelve.
- Un cliente con código irregular: ningún otro código cambia.
- Tres PCs abriendo a la vez: las tres arrancan.
- Se compara un respaldo de testing antes y después de arrancar: sin diferencias.

## Fuera de alcance

- Cambiar las migraciones ya aplicadas.
- Corregir datos viejos (se hace aparte, con spec y respaldo).

## Decisiones tomadas

- Las correcciones de datos puntuales se hacen con scripts supervisados, no en el arranque.

- Decisión de Luis: una base con datos **nunca** se toca al abrir. Ni siquiera una base vacía se siembra sola: se hace con el comando explícito.
