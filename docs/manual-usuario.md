# 📖 Manual de Usuario — NinOS

**Sistema Administrativo de Facturación, Cobranzas y Comisiones**
Versión 1.0.6 · Windows

---

> *Este manual está escrito para personas que no tienen experiencia con computadoras.
> No hace falta que sepas nada técnico: solo sigue los pasos tal como están escritos,
> en orden, y podrás usar el sistema completo. Si algo no coincide con lo que ves en
> pantalla, detente y consulta a tu administrador antes de presionar cualquier botón.*

---

## 📑 Contenido

1. [Antes de empezar](#1-antes-de-empezar)
2. [La pantalla principal](#2-la-pantalla-principal)
3. [Palabras que debes conocer](#3-palabras-que-debes-conocer)
4. [Emitir una nota de entrega (facturar)](#4-emitir-una-nota-de-entrega-facturar)
5. [Registrar un pago (cobrar un abono)](#5-registrar-un-pago-cobrar-un-abono)
6. [Consultar quién debe (Cuentas por Cobrar)](#6-consultar-quién-debe-cuentas-por-cobrar)
7. [Generar el reporte de ventas del mes](#7-generar-el-reporte-de-ventas-del-mes)
8. [Devolver mercancía (Nota de Crédito)](#8-devolver-mercancía-nota-de-crédito)
9. [Trabajar con clientes](#9-trabajar-con-clientes)
10. [Trabajar con vendedoras y zonas](#10-trabajar-con-vendedoras-y-zonas)
11. [Inventario: productos, precios y movimientos](#11-inventario-productos-precios-y-movimientos)
12. [Pro Venta: relaciones semanales](#12-pro-venta-relaciones-semanales)
13. [Comisiones de las vendedoras](#13-comisiones-de-las-vendedoras)
14. [Los colores y los estados](#14-los-colores-y-los-estados)
15. [Reglas importantes, explicadas sencillo](#15-reglas-importantes-explicadas-sencillo)
16. [Preguntas frecuentes y problemas comunes](#16-preguntas-frecuentes-y-problemas-comunes)
17. [Consejos de uso diario](#17-consejos-de-uso-diario)
18. [Referencia rápida de botones](#18-referencia-rápida-de-botones)

---

## 1. Antes de empezar

### 1.1. Cómo abrir el sistema

1. Enciende la computadora y espera a que termine de cargar Windows.
2. Busca en el escritorio el icono **NinOS — Sistema Administrativo** (un rayo sobre un círculo verde).
3. **Haz doble clic** con el botón izquierdo del mouse sobre el icono.
4. Espera unos segundos: aparecerá una pantalla de bienvenida mientras el sistema se prepara.
5. Se abrirá la ventana principal, maximizada (ocupa toda la pantalla).

> **No cierres el sistema con la "X" roja mientras haya una nota sin guardar.**
> Si presionaste un botón por error, usa **Cancelar** para salir sin guardar cambios.

### 1.2. Cómo se usa el mouse (lo básico)

| Acción | Cómo se hace | Para qué sirve |
|---|---|---|
| **Clic** | Presionar una vez el botón izquierdo | Presionar botones y elegir opciones |
| **Doble clic** | Presionar dos veces seguidas | Abrir un producto para ver su historial |
| **Escribir** | Hacer clic dentro de la caja blanca y escribir con el teclado | Poner nombres, cantidades, precios |
| **Elegir de una lista** | Hacer clic en la flecha **▼** de la caja y elegir con un clic | Seleccionar cliente, vendedor, mes, etc. |
| **Marcar una casilla** | Hacer clic dentro del cuadrito **☐** para que quede con **☑** | Activar opciones de un reporte |
| **Desplazar la pantalla** | Llevar el puntero al borde derecho de la lista y arrastrar la barra | Ver filas que no caben en la pantalla |

### 1.3. Signado de los asteriscos

Donde ves un **\*** rojo junto a un campo, significa que **es obligatorio**: no podrás guardar si lo dejas vacío.

---

## 2. La pantalla principal

Al abrir NinOS verás una franja verde en la parte superior con el nombre y versión del sistema, y debajo una fila de **pestañas**. Cada pestaña es un área de trabajo distinta:

| Pestaña | Para qué sirve |
|---|---|
| **Notas de Entrega** | Facturar: crear las notas que se le entregan a los clientes |
| **Vendedores** | Dar de alta, editar o buscar a las vendedoras |
| **Clientes** | El directorio de todos los clientes y sus datos |
| **Ventas** | Ver las ventas del mes y descargar el reporte |
| **Cuentas por Cobrar** | Ver quién debe, cuánto y registrar cobros |
| **Pagos** | El historial de todo lo cobrado y el reporte de pagos |
| **Comisiones** | Calcular y pagar las comisiones de cada vendedora |
| **Inventario** | Productos, precios, stock y lista de precios |
| **Notas de Crédito** | Registrar devoluciones y obsequios |
| **Pro Venta** | Las relaciones semanales de las zonas Pro Venta |

**Para cambiar de área:** basta con un clic sobre el nombre de la pestaña.

---

## 3. Palabras que debes conocer

Estas son las palabras que el sistema usa a diario. Guárdalas a mano:

- **Nota de Entrega:** el documento que se le entrega al cliente con los productos que compró. Es lo que antes hacías a mano.
- **Cliente:** el negocio o persona a quien se le vende.
- **Vendedora:** la persona que atiende y factura a ese cliente.
- **Zona:** el grupo o territorio al que pertenece un cliente. Los reportes se agrupan por zona.
- **Producto:** cada artículo del catálogo, con su código, nombre y precio.
- **Stock / Disponibles:** la cantidad de unidades que hay en almacén.
- **Abono o Pago:** el dinero que el cliente entrega a cuenta de lo que debe.
- **Saldo pendiente:** lo que falta por cobrar de una nota.
- **Nota de Crédito:** el documento que se emite cuando el cliente **devuelve** mercancía o cuando se le hace un **obsequio**. Reduce lo que debe.
- **Anular:** dejar un documento sin efecto, sin borrarlo. Sirve para corregir errores.
- **Pro Venta:** el sistema de relaciones semanales usado por las zonas configuradas en modo Pro Venta.
- **Comisión:** el porcentaje que le corresponde a cada vendedora por lo que cobró.

---

## 4. Emitir una nota de entrega (facturar)

Es la operación más usada. Sigue los pasos en orden:

1. **Entra a la pestaña “Notas de Entrega”.**
2. **Elige la vendedora:** en la casilla **Vendedor:\*** haz clic en la flecha ▼ y elige tu nombre.
3. **Elige el tipo de nota:** en **Tipo de nota:\*** selecciona el tipo que corresponda (General, Pro Venta, Promoción, etc.). *Si no estás seguro, pregunta a tu administrador cuál te corresponde usar.*
4. **Busca el cliente:** en **RAZÓN SOCIAL** escribe unas letras del nombre del cliente y el sistema irá filtrando la lista. Haz clic para elegirlo. Sus datos (nombre comercial, RIF, dirección, teléfono) se completan solos.
5. **Revisa la fecha de emisión:** aparece en **Fecha Emisión**. Cambia solo si es necesario (haz clic y elige el día en el calendario).
6. **Agrega los productos:** haz clic en el botón **+ Agregar Producto**. En la fila nueva:
   - Escribe o elige el **Código** del producto.
   - Revisa la **Descripción** y el **precio unitario**.
   - Cambia la **cantidad** (las unidades que lleva el cliente).
   - Si el producto está en promoción, se usará el **precio promocional** automáticamente.
7. **Revisa el TOTAL:** aparece al pie de la nota, con el sub total y el total general. Si el tipo de nota permite descuento por volumen, verás el campo **% Promoción** y **Descuento por volumen** en la parte superior.
8. **Guarda:** presiona el botón **GUARDAR NOTA**.
9. Se abrirá la **vista previa** de la nota, exactamente como se imprimirá:
   - **Guardar** → guarda la nota sin imprimir.
   - **Guardar y PDF** → guarda y genera el archivo PDF de la nota.
   - **Cancelar** → sale sin guardar (la nota no se registra).

> **El stock se descuenta solo** cuando guardas la nota. Si un producto no tiene suficiente existencia, el sistema te lo avisará y no dejará facturar más de lo disponible.

> **Si te equivocaste en una nota ya guardada**, no la borres: se **anula** desde la pestaña Cuentas por Cobrar (botón **Anular**). Anular devuelve el stock automáticamente.

---

## 5. Registrar un pago (cobrar un abono)

Cuando un cliente entrega dinero a cuenta de sus notas:

1. Entra a la pestaña **Cuentas por Cobrar** (o a **Pagos**).
2. **Busca la nota:** usa el buscador o los filtros de **Mes** y de vendedora hasta encontrar la nota del cliente.
3. Con la nota seleccionada, presiona **Registrar pago**.
4. Se abrirá la ventana **Registrar Pago**. Completa:
   - **Tipo:** Efectivo, Transferencia, Pago Móvil o Bolívares.
   - **Fecha:** el día en que se recibió el dinero.
   - **Monto USD:** la cantidad recibida en dólares.
   - **Tasa / Bs:** solo si cobraste en bolívares; escribe la tasa del día y el monto en bolívares.
   - **Banco**, **Ref.** (número de referencia) y **Obs.** (observación): solo si aplica.
5. Presiona **Registrar**.

**Dos reglas que el sistema no deja pasar:**

- **No se puede pagar de más.** Si escribes un monto mayor al saldo pendiente, el sistema lo rechaza.
- **Sí se puede pagar de menos.** Un abono menor al saldo se acepta y la nota queda con saldo pendiente para otro día.

---

## 6. Consultar quién debe (Cuentas por Cobrar)

1. Entra a la pestaña **Cuentas por Cobrar**.
2. Verás una pestaña por vendedora (**Todos**, **Sandra**, **Anais**, **Alejandra**, **Juan Luis**). Haz clic en la que necesites.
3. Usa los filtros de **Mes** y de estado:
   - **Por Cobrar** → solo las notas con saldo pendiente.
   - **Anuladas** → las notas anuladas.
   - **Devueltas** → las notas con devoluciones.
   - **Todas** → sin filtro.
4. En cada fila verás: NRO, CLIENTE, FECHA, MONTO, ABONADO, SALDO PEND. y ESTADO.
5. **Acciones disponibles** sobre cada nota:
   - **Registrar pago** → cobrar un abono.
   - **Editar** → corregir observaciones de cobranza o la fecha de despacho.
   - **Anular** → dejar la nota sin efecto (devuelve el stock).
   - **Vista Previa** / **PDF** → ver o descargar la nota.
6. Para un resumen impreso, presiona **Reporte** y configura:
   - **Mes** y **Año**.
   - **Agrupar en:** Por Vendedor o Por Zona.
   - Marca con ☑ solo los vendedores y zonas que quieras incluir.
     *Lo que esté desmarcado NO entra en el reporte.*
   - El reporte incluye **únicamente notas pendientes**: las pagadas, anuladas y devueltas no salen ni suman.

### El semáforo de colores del saldo

| Color del saldo | Significado |
|---|---|
| 🟢 **Verde** | Saldo en **0**: la nota está saldada |
| 🟠 **Naranja** | Saldo **bajo** (hasta $100): casi saldada |
| 🔴 **Rojo** | Saldo **alto** (más de $100): atención, está en mora |

---

## 7. Generar el reporte de ventas del mes

1. Entra a la pestaña **Ventas**.
2. Presiona el botón **Reporte**.
3. Se abrirá **CONFIGURAR REPORTE DE VENTAS**. Elige:
   - **Mes** y **Zona** / vendedor.
   - **TIPO DE NOTA:** todas o solo un tipo.
   - **AGRUPAR EN:** por vendedor o por zona (esto define cómo se separan las páginas del PDF).
   - **Opciones del reporte** (marca con ☑ lo que quieras incluir):
     - ☐ **Incluir cobranza (Abonado y Saldo)**
     - ☐ **Incluir anuladas / devueltas** *(se muestran en color, pero no suman)*
     - ☐ **Incluir bloque de Meta de Ventas**
   - Marca o desmarca vendedores y zonas con **Seleccionar todo** / **Quitar todo**.
4. Presiona **PDF** para descargar el archivo o **Vista Previa** para verlo antes de imprimir.

---

## 8. Devolver mercancía (Nota de Crédito)

1. Entra a la pestaña **Notas de Crédito**.
2. Presiona **Nueva Nota de Credito**.
3. Se abrirá la ventana de **Nota de Crédito**:
   - **Mes de la Nota:** elige el mes de la nota de entrega que se está revirtiendo.
   - **Nota de Entrega:** busca la nota. *Solo aparecen las notas que tienen saldo pendiente: una nota totalmente pagada no se puede devolver.*
   - El sistema muestra el **cliente**, la **vendedora**, la **zona** y el **tipo** de la nota original.
   - Elige la acción:
     - **Devolución** → el cliente devuelve productos y se le devuelve el dinero (o se le acredita).
     - **Obsequio** → se entrega producto gratis; se busca con **Buscar Producto a Obsequiar**.
4. En la lista de productos, escribe en **A DEVOLVER** la cantidad de cada artículo. Verás:
   - **ENTREGADO** (cuánto se le vendió), **DEVUELTO**, **DISPONIBLE** (lo que queda por devolver).
   - **PRECIO U.** con el precio neto real (con su descuento ya prorrateado).
   - El **TOTAL A DEVOLVER (USD)** al pie.
5. Escribe unas **Observaciones** (opcional) y presiona **Guardar** o **Guardar y PDF**.

**Anular una nota de crédito:** si la nota de crédito se hizo por error, selecciónala en la lista y presiona **Anular**. El sistema revierte exactamente lo que hizo: la devolución devuelve el abono a la nota de entrega y saca el stock que había ingresado; el obsequio regresa el stock al inventario. La nota **no se borra**: queda marcada como **Anulada**.

---

## 9. Trabajar con clientes

### Agregar un cliente nuevo

1. Entra a la pestaña **Clientes**.
2. Presiona el botón de agregar (＋).
3. Completa el formulario:
   - **Codigo del Cliente:** el sistema sugiere uno; el prefijo corresponde a la vendedora.
   - **Razon Social (Negocio/Nombre)\***: el nombre del negocio. *Obligatorio.*
   - **Identificacion:** RIF o cédula.
   - **Telefono** y **Nombre de Contacto.**
   - **Direccion Fiscal** y **Direccion de Entrega.**
   - **Zona \*:** elige la zona a la que pertenece. *Obligatorio: sin zona, los reportes no lo agrupan bien.*
4. Presiona **Guardar** (o **Cancelar** para salir sin guardar).

### Ver el historial de un cliente

- Selecciona al cliente en la lista y abre su **Historial**: verás todas sus notas de entrega, notas de crédito y pagos, y podrás descargarlo en PDF.

---

## 10. Trabajar con vendedoras y zonas

1. Entra a la pestaña **Vendedores**.
2. Botones disponibles:
   - **Agregar Vendedor:** pide **Código del Vendedor** (3 dígitos: 001, 002, 003, 004, 005…) y **Nombre Completo**. También puedes marcar sus **Zonas Asignadas**.
   - **Editar:** cambiar nombre o zonas.
   - **Borrar:** elimina *lógicamente* (la vendedora deja de aparecer, pero su historial se conserva siempre).
3. Usa **Buscar Vendedor** para encontrarla rápidamente.

> **Códigos en uso:** Sandra `001`, Anais `002`, Alejandra `003`, Juan Luis `004`. Las vendedoras nuevas continúan desde `005`.
>
> **Las zonas se configuran desde el panel del administrador** (Gestionar Zonas). Cada zona puede ser de tipo **General** o **Pro Venta**, y eso define qué tipos de nota puede usar la vendedora en esa zona.

---

## 11. Inventario: productos, precios y movimientos

1. Entra a la pestaña **Inventario**.
2. **Buscar:** escribe en **Buscar Producto o Combo** por código o nombre.
3. Botones disponibles:
   - **Agregar / Editar:** crea o corrige un producto con **Código**, **Nombre**, **Categoría**, **Precio ($)** y **Cantidad**.
     *Al cambiar la cantidad, el sistema te pide un **Motivo** y te muestra en cuánto cambia el stock. Ese movimiento queda registrado en el kárdex.*
   - **Borrar:** elimina el producto de forma lógica (va a la papelera, se puede restaurar).
   - **Editar Líneas…:** administra las marcas/categorías y el prefijo de sus códigos.
     > ⚠️ Cambiar el prefijo de una línea **reescribe todos los códigos** de esa marca y de su historial. El sistema te mostrará una pantalla de confirmación con la cantidad de registros que se van a tocar. Solo el administrador debe usarlo.
   - **Descargar lista de precios:** abre **LISTA DE PRECIOS**; marca las marcas/categorías que quieras incluir y genera el PDF.
4. **Ver el movimiento de un producto (kárdex):** haz **doble clic** sobre el producto. Verás cada entrada y salida con el saldo acumulado, y podrás exportarlo a PDF.

---

## 12. Pro Venta: relaciones semanales

1. Entra a la pestaña **Pro Venta**.
2. **Elige el Mes\***: hasta que selecciones un mes, verás el mensaje *“Seleccione un mes para ver el reporte”* y las tablas vacías. **Es normal: no se carga nada hasta que tú lo pides.**
3. Elige la **Semana** y la **Relación** que necesites.
4. Verás las notas de esa semana con sus columnas: MONTO NOTA, SALDO COMPLETO, MONTO POR PAGAR, FORMA DE PAGO, etc., más el desglose de **GASTOS ADM. 15%**, **GASTOS OPER. 25%** y **COM. LUIS 10%**.
5. Las tres vistas disponibles:
   - **Relacion por cobrar - pendientes**
   - **Relacion por cobrar - pagadas**
   - **Relacion por cobrar - reporte semanal**
6. Botones:
   - **Abonar / Pagar:** registrar el cobro de una relación.
   - **Anular:** dejar una relación sin efecto.
   - **Imprimir** / **Imprimir PDF** / **Vista Previa:** imprimir la relación.

> Las notas anuladas aparecen **en rojo** y **no suman** al total, ni al conteo, ni a la liquidación.

---

## 13. Comisiones de las vendedoras

1. Entra a la pestaña **Comisiones**.
2. **Elige la vendedora en el desplegable de Vendedora.** *Este paso es obligatorio y el desplegable arranca vacío a propósito:* hasta que no elijas, no se carga ninguna nota. Así nunca se mezclan las notas de dos liquidaciones distintas.
3. Verás la lista de notas de esa vendedora con: CLIENTE, VENTA, COMISION, ESTADO, ÚLT. PAGO.
4. Revisa los totales de abajo: **TOTAL VENTA**, **TOTAL COMISION** y **COMISION PENDIENTE**.
5. Para pagar, presiona **Pago de comision**, indica el monto y confirma.
6. Para el comprobante impreso, usa el botón **PDF** de la fila o el **PDF** general.
7. El **historial** (kárdex de comisiones) muestra lo devengado vs. lo pagado por cada vendedora.

---

## 14. Los colores y los estados

### Estados de una nota de entrega

| Estado | Qué significa |
|---|---|
| **Pendiente** | Se entregó y aún no se ha cobrado por completo |
| **Pagada** | Está saldada. Ya no admite devoluciones ni más abonos |
| **Devuelta** | Se le hizo una nota de crédito parcial o total |
| **Anulada** | Se anuló. No cuenta para nada, pero se conserva el registro |

### Colores en las listas y en los PDF

| Color | Significado |
|---|---|
| **Fila en rojo** | Nota **anulada**. Visible para auditoría, **nunca suma** |
| **Fila en morado** | Nota **devuelta**. Tampoco suma |
| **Saldo verde / naranja / rojo** | Semáforo de cobranza (ver sección 6) |
| **Blanco normal** | Nota vigente |

> En todos los PDF, las notas anuladas y devueltas van en un bloque aparte al pie con la leyenda *“Excluidas del subtotal y total”*.

---

## 15. Reglas importantes, explicadas sencillo

1. **Lo anulado y lo devuelto nunca suman.** Aparece en pantalla para que tengas el historial completo, pero los totales solo cuentan notas vigentes.
2. **Una nota pagada no se puede devolver.** Si el cliente ya pagó todo, no hay nada que acreditar.
3. **No se puede cobrar de más.** El sistema no acepta pagos mayores al saldo. Un pago menor sí.
4. **No borres información: anúlala.** Anular deja todo como estaba (devuelve el stock o el saldo) y conserva el registro. Borrar a mano provoca reportes cuadrados mal.
5. **La zona agrupa, la vendedora liquida.** Los reportes se agrupan por zona; el dinero y las comisiones se liquidan por vendedora.
6. **La fecha de una nota de crédito es la fecha en que se emitió el crédito**, no la de la nota original. El filtro por mes te muestra por esa fecha.
7. **Las vendedoras de zonas Pro Venta** usan notas Pro Venta (MAR / PVP) y las de zonas General usan General (GEN) y Promoción (PRM). Cualquier vendedora con una zona Pro Venta puede facturar en ella.
8. **Los códigos de producto van en mayúsculas y no se pueden repetir.**

---

## 16. Preguntas frecuentes y problemas comunes

**❓ La aplicación no abre / no responde.**
Espera 10 segundos y vuelve a intentar. Si pide cerrar, ciérrala y ábrela de nuevo desde el ícono del escritorio. Si persiste, reinicia la computadora y avisa a soporte.

**❓ Aparece un mensaje de que no se puede conectar a la base de datos.**
Presiona **Reintentar** (puedes hacerlo hasta 5 veces). Suele pasar si el servidor está reiniciándose o falló el internet. No presiones Salir a la primera.

**❓ Me dice que no hay stock suficiente.**
Esa mercancía no alcanza para lo que estás facturando. Revisa la cantidad escrita; si crees que hay un error de inventario, revisa el kárdex del producto (doble clic en Inventario).

**❓ No encuentro al cliente en la lista.**
Escribe solo parte del nombre (por ejemplo “MAR” en vez de “MARIA”). Si el cliente no existe, créalo en la pestaña **Clientes**.

**❓ No encuentro una nota en Cuentas por Cobrar.**
Revisa que el **Mes** y el **estado** (Por Cobrar / Todas) estén bien elegidos, y que estés en la pestaña de la vendedora correcta.

**❓ Ya guardé una nota con un error.**
No la borres. Anúlala desde **Cuentas por Cobrar → Anular** y vuelve a emitirla bien. Así el stock se reajusta solo.

**❓ Me equivoqué en un pago.**
Los pagos quedan en el historial. Consulta a tu administrador para corregirlo desde el registro; nunca dupliques un pago para “compensarlo”.

**❓ La letra se ve muy chica.**
Sube la escala de texto de Windows: *Configuración → Pantalla → Escala* (125% o 150%) y vuelve a abrir NinOS. El sistema respeta el tamaño que tú configures y nunca lo reduce.

**❓ Quiero imprimir una nota otra vez.**
Ve a **Cuentas por Cobrar**, búscala y usa **Vista Previa** o **PDF**; puedes imprimirla cuantas veces quieras.

**❓ Se me cerró la ventana mientras escribía.**
Vuelve a abrir la nota desde cero: lo que no se guardó, no se registró, así que no hay duplicados.

**❓ ¿Puedo usar el sistema a la vez que otra persona?**
Sí. Cada quien trabaja en su pestaña; el servidor es quien controla que no se pisen los números.

> **Panel de Administrador:** existe un área protegida con contraseña para el dueño del sistema (backups, papelera y ajustes). **No es necesaria para el trabajo diario** de las vendedoras.

---

## 17. Consejos de uso diario

- **Guarda con calma:** revisa total y cantidades antes de presionar **GUARDAR NOTA**.
- **Cobra al momento** cuando el cliente esté enfrente: usa **Registrar pago** en ese instante.
- **Revisa el semáforo** al inicio de cada semana: el rojo es lo que hay que cobrar primero.
- **Genera los reportes** al cierre de cada mes y archívalos en PDF.
- **No dejes notas viejas pendientes de anular**: corrige el día mismo del error.
- **Haz respaldos** (o pide al administrador que lo haga) antes de cambios grandes; el sistema guarda backups automáticos.
- **Aprende los atajos:** el buscador de cada lista filtra mientras escribes; no hace falta recorrer toda la lista con el mouse.

---

## 18. Referencia rápida de botones

| Botón | Qué hace | Dónde está |
|---|---|---|
| **GUARDAR NOTA** | Emite la nota de entrega y descuenta stock | Notas de Entrega |
| **+ Agregar Producto** | Añade una fila de producto a la nota | Notas de Entrega |
| **Guardar y PDF** | Guarda y genera el PDF (vista previa de nota y NC) | Notas / Notas de Crédito |
| **Registrar pago** | Cobra un abono sobre una nota | Cuentas por Cobrar |
| **Registrar** | Confirma el pago en la ventana de pago | Ventana Registrar Pago |
| **Anular** | Deja sin efecto una nota o NC, revirtiendo stock/saldo | CxC / Notas de Crédito / Pro Venta |
| **Editar** | Corrige observaciones o fecha de despacho | Cuentas por Cobrar |
| **Reporte** | Abre la configuración del reporte del mes | CxC / Ventas / Pagos / NC |
| **PDF / Descargar PDF** | Genera el archivo PDF del documento o reporte | Todas las pestañas |
| **Vista Previa** | Muestra el documento antes de imprimir | Todas las pestañas |
| **Nueva Nota de Credito** | Abre el formulario de devolución/obsequio | Notas de Crédito |
| **Agregar Vendedor** | Da de alta una vendedora | Vendedores |
| **Agregar (＋) cliente** | Da de alta un cliente | Clientes |
| **Editar Líneas…** | Administra marcas y prefijos de código ⚠️ *(solo administrador)* | Inventario |
| **Descargar lista de precios** | Genera el PDF del catálogo de precios | Inventario |
| **Abonar / Pagar** | Registra el cobro de una relación Pro Venta | Pro Venta |
| **Pago de comision** | Paga la comisión de la vendedora seleccionada | Comisiones |
| **Seleccionar todo / Quitar todo** | Marca o desmarca vendedores y zonas del reporte | Popups de reporte |

---

<div align="center">

**NinOS — Sistema Administrativo**
*Manual de usuario · Versión 1.0.6*

Ante cualquier duda, primero consulta a tu administrador antes de presionar botones que no reconozcas.

</div>
