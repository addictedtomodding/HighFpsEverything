#define AppName "BetterFps (TerrariaInjector)"
#define AppVersion "1.0.0"
#define Publisher "addict"
#define AppExeName "TerrariaInjector.exe"

[Setup]
AppId={{7AEA625A-E5BD-4D3C-BF04-D7FC0E8302D5}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
PrivilegesRequired=admin
DefaultDirName={code:GetTerrariaDir}
DisableProgramGroupPage=no
OutputBaseFilename=Vanilla_Mod_Loader
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DirExistsWarning=no
SetupIconFile=betterfps.ico



[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &Desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
Name: "showsteamreadme"; Description: "Add to Steam Library (Will Launch Steam & a README); GroupDescription: "Shortcuts:"; Flags: unchecked

[Dirs]
Name: "{app}\Mods"

[Files]
Source: "TerrariaInjector.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "0Harmony.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "BetterFps.dll"; DestDir: "{app}\Mods"; Flags: ignoreversion
Source: "README_steamapp.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\TerrariaInjector"; Filename: "{app}\{#AppExeName}"
Name: "{commondesktop}\TerrariaInjector"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Code]

function GetSteamPath(): string;
begin
  Result := '';
  RegQueryStringValue(HKCU, 'Software\Valve\Steam', 'SteamPath', Result);
end;

function NormalizePath(const S: string): string;
begin
  Result := S;
  StringChangeEx(Result, '/', '\', True);
  while (Length(Result) > 0) and (Result[Length(Result)] = '\') do
    Delete(Result, Length(Result), 1);
end;

function TerrariaExistsIn(const Root: string): string;
var
  Candidate: string;
begin
  Result := '';
  Candidate := NormalizePath(Root) + '\steamapps\common\Terraria';
  if FileExists(Candidate + '\Terraria.exe') then
    Result := Candidate;
end;

function ExtractQuotedValue(const Line: string; StartPos: Integer): string;
var
  i, j: Integer;
begin
  Result := '';
  i := StartPos;
  while (i <= Length(Line)) and (Line[i] <> '"') do i := i + 1;
  if i > Length(Line) then Exit;
  j := i + 1;
  while (j <= Length(Line)) and (Line[j] <> '"') do j := j + 1;
  if j > Length(Line) then Exit;
  Result := Copy(Line, i + 1, j - i - 1);
end;

function FindTerrariaByLibraryFolders(const SteamRoot: string): string;
var
  VdfPath: string;
  Lines: TArrayOfString;
  i: Integer;
  Line, PathVal, Candidate: string;
begin
  Result := '';
  VdfPath := NormalizePath(SteamRoot) + '\steamapps\libraryfolders.vdf';
  if not FileExists(VdfPath) then Exit;
  if not LoadStringsFromFile(VdfPath, Lines) then Exit;

  for i := 0 to GetArrayLength(Lines) - 1 do
  begin
    Line := Trim(Lines[i]);
    if Pos('"path"', Lowercase(Line)) > 0 then
    begin
      PathVal := ExtractQuotedValue(Line, Pos('"path"', Lowercase(Line)) + 6);
      if PathVal <> '' then
      begin
        StringChangeEx(PathVal, '\\', '\', True);
        Candidate := TerrariaExistsIn(PathVal);
        if Candidate <> '' then
        begin
          Result := Candidate;
          Exit;
        end;
      end;
    end;
  end;
end;

function AutoDetectTerrariaDir(): string;
var
  SteamPath, Candidate: string;
begin
  Result := '';
  SteamPath := GetSteamPath();
  if SteamPath = '' then Exit;

  SteamPath := NormalizePath(SteamPath);

  Candidate := TerrariaExistsIn(SteamPath);
  if Candidate <> '' then
  begin
    Result := Candidate;
    Exit;
  end;

  Candidate := FindTerrariaByLibraryFolders(SteamPath);
  if Candidate <> '' then
  begin
    Result := Candidate;
    Exit;
  end;
end;

function GetTerrariaDir(Param: string): string;
var
  Detected: string;
begin
  Detected := AutoDetectTerrariaDir();
  if Detected <> '' then
    Result := Detected
  else
    Result := ExpandConstant('{pf}\Steam\steamapps\common\Terraria');
end;

procedure ShowSteamReadme();
var
  ResultCode: Integer;
begin
  ShellExec('', 'steam://open/games', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);

  Sleep(1500);

  ShellExec(
    '',
    'notepad.exe',
    '"' + ExpandConstant('{app}\README_steamapp.txt') + '"',
    '',
    SW_SHOWNORMAL,
    ewNoWait,
    ResultCode
  );
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssDone) and WizardIsTaskSelected('showsteamreadme') then
    ShowSteamReadme();
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Chosen: string;
begin
  Result := True;

  if CurPageID = wpSelectDir then
  begin
    Chosen := NormalizePath(WizardForm.DirEdit.Text);

    if not FileExists(Chosen + '\Terraria.exe') then
    begin
      MsgBox(
        'That folder does not look like a Terraria install (Terraria.exe not found).',
        mbError,
        MB_OK
      );
      Result := False;
    end;
  end;
end;
