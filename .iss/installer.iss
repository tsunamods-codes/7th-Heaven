#define MyAppName "7th Heaven"
#define MyAppPublisher "Tsunamods"
#define MyAppURL "https://github.com/tsunamods-codes/7th-Heaven"

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0.0"
#endif

#ifndef MyAppPath
  #define MyAppPath "..\SeventhHeavenUI"
#endif

#ifndef MyAppRelease
  #define MyAppRelease "Debug"
#endif

#ifndef MyAppTargetFramework
  #define MyAppTargetFramework "net7.0-windows"
#endif

#define public Dependency_NoExampleSetup
#include "CodeDependencies.iss"

[Setup]
AppId={{E66AE545-C285-4B8C-8BD0-67282E160BF4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppCopyright="Microsoft Public License (MS-PL)"
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={userpf}\{#MyAppName}
PrivilegesRequired=lowest
DefaultGroupName={#MyAppName}
Compression=lzma2/ultra
InternalCompressLevel=ultra
SolidCompression=true
VersionInfoCompany={#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
SetupIconFile="{#MyAppPath}\7H.ico"
UninstallDisplayIcon="{app}\uninstall.ico"
UninstallDisplayName={#MyAppName}
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "{#MyAppPath}\bin\{#MyAppRelease}\{#MyAppTargetFramework}\*"; DestDir: "{app}"; Flags: recursesubdirs
Source: "{#MyAppPath}\7H.ico"; DestDir: "{app}"; DestName: "uninstall.ico"
Source: "netcorecheck.exe"; Flags: dontcopy noencryption
Source: "netcorecheck_x64.exe"; Flags: dontcopy noencryption

[Icons]
Name: "{group}\7th Heaven"; Filename: "{app}\7th Heaven.exe";
Name: "{group}\TurBoLog"; Filename: "{app}\TurBoLog.exe";
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; IconFilename: "{app}\uninstall.ico"

[Run]
Filename: "{app}\7th Heaven.exe"; Flags: nowait postinstall runascurrentuser skipifsilent; Description: "Launch {#MyAppName}"

[InstallDelete]
Name: "{app}\7thWorkshop\catalog.xml"; Type: files

[UninstallDelete]
Name: "{app}\7thWorkshop"; Type: filesandordirs

[Code]
function InitializeSetup: Boolean;
begin
  Dependency_ForceX86 := True;
  Dependency_AddVC2015To2022;
  Dependency_AddDotNet70Desktop;

  Dependency_ForceX86 := False;
  Dependency_AddVC2015To2022;
  Dependency_AddDotNet70Desktop;

  Result := True;
end;
