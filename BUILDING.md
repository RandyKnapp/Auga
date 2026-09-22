# Building Auga

## Requirements

* Valheim (Steam) with the current game version. The build reads the Unity and game assemblies straight from
  `<Valheim>\valheim_Data\Managed`.
* BepInEx 5.4.23.x for Valheim, either in the game folder or in a mod manager profile. By default the build looks
  for the Gale profile `%APPDATA%\com.kesomannen.gale\valheim\profiles\Auga`, then the game folder.
* Visual Studio 2022 (MSBuild) and the .NET SDK (any recent version; it only supplies the `dotnet tool` runner and
  the SDK-style project support). Build with `msbuild`, not `dotnet build`: the checked-in ILRepack task
  (`packages\ILRepack.Lib.MSBuild.Task.2.0.18.2`) is a .NET Framework MSBuild task.
* Unity 6000.0.x (the version Valheim ships with) only if you need to rebuild the `augaassets` bundle from
  `AugaUnity\`.

## One-time setup

```powershell
.\setup-references.ps1            # or: .\setup-references.ps1 -ValheimDir <path> -BepInExDir <path>
```

This publicizes the game assemblies into `References\Valheim` and builds `References\APIManager\APIManager.dll`.
Re-run it after every Valheim update. See `References\README.md`.

## Build

```powershell
msbuild Auga.sln -restore -p:Configuration=Debug      # or Release
msbuild Auga\Auga.csproj -p:Configuration=API         # AugaAPI.dll for other mods (also copied to References\AugaAPI)
```

`AugaUnityLib` builds first (`Unity.Auga.dll`, also copied into `AugaUnity\Assets\ExternalLibraries` so the Unity
prefabs keep their script references), then `Auga` embeds `Unity.Auga.dll`, `fastJSON.dll`, `APIManager.dll` and
the `augaassets` bundle and ILRepack merges `APIManager` into `Auga.dll`.

After a successful build the plugin (`Auga.dll` + `translations.json`) is copied to
`<BepInExDir>\BepInEx\plugins\Auga\` and, when it exists, to the test profile as well (`AugaTestProfile`, the Gale
profile `AugaAuto` by default). Pass `-p:DeployToValheim=false` to skip that, or `-p:BepInExDir=<path>` /
set `BEPINEX_DIR` to deploy somewhere else.

## Testing in game

`Tools\AugaAutoStart` is a development-only BepInEx plugin that runs an automated smoke test: it loads a character
and world, opens the HUD, inventory, map, pause menu, in-game settings and chat, deals damage, takes a screenshot of
every step and quits. It lives in the test profile only (`AugaAuto`), so the main profile is never hijacked. Build
and deploy it (it is not part of `Auga.sln`):

```powershell
msbuild Tools\AugaAutoStart\AugaAutoStart.csproj -restore -p:Configuration=Debug -p:DeployToValheim=true
```

Then launch the game directly with the test profile (Steam must be running; the same arguments Gale uses) with the
test settings in the environment, and read `%APPDATA%\..\LocalLow\IronGate\Valheim\Player.log` afterwards:

```powershell
$env:AUGA_TEST_CHARACTER = "auga test"   # profile file name (default "auga test")
$env:AUGA_TEST_WORLD     = "AugaAutoTest" # world name (default "AugaTest"); a missing world is created as a local world
$env:AUGA_TEST_SHOTS     = "C:\temp\augashots"
$env:AUGA_TEST_QUIT      = "12"          # seconds after the last step, 0 = keep running
$env:AUGA_TEST_DUMP      = "1"           # also dump every vanilla UI hierarchy before Auga replaces it
$env:AUGA_TEST_ROWS      = "6"           # also screenshot the inventory with this many player rows
$env:AUGA_TEST_SETTINGS  = "1"           # only screenshot every settings tab from the main menu, then quit
& "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim.exe" --doorstop-enabled true --doorstop-target-assembly "$env:APPDATA\com.kesomannen.gale\valheim\profiles\AugaAuto\BepInEx\core\BepInEx.Preloader.dll"
```

## Target framework

All projects are SDK-style and target `netstandard2.1`, the target BepInEx recommends for Unity 2021.2+ Mono games
that ship `netstandard.dll`. They compile against the game's own class libraries (`mscorlib.dll`, `netstandard.dll`,
`System*.dll` from `valheim_Data\Managed`) instead of the .NET reference packs, so every type resolves exactly as it
does at runtime. That matters because Unity 6 APIs expose `Span<T>` overloads and Harmony transpilers use
`System.Reflection.Emit.Label`, neither of which the .NET Framework 4.7.2 reference assemblies provide.

## Layout of the paths (Directory.Build.props)

| Property | Meaning | Override |
| --- | --- | --- |
| `ValheimDir` | Valheim install | `-p:ValheimDir=` or `VALHEIM_DIR` |
| `BepInExDir` | folder containing `BepInEx\` | `-p:BepInExDir=` or `BEPINEX_DIR` |
| `AugaRefs` / `ValheimRefs` | `References\` and `References\Valheim` | – |
| `DeployToValheim` | copy the plugin into `BepInExDir\BepInEx\plugins` | `-p:DeployToValheim=false` |
