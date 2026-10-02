; Script de Inno Setup para NinOS - Sistema Administrativo
; Versión: 1.0.3

#define MyAppName "NinOS"
#define MyAppFullName "NinOS - Sistema Administrativo"
#define MyAppVersion "1.0.3"
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
PrivilegesRequiredOverridesAllowed=dialog
PrivilegesRequired=lowest
OutputDir=C:\Users\Lulujax\Desktop
OutputBaseFilename=NinOS_Setup_v1.0.3
SetupIconFile=C:\Users\Lulujax\Desktop\Programacion Trabajo\NinOS\NinOS\src\NinOS.UI\Assets\ninOS_logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "C:\Users\Lulujax\Desktop\Programacion Trabajo\NinOS\NinOS\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppFullName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppFullName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppFullName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
