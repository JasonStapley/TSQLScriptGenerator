; Inno Setup script for the Windows desktop app.
; The setup.exe checked in at the repository root is the old .NET Framework 4.5 installer.
; Rebuild this script only after publishing the WinForms app on Windows:
;
;   dotnet publish src/TSQLScriptGenerator.WinForms/TSQLScriptGenerator.WinForms.csproj -c Release -r win-x64 --self-contained false
;
; That publish folder contains TSQLScriptGenerator.exe plus Microsoft.Data.SqlClient and its native assets.
; The .NET 8 desktop runtime must be installed on the target machine when the app is framework-dependent.

#define MyAppName "SQL Scripter"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "Jason Stapley"
#define MyAppExeName "TSQLScriptGenerator.exe"

[Setup]
AppId={{A7F80B47-7E48-47C7-BEFC-45AD95AABC57}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=setup
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\src\TSQLScriptGenerator.WinForms\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
