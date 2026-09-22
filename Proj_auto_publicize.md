# Valheim mod builds: auto-resolved paths + publicizing

**Goal:** a fresh clone builds with plain `dotnet build` or VS. It shouldn't need a hand-made
`publicized_assemblies` folder, and BepInEx doesn't have to be in the game install.
**Reference implementation:** `Valheim_Star_Levels_Expanded\Directory.Build.props`. The other fixed repos have
byte-identical copies.

## How the JotunnLib NuGet package wires references (its build/*.props)
- `Paths.props` sets `VALHEIM_INSTALL` (Steam registry, then Program Files), `VALHEIM_MANAGED` and
  `BEPINEX_PATH` (= `<install>\BepInEx`). Each is **only set if still empty**. It also imports
  `$(SolutionDir)Environment.props` / `DoPrebuild.props`, but neither import fires on a direct
  `.csproj` build, because `SolutionDir` is undefined there.
- `JotunnLibRefsCorlib.props` references `$(VALHEIM_MANAGED)/publicized_assemblies/*_publicized.dll`,
  `$(BEPINEX_PATH)/core/*.dll` and the Unity modules.
- `JotunnLib.props` runs `JotunnBuildTask` when `ExecutePrebuild=true` and the install exists. It
  publicizes `assembly_*.dll`, `SoftReferenceableAssets.dll` and `gui_framework.dll` into
  `Managed/publicized_assemblies`. It's hash-checked, so it's incremental and picks up game updates.
  It works under `dotnet build` and writes into the game folder.

**Typical failure:** the game is modded via Gale, r2modman or Thunderstore, so the install has no
`BepInEx\core`. The BepInEx/Harmony/Cecil/MonoMod refs then fail with MSB3245, which cascades into
hundreds of CS0246. Separately, `ExecutePrebuild` is off, so the build silently depends on stale
hand-publicized DLLs.

## Setup (Jotunn projects)
1. Add a repo-root `Directory.Build.props`. It's imported before the NuGet props, so it wins the
   only-if-empty race. Sections, in this order:
   1. `SolutionDir` falls back to `$(MSBuildThisFileDirectory)` when empty or nonexistent. When a
      .sln drives the build, `SolutionDir` is a global property and the fallback is ignored.
      This section must come before step 2.
   2. Import `Environment.props` (gitignored overrides), except when
      `'$(SolutionDir)' == '$(MSBuildThisFileDirectory)'`. Jotunn imports it in that case, and a
      second import is MSB4011.
   3. Import root `DoPrebuild.props` if `ExecutePrebuild` is empty, then default it to `true`.
   4. Probe `VALHEIM_INSTALL` (same order as Paths.props), then `VALHEIM_MANAGED`
      (`Valheim_Data` / `valheim_Data` / `valheim_server_Data`).
   5. `BEPINEX_PATH`: use the game folder if `core\BepInEx.dll` exists there. Otherwise use the
      first mod-manager profile that has one (snippet below). Reject a hit that lacks
      `core\0Harmony.dll`.
   6. Aliases for Azu-style projects (`GamePath`, `GamePathManaged`, `BepInExPath`,
      `PublicizedAssembliesPath`), each only set if empty.
   7. A `<Warning>` target (BeforeTargets=BeforeBuild) for a missing Managed folder or BepInEx.
      Run it only for projects with a JotunnLib PackageReference, so preloader and tool projects
      stay quiet.
2. Set root `DoPrebuild.props` to `ExecutePrebuild=true`. Delete any project-level
   `DoPrebuild.props`, since nothing ever imports them.
3. Commit `Environment.props.example`; keep `Environment.props` gitignored.
4. In the .csproj:
   - Make `VALHEIM_MANAGED` conditional: `Condition="'$(VALHEIM_MANAGED)' == ''"`.
   - Add a `BEPINEX_PATH` fallback.
   - Change `$(VALHEIM_INSTALL)/BepInEx/...` to `$(BEPINEX_PATH)/...`.
   - Replace hardcoded `..\..\..\Program Files (x86)\...` HintPaths with the variables.
   - Drop references to assemblies Valheim no longer ships:
     `<Reference Remove="UnityEngine.ProfilerModule" Condition="!Exists('$(VALHEIM_MANAGED)/UnityEngine.ProfilerModule.dll')" />`
     Older Jotunn versions also need `UnityEngine.TextCoreModule`, `UnityEngine.UNETModule`,
     `assembly_steamworks_publicized` and `Unity.MemoryProfiler` removed.

BepInEx probe. Items can't be read into properties at evaluation time, so glob via System.IO:
```xml
<_Hits Condition="'$(_Hits)'=='' And Exists('$(_Root)')">$([System.IO.Directory]::GetFiles('$(_Root)', 'BepInEx.dll', SearchOption.AllDirectories))</_Hits>
<_Dll Condition="'$(_Hits)'!=''">$(_Hits.Split(';')[0])</_Dll>
<BEPINEX_PATH Condition="'$(_Dll)'!=''">$([System.IO.Path]::GetFullPath($([System.IO.Path]::Combine($([System.IO.Path]::GetDirectoryName('$(_Dll)')), '..'))))</BEPINEX_PATH>
```
Profile roots, one line each, stopping at the first hit:
- `%AppData%\com.kesomannen.gale\valheim\profiles`
- `%AppData%\r2modmanPlus-local\Valheim\profiles`
- `%AppData%\Thunderstore Mod Manager\DataFolder\Valheim\profiles`

## No JotunnLib, or a custom reference list: BepInEx.AssemblyPublicizer.MSBuild
- `<PackageReference Include="BepInEx.AssemblyPublicizer.MSBuild" Version="0.4.3" PrivateAssets="all" />`,
  then reference the stock `Managed\*.dll` with `Publicize="true"`. Output goes to `obj/publicized/`,
  with no writes to the game folder. **It works in SDK-style projects only** (hard error otherwise).
- Put it in `Directory.Build.targets`, which runs after the csproj body, behind an opt-out:
  `<ItemGroup Condition="'$(PublicizeGameAssemblies)' != 'false'">` plus
  `<Reference Update="assembly_valheim" Publicize="true" />`. `Update` only matches a simple-name
  Include, not one with `, Version=...` appended.
- To find out which projects need it, build against the stock assemblies first. CS0122 or CS0117
  means the project needs publicizing.
- If a project also pulls JotunnLib, `Reference Remove` Jotunn's `*_publicized` refs and re-add the
  stock DLLs with Publicize. Otherwise it still reads the game folder. The ref names vary by Jotunn
  version, so check the version that actually resolved (it matters with a floating `2.*`).
- To verify, check that `dotnet msbuild X.csproj -t:ResolveReferences -getItem:ReferencePath`
  lists nothing under `publicized_assemblies`.

## Legacy (non-SDK) csproj
- Without `<Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" .../>`
  at the top, `Directory.Build.props` is never imported. Add it above any packages.config-style
  `JotunnLib.props` import.
- With legacy csproj + `PackageReference`, `dotnet build` doesn't resolve the package's lib assets
  (e.g. the `Jotunn` namespace comes up missing). Verify with VS MSBuild, or convert to SDK style.
- Converting to SDK style:
  - Set `GenerateAssemblyInfo=false` if `Properties\AssemblyInfo.cs` exists.
  - Set `AppendTargetFrameworkToOutputPath=false` to keep the `bin\Debug\` layout.
  - Confirm the `**/*.cs` glob picks up exactly the old `<Compile>` list.
  - Put `None Remove` before any `EmbeddedResource Include` for the same file.
  - Drop `ProjectGuid`, `TargetFrameworkProfile` and the packages.config restore targets.
  - Keep shared `.projitems` imports.

## Pitfalls
- `Directory.Build.props` is needed for **property precedence** (Jotunn's only-if-empty defaults).
  It isn't about HintPaths: items evaluate after all properties, so they see later values anyway.
- A relative `Exists('x')` inside package props doesn't resolve against the project directory.
- Test for `core\BepInEx.dll`, not the BepInEx folder. A bare `BepInEx\plugins` folder passes a
  folder check.
- If two properties point at different BepInEx folders (e.g. `BepInExPath` vs `BEPINEX_PATH`),
  the compile fails with CS1704 (duplicate assembly).
- Some repos already handle their own `Environment.props` import or `ExecutePrebuild` policy. For
  those, strip that section from `Directory.Build.props` rather than duplicating it.
- Preserve each file's existing BOM and line endings. A `utf-8-sig` round-trip adds a BOM to files
  that didn't have one.

## Verify
- `dotnet msbuild X.csproj -getProperty:ExecutePrebuild -getProperty:VALHEIM_MANAGED -getProperty:BEPINEX_PATH`
- Build through both the `.sln` and the `.csproj` directly. Expect "Executing Jotunn Prebuild Task",
  zero MSB3245 and no MSB4011.
- Pass a bogus `-p:VALHEIM_INSTALL=...` and check the diagnostics fire, and only for Jotunn projects.
- Some repos deploy on every build. Use `-t:ResolveReferences` when you need a check with no side effects.