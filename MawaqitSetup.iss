#define MyAppName "Mawaqit"
#define MyAppVersion "2.2.6"
#define MyAppExeName "Mawaqit.exe"
#define MyAppId "8B3E42A1-94F5-4B82-B2A8-E4C2E9D68A11"

#ifndef Standalone
  #define Standalone "0"
#endif

#if Standalone == "1"
  #define OutputSuffix "-Standalone"
  #define SourceFolder "publish_sc"
#else
  #define OutputSuffix ""
  #define SourceFolder "publish"
#endif

[Setup]
AppId={{{#MyAppId}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=installer_output
OutputBaseFilename=Mawaqit-{#MyAppVersion}{#OutputSuffix}-Setup
SetupIconFile=Resources\logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName},0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=yes
CloseApplications=yes
CloseApplicationsFilter=Mawaqit.exe
VersionInfoVersion=2.2.6.0
VersionInfoTextVersion=2.2.6
VersionInfoProductVersion=2.2.6.0

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
english.CreateDesktopIcon=Create a desktop shortcut
english.AdditionalShortcuts=Additional shortcuts:
english.StartupGroup=Startup:
english.StartWithWindows=Start Mawaqit with Windows (minimized to tray)

arabic.CreateDesktopIcon=إنشاء اختصار على سطح المكتب
arabic.AdditionalShortcuts=اختصارات إضافية:
arabic.StartupGroup=بدء التشغيل:
arabic.StartWithWindows=تشغيل مواقيت مع بدء تشغيل ويندوز (مصغراً في شريط المهام)

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalShortcuts}"
Name: "startup"; Description: "{cm:StartWithWindows}"; GroupDescription: "{cm:StartupGroup}"

[Files]
Source: "{#SourceFolder}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "logo.png,logo.ico,*.pdb"
Source: "Resources\Media\*"; DestDir: "{app}\Resources\Media"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Resources\Flags\*"; DestDir: "{app}\Resources\Flags"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"" --startup"; Tasks: startup; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"; ValueType: binary; ValueName: "{#MyAppName}"; ValueData: "02 00 00 00 00 00 00 00 00 00 00 00"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\{#MyAppExeName}"; Parameters: "--uninstall"; WorkingDir: "{app}"; Flags: runhidden; RunOnceId: "MawaqitUninstall"

[InstallDelete]
Type: filesandordirs; Name: "{userappdata}\Mawaqit\Media"

[UninstallDelete]
Type: files; Name: "{app}\*.log"
Type: filesandordirs; Name: "{userappdata}\Mawaqit\Media"

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  // Terminate any running instance of Mawaqit before updating/replacing files
  Exec('taskkill.exe', '/F /IM Mawaqit.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;

function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  // Terminate any running instance before uninstalling
  Exec('taskkill.exe', '/F /IM Mawaqit.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;

function BoolToJson(Value: Boolean): string;
begin
  if Value then
    Result := 'true'
  else
    Result := 'false';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigFolder, ConfigPath, SelectedLang, JsonContent: string;
begin
  if CurStep = ssPostInstall then
  begin
    SelectedLang := ActiveLanguage;
    ConfigFolder := ExpandConstant('{userappdata}\Mawaqit');
    ConfigPath := ConfigFolder + '\config.json';
    
    // If config does not exist, initialize it with Arabic as default language
    if not FileExists(ConfigPath) then
    begin
      ForceDirectories(ConfigFolder);
      if SelectedLang = 'english' then
      begin
        JsonContent := '{"config_version":2,"wilaya_ar":"\u0627\u0644\u062c\u0632\u0627\u0626\u0631","wilaya_en":"Alger","language":"en","adhan_enabled":false,"reminder_enabled":false,"prayer_adhan_enabled":{"Fajr":true,"Dhuhr":true,"Asr":true,"Maghrib":true,"Isha":true},"start_with_windows":' + 
          BoolToJson(WizardIsTaskSelected('startup')) + ',"theme_mode":"system","volume":0.33}';
      end
      else
      begin
        JsonContent := '{"config_version":2,"wilaya_ar":"\u0627\u0644\u062c\u0632\u0627\u0626\u0631","wilaya_en":"Alger","language":"ar","adhan_enabled":false,"reminder_enabled":false,"prayer_adhan_enabled":{"Fajr":true,"Dhuhr":true,"Asr":true,"Maghrib":true,"Isha":true},"start_with_windows":' + 
          BoolToJson(WizardIsTaskSelected('startup')) + ',"theme_mode":"system","volume":0.33}';
      end;
      SaveStringToFile(ConfigPath, JsonContent, False);
    end;
  end;
end;
