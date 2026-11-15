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
| Servidor de archivos | nginx ya instalado en el VPS. El puerto 80 responde desde internet (probado desde la PC de Luis). El 443 y el 8443 no respondieron: nginx aún no escucha ahí y no se sabe si el proveedor los bloquea |
| Dominio | Ninguno. Se usa la IP del VPS |

**Capacidad del VPS:** 1,9 GB de RAM (347 MB en uso), 1 vCore, 25,3 GB libres. nginx sirviendo archivos estáticos añade unos 20 MB de RAM y nada de CPU. Tres versiones completas ocupan ~540 MB. El servidor no es el cuello de botella.

## Arquitectura: tres piezas

**1. En la app (cliente).** Integración con Velopack para buscar, descargar y aplicar la actualización, más la lógica de aviso, versión mínima y comprobación de la huella del servidor.

**2. En el VPS (servidor).** nginx sirviendo una carpeta estática por HTTPS, en la IP del VPS, con un certificado propio (autofirmado) y en el puerto que el proveedor deje abierto. Solo entrega archivos; no recibe nada. La subida va por SSH, que ya existe.

**3. En la PC de Luis (publicación).** Un script `deploy.ps1` que pide la contraseña una vez y hace todo: compilar, empaquetar, subir, dejar solo 3 versiones y actualizar la política en la base.

## Herramienta: Velopack

Es el estándar actual para apps .NET de escritorio (sucesor de Squirrel) y resuelve de fábrica lo difícil: actualizaciones diferenciales, reemplazar un ejecutable que está corriendo, descargas cortadas, reinicio y vuelta atrás. Confirmaría la versión exacta y los nombres de su API al escribir la primera tarea de código, incluido cómo hacer que la descarga pase por un cliente HTTP propio que compruebe la huella del certificado.

**No hacerlo a mano.** Windows no permite reemplazar un `.exe` en uso, así que haría falta un segundo programa actualizador, manejar descargas parciales, verificar integridad y poder revertir. Es mucho más código y más frágil que usar la herramienta.

## Autenticidad de los paquetes: qué se puede y qué cuesta

Esto es lo más delicado del plan, porque el actualizador ejecuta código en las PCs del cliente.

| Capa | Protege de | Costo |
|---|---|---|
| **HTTPS con certificado propio (autofirmado)** | Que alguien en el medio de la conexión lea o cambie el paquete mientras viaja | Gratis (se genera en el VPS) |
| **Huella del certificado fijada en la app** | Que otro servidor, o un certificado falso aunque sea válido de una autoridad pública, se haga pasar por el servidor de Luis | Gratis, poco código |
| **Hashes de integridad** (los trae Velopack) | Archivos corruptos o alterados en el servidor | Gratis, incluido |
| **Certificado de firma de código de Windows** | Advertencias de SmartScreen y antivirus | ~200 USD al año |

**Plan: las tres primeras capas, que son gratis.** Son suficientes para el riesgo real.

**Por qué certificado propio y huella fijada:** sin dominio, ninguna autoridad pública puede respaldar el certificado. Entonces la app no confía en autoridades: confía en una huella concreta (la huella no es un secreto, solo identifica al servidor correcto) que ya lleva dentro. Un certificado falso, aunque lo respalde una autoridad, tiene otra huella y es rechazado. Esto es tan seguro como el esquema con dominio, y más estricto.

**Qué se pierde y cómo se compensa:** si el certificado cambia o vence, las PCs dejan de aceptar al servidor y no pueden actualizarse solas hasta una visita manual. Se mitiga con tres cosas:

1. Certificado con vigencia larga (por ejemplo 10 años), con la fecha de vencimiento anotada.
2. La app lleva **dos huellas**: la actual y una de respaldo, de un certificado ya generado y guardado fuera del servidor.
3. Para renovar sin visita: publicar una versión que acepte ambas huellas, esperar a que las 3 PCs la tengan, cambiar el certificado del servidor y, en la versión siguiente, retirar la huella vieja.

**Riesgo que queda sin cubrir y hay que vigilar:** sin certificado de firma de código, Windows Defender o SmartScreen pueden marcar la actualización como "programa no reconocido" y bloquearla. Hoy ya pasa con el instalador manual y Luis lo acepta haciendo clic, pero con una actualización automática nadie está ahí para hacer ese clic. **Esto hay que probarlo en una PC real parecida a las del cliente antes de confiar en el sistema** (es la tarea 14). Si Windows lo bloquea, las salidas son: añadir una exclusión en el antivirus de cada PC durante la última visita manual, o comprar el certificado más adelante.

**Lo que no se hará:** escribir una firma criptográfica propia a mano. Hacer criptografía a mano es un error clásico; entre eso y HTTPS con huella fijada, lo segundo es más seguro.

### Plan B (solo si el proveedor deja únicamente el puerto 80)

Se activa solo si la tarea 2 demuestra que no se puede abrir ningún puerto para HTTPS.

- Los paquetes se sirven por HTTP, sin cifrar.
- La seguridad pasa a depender de una **firma del paquete**: Luis firma cada versión con una llave privada que vive solo en su PC, protegida por contraseña (encaja con RF-1 y RF-22); la app lleva únicamente la llave pública y rechaza todo lo que no esté firmado por esa llave.
- Hay que usar una herramienta o formato de firma estándar y ya probado, nunca una creada a mano.
- Se pierde la confidencialidad en tránsito: cualquiera en el camino puede ver el paquete (no es un secreto, pero queda expuesto), aunque no puede modificarlo sin que la firma falle.
- La política de versión mínima, el aviso y la versión recomendada siguen yendo por la base de datos, con su propia conexión cifrada, así que no cambian.
- Cuesta más trabajo que el plan principal: generar y guardar la llave, firmar en `deploy.ps1` y verificar en la app.

[NECESITA ACLARACIÓN: si la tarea 2 confirma que solo el puerto 80 está disponible, ¿aceptas el plan B de arriba, o prefieres buscar otra salida (por ejemplo pedir al proveedor que abra un puerto)? No elijo yo; espero tu decisión después de la tarea 2.]

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

- **Subdominio gratuito (DuckDNS) con certificado de Let's Encrypt (certbot):** lo descartó Luis. Exige un dominio y que el proveedor deje abiertos los puertos 80 y 443, y el 443 aún no se sabe si se puede abrir. Con la IP del VPS y la huella fijada se consigue la misma protección sin depender de eso.
- **GitHub Releases** en vez del VPS: gratis y sin mantenimiento, pero el repo es público y el binario del cliente quedaría a disposición de cualquiera.
- **Hacer el actualizador a mano:** mucho más código y más frágil, por lo explicado arriba.
- **ClickOnce:** no soporta bien apps autocontenidas y su experiencia de actualización es peor.
- **Actualizar sin avisar al usuario:** le haría perder una nota a medio hacer.
- **Dejar que cada PC quede en su versión:** es justo el problema que se quiere evitar.
- **Un pequeño servidor propio para recibir las subidas:** más código expuesto a internet. nginx solo entrega archivos y la subida va por SSH, que ya está.
- **Confiar en cualquier certificado y no fijar la huella:** un certificado autofirmado sin huella fijada no demuestra nada; cualquiera en el medio podría presentar el suyo.

## Riesgos

1. **Una actualización mala llega a las 3 PCs a la vez.** Se mitiga: probar en testing siempre, y la vuelta atrás.
2. **SmartScreen o el antivirus bloquean la actualización.** Es el riesgo más probable. Se prueba en PC real (tarea 14).
3. **Cambia la carpeta de instalación.** Velopack instala en la carpeta del usuario, sin pedir administrador en cada actualización. Eso obliga a la mudanza manual de la última visita: desinstalar la vieja, instalar la nueva y mover el `appsettings.json`.
4. **Espacio en las PCs.** Velopack conserva la versión anterior para poder revertir: unos 400 MB por PC.
5. **Si se sube la versión mínima y una PC no puede descargar, esa persona no trabaja.** Por eso la mínima se usa solo cuando es imprescindible.
6. **Ubuntu 20.04 ya no recibe parches gratis normales.** Se activa Ubuntu Pro, que es gratis hasta 5 máquinas y no obliga a reinstalar.
7. **El proveedor no deja abrir ningún puerto para HTTPS.** Se averigua en la tarea 2; si pasa, Luis decide sobre el plan B.
8. **El certificado cambia o vence y las PCs no aceptan al servidor.** Se mitiga con vigencia larga, una huella de respaldo ya incluida en la app y el procedimiento de renovación descrito arriba.
9. **Sin dominio, si el proveedor cambia la IP del VPS**, dejan de conectar la base de datos y el actualizador. Se corrige cambiando la dirección en la configuración de cada PC; el riesgo ya existe hoy con la base.

## Pruebas (a mano)

Todo se prueba primero contra testing y en una PC de prueba, nunca directo en las PCs del cliente. La prueba que manda es la tarea 14: una actualización real, de punta a punta, en una PC que no sea la de desarrollo.

## Rama

`feature/actualizacion-automatica` saliendo de `TESTING`. Se trabaja después de las tareas 1 a 7 de la spec 002 (porque el actualizador necesita que la conexión ya no esté en el código) y se despliega en la misma última visita manual.
