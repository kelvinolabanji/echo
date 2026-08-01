; echo-setup.iss
; Build with Inno Setup Compiler (iscc.exe echo-setup.iss) or the IDE.
;
; FRONTEND-ONLY INSTALLER. The backend (PyInstaller-frozen echo-backend.exe +
; CLIP weights, ~600MB-1GB+) is NOT bundled here — it's downloaded by EchoApp
; on first run via BackendDownloader.cs, from wherever you host the zip
; (e.g. a GitHub Release asset). This keeps the installer itself small.
;
; Expects this to already exist before compiling:
;   frontend\EchoApp\bin\Release\net8.0-windows\win-x64\publish\  (from dotnet publish, self-contained, single-file)
;
; If you're near the 100MB line, also try:
;   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true

#define MyAppName "Echo"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Nelson"
#define MyAppExeName "EchoApp.exe"

[Setup]
AppId={{A3F0B6E2-6D1E-4C9A-9B7F-000000000001}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Echo runs as a background/tray app, so no desktop shortcut icon needed by default
OutputDir=installer_output
OutputBaseFilename=EchoSetup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; .NET self-contained single-file publish output — this is the entire installer payload now
Source: "frontend\EchoApp\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; {app}\backend is intentionally left empty here — EchoApp creates and populates
; it on first run by downloading and extracting the backend package.

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

[Registry]
; Launch on Windows startup. Program.cs (StartupManager.SetStartup) re-asserts
; this same key on every launch anyway, so no install-time checkbox is needed —
; this just gives a clean first run before EchoApp.exe has run once.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; \
    Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName} now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Make sure the backend process isn't left running / locking files during uninstall
Filename: "{cmd}"; Parameters: "/C taskkill /F /IM echo-backend.exe /T"; Flags: runhidden; RunOnceId: "KillBackend"
Filename: "{cmd}"; Parameters: "/C taskkill /F /IM {#MyAppExeName} /T"; Flags: runhidden; RunOnceId: "KillFrontend"
