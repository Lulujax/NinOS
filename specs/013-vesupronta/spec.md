# 013 - Vesupronta

## Qué y por qué

La clienta administra hoy dos empresas dentro de NinOS: **General** y **Pro Venta** (Maracay). Ahora suma una tercera, **Vesupronta**, que trabaja igual que General: tiene los mismos módulos y las mismas reglas. Pro Venta, en cambio, trabaja distinto y con menos módulos.

Vesupronta se administra desde su propia pestaña, con sub-pestañas propias, y sus números no se mezclan con los de General ni con los de Pro Venta. Lo que divide a las tres empresas es la **zona**: una zona es General, Maracay (Pro Venta) o Vesupronta. Las tres comparten clientes, productos, inventario (el mismo stock), listas de precios y vendedoras, y llevan el mismo correlativo global de notas.

## Requisitos

- **RF-1** EL SISTEMA manejará tres tipos de empresa: General, Pro Venta y Vesupronta. Cada zona pertenecerá a uno solo de los tres.
- **RF-2** EL SISTEMA decidirá la empresa de una nota por la zona del cliente, igual que hoy con Pro Venta: la vendedora no elige la empresa.
- **RF-3** EL SISTEMA mostrará la pestaña **Vesupronta** con estas sub-pestañas propias: Ventas, Cuentas por Cobrar, Pagos y Notas de Crédito. Las notas de entrega de Vesupronta **no** se crean dentro de esta pestaña.
- **RF-4** EL SISTEMA aplicará a Vesupronta las mismas reglas que a General: descuento de condición, descuento de volumen sumado al de condición (10% + 5% = 15%), promociones, días de crédito, stock negativo permitido y edición de montos y descuentos en Cuentas por Cobrar.
- **RF-5** EL SISTEMA no mostrará notas, pagos, saldos, ventas ni notas de crédito de Vesupronta en las pestañas de General ni de Pro Venta, y tampoco mostrará los de General o Pro Venta dentro de Vesupronta.
- **RF-6** EL SISTEMA creará las notas de entrega de las tres empresas desde la pestaña **Notas de Entrega** que ya existe. CUANDO se elija un cliente de una zona Vesupronta, EL SISTEMA ofrecerá solo los tipos de nota de Vesupronta, igual que hoy ofrece solo los de Pro Venta para un cliente de Maracay.
- **RF-7** EL SISTEMA no generará comisiones de vendedoras por notas ni pagos de Vesupronta, y Vesupronta no tendrá sub-pestaña de Comisiones.
- **RF-8** EL SISTEMA usará para Vesupronta el mismo inventario que General y Pro Venta: una nota o nota de crédito de Vesupronta mueve el mismo stock y queda en el mismo kardex.
- **RF-9** EL SISTEMA numerará las notas de entrega y las notas de crédito de Vesupronta con el mismo correlativo global que las otras empresas.
- **RF-10** CUANDO se emita una nota de Vesupronta, EL SISTEMA la guardará congelada como cualquier nota, y su PDF, Excel e impresión saldrán en tamaño carta con el mismo formato que General (por ahora).
- **RF-11** CUANDO se instale la versión con Vesupronta, EL SISTEMA no moverá, reclasificará ni modificará ninguna zona, nota, pago ni comisión que ya exista: lo existente sigue siendo General o Pro Venta como hoy.
- **RF-12** CUANDO la clienta abra Vesupronta sin datos todavía, EL SISTEMA mostrará las sub-pestañas vacías, sin errores y sin totales inventados.
- **RF-13** SI una pantalla, reporte o consulta no puede saber a qué empresa pertenece una nota, ENTONCES EL SISTEMA no la contará en Vesupronta y lo dejará registrado en el log.
- **RF-14** CUANDO se cree o edite una zona, EL SISTEMA permitirá elegir su empresa entre General, Pro Venta y Vesupronta.
- **RF-15** CUANDO se cambie la empresa de una zona que ya tiene notas emitidas, EL SISTEMA lo permitirá y cada nota ya emitida conservará la empresa con la que se emitió. Solo las notas nuevas saldrán con la empresa nueva de la zona.

## Cómo sé que está listo

- La pestaña Vesupronta abre con sus cuatro sub-pestañas y, en una base sin notas de Vesupronta, todas salen vacías y sin errores.
- Desde la pestaña Notas de Entrega, un cliente de zona Vesupronta solo ofrece tipos de nota de Vesupronta.
- Al pasar una zona de General a Vesupronta, sus notas viejas siguen en General y las nuevas salen en Vesupronta.
- Una nota emitida a un cliente de una zona Vesupronta aparece en Ventas, Cuentas por Cobrar y Pagos de Vesupronta, y **no** aparece en las pestañas de General ni de Pro Venta.
- Una nota General y una de Pro Venta no aparecen en Vesupronta.
- Una nota de Vesupronta baja el mismo stock que vería General en Inventario.
- Un pago a una nota de Vesupronta no crea ninguna comisión.
- El correlativo sigue la misma serie: si la última nota General es 000_120, la siguiente de Vesupronta es 000_121.
- Los totales de General y Pro Venta en testing son los mismos antes y después de instalar la versión con Vesupronta.
- La nota de Vesupronta reimpresa sale igual a como se emitió, en tamaño carta.
- Pruebas unitarias de la regla que decide la empresa: caso feliz, límite y error.
- `dotnet build NinOS.slnx` con 0 errores y 0 advertencias; `dotnet test NinOS.slnx` pasa.

## Fuera de alcance

- Diseño propio de la nota impresa de Vesupronta (nombre, RIF, logo): por ahora es igual a General; Luis dirá después qué cambia.
- Comisiones de vendedoras en Vesupronta.
- Login de usuarios o permisos por empresa (decisión del cliente: no existen).
- Pasar notas viejas de General o Pro Venta a Vesupronta. Si hace falta, va con script supervisado, respaldo y spec propia.
- Contabilidad o impuestos separados por empresa.
- Cambios en Pro Venta (van en la spec 012).

## Decisiones tomadas (Luis, 2026-11-16)

- Vesupronta tiene los mismos módulos que General, separados en su propia pestaña.
- Lo que divide a las tres empresas es la zona: General, Maracay (Pro Venta) y Vesupronta.
- Comparte clientes, productos, inventario, listas de precios y vendedoras con las otras dos.
- Usa el correlativo global.
- No tiene comisiones.
- La impresión es igual a la de General por ahora.
- Las notas de entrega de las tres empresas se crean desde la pestaña Notas de Entrega actual; Vesupronta no tiene sub-pestaña de notas de entrega.
- Una zona puede cambiar de empresa; cada nota emitida conserva la empresa con la que se emitió.
