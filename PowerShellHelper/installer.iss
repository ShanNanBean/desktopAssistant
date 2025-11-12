; ==========================================================================
; PowerShell助手 Inno Setup 安装脚本
; 描述: 自然语言转PowerShell命令的智能助手
; 版本: 1.0.0
; 作者: cunyu
; ==========================================================================

#define MyAppName "PowerShell智能助手"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "cunyu"
#define MyAppExeName "PowerShellHelper.exe"
#define MyAppURL "https://github.com/cunyu/PowerShellHelper"

[Setup]
; 应用程序基本信息
AppId={{A5B6C7D8-E9F0-1234-5678-9ABCDEF01234}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppCopyright=Copyright (C) 2025 {#MyAppPublisher}

; 安装目录配置
DefaultDirName={autopf}\PowerShellHelper
DefaultGroupName={#MyAppName}
DisableDirPage=no
DisableProgramGroupPage=no
AllowNoIcons=yes

; 输出配置
OutputDir=installer_output
OutputBaseFilename=PowerShellHelper_Setup_v{#MyAppVersion}
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; 压缩配置
Compression=lzma2
SolidCompression=yes
LZMAUseSeparateProcess=yes
LZMANumBlockThreads=2

; 外观配置
WizardStyle=modern

; 权限和系统要求
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
MinVersion=10.0

; 安装日志
SetupLogging=yes

; 版本信息
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} 安装程序
VersionInfoTextVersion={#MyAppVersion}
VersionInfoCopyright=Copyright (C) 2025 {#MyAppPublisher}

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[CustomMessages]
chinesesimp.AppDescription=基于AI的自然语言PowerShell命令助手
chinesesimp.LaunchAfterInstall=安装完成后启动 PowerShell智能助手
chinesesimp.CreateDesktopIcon=创建桌面快捷方式
chinesesimp.AdditionalIcons=附加图标:

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; 主程序文件（发布后的所有文件）
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; 图标文件
Source: "icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
; 创建应用数据目录
Name: "{localappdata}\PowerShellHelper"; Flags: uninsneveruninstall

[Icons]
; 开始菜单快捷方式
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"; Comment: "AI-powered PowerShell command assistant"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"; Comment: "Uninstall PowerShell Helper"
; 桌面快捷方式
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon; Comment: "AI-powered PowerShell command assistant"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchAfterInstall}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; 清理用户数据(可选,保留用户配置)
Type: files; Name: "{localappdata}\PowerShellHelper\*.log"

[Registry]
; 添加到系统卸载程序列表
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}"; Flags: uninsdeletekeyifempty
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}\Settings"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}\Settings"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKCU; Subkey: "Software\{#MyAppPublisher}\{#MyAppName}\Settings"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"

[Code]
// 检查是否已安装旧版本
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{A5B6C7D8-E9F0-1234-5678-9ABCDEF01234}_is1';
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

// 卸载旧版本
function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
  Result := 0;
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

// 初始化安装前检查
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  
  // 检查是否有旧版本需要卸载
  if GetUninstallString() <> '' then
  begin
    if MsgBox('检测到已安装旧版本的 PowerShell助手。' + #13#10 + #13#10 + '是否先卸载旧版本？', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ResultCode := UnInstallOldVersion();
      if ResultCode <> 3 then
      begin
        MsgBox('卸载旧版本失败，错误代码: ' + IntToStr(ResultCode) + #13#10 + #13#10 + '请手动卸载后重试。', mbError, MB_OK);
        Result := False;
      end;
    end;
  end;
end;

// 安装过程中的步骤处理
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 安装完成后的操作
    // 可以在这里添加首次启动初始化等操作
  end;
end;

// 卸载前确认
function InitializeUninstall(): Boolean;
begin
  Result := True;
  if MsgBox('确定要完全卸载 PowerShell助手 及其所有组件吗？', mbConfirmation, MB_YESNO) = IDNO then
    Result := False;
end;

// 卸载完成处理
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // 询问是否删除用户数据
    if MsgBox('是否删除用户配置数据?' + #13#10 + #13#10 + '(如果选择"否",下次安装时将保留您的设置)', mbConfirmation, MB_YESNO) = IDYES then
    begin
      DelTree(ExpandConstant('{localappdata}\PowerShellHelper'), True, True, True);
    end;
  end;
end;

[Messages]
chinesesimp.WelcomeLabel2=本程序将安装 [name/ver] 到您的计算机上。%n%n版本 1.0.0 %n建议在继续安装前关闭所有其他应用程序。