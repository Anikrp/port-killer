; Inno Setup 6 — PortKiller Windows installer (x64)
; Payload: self-contained publish at publish/win-x64-installer (bundles .NET; no runtime on target PC)
; Build (from repo root): dotnet publish platforms/windows/PortKiller/PortKiller.csproj -c Release -r win-x64 --self-contained -o publish/win-x64-installer
; Before compile: generate setup.ico + wizard PNGs (see platforms/windows/scripts/build-installer.ps1)
; Compile: ISCC.exe PortKiller.iss /DMyAppVersion=3.3.1

#ifndef MyAppVersion
  #define MyAppVersion "4.1.0"
#endif

#define MyAppName "PortKiller"
#define MyAppPublisher "arpcodes.com"
#define MyAppExeName "PortKiller.exe"
#define MyAppSource "..\..\..\publish\win-x64-installer"

[Setup]
AppId={{8E4F1B2A-9D3C-4E5F-A1B2-C3D4E5F67890}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://arpcodes.com/
AppSupportURL=https://arpcodes.com/
AppUpdatesURL=https://arpcodes.com/
AppCopyright=Copyright (C) arpcodes.com
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\..\..\publish\installer
OutputBaseFilename=PortKiller-{#MyAppVersion}-Setup-x64
; Same folder as this .iss (copied by build-installer.ps1)
SetupIconFile=setup.ico
WizardImageFile=wizard-large.png
WizardSmallImageFile=wizard-small.png
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoDescription={#MyAppName} Installer
VersionInfoCopyright=Copyright (C) arpcodes.com

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppSource}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; No post-install launch: PortKiller.exe requires admin (manifest). Launching from the wizard hits Win32 error 740.
; User starts the app from the Start Menu / desktop shortcut (Windows will prompt UAC as needed).
