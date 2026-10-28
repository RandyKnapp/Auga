# Building Auga

## Requirements

* Valheim (Steam) with the current game version. The build reads the Unity and game assemblies straight from
  `<Valheim>\valheim_Data\Managed`.
* BepInEx 5.4.23.x for Valheim, either in the game folder or in a mod manager profile. The build finds it on its
  own (see "Paths" below).
* Visual Studio 2022 or later (MSBuild) or the .NET SDK. Every project is SDK-style and every dependency is a NuGet
  package, restored by `-restore`.
* Unity 6000.0.x (the version Valheim ships with) only if you need to rebuild the `augaassets` bundle from
  `AugaUnity\`.
* [mod-tools](https://github.com/MidnightsFX/Valheim-tools) for packaging, release checks and deploying builds into
  test profiles. Optional: without it the build compiles and warns that it skipped those steps.

There is no setup step. JotunnLib's build task publicizes the game assemblies into
`<Valheim>\valheim_Data\Managed\publicized_assemblies` before each build whenever the game has changed, so a game
update is picked up by the next build.

## Build

```powershell
msbuild Auga.sln -restore -p:Configuration=Debug      # or Release
```

| Project | Output | Notes |
| --- | --- | --- |
| `AugaUnityLib` | `AugaUnityLib\bin\<Configuration>\Unity.Auga.dll` | also copied into `AugaUnity\Assets\ExternalLibraries` so the prefabs keep their script references |
| `UiLibShim` | `UiLibShim\bin\<Configuration>\ui_lib.dll` | |
| `Auga` | `Auga\bin\<Configuration>\net48\Auga.dll` | embeds the two above, the `augaassets` bundle and `Auga\Assets\translations.json` |
| `AugaAPI` | `AugaAPI\bin\<Configuration>\AugaAPI.dll` | the stub other mods reference; compiles `Auga\API\API.cs` and `API.Common.cs` with `API` defined |
| `AugaApiExample` | `AugaApiExample\bin\<Configuration>\net48\AugaApiExample.dll` | compiles the same API stub source in, as a consuming mod would |

`Auga.dll` needs Jötunn and Newtonsoft.Json at runtime. Neither is shipped with it: both are Thunderstore
dependencies in `Auga\Package\manifest.json` (`ValheimModding-Jotunn`, `ValheimModding-JsonDotNET`).

### Packaging and deploying (mod-tools)

`release.yml` at the repository root configures mod-tools, and `Auga.csproj` carries its managed build block:

* Every build runs `mod-tools preflight`, then `mod-tools deploy`, which copies `Auga.dll` into each profile listed
  in mod-tools' machine `config.yml`. Deploy is **off** in `release.yml` for now (see the comment there): turn it on
  once `config.yml` points at a profile meant for Auga, such as the `AugaAuto` test profile below.
* A Release build also runs `mod-tools check`: the version in `Auga.Version` (`Auga\Auga.cs`), the top entry of
  `Auga\Package\CHANGELOG.md` (a bare `### x.y.z` heading, its title in italics on the next line) and
  `Auga\Package\manifest.json` must agree, and the dependencies are compared with the `PackageReference`s and
  Thunderstore. When every check passes it packages `Auga\Package\` (manifest, icon, README, changelog, with the DLL
  staged into `Auga\Package\plugins\`) into `Auga-<version>.zip` in the Release output folder, and creates the
  release tag `v<version>`, so run a Release build only when the commit is the one to release.
  `mod-tools print-config` shows what `release.yml` resolves to.

## Testing in game

`Tools\AugaAutoStart` is a development-only BepInEx plugin that runs an automated smoke test: it loads a character
and world, opens the HUD, inventory, map, pause menu, in-game settings and chat, deals damage, takes a screenshot of
every step and quits. It lives in the test profile only (`AugaTestProfile`, the Gale profile `AugaAuto` by default),
so a profile you play in is never hijacked. Auga itself has to be in that profile too: copy
`Auga\bin\Debug\net48\Auga.dll` there, or add the profile to mod-tools' `config.yml` and enable deploy. Build and
deploy the helper (it is not part of `Auga.sln`):

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
$env:AUGA_TEST_CHARSELECT = "1"          # only screenshot the character selection, new character, manage saves and the remove dialog, then quit
$env:AUGA_TEST_STARTGAME = "1"           # only screenshot the world list, new world, server list and add server dialogs, then quit
$env:AUGA_TEST_CROSSPLAY = "1"           # host the test world as a password-protected crossplay server and log the join code overlay
& "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim.exe" --doorstop-enabled true --doorstop-target-assembly "$env:APPDATA\com.kesomannen.gale\valheim\profiles\AugaAuto\BepInEx\core\BepInEx.Preloader.dll"
```

## Target framework and class libraries

Every project targets `net48`, like the other Jotunn-template mods, and gets its game, Unity and BepInEx references
from the JotunnLib package. Unlike them, Auga compiles against the game's own class libraries (`mscorlib.dll`,
`netstandard.dll`, `System*.dll` from `valheim_Data\Managed`) instead of the .NET Framework 4.8 reference
assemblies: Unity 6's engine modules are built against netstandard 2.1 and expose `Span<T>` overloads (for example
`Texture2D.LoadImage`), and the 4.8 reference assemblies have neither. `Directory.Build.targets` makes that swap for
every project that references JotunnLib; see the comment there.

## Paths

`Directory.Build.props` is the shared Jotunn template file, the same as in the other mod repositories; do not edit it
here. It resolves:

| Property | Meaning | Default |
| --- | --- | --- |
| `VALHEIM_INSTALL` | Valheim install | the Steam registry entry, then the usual Steam folders |
| `VALHEIM_MANAGED` | `<VALHEIM_INSTALL>\valheim_Data\Managed` | |
| `BEPINEX_PATH` | the `BepInEx` folder holding `core\BepInEx.dll` | the game folder, then the first Gale, r2modman or Thunderstore Mod Manager profile that has one |
| `ExecutePrebuild` | publicize the game assemblies before building | `true` (`DoPrebuild.props`) |

Copy `Environment.props.example` to `Environment.props` (git-ignored) to pin any of them on your machine, or pass
`-p:NAME=...` on the command line. Auga-specific wiring shared by the projects (the class library swap, the
access-check attributes in `Build\IgnoresAccessChecksTo.cs`) lives in `Directory.Build.targets`.
