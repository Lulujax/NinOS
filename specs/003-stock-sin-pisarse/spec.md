# 003 - Stock que no se pisa entre usuarios

## Qué y por qué

El stock se calcula en la PC y se guarda como número fijo. Con dos personas a la vez, una pisa el cambio de la otra. Peor: al editar un producto (aunque solo se cambie el precio), la pantalla manda la cantidad que tenía al abrirse, y el sistema la guarda y registra en el kardex un "ajuste" que nadie hizo. Además hay que poder corregir el stock por conteo manual sin perder lo que ocurrió mientras tanto.

## Requisitos

- **RF-1** CUANDO se registra una salida o entrada de stock (nota, nota de crédito, anulación, ajuste), EL SISTEMA aplicará el cambio sobre la cantidad **actual en la base**, no sobre la que la pantalla tenía cargada.
- **RF-2** SI dos personas mueven el mismo producto al mismo tiempo, ENTONCES EL SISTEMA aplicará ambos movimientos y el stock final será el resultado de los dos.
- **RF-3** CUANDO se edita un producto sin cambiar la cantidad, EL SISTEMA no modificará el stock ni creará movimiento de kardex.
- **RF-4** CUANDO se usa "agregar" o "quitar" cantidad, EL SISTEMA sumará o restará la cantidad indicada al stock actual de la base.
- **RF-5** CUANDO se hace un conteo manual (fijar la cantidad), EL SISTEMA pedirá el motivo, fijará la cantidad contada y registrará en el kardex la diferencia contra el stock actual de la base en ese momento.
- **RF-6** SI el stock de la pantalla ya no coincide con el de la base al guardar un conteo, ENTONCES EL SISTEMA avisará el valor actual y pedirá confirmar antes de fijar.
- **RF-7** EL SISTEMA permitirá stock negativo solo en notas de entrega (decisión del cliente); en las demás operaciones se mantiene el bloqueo por stock insuficiente.
- **RF-8** EL SISTEMA mantendrá siempre: stock del producto = suma de entradas − suma de salidas del kardex.

## Cómo sé que está listo

- Dos PCs despachan el mismo producto a la vez: el stock final baja la suma de ambas.
- Abrir un producto, vender 10 desde otra PC, cambiar solo el precio y guardar: el stock sigue igual y el kardex no muestra ajuste.
- Un conteo manual deja el kardex cuadrado con el stock.
- La consulta "stock vs kardex" no devuelve diferencias.

## Fuera de alcance

- Cambiar las reglas de códigos de producto o marcas.
- Reservar stock mientras se arma una nota.

## Decisiones tomadas

- Stock negativo permitido en notas de entrega, confirmado por el cliente.
