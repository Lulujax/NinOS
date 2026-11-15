# 009 - Correcciones menores

## Qué y por qué

Varios detalles pequeños que, juntos, generan datos incorrectos o confusión. Se agrupan en una spec y se hacen tarea por tarea.

## Requisitos

- **RF-1** CUANDO se pulsa "guardar" en una nota de crédito (y en cualquier formulario que registre dinero o stock), EL SISTEMA desactivará el botón hasta terminar, para que un doble clic no cree dos registros.
- **RF-2** EL SISTEMA guardará el porcentaje de comisión de la vendedora con al menos 4 decimales (7,5% no se convertirá en 8%).
- **RF-3** CUANDO una nota lleva descuento de condición y descuento de volumen, EL SISTEMA los **sumará** sobre la misma base (10% + 5% = 15%) en el total, la vista previa, el PDF y el Excel.
- **RF-4** EL SISTEMA guardará el descuento de volumen separado del de condición, y la suma coincidirá con el descuento total aplicado.
- **RF-5** CUANDO cambia el estado de una nota (pagada, anulada, devuelta), EL SISTEMA reflejará el estado actual en la columna de estado del historial de producto.
- **RF-6** EL SISTEMA no mostrará mensajes que mencionen una función inexistente: el texto "Elimine primero los pagos" se reemplazará por una instrucción que exista.
- **RF-7** EL SISTEMA obtendrá las pestañas por vendedora de la lista de vendedoras activas, no de nombres escritos en el código ("Sandra", "Anais", "Juan Luis").
- **RF-8** EL SISTEMA tendrá la ciudad, los porcentajes de Pro Venta y la vendedora administrativa de los obsequios como constantes con nombre en un solo lugar (no repartidos por el código).
- **RF-9** EL SISTEMA permitirá subir o bajar el nivel de los logs por configuración y no dejará datos sensibles en ellos.
- **RF-10** EL SISTEMA tendrá los archivos de comisiones sin líneas en blanco de más (formato normal).
- **RF-11** EL SISTEMA tendrá pruebas unitarias de las reglas de dinero (saldos, descuentos, comisiones, notas de crédito, redondeo), cada una con caso feliz, caso límite y caso de error.
- **RF-12** CUANDO se crea o edita un cliente, EL SISTEMA no le asignará ninguna vendedora: el cliente pertenece a una zona y la vendedora sale de la zona. El campo viejo `seller_name` quedará vacío en los clientes nuevos y no se usará para nada.
- **RF-13** EL SISTEMA no usará `customer.seller_name` para calcular números de vendedora ni para agrupar clientes; los 247 clientes antiguos conservan su valor sin cambios.

## Cómo sé que está listo

- Doble clic en "Guardar" de una nota de crédito: se crea una sola.
- Comisión 7,5%: se guarda y se ve 7,5%.
- Nota con 10% de condición + 5% de volumen sobre 1.000: total 850, igual en pantalla, PDF y Excel.
- Una nota pagada ya no aparece como "Pendiente" en el historial de producto.
- Una vendedora nueva aparece sola en las pestañas.
- `dotnet build NinOS.slnx` queda en 0 errores y 0 advertencias, `dotnet test NinOS.slnx` pasa, y cada prueba manual de las tareas pasa en el debug.
- Un cliente nuevo no queda asociado a "Anais" sin que nadie lo elija.

## Fuera de alcance

- Cambiar cuántas vendedoras existen o sus zonas.
- Quitar el límite de 20 renglones por nota.

## Decisiones tomadas

- Volumen suma, no cascada: confirmado por el cliente.

- Sí hay proyecto de pruebas (`tests/NinOS.Tests`, xUnit), creado en la spec 002. Lo que no se puede automatizar (ventanas, PDF) se prueba a mano.
