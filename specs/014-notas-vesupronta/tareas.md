# Tareas 014 - Notas de entrega Vesupronta

Cada tarea está pendiente de la aclaración que indique. Una tarea marcada como bloqueada no empieza hasta que Luis conteste.

- [ ] **T1. Cálculo de la tabla de liquidación.** Cubre: RF-5, RF-6, RF-7, RF-9.
  - Función pura en el dominio que recibe el total general sin descuento y devuelve las seis cifras redondeadas.
  - Pruebas: caso feliz (344,48 da 86,12; 258,36; 38,75; 219,61; 124,87), caso límite (total 0 y total con centavos de redondeo), caso de error (total negativo).
  - Hecha y probada: [ ]

- [ ] **T2. Hoja ADMINISTRACIÓN.** Cubre: RF-1, RF-4, RF-8, RF-13.
  - Agregar en el generador de PDF la tercera hoja para notas de Vesupronta, con la tabla de T1 al final.
  - Nota sin renglones: no genera la hoja y avisa.
  - Hecha y probada: [ ]

- [ ] **T3. Hojas ORIGINAL y COPIA sin datos de pago.** Cubre: RF-1, RF-3.
  - Para notas de Vesupronta, quitar el bloque de datos de pago de ORIGINAL y COPIA.
  - Hecha y probada: [ ]

- [ ] **T4. Tres tipos de nota de Vesupronta.** Cubre: RF-2, RF-12.
  - Confirmar que General, Volumen y Promoción de Vesupronta usan el mismo formato y no cambian el correlativo.
  - Hecha y probada: [ ]

- [ ] **T5. Título de la nota.** Cubre: RF-11.
  - Poner «V.P» al inicio y al final del título. Falta confirmar qué es «<título de la nota>» (ver RF-11).
  - Hecha y probada: [ ]

- [ ] **T6. Límite de 20 productos.** Cubre: RF-15, RF-16.
  - Validar el máximo de 20 renglones al guardar una nota de Vesupronta. Pruebas: 20 aceptado, 21 rechazado, 0 rechazado.
  - Comprobar que las tres hojas con 20 productos caben en una página carta.
  - Hecha y probada: [ ]

- [ ] **T7. Excel de Vesupronta con tres hojas.** Cubre: RF-14.
  - Agregar la hoja ADMINISTRACIÓN al Excel de notas de Vesupronta con la misma tabla del PDF.
  - Hecha y probada: [ ]

- [ ] **T8. Tres notas de prueba en octubre.** Cubre: RF-17.
  - Crear en la base de testing tres notas de Vesupronta con fecha de octubre de 2026. Una con 20 productos.
  - Hecha y probada: [ ]
