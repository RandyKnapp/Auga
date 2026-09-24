<#
.SYNOPSIS
    Generates the local, git-ignored References\ folder the solution compiles against.

.DESCRIPTION
    1. Publicizes the Valheim game assemblies (private members become public so the mod can patch them)
       into References\Valheim\*_publicized.dll using BepInEx.AssemblyPublicizer.Cli.
    2. Builds Blaxxun's APIManager (https://github.com/blaxxun-boop/APIManager) from source, with
       Tools\APIManager\nested-types.patch applied, into References\APIManager\APIManager.dll. Auga merges it
       into Auga.dll with ILRepack.

    Requirements: .NET SDK (for the publicizer tool), Visual Studio 2022 MSBuild, git, a Valheim install and a
    BepInEx install (a Gale/r2modman profile or the game folder). Re-run after every Valheim update.

.PARAMETER ValheimDir
    Valheim install folder. Defaults to $env:VALHEIM_DIR, then the Steam registry entry, then the default Steam path.

.PARAMETER BepInExDir
    Folder that contains BepInEx\ (core, plugins). Defaults to $env:BEPINEX_DIR, then the Gale profile
    %APPDATA%\com.kesomannen.gale\valheim\profiles\Auga, then the Valheim folder. Must match Directory.Build.props.
#>
[CmdletBinding()]
param(
    [string]$ValheimDir,
    [string]$BepInExDir
)

$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot

# --- locate Valheim ------------------------------------------------------------------------------
if (-not $ValheimDir) { $ValheimDir = $env:VALHEIM_DIR }
if (-not $ValheimDir -or -not (Test-Path $ValheimDir)) {
    $reg = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 892970' -ErrorAction SilentlyContinue
    if ($reg) { $ValheimDir = $reg.InstallLocation }
}
if (-not $ValheimDir -or -not (Test-Path $ValheimDir)) {
    $ValheimDir = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim'
}
$managed = Join-Path $ValheimDir 'valheim_Data\Managed'
if (-not (Test-Path (Join-Path $managed 'assembly_valheim.dll'))) {
    throw "Valheim not found at '$ValheimDir' (pass -ValheimDir or set VALHEIM_DIR)."
}
Write-Host "Valheim:  $ValheimDir"

# --- locate BepInEx ------------------------------------------------------------------------------
function Test-BepInEx([string]$dir) { $dir -and (Test-Path (Join-Path $dir 'BepInEx\core\BepInEx.dll')) }
if (-not (Test-BepInEx $BepInExDir)) { $BepInExDir = $env:BEPINEX_DIR }
if (-not (Test-BepInEx $BepInExDir)) { $BepInExDir = Join-Path $env:APPDATA 'com.kesomannen.gale\valheim\profiles\Auga' }
if (-not (Test-BepInEx $BepInExDir)) { $BepInExDir = $ValheimDir }
if (-not (Test-BepInEx $BepInExDir)) {
    throw 'BepInEx not found (pass -BepInExDir or set BEPINEX_DIR to the folder that contains BepInEx\core).'
}
Write-Host "BepInEx:  $BepInExDir"

# --- 1. publicized game assemblies ---------------------------------------------------------------
if (-not (Get-Command assembly-publicizer -ErrorAction SilentlyContinue)) {
    Write-Host 'Installing BepInEx.AssemblyPublicizer.Cli (dotnet global tool)...'
    dotnet tool install -g BepInEx.AssemblyPublicizer.Cli | Out-Host
    $env:PATH = "$env:PATH;$env:USERPROFILE\.dotnet\tools"
}

$refDir = Join-Path $repo 'References\Valheim'
New-Item -ItemType Directory -Force $refDir | Out-Null
$gameAssemblies = @(
    'assembly_valheim', 'assembly_guiutils', 'assembly_utils', 'gui_framework',
    'assembly_postprocessing', 'assembly_sunshafts'
)
foreach ($name in $gameAssemblies) {
    $src = Join-Path $managed "$name.dll"
    $dst = Join-Path $refDir "${name}_publicized.dll"
    Write-Host "Publicizing $name.dll"
    assembly-publicizer $src -o $dst --overwrite | Out-Null
    if (-not (Test-Path $dst)) { throw "Publicizer did not produce $dst" }
}

# --- 2. APIManager -------------------------------------------------------------------------------
# APIManager.csproj expects one GamePath containing both valheim_Data\Managed and BepInEx\core, so build it
# against a temporary folder of directory junctions pointing at the real locations.
$apiDir = Join-Path $repo 'References\APIManager'
New-Item -ItemType Directory -Force $apiDir | Out-Null
$work = Join-Path ([System.IO.Path]::GetTempPath()) 'AugaAPIManagerBuild'
if (Test-Path $work) { Remove-Item -Recurse -Force $work }
New-Item -ItemType Directory -Force $work | Out-Null

Write-Host 'Cloning blaxxun-boop/APIManager...'
$src = Join-Path $work 'src'
git clone -q https://github.com/blaxxun-boop/APIManager $src
if (-not (Test-Path (Join-Path $src 'APIManager\APIManager.csproj'))) { throw 'APIManager clone failed.' }

# Tools\APIManager\nested-types.patch: upstream redirects a mod's types nested in an embedded API copy (closures,
# iterators) to the outer Auga type and rewrites definitions, which corrupts the assembly when Cecil writes it
# ("Failed patching ... InvalidCastException" at load for mods that embed the old Auga API shim).
Write-Host 'Applying Tools\APIManager\nested-types.patch...'
git -C $src apply --whitespace=nowarn (Join-Path $repo 'Tools\APIManager\nested-types.patch')
if ($LASTEXITCODE -ne 0) { throw 'APIManager patch did not apply (upstream changed?). See Tools\APIManager\nested-types.patch.' }

$gamePath = Join-Path $work 'GamePath'
New-Item -ItemType Directory -Force $gamePath | Out-Null
New-Item -ItemType Junction -Path (Join-Path $gamePath 'valheim_Data') -Target (Join-Path $ValheimDir 'valheim_Data') | Out-Null
New-Item -ItemType Junction -Path (Join-Path $gamePath 'BepInEx') -Target (Join-Path $BepInExDir 'BepInEx') | Out-Null

$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = $null
if (Test-Path $vswhere) {
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}
if (-not $msbuild) { throw 'MSBuild.exe (Visual Studio 2022) not found.' }
Write-Host 'Building APIManager.dll...'
& $msbuild (Join-Path $src 'APIManager\APIManager.csproj') -p:Configuration=Release "-p:GamePath=$gamePath" -nologo -v:m | Out-Host
$built = Join-Path $src 'APIManager\bin\Release\APIManager.dll'
if (-not (Test-Path $built)) { throw 'APIManager build failed.' }
Copy-Item $built (Join-Path $apiDir 'APIManager.dll') -Force

# junctions must be removed as links, not recursed into
(Get-Item (Join-Path $gamePath 'valheim_Data')).Delete()
(Get-Item (Join-Path $gamePath 'BepInEx')).Delete()
Remove-Item -Recurse -Force $work

Write-Host ''
Write-Host 'Done. References\ is ready; build with:'
Write-Host '  msbuild Auga.sln -restore -p:Configuration=Debug'
