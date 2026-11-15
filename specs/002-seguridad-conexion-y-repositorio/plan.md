# Plan 002 - Seguridad de la conexión y del repositorio

Cómo cumplir [spec.md](spec.md). Todavía no se ha tocado código.

## Qué hay hoy (verificado en el código)

- [DbConnectionFactory.cs](../../src/NinOS.Infrastructure/data/DbConnectionFactory.cs) trae una cadena por defecto con IP, usuario y contraseña de **producción**. Si no hay variable ni `appsettings.json`, se conecta a producción.
- `src/NinOS.UI/appsettings.json` está en Git (uno por rama) y llega a las PCs porque el instalador ([NinOS_Setup.iss](../../installer/NinOS_Setup.iss)) copia todo `publish\*` a `{app}`. Esa carpeta tiene permiso de modificación para cualquier usuario de Windows.
- [AdminSecurity.cs](../../src/NinOS.UI/Common/AdminSecurity.cs) trae `DefaultPassword` en el código y la regenera si falta `admin_security.json`.
- El repo (público) contiene `backup_ninos_db_pre_v1.0.9.dump` y `scratch/*.json|sql`.

## Enfoque

1. **Rotar primero, limpiar después.** Como el repo es público, la contraseña vieja ya es pública. Cambiarla es lo único que realmente protege; limpiar el historial solo reduce la exposición futura.
2. **La conexión sale de afuera del código.** Orden de búsqueda: variable `NINOS_DB_CONNECTION` → `appsettings.json` local (fuera de Git). Sin ninguna de las dos, la app muestra un error claro y no arranca.
3. **Elegir testing o producción deja de depender de la rama.** Hoy se elige por el `appsettings.json` que trae cada rama. Pasa a elegirse con la variable de entorno en tu PC (testing) y con el archivo que pone el instalador en las del cliente (producción). Hay que actualizar luego el CLAUDE.md (secciones de ramas).
4. **Instalador sin secretos.** `publish\*` se copia con `Excludes: appsettings.json`. El archivo del cliente vive en `installer\private\appsettings.client.json` (ignorado por Git) y se instala con `onlyifdoesntexist`, para que una actualización no lo pise.
5. **Panel Admin.** Se elimina `DefaultPassword`. Si `admin_security.json` falta o está dañado, el panel queda bloqueado y se registra en el log. La clave se crea con un argumento oculto, `NinOS.UI.exe --set-admin-password`, que abre una ventana de contraseña, escribe el archivo y cierra. Solo lo usas tú.
6. **Historial de Git.** Se reescribe con `git filter-repo` quitando el `.dump`, `scratch/`, los `appsettings.json` y reemplazando el texto de las contraseñas. Se hace sobre un clon espejo y solo después de que confirmes, y se termina con force-push. Luego se pide a GitHub Support que purgue los commits en caché.

## Opciones descartadas

- **Hacer el repo privado:** descartado por ti (queda público).
- **Solo borrar los archivos en un commit nuevo:** el historial público los conserva.
- **BFG Repo-Cleaner:** funciona, pero `git filter-repo` es la herramienta recomendada por Git y reemplaza texto con más control.
- **Montar una API intermedia (la app deja de hablar directo con la base):** es lo más seguro, pero es reescribir la capa de datos de todo el sistema. Fuera de alcance; se deja anotado como mejora futura.
- **Poner credenciales por PC en el registro de Windows o cifradas con DPAPI:** un empleado con acceso a su propia PC igual podría usarlas; no aporta frente a un usuario de base con permisos mínimos y red restringida.

## Riesgo que se acepta

La app de escritorio necesita la contraseña en cada PC, así que un empleado técnico podría leerla. Se reduce con: usuario de base con permisos mínimos (RF-12), red restringida (RF-5) y contraseña distinta por ambiente. No se elimina del todo.

## Decisiones tomadas por Luis

- **Red (RF-5): puerto reforzado, no VPN.** Las PCs entran desde cualquier internet. El puerto queda alcanzable, con contraseña larga y aleatoria, TLS obligatorio (`SSL Mode=Require`), usuario sin superusuario y puerto distinto del 5432. Se acepta el riesgo de que siga expuesto; lo que lo protege es la contraseña.
- **Migraciones: las aplica la app al arrancar**, como hoy. El usuario de la app es dueño de las tablas de NinOS y puede crearlas y modificarlas, pero no es superusuario. Se acepta el riesgo de que esa contraseña permita cambiar la estructura.
- **Permisos de Windows:** la instalación sigue pidiendo administrador y la carpeta sigue abierta a todos los usuarios de la PC. Riesgo aceptado, no se toca.

## Pruebas (a mano)

Cada tarea trae sus pasos. En resumen: sin configuración la app no arranca; con la variable arranca contra testing; `git status` no muestra archivos con secretos; al buscar la contraseña vieja en todo el historial no hay resultados; desde fuera de la red permitida el puerto 5432 no responde.

## Rama

`feature/seguridad-conexion` saliendo de `TESTING`. No se toca `main` hasta que el cliente apruebe.
