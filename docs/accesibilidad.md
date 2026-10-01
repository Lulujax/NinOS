# Accesibilidad y escalado de la interfaz

Este documento explica cómo maneja NinOS el tamaño de la interfaz, por qué el ajuste
automático está activo y con qué límite, y qué deuda técnica queda para que la app se
vea bien en cualquier máquina y para cualquier persona.

## Resumen en una línea

La app **respeta la escala que configuró el usuario en Windows** y nunca se la achica: si
alguien sube Windows al 200%, recibe su 200%. Lo que sí hace es **agrandar** lo que se ve
pequeño (el 100% y el 125% se dibujan al tamaño de diseño) para que la mayoría vea la app
como fue pensada.

## Las tres capas del problema

El problema de "en una máquina se ve diminuto y en otra bien" es en realidad tres
problemas distintos, y conviene no mezclarlos:

| Capa | Qué significa | Estado |
|---|---|---|
| **1. Conciencia de DPI** | Que Windows no pixele la ventana ni la dibuje con el factor equivocado | **Resuelto** |
| **1b. Compensación de escala** | Que la app se vea al tamaño de diseño aunque Windows esté bajo | **Resuelto** (parche, ver abajo) |
| **2. Layout flexible** | Que la ventana se adapte a cualquier tamaño sin cortarse | **Pendiente** |
| **3. Preferencia del usuario** | Que el usuario pueda agrandar la letra sin tocar Windows | **Pendiente** |

### Capa 1 — Conciencia de DPI (resuelto)

Windows y .NET saben dibujar las ventanas a la escala correcta si se lo declaran.
Está declarado en `src/NinOS.UI/app.manifest`:

- `dpiAware` = `true/pm`
- `dpiAwareness` = `PerMonitorV2, PerMonitor`

Ese manifiesto lo registra `src/NinOS.UI/NinOS.UI.csproj` con la etiqueta
`<ApplicationManifest>`, y es la **única** forma de que Windows respete el DPI: sin eso,
la app se ve borrosa y con el tamaño equivocado en cualquier pantalla que no sea la del
monitor principal.

Con esto ya se cumple lo que Windows ofrece de fábrica. No hay que hacer nada más en esta
capa, y no se debe tocar el registro ni la configuración de escala del sistema.

### Capa 2 — Layout flexible (pendiente, la deuda real)

El problema de fondo: **la interfaz fue diseñada y está escrita con medidas fijas en
píxeles**, no con medidas que se adapten.

Hoy hay **408 valores de `FontSize` fijos** repartidos en **32 archivos `.xaml`**, con
valores que van de 9 a 26, y muchos de ellos fraccionarios (10.5, 11.5, 12.5, 13.5, 14.5,
16.5). Además, las grillas tienen anchos de columna fijos en píxeles.

Consecuencias:

- Si Windows está al 200% o más, el texto crece (DIP escalado), pero los contenedores de las
  grillas y las cajas siguen medidos en el mismo número, así que el texto queda apretado,
  se corta, o aparecen barras de scroll horizontales que antes no había.
- En pantallas chicas (1366x768) la app queda apretada.
- El problema de "se ve chico al 100%" ya no aparece: lo cubre la compensación de escala
  (sección de más abajo). Lo que queda pendiente es que nada se corte.

**Reglas para ir corrigiéndolo (a partir de ahora):**

1. Toda vista **nueva** usa los recursos de tipografía globales (ver abajo), nunca un
   `FontSize` fijo.
2. Cuando se toque una vista vieja por otro motivo, de paso se convierten sus
    `FontSize` a recursos. Sin barrido masivo, sin sprint de refactor.
3. Los anchos de columna fijos en píxeles pasan a `*` o `Auto` cuando se toque esa grilla.
   Sin excepción.

### Capa 3 — Preferencia del usuario (pendiente)

Hoy **no hay** forma de que el usuario agrande la letra de la app sin cambiar la de todo
Windows. Cuando la capa 2 esté madura, la idea es agregar en Ajustes algo como:

```
Tamaño de interfaz:  [ Normal ▾ ]
                     Normal (100%)
                     Grande (115%)
                     Muy grande (130%)
```

que se guarde por usuario y, al iniciar sesión, se aplique a los recursos globales. Eso es
accesibilidad de verdad: **el usuario decide el tamaño, no la aplicación**, y no se toca
Windows.

Esta capa es la que reemplaza de forma definitiva al ajuste automático, que hoy está
activo como parche (ver más abajo). Cuando exista, el motor se apaga con `DESIGN_DPI = 0`.

## El ajuste automático: activo, y con un límite importante

`src/NinOS.UI/Common/UiScale.cs` compensa la escala de Windows para que la interfaz se vea
como está diseñada (150%). Si Windows está al 100%, la app se dibuja un 50% más grande por
dentro, con un `LayoutTransform` sobre la raíz de cada ventana.

**Está activo** (`DESIGN_DPI = 144`) porque la app fue diseñada para verse bien al 150%, y
la mayoría de los usuarios no cambia la escala de Windows: se quedan en 100% y la veían
diminuta. Era un problema de producto, no solo técnico.

### La regla que no se negocia: compensa hacia arriba, nunca hacia abajo

Con la compensaciónActivada sin más, la app quedaba con un **techo de 1.5x** para siempre.
La razón es que el producto `(144 / dpi) * (dpi / 96)` da **1.5 exacto** para cualquier
escala de Windows. Consecuencia concreta: una persona con baja visión que sube Windows al
200% porque necesita ver grande recibía 1.5x en vez de su 2.0x, y al 300% seguía
recibiendo 1.5x. Subir Windows no le servía de nada. Eso no es "se ve legible", es un muro
de accesibilidad para el único grupo que más lo necesita.

Por eso `compute_factor` nunca devuelve menos de 1.0. El factor solo agranda (para que el
100% y el 125% vean la app al tamaño de diseño) y nunca achica (para que el 200%, 250% o
300% vean lo que Windows les dio). Comportamiento resultante, medido con la fórmula real:

| Escala de Windows | Factor interno | Lo que ve el usuario | Antes (techo 1.5x) |
|---|---|---|---|
| 50% | 300% | 1.50x | 1.00x |
| 100% | 150% | **1.50x** | 1.50x |
| 125% | 120% | **1.50x** | 1.50x |
| 150% (diseño) | 100% | 1.50x | 1.50x |
| 200% | 100% | **2.00x** | 1.50x |
| 250% | 100% | **2.50x** | 1.50x |
| 300% | 100% | **3.00x** | 1.50x |

En negrita, los casos que cambian respecto de la versión con techo: los usuarios que
necesitaban letra grande la recuperan. Los que estaban en 100% y 125% siguen viendo la app
al tamaño de diseño, que es el objetivo original.

La única concesión que queda es en pantallas chicas (1366x768): a 100% da 1.17x en vez de
1.50x, porque 1.5x no entra. Eso agranda lo que el usuario pidió, no lo achica, así que no
hay conflicto con la regla.

### Limitaciones que siguen siendo reales

1. **Es una transformación global**, que es justo lo que las buenas prácticas de DPI piden
   evitar. Puede provocar artefactos y es difícil de predecir. Se acepta como parche
   mientras la capa 2 no esté resuelta.
2. **Apaga el síntoma, no la causa.** El problema de fondo (medidas fijas, 408 `FontSize`)
   sigue debajo. El plan de fondo es la capa 3: que el usuario elija el tamaño desde
   Ajustes, y en ese momento este motor se puede apagar con `DESIGN_DPI = 0` sin perder
   nada.
3. **Ajustar la altura de un `DataGrid` con `FontSize` fijo se desalinea.** Por eso el
   plan de la capa 3 va por recursos de texto, no por escalar el alto de las filas.

Lo que sí conserva `UiScale.cs`, y está funcionando, es el **diagnóstico**: registra en
el log, ventana por ventana, qué DPI y qué área útil detectó. Eso sirve para ver cómo se
comporta la app en cada máquina:

```
[INFO] Escala de la ventana "NinOS v1.0.1 - Sistema Administrativo": 100% (Windows al 150%, area util 1920x1008 px)
```

El log vive en `src/NinOS.UI/bin/Debug/net10.0-windows/ninos-ui.log`. La clave de estos
mensajes es la que dice `Windows al N%`: ese es el valor real que el usuario configuró y
que la app está respetando.

Hay dos formatos, y el segundo es el que sirve como evidencia en la prueba de dos
monitores:

**1. Cuándo se mide una ventana por primera vez** (uno por cada ventana que se abre):

```
[2026-09-30 18:38:22.433] [INFO] Escala de la ventana "NinOS v1.0.1 - Sistema Administrativo": 100% (Windows al 150%, area util 1920x1008 px)
```

**2. Cuando la ventana cambia de monitor y el DPI cambia** (con el antes y el después, para
poder verificarlo leyendo el log):

```
[2026-09-30 19:04:12.771] [INFO] Cambio de DPI en la ventana "NinOS v1.0.1 - Sistema Administrativo": Windows al 100% -> 150% (area util 1920x1040 -> 2560x1408 px)
```

El ejemplo 2 es **ilustrativo, de una máquina con dos monitores**: saldría así al arrastrar
la ventana del monitor al 100% al monitor al 150%. No se ha podido generar en una máquina
real todavía, porque la prueba con dos monitores sigue pendiente. Lo que hay que ver en esa
prueba es exactamente eso: la línea con `100% -> 150%` (o al revés) y que la ventana se vea
nítida y sin cortes en los dos monitores.

## Recursos globales de tipografía

Ya están definidos en `src/NinOS.UI/App.xaml` para que las vistas nuevas los usen desde el
primer día:

```xml
<sys:Double x:Key="AppFontSizeSmall">12</sys:Double>
<sys:Double x:Key="AppFontSize">14</sys:Double>
<sys:Double x:Key="AppFontSizeLarge">16</sys:Double>
<sys:Double x:Key="AppFontSizeTitle">20</sys:Double>
```

Se usan así:

```xml
<TextBlock FontSize="{DynamicResource AppFontSize}" />
```

### Decisión de producto: por qué el texto base es 14 (y no 11)

Esta es una decisión de producto, no técnica. Queda escrita acá para no volver a
discutirla.

El problema original era: "en una máquina con Windows al 100% todo se ve diminuto; en la
otra, al 150%, se ve bien". Hay dos maneras de resolverlo y son incompatibles:

| Objetivo | `AppFontSize` | Efecto |
|---|---|---|
| Mantener exactamente el tamaño que hay hoy | 11 o 12,5 | No cambia nada visualmente |
| Agrandar un 27% sobre el diseño actual | **14** (elegido) | Todos los usuarios ven texto más grande, en todas las escalas |
| Punto medio | 12 | Un 9% más grande que hoy |

**Se eligió 14**, con el objetivo declarado de que la app se vea cómoda sin depender de que
el usuario haya tocado la escala de Windows. La app no le decide el tamaño a nadie: si
alguien necesita más grande, sube Windows, y con el ajuste activo lo obtiene completo (ver
la tabla de la sección anterior, que no tiene techo).

> **Atención: el 14 se decidió bajo una premisa que después cambió, y hay que revisarlo.**
>
> El 14 se eligió cuando el ajuste automático estaba apagado, bajo el supuesto de que el
> texto de 11 se vería como 16,5 px físicos al 100%. Con la compensación activada eso ya no
> es necesario: la compensación sola ya lleva el 100% al mismo aspecto que el 150%, porque
> `11 × 1,5 = 16,5` en ambos casos.
>
> O sea que hoy, con la compensación activa, un `FontSize="11"` fijo ya se ve igual en
> Windows al 100% y al 150%. Poner `AppFontSize = 14` haría que el texto se viera a
> `14 × 1,5 = 21` px físicos, es decir **un 27% más grande que el diseño actual**, para
> todos los usuarios, en todas las escalas.
>
> Esto es una decisión de producto abierta: si el objetivo es "que se vea como el diseño
> actual", el valor a migrar debería ser el tamaño que ya se usa (11 o 12,5), no 14. Si el
> objetivo es "que se vea más grande de lo que se veía", entonces 14 es correcto y hay que
> aceptarlo como un cambio visual deliberado, revisando vista por vista que nada se corte.
>
> Lo que **no** conviene es dejar 14 sin revisar, porque la premisa con la que se decidió
> (que no había compensación) ya no es cierta. Nadie migró ninguna vista todavía, así que
> todavía no se rompió nada: se puede corregir antes de que empiece.

### Progreso de la migración

**0 / 408 `FontSize` migrados.**

Los recursos existen, pero **todavía no los usa ninguna vista**. Que el recurso exista no
quiere decir que la migración esté hecha: esto es trabajo en curso, y este es el
indicador de cuánto falta.

- Cuando se migre una vista, actualizar este número en el mismo commit.
- Al tocar una vista vieja por otro motivo, es el momento de migrar sus `FontSize` y los
  anchos de columna fijos de esa grilla.
- No hacer un barrido masivo: revisar vista por vista que no aparezcan cortes.

Son `DynamicResource` (no `StaticResource`) a propósito: cuando exista la preferencia de
la capa 3, cambiar el valor del recurso alcanza para que toda la app cambie de tamaño en
vivo, sin reiniciar.

**Ojo al migrar vistas viejas:** el tamaño más usado hoy en la app es `11` (94 veces) y
`12.5` (61 veces). Como el ajuste automático ya agranda lo que se ve chico, migrar a `14`
ya no compensa el 100%: lo pasa del diseño actual. Ver la advertencia de la sección
anterior antes de migrar la primera vista.

## Qué falta probar

Estos puntos siguen abiertos y necesitan una máquina real, no se pueden cerrar desde una
sola computadora:

- [ ] **Escenario 1 — Windows al 100%, un monitor.** Abrir la app en la máquina de un
      compañero con la escala al 100% y responder la pregunta más simple: *¿se ve bien o
      se ve chico?* Esa frase sola ya orienta la decisión. Con el ajuste activo, el log
      debería decir `150%` como factor interno. **Este es el escenario que más importa**,
      porque es el caso de la mayoría de los usuarios.
- [ ] **Escenario 2 — Dos monitores con distinta escala** (por ejemplo 100% + 150%).
      Arrastrar la ventana de uno al otro y confirmar que no se pixela ni se corta nada.
      El log debería mostrar una línea `Cambio de DPI ...` por cada salto de monitor.
- [ ] **Escenario 3 — Pantallas chicas** (1366x768) con Windows al 100% y 150%.
- [ ] **Escenario 4 — Escala alta, para confirmar que no hay techo.** Poner Windows al
      200% (o más) y confirmar que el texto se ve **más grande** que en 150%, no igual ni
      más chico. El log debería decir `100%` como factor interno, que es lo correcto:
      sin compensación, porque el usuario ya pidió más.

Para los cuatro, el log es la evidencia: el factor interno nunca debe bajar de 100%, y si
baja, hay un error en `compute_factor`.

## Conclusión para el equipo

- La app **respeta la escala de Windows y nunca la achica**. Si subís Windows al 200%,
  ves el 200%.
- La app **agranda lo que se ve chico**: al 100% y al 125% se dibuja al tamaño de diseño,
  porque la app fue diseñada al 150% y la mayoría no cambia la escala de Windows.
- El ajuste automático está activo con `DESIGN_DPI = 144` **a propósito**, no por falta de
  tiempo. Se puede desactivar con `DESIGN_DPI = 0` cuando exista la capa 3, sin perder nada.
- La deuda real son los 408 `FontSize` fijos y las columnas de ancho fijo: se corrige de a
  poco, con reglas simples, en cada vista que se vaya tocando.
- La capa 3 (preferencia de tamaño en Ajustes) es el paso siguiente de verdad, y depende
  de que la capa 2 esté suficientemente avanzada.
- El usuario decide el tamaño. La app tiene que asegurarse de que no se rompa con ninguna
  decisión, y no pisársela por arriba.
