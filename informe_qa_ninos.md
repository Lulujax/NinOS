# INFORME FINAL DE QA — Sistema NinOS

**Alcance:** análisis estático del repositorio (solo lectura; no se ejecutaron builds ni se modificó ningún archivo). Comandos usados: git status/diff/log, glob, grep y lectura de archivos.
**Raíz absoluta del proyecto:** C:\Users\Lulujax\Desktop\Programacion Trabajo\NinOS\NinOS (en adelante, RUTA_BASE). Todas las citas rchivo:Línea se leen relativas a RUTA_BASE; el anexo final lista las rutas absolutas.
**Snapshot de rama:** main = 608dc10 "feat: Add support for Devueltas status in Payments and Sales views" (2026-10-06), 0 commits adelante de origin/main, 278 archivos trackeados.

## 1. Resumen ejecutivo

NinOS es una app WPF (.NET 10) con EF Core 10 + PostgreSQL remoto y QuestPDF, dividida en 3 proyectos (Domain / Infrastructure / UI) sin proyecto de pruebas. El código muestra buen nivel de detalle en validaciones de negocio, transacciones en rutas de pago y documentación de accesibilidad, pero presenta **cuatro bloqueadores**: (1) credenciales de la BD en IP pública en texto plano dentro del binario y del instalador, más volcados completos de BD y CSV con PII de clientes versionados en git; (2) cambio del signo del asiento de Nota de Crédito sin migración de datos históricos, lo que contamina todas las sumas de pagos; (3) borrado de comisiones ya liquidadas con cascada FK; (4) DbInitializer que muta datos de producción en cada arranque, sin transacción y en segundo plano. Además no existe ninguna prueba automatizada y hay 87 sync void sin guardas de reentrada.

**Corrección de supuestos del briefing:** los "21 archivos sin commit" **no se reprodujeron** (el árbol estaba limpio al inicio de la auditoría); al cierre hay **8 modificados + 1 sin rastrear** por trabajo en vivo (is_pro_venta en zonas). El conteo medido de FontSize fijos es **411 en 35 XAML** (docs dice 408/32). ilter_selection_option.cs **sí está commiteado** (nace en 608dc10) y **está en uso**. sales_goal existía desde la migración 20260925023737; el bloque "META DEL MES" en PDF se agregó en a26f3d y 608dc10 solo ajustó su condición.

## 2. Calificación: **D** (no apto para liberación sin cerrar P0)

| Dimensión | Nota | Razón |
|---|---|---|
| Seguridad / configuración | **F** | Creds en repo + binario + instalador; PII y dumps de BD trackeados; password admin por defecto en código |
| Integridad de datos | **D** | Cambio de signo sin migración; cascada sobre comisiones pagadas; mutaciones de arranque sin transacción |
| Lógica de negocio | **C** | Reglas GEN/PRM/MAR/PVP bien implementadas y validadas, pero con 2 fuentes de verdad y umbrales incoherentes |
| Arquitectura / código | **C** | Separación por capas correcta; DI y transacciones bien usadas; acoplamiento UI↔servicio y duplicación |
| UI / DPI / accesibilidad | **C** | PerMonitorV2 + escalado propio resuelto; layout fijo y sin preferencia de usuario pendientes |
| PDF / reportes | **B–** | 8 generadores funcionales con SaveFileDialog; bloque de metas con casos borde y hilos de UI bloqueados |
| Pruebas / QA | **F** | 0 proyectos de prueba, 0 tests, sin CI |
| **Global** | **D** | Con P0 resueltos y smoke tests, subiría a **C**; el nivel real del código es de C |
