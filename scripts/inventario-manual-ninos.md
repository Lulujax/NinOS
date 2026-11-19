# Inventario de módulos NinOS — base para el manual de usuario

> Generado explorando el código real de src/NinOS.UI. El Panel Admin se omite a propósito (secreto, exclusivo de Luis) y no debe aparecer en el manual.

**Orden del menú principal:** Notas de Entrega, Vendedores, Clientes, Ventas, Cuentas por Cobrar, Pagos, Comisiones, Inventario, Notas de Crédito, Pro Venta, Vesupronta.

La ventana principal tiene una barra de título con el número de versión (sale solo del ejecutable) y, debajo, una banda naranja de aviso de actualización con botón "Actualizar ahora" que solo aparece cuando hay una versión nueva disponible. Si el usuario intenta cerrar la app con una Nota de Entrega sin guardar, aparece una advertencia Sí/No que avisa que se pierden los datos y se libera el stock reservado.

---

## 1. NOTAS DE ENTREGA
Encabezado con combos **Vendedor*** y **Tipo de nota*** (obligatorios); al elegirlos se habilita el resto. Combo opcional **Lista de precios** (Principal + listas personalizadas). Si el tipo es Promoción aparece un campo **% Promoción**.

Formulario de la nota (estilo papel): Razón Social (buscador con lista desplegable de clientes de la zona del vendedor), Fecha Emisión (date picker), Nombre Comercial/Domicilio Fiscal/Dirección de Entrega/RIF/Teléfono (autocompletados del cliente, solo lectura), Crédito (días, editable, recalcula automáticamente la Fecha de Vencimiento), Número de documento (NOTA DE ENTREGA o NOTA DE DESPACHO en Pro Venta) — es solo una vista previa del correlativo, el número definitivo se asigna al guardar.

**Tabla de productos** (máx. 20 renglones — límite duro, mensaje: "La nota de entrega solo puede tener un máximo de 20 items"): Cantidad, Código (con buscador desplegable que marca en rojo lo agotado), Descripción (mismo buscador), Precio Unitario (solo lectura), columna de descuento de promoción si aplica, Precio Promoción (editable manualmente o calculado), Total, botón "–" para quitar el renglón. Botón "+ Agregar Producto". No se permite repetir el mismo producto dos veces ("El artículo ... ya está en la nota").

Bloque de condiciones y descuentos: texto de Condiciones (editable, obligatorio), Sub total, Descuento de condición % (editable), en notas tipo Volumen aparece además un campo de Descuento por Volumen % (los dos se SUMAN, no se aplican en cascada), Total con Descuento, Total a Pagar. Datos de pago (título, transferencia, cuenta, pago móvil) editables con botón "↺ Por Defecto" para restablecerlos. Campo Observaciones opcional. Botón **GUARDAR NOTA**, que abre una vista previa en PDF (ventana aparte) con botones Cancelar / Guardar / Guardar y Descargar Excel / Guardar y Descargar PDF — solo después de confirmar ahí se genera el número definitivo y, si otra persona lo tomó primero, se avisa con el número nuevo.

Validaciones al guardar: nota vacía, días de crédito vacíos, condiciones vacías, descuento fuera de 0-100%, renglón sin producto o cantidad ≤0, producto repetido, zona del cliente incompatible con el tipo de nota. Cambiar de vendedor o tipo de nota con datos sin guardar pide confirmación (Sí/No) y, si se acepta, descarta todo y libera el stock reservado.

## 2. VENDEDORES
Buscador por nombre + botón "Agregar Vendedor". Tabla: #, Código, Nombre Completo, % Comisión, Zonas Asignadas, Acciones (Editar/Borrar). Ventana Agregar/Editar: Código (autogenerado, solo lectura), Nombre Completo*, checkbox "Permitir pago de comisiones" + % Comisión, lista de Zonas Asignadas (checkboxes).

## 3. CLIENTES
Buscador + botones "Editar Zonas" y "Agregar Cliente". Botón "📄 Maestro de Clientes ▾" abre un panel de reporte: selector de zonas a incluir (Todas/Quitar), filtro de búsqueda activa, y 4 botones (Vista Previa/Imprimir/PDF/Excel). Pestañas por zona con tabla: #, Código, Razón Social, RIF, Contacto, Teléfono, Dir. Fiscal, Dir. Entrega, Zona, Acciones (Historial/Editar/Borrar) — también menú contextual (clic derecho) con las mismas opciones y doble clic abre el Historial.

Ventana Agregar/Editar Cliente: Código (autogenerado), Razón Social*, Identificación* (tipo RIF + número), Nombre de Contacto, Teléfono, Dirección Fiscal, Dirección de Entrega, Zona* (combo).

Ventana "Editar Zonas": tabla de zonas (Código, Nombre, Modalidad General/Pro Venta/Vesupronta, Descripción, Editar/Eliminar) + formulario Agregar/Editar Zona (Nombre*, Modalidad, Descripción opcional). Nota: la modalidad de la zona es lo que determina si sus clientes trabajan con notas General/Promoción, Pro Venta, o Vesupronta.

**Historial del Cliente** (ventana aparte, accesible desde el botón Historial): cabecera con datos del cliente + 4 tarjetas (Total Facturado, Total Abonado, Saldo Pendiente, Notas de Crédito), filtro por rango de fechas con botones Vista Previa/Imprimir/PDF/Excel, y 4 pestañas: Notas de Entrega (con Vista Previa/PDF/Abonos por fila), Notas de Crédito (con filtro de categoría), Cobranzas y Abonos Realizados, Artículos Más Comprados.

## 4. VENTAS
Buscador + Año*/Mes*/Zona (combos) + botón "Reporte" (panel con Tipo de Nota, Agrupar en Vendedor/Zona, checklists de Vendedores/Zonas, opciones de incluir Meta/Cobranza/Anuladas-Devueltas, y 4 botones de salida). Pestañas por vendedora con tabla: NRO, Vendedor, Cliente, Zona, Total, Estado (colores: Pagada verde, Pendiente naranja, Anulada gris cursiva), Fecha, Despacho, Tipo de Nota, Observación (editable in-line), Acciones (Vista Previa/PDF/Excel). Pie: Total Venta + Meta de ventas editable con barra de progreso.

## 5. CUENTAS POR COBRAR
Buscador + Año*/Mes* + filtro de estado + botón "Registrar pago" + botón "Reporte" (panel con criterio Mes/Año/Por Zona, Agrupar en, checklists de filtro, 4 botones de salida — las zonas no se incluyen pero pagadas/anuladas/devueltas no suman). Pestañas por vendedora: NRO, Despachado (date picker editable), Cliente, Zona, Monto* (editable in-line), Dcto (editable in-line), Abonado, Saldo Pend., Observación (editable), Estado, Acciones: Vista Previa, Exportar (PDF/Excel), Editar (monto/descuento, oculto si Anulada/Pagada/Devuelta), Pago (registrar abono), Anular. Pie: 3 totales (Facturado/Abonado/Por Cobrar).

Ventana "Registrar Pago": buscador de nota con lista desplegable, tipo Bolívares/Efectivo, Fecha*, Tasa + Monto en Bs* (si Bolívares), Monto USD* (no puede superar el saldo pendiente — error si se excede), Referencia*/Banco* (si Bolívares), Observaciones. Confirma con diálogo Sí/No mostrando el nuevo saldo y si queda en 0 avisa que la nota pasa a Pagada.

## 6. PAGOS
Buscador + Año/Mes + botón "Reporte" (Agrupar por Vendedor/Zona, checklists, 4 salidas). Pestañas por vendedora: NRO, Cliente, Monto, Dcto, Pagos, Saldo Pend., Forma Pago, Banco, Últ. Fecha, Estado. Doble clic abre el **Historial de Abonos de la nota**: cabecera (Nota/Cliente/Vendedor + Facturado/Total Abonado/Saldo Pendiente), tabla de abonos (Fecha Pago, Fecha Registro, Últ. Edición, Tipo, Banco, Referencia, Monto Bs, Tasa, Monto, Observaciones, botón ✏️ Editar por fila — oculto en notas de crédito o anulaciones), botón "+ Registrar Nuevo Abono". Pie del módulo Pagos: Total Facturado + Total Cobrado.

## 7. COMISIONES
Buscador + Año/Mes + filtro + botones Vista Previa/Imprimir/PDF/Excel + "Pago de comisión". Pestañas por vendedora: NRO, Cliente, Monto Facturado, Fecha de Pago, Monto Pagado, Pronto Pago, Comisión, Total Comisión, Pago Comisión, Comprobante (Vista Previa/Imprimir/Exportar — solo si está Pagada). Pie: Total Facturado/Total Comisión/Comisión Pendiente.

Ventana "Liquidación de Comisiones": selecciona Vendedora* primero, luego busca y agrega notas a una tabla (con botón X para quitar), resumen de totales, Tasa BS/USD*, Monto USD (calculado, solo lectura), Monto BS*, Tipo (Pago Móvil/Efectivo), Fecha, Referencia*, Banco, Observaciones. Botón "Liquidar".

## 8. INVENTARIO
Buscador de producto/combo + botón "📄 Reportes ▾" (panel con dos columnas: Lista de Precios —checklists de listas y marcas, 4 salidas— y Reportes de Inventario —"Solo inventario actual" y "Todos los movimientos", cada uno con Vista Previa/Imprimir/PDF/Excel). Combo "Lista" + botón "EDITAR LISTAS". Botones "Editar Marcas" y "Agregar Producto/Combo".

Pestañas: Todos (productos+combos, con columna Categoría), por cada Marca, y Promociones. Columnas: #, Código, Nombre, Categoría, Disponibles (en rojo si negativo = "deuda de inventario"), Precio, Acciones (Editar/Borrar — Borrar deshabilitado con tooltip si no se puede eliminar).

**Ventana Producto**: Código (bloqueado si el apartado de stock está abierto), Nombre*, Categoría* (marca/línea — cambiarla reasigna el código), Cantidad Inicial* (solo en producto nuevo) o, si ya existe, Stock actual (solo lectura) + botones "Agregar Stock"/"Quitar Stock"/"Editar Stock" que despliegan un apartado: Cantidad a agregar/quitar o Nuevo stock (conteo físico), Motivo del ajuste* (combo), Total resultante. Precio*. **Permite stock negativo** (deuda de inventario). Cada movimiento queda en el historial del producto.

**Ventana Promoción** (combos/kits): Tipo* (Producto Individual en Oferta / Armar Kit / Armar Combo), Nombre, Precio Total*, buscador de productos + tabla de Composición (Código, Descripción, Cantidad editable, botón X para quitar).

**Editar Listas de Precios**: tabla (Nombre, Descripción, Editar/Eliminar — la lista "Principal" no se puede eliminar) + formulario Agregar/Editar (Nombre*, Descripción opcional).

**Editar Líneas de Productos (marcas)**: tabla (Prefijo, Nombre, Orden, Estado, Editar/Ocultar-Mostrar/Eliminar) + formulario (Prefijo* de hasta 4 caracteres, Nombre*, checkbox Visible en inventario). Al guardar muestra overlay "Actualizando códigos" porque reescribe todo el inventario y el historial de esa línea.

**Historial del Producto**: cabecera (Producto/Código/Categoría/Precio/Stock) + tabla de movimientos (Nota, Fecha, Documento, Motivo, Cliente, Vendedor, Movimiento ENTRADA/SALIDA con color, Unidades, Subtotal Stock, Precio, Estado) + Vista Previa/Imprimir/PDF/Excel.

**Historial de la Promoción**: cabecera con nombre/código/categoría/tipo/precio + tabla "Está compuesta por" + historial de ventas de esa promoción.

## 9. NOTAS DE CRÉDITO
Combo Empresa + Categoría + Año/Mes + botón Reporte (General o Mensual, por Categoría, incluir anuladas, 4 salidas) + botón "Nueva Nota de Crédito". Pestañas por vendedor: Fecha NC, Categoría, Tipo Nota, Empresa, Nro NC, Nota de Entrega origen, Cliente, Zona, Vendedor, Monto, Acciones (Vista Previa/PDF/Excel/Anular). Filas anuladas en rosa/rojo. Pie: total de notas de crédito (las anuladas no suman).

**Ventana Nueva Nota de Crédito**: Categoría* (Devolución / Obsequio).
- *Devolución*: Vendedor* → Mes de la Nota* → buscador de Nota de Entrega* de ese vendedor/mes. Tabla editable por producto: A Devolver (editable, tope = columna Disponible), Entregado, Devuelto (ya devuelto antes), Disponible, Precio U., Subtotal. El importe se recorta solo si supera el saldo que le queda por devolver a la nota original.
- *Obsequio*: Zona (opcional) → buscador de Cliente* → buscador de Producto a Obsequiar (agrega filas con botón X para quitar).
Total a Devolver (USD), Observaciones, botones Vista previa / Guardar y PDF / Guardar / Cancelar.

**Detalle de Nota de Crédito**: cabecera (Nro NC, Nota de entrega origen, Cliente, Vendedor, Fecha, Total Devuelto, Cantidad Devuelta) + tabla de ítems devueltos + botón Generar PDF.

## 10. PRO VENTA
Pestaña separada con fondo azul, "EMPRESA: PRO VENTA". Combo "Relación Pro Venta": Reporte Semanal / Relaciones Pendientes / Relaciones Pagadas.

**Reporte Semanal**: Mes + Semana + Relación (combo editable) + botón Reporte (incluir anuladas, 4 salidas). Tabla: Nota, Cliente, Monto Orig., Com. Luis 10%, Gastos Oper. 25%, Gastos Adm. 15%, Fecha Pago Cliente, Forma de Pago, Acciones (Vista Previa/Imprimir/Excel/Editar/Anular). Totales de la fila + bloque "LIQUIDACIÓN": Total Cobrado (notas menos comisión Luis), Nota de Entrega por Pagar a Pro Venta, Diferencial. Fórmulas confirmadas: comisión 10%, gastos operativos 25%, gastos administrativos 15% de (monto original − gastos operativos), todo calculado por nota sobre el monto original (sin descuento).

**Relaciones Pendientes/Pagadas**: Mes + botón Reporte. Tabla: Relación Nro, Monto por Pagar, Saldo Completo, Estado, Acciones (Historial/Vista Previa/PDF/Excel/Editar Notas/Abonar-Pagar en pendientes).

**Registrar Pago Pro Venta**: info de la relación + botón "Editar notas de esta relación", Fecha*, Monto USD*, Observación.

**Editar Descuento y Monto de Notas (Pro Venta)**: selector de nota de la relación, tarjetas (Cliente/Monto Original/Descuento Actual/Monto a Cobrar Actual), campos Descuento % + botón Aplicar descuento, o Monto a Cobrar USD + botón Aplicar monto, tabla de todas las notas de la relación con sus totales.

## 11. VESUPRONTA
Pestaña morada "EMPRESA: VESUPRONTA" con 3 sub-pestañas que reutilizan exactamente los mismos módulos: Ventas Vesupronta, Cuentas por Cobrar Vesupronta, Pagos Vesupronta (misma funcionalidad que los módulos 4-6, pero filtrados a la empresa Vesupronta; sin comisiones).

---

### Comportamiento transversal (aplica a todos los módulos)
- Botón "Vista Previa" en reportes abre una ventana genérica con visor de PDF integrado y botones Imprimir / Descargar PDF / Descargar Excel (si aplica) / Cerrar.
- Todo documento es tamaño carta; una nota de entrega/crédito nunca ocupa más de 1 hoja ni más de 20 productos.
- Cambiar de pestaña/selección con datos sin guardar en Notas de Entrega siempre pregunta antes de descartar.
- Mensajes de error y confirmación están en español, tono directo (ej.: "El monto supera el saldo pendiente de la nota", "La nota de entrega solo puede tener un máximo de 20 items").
