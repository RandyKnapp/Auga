[CmdletBinding()]
param(
    [string]$ValheimDir = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$refs = Join-Path $repo 'References'
$managed = Join-Path $ValheimDir 'valheim_Data\Managed'
$core = Join-Path $ValheimDir 'BepInEx\core'
$msbuild = & 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe' -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild -or !(Test-Path "$core\BepInEx.dll") -or !(Test-Path "$managed\assembly_valheim.dll")) {
    throw 'Visual Studio MSBuild, Valheim and BepInEx 5 are required.'
}
function Invoke-Checked([string]$Command, [string[]]$Arguments) {
    if ($Command -eq 'git') { $Arguments = @('-c',"safe.directory=$apiSource") + $Arguments }
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE" }
}
New-Item -ItemType Directory -Force $refs | Out-Null
$config = Join-Path $refs 'NuGet.Config'
Set-Content $config '<?xml version="1.0" encoding="utf-8"?><configuration><packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>'
$toolDir = Join-Path $refs 'Tools'
$publicizer = Join-Path $toolDir 'assembly-publicizer.exe'
if (!(Test-Path $publicizer)) {
    Invoke-Checked 'dotnet' @('tool','install','BepInEx.AssemblyPublicizer.Cli','--version','0.4.3','--tool-path',$toolDir,'--configfile',$config)
}
New-Item -ItemType Directory -Force "$refs\Valheim" | Out-Null
$assemblyNames = @('assembly_valheim','assembly_guiutils','assembly_utils','gui_framework','assembly_postprocessing','assembly_sunshafts')
foreach ($name in $assemblyNames) {
    Invoke-Checked $publicizer @("$managed\$name.dll",'-o',"$refs\Valheim\${name}_publicized.dll",'--overwrite')
}
# Keep the APIManager source at the revision verified by the baseline. No junctions or recursive cleanup.
$apiSource = Join-Path $refs 'APIManagerSource'
$apiRevision = '289ad9c6655f61dffb47675493f6e6fe0c6a33da'
if (!(Test-Path "$apiSource\.git")) {
    Invoke-Checked 'git' @('clone','https://github.com/blaxxun-boop/APIManager.git',$apiSource)
    Invoke-Checked 'git' @('-C',$apiSource,'checkout','--detach',$apiRevision)
}
$actualRevision = & git -c "safe.directory=$apiSource" -C $apiSource rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot read APIManager revision.' }
if ($actualRevision -ne $apiRevision) { throw 'APIManager revision differs from the verified baseline.' }
$patch = Join-Path $repo 'Tools\APIManager\nested-types.patch'
& git -c "safe.directory=$apiSource" -C $apiSource apply --reverse --check $patch 2>$null
if ($LASTEXITCODE -ne 0) { Invoke-Checked 'git' @('-C',$apiSource,'apply','--whitespace=nowarn',$patch) }
$apiGame = Join-Path $refs 'APIManagerGame'
New-Item -ItemType Directory -Force "$apiGame\valheim_Data\Managed","$apiGame\BepInEx\core","$refs\APIManager" | Out-Null
Copy-Item "$managed\UnityEngine.dll","$managed\UnityEngine.CoreModule.dll" "$apiGame\valheim_Data\Managed" -Force
Copy-Item "$core\*.dll" "$apiGame\BepInEx\core" -Force
Invoke-Checked $msbuild @("$apiSource\APIManager\APIManager.csproj",'-p:Configuration=Release',"-p:GamePath=$apiGame",'-nologo','-v:minimal')
Copy-Item "$apiSource\APIManager\bin\Release\APIManager.dll" "$refs\APIManager\APIManager.dll" -Force
$common = @('-p:DeployToValheim=false',"-p:ValheimDir=$ValheimDir", "-p:BepInExDir=$ValheimDir", "-p:RestoreConfigFile=$config",'-nologo','-v:minimal')
# The example embeds the API DLL, so that DLL must exist before the solution is built.
Invoke-Checked $msbuild (@("$repo\Auga\Auga.csproj",'-restore','-p:Configuration=API') + $common)
Invoke-Checked $msbuild (@("$repo\Auga.sln",'-restore','-p:Configuration=Release') + $common)
Invoke-Checked $msbuild (@("$repo\Tools\AugaAutoStart\AugaAutoStart.csproj",'-restore','-p:Configuration=Release') + $common)
$provenance = [ordered]@{
    SourceCommit = (& git -c "safe.directory=$repo" -C $repo rev-parse HEAD)
    ApiManagerCommit = $apiRevision
    PublicizerVersion = '0.4.3'
    GameAssemblySha256 = (Get-FileHash "$managed\assembly_valheim.dll").Hash
    PluginSha256 = (Get-FileHash "$repo\Auga\bin\Release\Auga.dll").Hash
    BuiltAtUtc = [DateTime]::UtcNow.ToString('o')
}
$provenance | ConvertTo-Json | Set-Content "$refs\build-provenance.json"
Write-Host "Build complete: $repo\Auga\bin\Release\Auga.dll"
