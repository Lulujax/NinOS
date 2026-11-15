# Plan 010 - Actualización automática

Cómo cumplir [spec.md](spec.md). Todavía no se ha tocado código.

## Qué hay hoy (medido)

| Dato | Valor |
|---|---|
| Paquete completo (`publish\`) | 179 MB, 283 archivos, autocontenido (trae el runtime de .NET) |
| Código propio | 2,9 MB (`NinOS.UI.dll` 1,6 MB, `NinOS.Infrastructure.dll` 1,2 MB, `NinOS.Domain.dll` 0,1 MB) |
| Versión declarada | `<Version>1.0.10</Version>` en [NinOS.UI.csproj](../../src/NinOS.UI/NinOS.UI.csproj) |
| Mecanismo de actualización | No existe ninguno |
| Instalación | Inno Setup, `Program Files`, pide administrador, carpeta abierta a todos los usuarios |
| Pantalla de carga | [SplashWindow](../../src/NinOS.UI/Views/SplashWindow.xaml.cs) ya tiene `SetLoading`, `SetReady`, `SetError`, `RetryRequested`, `ExitRequested` |

**Capacidad del VPS:** 1,9 GB de RAM (347 MB en uso), 1 vCore, 25,3 GB libres. nginx sirviendo archivos estáticos añade unos 20 MB de RAM y nada de CPU. Tres versiones completas ocupan ~540 MB. El servidor no es el cuello de botella.

## Arquitectura: tres piezas

**1. En la app (cliente).** Integración con Velopack para buscar, descargar y aplicar la actualización, más la lógica de aviso y versión mínima.

**2. En el VPS (servidor).** nginx sirviendo una carpeta estática por HTTPS. Solo entrega archivos; no recibe nada. La subida va por SSH, que ya existe.

**3. En la PC de Luis (publicación).** Un script `deploy.ps1` que pide la contraseña una vez y hace todo: compilar, empaquetar, subir, dejar solo 3 versiones y actualizar la política en la base.

## Herramienta: Velopack

Es el estándar actual para apps .NET de escritorio (sucesor de Squirrel) y resuelve de fábrica lo difícil: actualizaciones diferenciales, reemplazar un ejecutable que está corriendo, descargas cortadas, reinicio y vuelta atrás. Confirmaría la versión exacta y los nombres de su API al escribir la primera tarea.

**No hacerlo a mano.** Windows no permite reemplazar un `.exe` en uso, así que haría falta un segundo programa actualizador, manejar descargas parciales, verificar integridad y poder revertir. Es mucho más código y más frágil que usar la herramienta.

## Autenticidad de los paquetes: qué se puede y qué cuesta

Esto es lo más delicado del plan, porque el actualizador ejecuta código en las PCs del cliente.

| Capa | Protege de | Costo |
|---|---|---|
| **HTTPS con Let's Encrypt** | Que alguien en el medio de la conexión cambie el paquete | Gratis (subdominio DuckDNS + certbot) |
| **Hashes de integridad** (los trae Velopack) | Archivos corruptos o alterados en el servidor | Gratis, incluido |
| **Fijar la identidad del servidor en la app** | Que un certificado falso de otra autoridad se haga pasar por el servidor | Gratis, poco código |
| **Certificado de firma de código de Windows** | Advertencias de SmartScreen y antivirus | ~200 USD al año |

**Plan: las tres primeras capas, que son gratis.** Son suficientes para el riesgo real.

**Riesgo que queda sin cubrir y hay que vigilar:** sin certificado de firma de código, Windows Defender o SmartScreen pueden marcar la actualización como "programa no reconocido" y bloquearla. Hoy ya pasa con el instalador manual y Luis lo acepta haciendo clic, pero con una actualización automática nadie está ahí para hacer ese clic. **Esto hay que probarlo en una PC real del cliente antes de confiar en el sistema** (es la tarea 10). Si Windows lo bloquea, las salidas son: añadir una exclusión en el antivirus de cada PC durante la última visita manual, o comprar el certificado más adelante.

**Lo que no se hará:** escribir una firma criptográfica propia. Hacer criptografía a mano es un error clásico; entre eso y HTTPS bien hecho, HTTPS es más seguro.

## Flujo de publicación (el que aprobó Luis, con su aviso previo)

1. **Luis programa el aviso.** Escribe en la base: "actualización en 15 minutos" más el mensaje.
2. **Las PCs muestran la cuenta atrás.** Un recuadro visible, revisado cada 3 minutos. La gente sigue trabajando.
3. **Al llegar a cero**, cada PC pide guardar y actualizar. Si hay una nota a medio llenar, lo advierte antes de cerrar.
4. **Luis publica** con `deploy.ps1`: una contraseña, y el script compila, empaqueta, sube y deja 3 versiones.
5. **Luis fija la política** en la base: versión recomendada y, solo si hace falta, versión mínima.
6. **Cada PC se actualiza** al abrir: descarga unos 3 MB, aplica y vuelve a abrir en la pantalla de carga.
7. **La primera PC que abre con la versión nueva aplica la migración** sola, como decidió Luis.

## Coordinación con las migraciones

Como la app migra al arrancar, el orden importa. Hay dos casos:

| Tipo de cambio | Ejemplo | Qué hacer |
|---|---|---|
| **Aditivo** (columna o tabla nueva, nula) | La foto de impresión de la spec 004 | Publicar normal. Una PC en la versión anterior sigue trabajando sin problema. Solo se sube la **versión recomendada** |
| **Destructivo** (borrar, renombrar, cambiar tipo) | Cambiar el tipo de una columna en uso | Programar el aviso, esperar a que las 3 PCs cierren, publicar y subir la **versión mínima**. Nadie trabaja con la versión vieja contra la base nueva |

La regla de oro, que ya está en el CLAUDE.md: **escribir migraciones aditivas siempre que se pueda.** Así las actualizaciones nunca necesitan ventana de mantenimiento.

**Por qué hay versión "mínima" y "recomendada" por separado:** si siempre se exigiera la mínima, un problema de descarga en una PC dejaría a esa persona sin poder trabajar. Con la recomendada, trabaja con aviso. La mínima se reserva para cuando de verdad no puede seguir.

## Volver atrás

Luis pidió poder volver atrás rápido, con las 2 versiones anteriores guardadas.

**No se hace bajando de versión**, porque pasar de la 1.1 a la 1.0 es un camino que las herramientas de actualización manejan mal. Se hace **publicando hacia adelante**: el script toma el código de la versión anterior, que ya está en el servidor, y lo publica como una versión nueva (de 1.1 se vuelve a la 1.0 publicándola como 1.2). Tarda un minuto y es el método que usa cualquier equipo de software.

**Limitación que hay que tener clarísima:** volver atrás recupera el **código**, no los datos. Si la versión 1.1 escribió montos mal en la base, volver a la 1.0 no los arregla; eso sale del respaldo. Por eso la tarea 1 es un respaldo y la prueba en testing es obligatoria antes de cada publicación.

## Dónde guardar la política: una tabla en la base

Tabla nueva `app_update_policy`, de una sola fila: versión mínima, versión recomendada, momento del aviso, mensaje del aviso.

Va en la base y no en el servidor de archivos porque todas las PCs ya hablan con la base, la consulta es barata (un `SELECT` cada 3 minutos) y así el aviso funciona aunque nginx esté caído.

## Opciones descartadas

- **GitHub Releases** en vez del VPS: gratis y sin mantenimiento, pero el repo es público y el binario del cliente quedaría a disposición de cualquiera.
- **Hacer el actualizador a mano:** mucho más código y más frágio, por lo explicado arriba.
- **ClickOnce:** no soporta bien apps autocontenidas y su experiencia de actualización es peor.
- **Actualizar sin avisar al usuario:** le haría perder una nota a medio hacer.
- **Dejar que cada PC quede en su versión:** es justo el problema que se quiere evitar.
- **Un pequeño servidor propio para recibir las subidas:** más código expuesto a internet. nginx solo entrega archivos y la subida va por SSH, que ya está.

## Riesgos

1. **Una actualización mala llega a las 3 PCs a la vez.** Se mitiga: probar en testing siempre, y la vuelta atrás.
2. **SmartScreen o el antivirus bloquean la actualización.** Es el riesgo más probable. Se prueba en PC real (tarea 10).
3. **Cambia la carpeta de instalación.** Velopack instala en la carpeta del usuario, sin pedir administrador en cada actualización. Eso obliga a la mudanza manual de la última visita: desinstalar la vieja, instalar la nueva y mover el `appsettings.json`.
4. **Espacio en las PCs.** Velopack conserva la versión anterior para poder revertir: unos 400 MB por PC.
5. **Si se sube la versión mínima y una PC no puede descargar, esa persona no trabaja.** Por eso la mínima se usa solo cuando es imprescindible.
6. **Ubuntu 20.04 ya no recibe parches gratis normales.** Se activa Ubuntu Pro, que es gratis hasta 5 máquinas y no obliga a reinstalar.

## Pruebas (a mano)

Todo se prueba primero contra testing y en una PC de prueba, nunca directo en las PCs del cliente. La prueba que manda es la tarea 10: una actualización real, de punta a punta, en una PC que no sea la de desarrollo.

## Rama

`feature/actualizacion-automatica` saliendo de `TESTING`. Se trabaja después de las tareas 1 a 7 de la spec 002 (porque el actualizador necesita que la conexión ya no esté en el código) y se despliega en la misma última visita manual.
