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

The in-game test runs on ValheimAutoTest, the test harness shared by all the Valheim mod projects, in
`..\ValheimAutoTest` next to this repo (its README is the full reference). The harness starts the game, loads a
character and world, runs the test steps, writes `summary.txt` and quits; `Tools\AugaAutoStart` is Auga's set of
steps on top of it: the HUD, inventory, map, pause menu, in-game settings and chat, damage text, a screenshot of every
step. Both are development-only BepInEx plugins that take over every launch of the profile they are in, so they live
in the test profile only (the Gale profile `AugaAuto`, which has a `valheim-autotest.txt` marker), never in a profile
you play in. Build Auga and the helper (it is not part of `Auga.sln`, and builds the harness with it), then run the
harness's launcher, which copies the harness and both dlls into the profile, starts the game (Steam must be running,
Valheim must not be), waits for it to quit and prints the result:

```powershell
msbuild Tools\AugaAutoStart\AugaAutoStart.csproj -restore -p:Configuration=Debug
..\ValheimAutoTest\Start-ValheimTest.ps1 -TestProfile AugaAuto -Plugin Auga\bin\Debug\net48\Auga.dll, Tools\AugaAutoStart\bin\Debug\net48\AugaAutoStart.dll -Variables @{ AUGA_TEST_DUMP = '1' }
```

It exits with 0 when the run passed, 1 when it failed and 2 when it did not finish. The screenshots, the hierarchy
dumps, `summary.txt` and the run's own `Player.log` go to a new folder under `%TEMP%\ValheimAutoTest\AugaAuto\`.
Auga's own settings go in `-Variables`; the harness's (`-Character`, `-World`, `-Quit`, ...) are launcher parameters.

Without the launcher, `-p:DeployToValheim=true` on the build deploys the helper and the harness into `AugaAuto`
(`-p:AugaTestProfile=` for another); copy `Auga.dll` there yourself, set the settings in the environment, and start
the game with the test profile, as Gale does:

```powershell
$env:AUGA_TEST_CHARACTER = "auga test"   # profile file name (default "auga test"); a missing one is created as a local character
$env:AUGA_TEST_WORLD     = "AugaAutoTest" # world name (default "AugaAutoTest"); a missing world is created as a local world
$env:AUGA_TEST_SHOTS     = "C:\temp\augashots"
$env:AUGA_TEST_QUIT      = "12"          # seconds after the last step, 0 = keep running
# the four above are Auga's names for the harness's VALHEIM_TEST_CHARACTER, _WORLD, _SHOTS and _QUIT, which win when set
$env:AUGA_TEST_DUMP      = "1"           # also dump every vanilla UI hierarchy before Auga replaces it
$env:AUGA_TEST_ROWS      = "6"           # also screenshot the inventory with this many player rows
$env:AUGA_TEST_EFFECTS   = "11,24"       # also screenshot the player panel's Active Effects with this many status effects
$env:AUGA_TEST_SETTINGS  = "1"           # only screenshot every settings tab from the main menu, then quit
$env:AUGA_TEST_CHARSELECT = "1"          # only screenshot the character selection, new character, manage saves and the remove dialog, then quit
$env:AUGA_TEST_STARTGAME = "1"           # only screenshot the world list, new world, server list and add server dialogs, then quit
$env:AUGA_TEST_CROSSPLAY = "1"           # host the test world as a password-protected crossplay server and log the join code overlay
$env:AUGA_TEST_FAVORITES = "1"           # mark test items with Item Favorite Framework and screenshot the inventory and their tooltips
$env:AUGA_TEST_ENEMIES   = "1"           # spawn creatures of several levels and a boss and screenshot their health bars; "only" runs just that step
$env:AUGA_TEST_STATBARS  = "1"           # fix the health bar's length, eat and lose food, and check where its ticks sit; "only" runs just that step
& "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim.exe" --doorstop-enabled true --doorstop-target-assembly "$env:APPDATA\com.kesomannen.gale\valheim\profiles\AugaAuto\BepInEx\core\BepInEx.Preloader.dll"
```

Read `%USERPROFILE%\AppData\LocalLow\IronGate\Valheim\Player.log` and `summary.txt` in the shots folder afterwards.

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
