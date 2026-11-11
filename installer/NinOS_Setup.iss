; Script de Inno Setup para NinOS - Sistema Administrativo
; Versión: 1.0.10
;
; Estándar de empaquetado y permisos:
;   1. Sin elevación diaria (No RUNASADMIN): La aplicación no requiere ser ejecutada como
;      administrador para su uso rutinario. Se eliminó la clave de compatibilidad RUNASADMIN.
;   2. Permisos en carpeta destino ([Dirs] users-modify): Se otorgan permisos de modificación
;      a usuarios estándar sobre {app} para que NinOS pueda escribir sus logs locales
;      (ninos-ui.log, ninos-ui-error.log) y configuraciones sin alertas UAC.
;   3. Compresión no sólida (lzma2/normal, SolidCompression=no): Permite a Microsoft Defender
;      y SmartScreen inspeccionar transparentemente los binarios individuales, reduciendo
;      falsos positivos heurísticos y sospechas de malware.
;
; Actualización sobre una instalación existente:
;   El AppId es fijo, así que al ejecutar este setup sobre una versión ya instalada
;   se reemplazan los archivos en la misma carpeta, se conservan los accesos
;   directos y el desinstalador, y no se toca ningún dato del usuario.
;   [Files] usa "ignoreversion", por lo que los binarios se sobrescriben aunque
;   la versión sea la misma. Limpia automáticamente cualquier rastro previo de RUNASADMIN.

#define MyAppName "NinOS"
#define MyAppFullName "NinOS - Sistema Administrativo"
#define MyAppVersion "1.0.10"
#define MyAppPublisher "NinOS"
#define MyAppExeName "NinOS.UI.exe"

[Setup]
AppId={{A87D0E52-95DF-4D04-8E4B-2B2E9291079D}}
AppName={#MyAppFullName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
PrivilegesRequiredOverridesAllowed=commandline
PrivilegesRequired=admin
OutputDir=C:\Users\Lulujax\Desktop
OutputBaseFilename=NinOS_Setup_v1.0.10
SetupIconFile=C:\Users\Lulujax\Desktop\Programacion Trabajo\NinOS\NinOS\src\NinOS.UI\Assets\ninOS_logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/normal
SolidCompression=no
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
; Si la app esta abierta en la PC del cliente, el setup la cierra (Restart Manager),
; instala y NO la vuelve a abrir solo: el arranque queda en la casilla final [Run],
; para que no se abran dos instancias.
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Dirs]
; Concede permisos de lectura/escritura a usuarios locales en la carpeta de instalación
; para que la app guarde sus logs y configs locales sin requerir permisos de administrador.
Name: "{app}"; Permissions: users-modify

[Files]
Source: "C:\Users\Lulujax\Desktop\Programacion Trabajo\NinOS\NinOS\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
; Limpieza de versiones previas que forzaban la ejecución como administrador:
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"; ValueType: none; ValueName: "{app}\{#MyAppExeName}"; Flags: deletevalue uninsdeletevalue

[Icons]
Name: "{group}\{#MyAppFullName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppFullName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppFullName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser
