# 010 - Actualización automática

## Qué y por qué

Hoy cada corrección obliga a Luis a ir PC por PC con un instalador de 179 MB. La dueña de la empresa pidió que eso termine: quiere que los arreglos lleguen solos, sin perder tiempo coordinando una visita por cada bug.

La app pesa 179 MB, pero el código propio son solo 2,9 MB (`NinOS.UI.dll`, `NinOS.Infrastructure.dll`, `NinOS.Domain.dll`): el resto es el runtime de .NET, que no cambia entre versiones. Por eso una actualización puede ser de unos 3 MB y bajar en segundos.

El modelo es el de los juegos en línea (WoW, Albion): se anuncia con antelación, cuenta atrás, el sistema se reinicia y vuelve actualizado.

**Esto es un canal para ejecutar código en las PCs del cliente.** Si alguien pudiera colocar un paquete falso, instalaría cualquier programa en todas las PCs a la vez. Por eso la conexión cifrada y la comprobación de identidad del servidor son parte del requisito, no un extra.

**Punto de partida real:** el VPS no tiene dominio ni subdominio; se usa por su dirección IP. nginx ya está instalado y el puerto 80 responde desde internet. Todavía no se sabe si el proveedor deja abrir otro puerto para la conexión cifrada.

## Requisitos

### Publicar

- **RF-1** CUANDO Luis publica una versión, EL SISTEMA le pedirá una contraseña antes de subir nada.
- **RF-2** EL SISTEMA garantizará que una PC solo acepte paquetes que vengan de verdad del servidor de Luis, y no de alguien que se haya metido en el medio de la conexión. El mecanismo concreto se elige en el plan (ver "Autenticidad de los paquetes").
- **RF-3** EL SISTEMA publicará los paquetes como **diferenciales** cuando sea posible, de modo que una corrección de código pese unos pocos MB y no 179 MB.
- **RF-4** EL SISTEMA conservará en el servidor la versión actual **y las 2 anteriores**, y borrará las más viejas.
- **RF-5** SI la publicación falla a medias (se corta la subida), ENTONCES EL SISTEMA no dejará la versión nueva marcada como disponible, y las PCs seguirán con la anterior.

### Descargar y aplicar

- **RF-6** EL SISTEMA servirá los paquetes por una conexión cifrada (**HTTPS**), usando la dirección IP del VPS, sin depender de un dominio.
- **RF-7** ANTES de aplicar una actualización, EL SISTEMA verificará que el paquete esté completo y sin alterar; SI la verificación falla, ENTONCES no lo aplicará, lo registrará en el log y seguirá con la versión actual.
- **RF-7b** EL SISTEMA aceptará actualizaciones **solo** del servidor de Luis, reconociéndolo por la huella de su certificado que la app ya trae, y rechazará cualquier otro origen aunque responda en la misma dirección o presente un certificado válido de una autoridad pública.
- **RF-7c** SI el certificado del servidor debe cambiar, ENTONCES la app aceptará la huella nueva siempre que ya la traiga como huella de respaldo; SI no la trae, ENTONCES no actualizará, mostrará el motivo y seguirá trabajando con su versión actual.
- **RF-8** CUANDO se abre la app, EL SISTEMA comprobará si hay una versión nueva y, si la hay, la descargará y la aplicará mostrando el avance en la pantalla de carga que ya existe.
- **RF-9** SI la descarga o la instalación falla, ENTONCES EL SISTEMA seguirá funcionando con la versión actual y mostrará el motivo, sin quedar inservible.
- **RF-10** CUANDO una actualización termina, EL SISTEMA se reiniciará solo y abrirá en la versión nueva.

### Avisar mientras se trabaja

- **RF-11** CUANDO Luis programa una actualización, EL SISTEMA mostrará en todas las PCs un aviso con cuenta atrás: "en X minutos el sistema se reinicia para actualizar".
- **RF-12** EL SISTEMA revisará ese aviso cada pocos minutos mientras la app está abierta, sin que el usuario haga nada.
- **RF-13** CUANDO la cuenta atrás llega a cero, EL SISTEMA pedirá al usuario guardar su trabajo y actualizar.
- **RF-14** SI el usuario tiene una nota o un formulario a medio llenar sin guardar, ENTONCES EL SISTEMA se lo advertirá antes de cerrar y no perderá ese trabajo sin avisar.
- **RF-15** EL SISTEMA permitirá a Luis cancelar un aviso programado.

### Versión mínima

- **RF-16** EL SISTEMA guardará en la base la **versión mínima permitida** y, por separado, la **versión recomendada**.
- **RF-17** SI la versión de una PC es menor que la **mínima**, ENTONCES EL SISTEMA no la dejará trabajar y la obligará a actualizar.
- **RF-18** SI la versión de una PC es menor que la **recomendada** pero no que la mínima, ENTONCES EL SISTEMA la dejará trabajar y mostrará un aviso.
- **RF-19** EL SISTEMA solo exigirá versión mínima cuando el cambio lo requiera (una migración que la versión anterior no soporte), para que un problema de descarga no deje a la empresa sin poder trabajar.

### Volver atrás

- **RF-20** EL SISTEMA permitirá a Luis volver al código de una de las 2 versiones anteriores sin tener que recompilar.
- **RF-21** CUANDO se vuelve atrás, EL SISTEMA dejará claro que se recupera el **código**, no los datos: lo que una versión mala haya escrito en la base se arregla con el respaldo.

### Seguridad

- **RF-22** EL SISTEMA no llevará dentro de la app ninguna contraseña ni llave que permita publicar una actualización; la app solo verifica identidad y firmas, no las crea.
- **RF-23** EL SISTEMA no expondrá en el servidor ninguna forma de subir paquetes sin contraseña: nginx solo entrega archivos, no los recibe.

## Cómo sé que está listo

- Luis publica una corrección de código y el paquete que se sube pesa unos pocos MB, no 179 MB.
- Publicar sin la contraseña correcta no sube nada.
- En el servidor quedan exactamente 3 versiones.
- Una PC abierta trabajando muestra la cuenta atrás y, al llegar a cero, pide guardar y actualizar.
- Una PC con una nota a medio llenar avisa antes de cerrarse.
- Una PC desactualizada por debajo de la mínima no deja trabajar; por debajo de la recomendada sí, con aviso.
- Si se altera a mano un archivo del paquete en el servidor, la PC rechaza la actualización y lo deja en el log.
- Si se apaga nginx, la app sigue abriendo y trabajando con su versión actual.
- Un servidor de prueba con otro certificado, aunque sea válido de una autoridad pública, es rechazado por la PC y queda en el log.
- Con el certificado del servidor cambiado a uno cuya huella la app ya trae de respaldo, la PC sigue actualizando.
- Luis vuelve al código anterior y las PCs quedan en esa versión.
- `dotnet build NinOS.slnx` con 0 errores y 0 advertencias.

## Fuera de alcance

- Actualizar PostgreSQL o el sistema operativo del VPS por esta vía.
- Actualizaciones silenciosas sin avisar al usuario.
- Que distintas PCs trabajen a propósito con versiones distintas.
- Desinstalar la app a distancia.
- Conseguir un dominio o un certificado emitido por una autoridad pública: se usa la IP del VPS con un certificado propio.

## Decisiones tomadas

- Lo pidió la dueña de la empresa; es un requisito del negocio, no una mejora opcional.
- Publicar **siempre** pide contraseña a Luis.
- Se guardan la versión actual y las **2 anteriores**.
- Se anuncia con antelación y cuenta atrás, estilo WoW o Albion.
- Se acepta que una actualización mala llegue a las 3 PCs a la vez; se mitiga probando en testing primero y con la vuelta atrás.
- Se hará **una última instalación manual** PC por PC para dejar el actualizador puesto, junto con la ronda de la spec 002.
- El VPS seguirá en Ubuntu 20.04 (es lo que ofrece el proveedor).
- **Sin dominio:** el servidor de actualizaciones se usa por la IP del VPS. Se descarta el subdominio gratuito con certbot; la identidad del servidor se comprueba con la huella de su certificado propio.
- nginx ya está instalado en el VPS y el puerto 80 responde desde internet (probado desde la PC de Luis). El 443 y el 8443 no respondieron todavía, pero nginx aún no escucha en ellos, así que no se sabe si el proveedor los bloquea.

## Sin presupuesto: cómo se resuelve la conexión cifrada gratis

No hace falta comprar nada:

- **Dominio:** ninguno. Se usa la IP del VPS.
- **Certificado:** uno propio (autofirmado) generado en el VPS, gratis y con vigencia larga. No lo respalda ninguna autoridad pública; por eso la app no confía en autoridades, sino en la huella exacta de este certificado.
- **Autenticidad de los paquetes:** conexión cifrada más huella del certificado fijada en la app, más la verificación de integridad que ya trae la herramienta de actualización. Lo que **sí cuesta dinero** es un certificado de firma de código de Windows (unos 200 USD al año o más): no es obligatorio, pero sin él Windows puede mostrar advertencias de "programa no reconocido". Esto se detalla en el plan, con el riesgo asociado.
- **Parches del sistema operativo:** Ubuntu 20.04 ya está fuera del soporte gratuito normal, pero Ubuntu Pro da parches de seguridad **gratis hasta 5 máquinas** y se activa sobre el mismo Ubuntu 20.04, sin reinstalar nada.

Resuelto el 2026-11-15 (tarea 2): el puerto 443 responde desde internet por HTTPS, así que se usa el plan principal y el plan B queda descartado.
