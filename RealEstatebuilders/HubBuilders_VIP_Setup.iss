; HubBuilders VIP Setup — packaging only (app code/UI untouched)
#define MyAppName "HubBuilders VIP"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Hub Builders"
#define MyAppExeName "HubBuilders.exe"

[Setup]
AppId={{A8C3E5F1-9B2D-4E71-8F40-HUBBUILDERSVIP01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\HubBuilders VIP
DefaultGroupName=HubBuilders VIP
DisableProgramGroupPage=yes
OutputDir=dist\installer
OutputBaseFilename=HubBuilders_VIP_Setup
SetupIconFile=ChatGPT Image Jul 30, 2026, 03_09_36 PM.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion=1.0.0.0
VersionInfoProductName=HubBuilders VIP
VersionInfoCompany=Hub Builders
VersionInfoDescription=HubBuilders VIP Complete Installer (Reports + VIP modules)

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &Desktop shortcut"; GroupDescription: "Additional icons:"; Flags: checkedonce

[Files]
; Full Release runtime: EXE, DLLs, Reports (rdlc+png), SQLite natives, locales, seed DB
Source: "dist\HubBuilders_VIP\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch HubBuilders VIP"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;
