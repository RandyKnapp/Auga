# Mod compatibility

Where the 1.x port stands against other mods, what still has to be done for EpicLoot, Equipment and Quick Slots
and the rest, and which mods Auga should refuse to load next to. Keep this file current: when a compatibility
patch lands or a mod is tested in game, change its row here in the same commit.

The facts below come from the sources next to this repo (`../Randy_Vapok_ValheimMods` for EpicLoot and EAQS,
`../OtherModders` for third-party mods, decompiled where closed) and from the current Thunderstore packages
(GUIDs read from the plugin DLLs on 2026-10-27). The GitHub issue numbers are on RandyKnapp/Auga.

## How Auga meets other mods

**Load order.** Auga requires Jotunn (a hard `BepInDependency`, for the server config sync) and declares a soft
one on BetterTrader, MultiCraft, Chatter, Sears Catalog, SimpleRecycling and Jewelcrafting, so they load first and `Chainloader.PluginInfos` already lists them in
`Auga.Awake`. Mods that use the Auga API declare a soft dependency on `randyknapp.mods.auga`, which makes them
load after Auga and opts them in to APIManager's rewrite (see "Auga API" in `CLAUDE.md`). `API.IsLoaded()` is a
constant: `true` inside `Auga.dll`, `false` in the stub. The old reflection shim's expensive `IsLoaded` (issue
#155, `AppDomain.GetAssemblies` on every call) only survives in mods that still embed `API.External.cs`, such as
Better Archery.

**How other mods detect Auga.** Three ways, and each needs something different from us:

| Detection | Mods | What they need |
| --- | --- | --- |
| `Auga.API.IsLoaded()` through the stub mirror | EpicLoot, Equipment and Quick Slots, Recycle N Reclaim | public signatures never change; the mirror in each mod matches `API.cs` |
| `Chainloader.PluginInfos.ContainsKey("randyknapp.mods.auga")` | Quick Stack Store, Better Archery, Valheim Admin Tool, Path of Valheiman, Carturs UI HUD, AAA Crafting, TradersExtended | the objects they then `Find` by vanilla name exist under the Auga hierarchy |
| `[BepInIncompatibility("randyknapp.mods.auga")]` | AzuExtendedPlayerInventory, ExtraSlots, MyLittleUI, Almanac | nothing: BepInEx skips *them* when Auga is installed |

Valheim Admin Tool reaches `Auga.AugaPanelRestyler` and `Auga.RestyleOptions` by reflection, not through the
API. Those two names stay public and stable; `API.Panel_Restyle` is the supported entry for new code.

**What Auga does to keep other mods' patches working** (most of it from commit `844c8a0`):

- `UiAwakeGuards`: Harmony finalizers on `InventoryGui.Awake`, `Hud.Awake`, `KeyHints.Start`,
  `Minimap.Awake/Start`, `TextsDialog.Awake`, `StoreGui.Awake` and `Menu.Start` log an exception thrown by another
  mod's patch and swallow it, so Unity does not disable the screen.
- Auga's `Awake` postfixes run at `Priority.First`, so every other mod's postfix sees the Auga hierarchy.
- Replaced vanilla objects keep vanilla's child names: the status effect template (`Icon`, `Name`, `TimeText`,
  `Cooldown`, `TimeBar`), every compendium `TextsDialog` (`Texts_frame`, `TextArea`, `Closebutton`), the key hint
  entries (vanilla children stay, hidden; a watcher rebuilds the Auga row when another mod writes into them).
- `SetupHelper.ForeignChildren` / `AdoptChildren` move objects other mods hung on a vanilla root (TargetPortal's
  favourites on the map) to the Auga replacement. `CopyVanillaButtonBits` gives the craft, cancel and tab buttons
  vanilla's `UIGamePad` and `ButtonSfx` for mods that clone them.
- The inventory grid leaves a slot that another mod parented elsewhere (EAQS's `EaqsSlotRoot`) where it is, and
  moves its own slots once rather than every frame.
- `ComplexTooltip` puts back the parts a mod moved out of the tooltip on hover (EpicLoot's scroll view takes the
  first Topic and Text) and a stat preprocessor that returns `null` hides the line instead of breaking the next
  mod's preprocessor.

**What still breaks other mods, by design:**

- Root swaps in an `Awake` prefix (TextViewer, TextInput, DamageText, EnemyHud, Barber, Chat) destroy the vanilla
  root. A mod that edited that prefab before Auga's prefix, or `Find`s under it afterwards, gets nothing.
- `Hud.UpdateStatusEffects`, `UpdateFood`, `UpdateHealth`, `SetHealthBarSize` and `SetStaminaBarSize` are prefixed
  out; Auga draws those itself (`AugaStatusEffects`, the stat bars). A postfix on them runs against empty vanilla
  state. EpicLoot's health bar cap and critical-health effect already stand down for this reason, and
  MinimalStatusEffects' postfix does nothing under Auga.
- Only TextMeshPro: a mod that adds `UnityEngine.UI.Text` under an Auga panel renders with the default font, and a
  mod that reads an Auga label as `Text` gets `null`. `TooltipTextBox_AddLine(GameObject, Text, ...)` still works
  because it writes into the box's TMP text.

## Status by mod

Tested means the mod ran next to this port with the smoke test or by hand and the result was looked at. Untested
means the compatibility code exists but nobody has run it on 1.x since the port.

| Mod | GUID | How it meets Auga | 1.x status |
| --- | --- | --- | --- |
| EpicLoot | `randyknapp.mods.epicloot` | API mirror, tooltip listener and stat preprocessor, restyles its own panels | tested 2026-10-25; to-do list below |
| Equipment and Quick Slots | `randyknapp.mods.equipmentandquickslots` | API mirror, `AugaPanel` beside the player panel | tested 2026-10-25; to-do list below |
| AAA Build Menu | `Azumatt.AAABuildMenu` | patches the build menu Auga restyles in place | tested 2026-10-25 |
| TargetPortal | `org.bepinex.plugins.targetportal` | favourites list on the map, cloned key hints | tested 2026-10-25 (`Minimap_Setup`) |
| PlantEasily | `advize.PlantEasily` | HUD text | tested 2026-10-25 |
| Jewelcrafting | `org.bepinex.plugins.jewelcrafting` | `Auga/Compat/Jewelcrafting.cs` transpiles JC's nested patch types; hotkey bar left to JC | untested; a failed lookup sleeps 15 s at startup |
| Chatter | `redseiko.valheim.chatter` | `Auga/Compat/Chatter.cs` swaps `ToggleCell` for Auga toggles | untested |
| Sears Catalog | `redseiko.valheim.searscatalog` | `Auga/Compat/SearsCatalog.cs` nudges `m_pieceListRoot` 3 px | probably obsolete since the build menu moved to `BuildUIV2` (`4e6f966`) |
| MultiCraft | `maximods.valheim.multicraft` | `Auga/Compat/MultiCraft.cs` patches `MultiCraft_UI` and `MultiCraft_Logic` | untested; issues #141, #220 |
| SimpleRecycling | `com.github.abearcodes.valheim.simplerecycling` | `Auga/Compat/SimpleRecycling.cs`, a vanilla workbench tab | untested; the mod is abandoned, Recycle N Reclaim replaced it |
| BetterTrader | `Menthus.bepinex.plugins.BetterTrader` | branch in `Store_Setup.cs` | untested |
| Recycle N Reclaim | `Azumatt.Recycle_N_Reclaim` | `API.IsLoaded`, vanilla workbench tab, container button | untested; in most play profiles |
| Quick Stack Store | `goldenrevolver.quick_stack_store` | `PluginInfos`; copes with a missing "take all" button | untested; issue #145 says its buttons vanish |
| AAA Crafting | `Azumatt.AzuAntiArthriticCrafting` | references Auga's GUID; wants the crafting input field | untested; issue #144, Auga 1.3.12 added `GetCraftingControls` for it |
| Better Archery | `ishid4.mods.betterarchery` | old reflection shim (`API.External.cs` copy) and `PluginInfos` | untested; issue #52 (quiver), EAQS has its own compat |
| Smoothbrain Backpacks | `org.bepinex.plugins.backpacks` | opens a fake container under the inventory | untested; issue #136 NRE in `OpenFakeItemsContainer` |
| Adventure Backpacks | `vapok.mods.adventurebackpacks` | 13 `InventoryGui` patches, `KeyHints`, `Hud`, `GuiBar` | untested |
| TradersExtended | `shudnal.TradersExtended` | references Auga's GUID | untested; issue #210 (cursor stuck) |
| Path of Valheiman | `treextr.pathofvalheiman` | lists Auga as a UI mod and warns; writes `Player.m_inventory.m_height` itself | untested |
| Valheim Admin Tool | `com.icecub.valheimadmintool` | reflection on `AugaPanelRestyler` and `RestyleOptions` | works while those names stay |
| MinimalStatusEffects | `randyknapp.mods.minimalstatuseffects` | postfix on `Hud.UpdateStatusEffects`, lays out vanilla's entries | inert under Auga: the prefix skips vanilla, Auga's own list is drawn by `AugaStatusEffects` |
| Cheb's Necromancy / Mercenaries | | clone key hint rows | untested; issue #204, the key hint watcher was built for this |
| Groups / Guilds | `org.bepinex.plugins.groups` | chat | untested; issue #226 (chat error) |
| VNEI | `com.maxsch.valheim.vnei` | panel beside the inventory | untested |
| Valheim Plus | `org.bepinex.plugins.valheim_plus` | HUD and inventory options | untested; issue #159 (main menu) |
| AzuExtendedPlayerInventory, ExtraSlots, MyLittleUI, Almanac | `Azumatt.AzuExtendedPlayerInventory`, `shudnal.ExtraSlots`, `shudnal.MyLittleUI` | declare Auga incompatible on their side | BepInEx skips them when Auga is installed (issues #223, #228 are moot) |

## Improvements to make

### EpicLoot

1. **Refresh the API mirror.** `EpicLoot/src/integrations/AugaAPI.cs` and `AugaAPI.Common.cs` were copied on
   2026-09-24. Since then `API.cs` gained `GetBoldFontAsset`, `GetSemiBoldFontAsset`, `GetRegularFontAsset`,
   `Panel_Restyle`, `Text_Restyle`, `Scrollbar_Restyle`, `Slider_Restyle` and `Toggle_Restyle` (`844c8a0`), and the
   mirror never had `GetCraftingControls` or the `CraftingControls` class from 1.6.0. First bump the API version
   (still 1.6.0 in `AugaAPI/Properties/AssemblyInfo.cs`, no CHANGELOG entry yet), rebuild `AugaAPI.dll`, then copy the
   signatures over. `EpicLootAuga.FixFonts` only restyles legacy `Text`; the TMP labels on the enchanting table,
   temper panel and tabs can use the font asset getters or `Text_Restyle` once the mirror has them.
2. **Run the new tooltip code under Auga.** EpicLoot's item tooltip was rebuilt on 2026-10-24 (gather, edit,
   draw) and has not been seen with Auga's `ComplexTooltip` since. Check with `AUGA_TEST_MAGIC`: rarity
   background, subtitle, set text, the augment choice dialog and the craft success panel (both are Auga results
   panels), long descriptions (issue #225) and the Ancient rarity colour.
3. **Skills tab bonuses.** `AddSkillLevel` writes into Auga's `SkillsPanelSkillController`, which rewrites its
   level text every second (issue #209). Confirm the bonus shows and stays.
4. **Features that go dark under Auga.** `HealthBarSize_Patch` (the bar's growth cap) and `HealthCriticalHud`
   skip when `HasAuga` because Auga draws its own health bar. Either accept that or give Auga's bar a hook
   EpicLoot can call.
5. **Compendium contract.** EpicLoot adds its pages to every `TextsDialog` except the one whose name contains
   "Lore", at index 0 under Auga, and finds `Texts_frame`, `TextArea` and `Closebutton`. Auga must keep those names
   and the "Lore" suffix on the lore dialog.
6. **Smoke test flag.** `AUGA_TEST_EPICLOOT_HASAUGA` forces `EpicLoot.HasAuga`; its comment says EpicLoot never
   sets the flag, which is no longer true (`EpicLoot.InitializeAuga` sets it from `IsLoaded()`). Keep the flag as a
   fallback for a failed APIManager rewrite and fix the comment.
7. EpicLoot's stat preprocessor never returns `null` because the old Auga threw on it. Since `844c8a0` a `null`
   hides the line, so EpicLoot may use it.

### Equipment and Quick Slots

1. **Refresh the API mirror** (`EquipmentAndQuickSlots/src/Auga/AugaAPI*.cs`), same gap as EpicLoot's.
2. **Panel layout.** `src/AugaPanel.cs` lays the equipment diamond, quick row and API columns out from constants
   (64 px cell, 70 px pitch, 20 px gap to the player panel) written against the Auga player panel. The smoke
   test's inventory step logs where `EAQS`, `EaqsSlotRoot`, `Divider` and `Paperdolls` land; look at that
   screenshot after any change to the player panel prefab or the slot prefab.
3. **Quick slot hotkey bar.** EAQS pins its bar to y = 74 under Auga to clear the eitr bar (issue #179). If the
   stat bars move or grow, that number is wrong again. The Auga-side fix is a `MovableHudElement` on the EAQS bar.
4. **Gamepad.** `GamepadNavigation` walks the Auga diamond; nobody has tried it with a controller.
5. **Panel height.** `AugaPanel.UpdatePanel` calls `InventoryGui.SetInventorySize` whenever the player panel height
   differs from what EAQS wants, and Auga's `SetInventorySize` postfix repositions the container each time.
   Harmless so far; watch for a fight if Auga ever sizes the panel itself.
6. EAQS already refuses to load next to AzuExtendedPlayerInventory, aedenthorn's ExtendedPlayerInventory,
   ExtraSlots and ComfyQuickSlots. Those also fight Auga's inventory layout, but they need no tag here: the first
   three already tag Auga, and the EAQS tag covers the usual pairing.

### Other mods

- **Jewelcrafting:** the compat reflects JC's nested patch types by name and sleeps 15 s on any miss. Replace the
  sleep with a log line, check the lookups against the JC version in the EpicHeim profile, and add a JC step to
  the smoke test. Auga also leaves the hotkey bar to JC when it is present (`Hud_Setup.cs`), which means the
  movable hotkey bar is lost under JC.
- **Run once each:** Chatter, MultiCraft, BetterTrader, Recycle N Reclaim, Quick Stack Store, AAA Crafting,
  Better Archery, Smoothbrain Backpacks, Adventure Backpacks, TradersExtended, VNEI. Record the result in the
  table above and drop the Sears Catalog nudge if the new build menu no longer needs it.
- **Container panel:** Quick Stack Store and Recycle N Reclaim add buttons next to vanilla's "take all". Keep a
  stable, vanilla-named button row on the Auga container panel so they have something to attach to.
- **MinimalStatusEffects:** decide whether Auga's `AugaStatusEffects` should honour its layout settings or whether
  the two should simply not load together (it is our own mod, so a tag is the cheap answer).
- **Valheim Admin Tool:** keep `AugaPanelRestyler` and `RestyleOptions` public; point its author at
  `API.Panel_Restyle`.
- **README:** the Thunderstore page's compatibility list names the tagged mods now; add the tested rows to it as
  they are confirmed on 1.x.

## Incompatibility tags

`[BepInIncompatibility("<guid>")]` on the plugin class makes BepInEx skip **Auga** when that mod is installed,
with a warning in the log, instead of loading two mods that both redraw the same screens. The attributes belong
next to the `BepInDependency` lines on `Auga` in `Auga/Auga.cs`. BepInEx sorts incompatibilities like
dependencies, so one side declaring it is enough; do not mirror a tag a mod already carries against Auga, since a
mutual tag risks a cycle in that sort.

**Tagged** (on the `Auga` class since 2026-10-27). Full UI replacements that cannot share a screen with Auga:

| Mod | GUID | Why |
| --- | --- | --- |
| SeneaL UI | `seneaL.valheim.ui` | redraws HUD, inventory, crafting, map, menus and chat with its own prefabs; patches `Minimap.Start`, `UITooltip`, `InventoryGui`, `Hud`, `Chat`, `FejdStartup`; its page says other UI mods should be disabled |
| BetterUI Reforged | `MK_BetterUI` | HUD, skills dialog, inventory and tooltip patches on vanilla objects Auga destroys; issue #193; the Nexus page already lists it as incompatible; deprecated on Thunderstore but still installed |
| BetterUI (Zenox) | `zenox.betterui` | the 1.0 rewrite: movable HUD bars, enemy HUD, combined tooltips, party panel, all on the vanilla HUD |
| Minimal UI | `Azumatt.MinimalUI` | hides and repositions the vanilla panels by path; deprecated, but widely installed |
| AugaLite | `ZenDragon.AugaLite` | a trimmed copy of Auga 1.3.11 with the same stat bars, loading screens and fonts; two copies of the same replacement |

**Check before tagging.** UI mods whose scope is unclear or that know about Auga; run each once with the smoke
test and tag the ones that break the HUD or inventory:

| Mod | GUID | Note |
| --- | --- | --- |
| ZenUI | `ZenDragon.ZenUI` | crafting panel, inventory, food and skills panels, status effects, ship HUD, chat, tooltips, each a config section; knows AugaLite, not Auga |
| Carturs UI HUD | `com.jekkle.valheim.carturuihud` | references both Auga and AugaLite GUIDs, so it may already stand down |
| GenesisUI | `Genesis.GenesisUI` | scope unknown |
| ValHUD | `DarthVladi.ValHUD` | builds its own stat bars and can hide the vanilla HUD |
| Wire's Smooth UI | `wire.valheim.smoothui` | knows ExtraSlots and MyLittleUI; scope unknown |
| HUD Update | `crevitka.hudupdate` | replaces the lower-left HUD block |
| HUD Revizualized | `com.shadow.hudrevizualized` | replaces the hotbar |
| SeneaL API | `seneaL.valheim.api` | patches `UnifiedPopup`, `MessageHud` and `TextInput`, all of which Auga replaces; harmless alone, but mods built on it expect SeneaL UI |

**Do not tag.** MyLittleUI, ExtraSlots, AzuExtendedPlayerInventory and Almanac tag Auga themselves. Valheim Plus
and GUI Scale Unlocked change scale and options, not the screens. MinimalStatusEffects is our own mod: fix or tag
from its side. Mods that only conflict with EAQS are EAQS's problem.
