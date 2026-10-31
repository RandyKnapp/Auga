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
  `Minimap.Awake/Start`, `TextsDialog.Awake`, `StoreGui.Awake`, `Menu.Start` and `EnemyHud.Awake` log an exception
  thrown by another mod's patch (always, whatever Auga's logging settings) and swallow it, so Unity does not disable
  the screen. An exception from a postfix on the vanilla `EnemyHud` that Auga's prefix already destroyed is swallowed
  without a log line.
- Auga's `Awake` postfixes run at `Priority.First`, so every other mod's postfix sees the Auga hierarchy.
- Replaced vanilla objects keep vanilla's child names: the status effect template (`Icon`, `Name`, `TimeText`,
  `Cooldown`, `TimeBar`), every compendium `TextsDialog` (`Texts_frame`, `TextArea`, `Closebutton`), the key hint
  entries (vanilla children stay, hidden; a watcher rebuilds the Auga row when another mod writes into them), the
  inventory panels' `weight_icon`, the crafting progress label `CraftProgress/Text`, the enemy HUD's stars (the lit
  star is `star (1)` inside a `star` that carries a disabled `Image`) and the mount HUD's `Stamina/stamina_fast`,
  `Stamina/StaminaText` and `Health/HealthText`.
- `SetupHelper.ForeignChildren` / `AdoptChildren` move objects other mods hung on a vanilla root (TargetPortal's
  favourites on the map) to the Auga replacement. `CopyVanillaButtonBits` gives the craft, cancel and tab buttons
  vanilla's `UIGamePad` and `ButtonSfx` for mods that clone them.
- The inventory grid leaves a slot that another mod parented elsewhere (EAQS's `EaqsSlotRoot`) where it is, and
  moves its own slots once rather than every frame.
- `ComplexTooltip` puts back the parts a mod moved out of the tooltip on hover (EpicLoot's scroll view takes the
  first Topic and Text) and a stat preprocessor that returns `null` hides the line instead of breaking the next
  mod's preprocessor.
- `ComplexTooltip` builds an item's tooltip again when the item's vanilla tooltip text changes while it is shown,
  and shows the lines mods append to that text (as the extra text at the bottom) for every item type, food and
  meads included; only the status effect text a mead's own box already shows is left out of it.
- The hover tooltip scrolls its body itself once it would be taller than the screen (`TooltipScroll`). Its scroll
  view is named `Scroll View`, the name EpicLoot looks for before building its own scroll view into a tooltip, so
  EpicLoot leaves Auga's tooltip alone and does not size or place it. The scroll view is built once into a copy of
  each Auga tooltip prefab (under the inactive `Auga Tooltip Templates` object), and `UITooltip.OnHoverStart` points
  the `UITooltip`'s `m_tooltipPrefab` at that copy, so a mod that reads `m_tooltipPrefab` from a slot gets the copy.
  Each tooltip is still named after the prefab (`InventoryTooltip(Clone)`).
- A creature above level 3 gets Auga's own star row (`level_4` to `level_6`, then `level_X` with a count) only when
  neither Creature Level Control nor StarLevelSystem is installed (`Auga.HasLevelStarMod`): both draw those stars
  themselves, and Auga's would sit on top of theirs.

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
- Vanilla's row of round inventory buttons (`root/Info`: compendium, trophies, skills) is destroyed. Mods that clone
  one of those buttons or place theirs next to them (Clans before 1.1.0, Path of Valheim Night's loot filter,
  TalentForge, Praetoris' server guide) find nothing. `API.PlayerPanel_AddLink` is the supported replacement: the
  mod's button goes in the tab row under the player name.

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
| Item Favorite Framework | `MidnightsFX.ItemFavoriteFramework` | an `InventoryGrid.UpdateGui` postfix hangs its frames, trash veil, mark icon and wishlist heart on each `InventoryElement`; postfixes on `ItemData.GetTooltip` add its tooltip lines | tested 2026-10-29 (`AUGA_TEST_FAVORITES`): marks draw on Auga's player and container slots; their tooltip lines needed the two `ComplexTooltip` fixes above; a custom mark's icon covers the hotbar number, as in vanilla |
| Clans | `MidnightsFX.Clans` | API mirror (`Compat/AugaAPI.cs`): registers its Clan and activity tracker buttons with `PlayerPanel_AddLink`; leaves Auga's PvP toggle in the PvP tab | tested 2026-10-30 (`AUGA_TEST_LINKS=Clans_Button`): the Clan button sits in the tab row, shows its tooltip and opens the Clans window above Auga's panels; the tracker button stays hidden with no activity under way (its menu was not opened) |
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
| StarLevelSystem | `MidnightsFX.StarLevelSystem` | clones the enemy HUD's `level_2/star` for its star rows, restacks boss bars, transpiles `EnemyHud.UpdateHuds` | its `EnemyHud.Awake` postfix threw on Auga's star and left no enemy health bars (Player.log, 2026-10-29); vanilla star names fixed the same day, built, not yet run in game. Since the 1.4.0 bar restyle (`EnemyHudBars.cs`) SLS's boss bar sizing (`SetBossBarWidth`: the `bar` sizes, `bkg`/`darken` moved into `Background`) breaks the slanted boss bar; SLS is being changed to leave the enemy HUD to Auga and expose an API, see below |
| MinimalStatusEffects | `randyknapp.mods.minimalstatuseffects` | postfix on `Hud.UpdateStatusEffects`, lays out vanilla's entries | inert under Auga: the prefix skips vanilla, Auga's own list is drawn by `AugaStatusEffects` |
| Cheb's Necromancy / Mercenaries | | clone key hint rows | untested; issue #204, the key hint watcher was built for this |
| Groups / Guilds | `org.bepinex.plugins.groups` | chat | untested; issue #226 (chat error) |
| VNEI | `com.maxsch.valheim.vnei` | panel beside the inventory | untested |
| Valheim Plus | `org.bepinex.plugins.valheim_plus` | HUD and inventory options | untested; issue #159 (main menu) |
| MyLittleUI | `shudnal.MyLittleUI` | declares Auga incompatible on its side (still in 1.2.33) | BepInEx skips it; 1.2.33 read against the port without the tag on 2026-10-29, plan below |
| AzuExtendedPlayerInventory, ExtraSlots, Almanac | `Azumatt.AzuExtendedPlayerInventory`, `shudnal.ExtraSlots` | declare Auga incompatible on their side | BepInEx skips them when Auga is installed (issues #223, #228 are moot) |

## Improvements to make

### EpicLoot

1. **Refresh the API mirror.** `EpicLoot/src/integrations/AugaAPI.cs` and `AugaAPI.Common.cs` were copied on
   2026-09-24. Since then `API.cs` gained `GetBoldFontAsset`, `GetSemiBoldFontAsset`, `GetRegularFontAsset`,
   `Panel_Restyle`, `Text_Restyle`, `Scrollbar_Restyle`, `Slider_Restyle` and `Toggle_Restyle` (`844c8a0`), and the
   mirror never had `GetCraftingControls` or the `CraftingControls` class from 1.6.0. The API version is now 1.7.0
   (`AugaAPI/Properties/AssemblyInfo.cs`), which also added the `PlayerPanel_*Link` methods; rebuild `AugaAPI.dll`,
   then copy the signatures over. `EpicLootAuga.FixFonts` only restyles legacy `Text`; the TMP labels on the enchanting table,
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

### MyLittleUI

MyLittleUI has tagged Auga incompatible since its first week (March 2024), and 1.2.33 still does, so BepInEx skips
it and its players lose all of it with one line in the log. Only its author, shudnal, can drop the tag. MyLittleUI
already meets ZenUI and AAA Crafting with `Chainloader.PluginInfos` flags that switch off its overlapping features
(`CraftFilter.cs:31`, `MultiCraft.cs:52`), which is the model to propose for Auga.

1.2.33 was read against this port on 2026-10-29 as if the tag were gone; nothing was run. Most of it would work next
to Auga: the station, plant and creature hover timers, chest names and contents, the clock, ammo and bait icons on
the hotbar, the radial menu hints, hold to repair and hold to fuel, item links in chat, available amounts on
requirements and the "new" recipe mark. The rest:

- **One exception.** The HUD weight and free-slots block clones the `weight_icon` beside `InventoryGui.m_weight`
  (`InventoryPanel.cs:98`). Auga's player panel calls it `Weight/Icon`, so `Instantiate(null)` throws in `Hud.Awake`.
  `UiAwakeGuards` swallows it, but every `Hud.Awake` postfix ordered after it is skipped: MyLittleUI's hover text
  width, Auga's SearsCatalog postfix and any other mod's.
- **Misplaced.** The craft filter field and the sort panels are parented the vanilla way (the recipe scroll's
  grandparent, `m_repairPanel`), which under Auga is the whole right panel and the `DefaultContent` a station hides.
  The queued multicraft clones the craft button into Auga's `BottomBar` layout group, which collapses its +/-
  buttons, and its count looks for a `CraftProgress/Text` that Auga does not have. The weather and wind forecasts
  sit over the bottom of Auga's minimap and lose their background. The armor and weight tooltips anchor to the
  container grid, so they only show while a container is open. Quality stars spill over Auga's quality badge. The
  character-select stats go into `m_csSourceInfo`, whose panel Auga hides when cloud saves are on.
- **Inert, and harmless.** The status effect layout and sailing indicator (Auga prefixes `Hud.UpdateStatusEffects`
  out and places both itself), tooltip reordering (ComplexTooltip builds its own sections) and durability colours
  (BetterDurabilityBar repaints the visible bar every frame).

On Auga's side, each of these also helps other mods. Steps 1 to 3 are done (2026-10-29, built, not yet run in game):

1. **Vanilla names.** Done in `PlayerInventory_Setup`: both inventory panels call their weight icon `weight_icon`,
   and the "Crafting..." label moves out of `CraftProgressButton` to `CraftProgress/Text`, over the same rect and
   letting clicks through to the cancel button. Its `$inventory_craftingprog` text is now set in code, because the
   prefab's override of it points at a component `ButtonFancy` no longer has; until now it probably read "Label".
2. **Log what the guards swallow.** Done: `UiAwakeGuards` writes a `[Auga]` warning with the exception through
   `Debug.LogWarning`, whatever the logging settings, so a mod that fails in `Hud.Awake` is no longer silent.
3. **Character select.** Done: the source info panel shows whenever its text is not empty (checked in
   `AugaCharacterSelect.LateUpdate`), Auga's `FejdStartup.UpdateCharacterList` postfix runs at `Priority.First` and
   writes the save notices even while the panel is hidden, and showing the panel no longer clears that text. A
   legacy save's notice now shows with cloud saves on too, as in vanilla.
4. **Test before asking.** Build MyLittleUI from `../OtherModders/MyLittleUI` (public domain) with the tag removed,
   put it in a test profile next to Auga, and run the smoke test plus the crafting tab and character select by hand.
   Check Player.log for the `Hud.Awake` exception and the screenshots for the top right of the HUD and the crafting
   tab.

On MyLittleUI's side, to propose to shudnal with those results:

5. Replace the tag with an `Auga` flag beside `ZenUI` and `AAA_Crafting` that turns off what Auga draws itself: the
   status effect layout and sailing indicator, tooltip reordering, durability colours and quality symbols.
6. Under Auga, drive Auga's own +/- buttons and amount through `Auga.API.GetCraftingControls()` (API 1.6.0), as
   Auga's compat for maximods MultiCraft does (`Auga/Compat/MultiCraft.cs`), instead of cloning the craft button.
7. Under Auga, parent the craft filter and sort panels inside the crafting tab (`m_recipeListRoot.parent` is its left
   column), anchor the stats tooltips to the player grid, place the forecasts below the minimap and null-check the
   `weight_icon` lookup. Item link tooltips can use `Auga.API.Tooltip_MakeItemTooltip` for Auga's item layout.

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
- **StarLevelSystem:** under Auga, SLS should leave the enemy HUD's layout alone (no boss bar sizing or stacking,
  no per-frame bar rect edits) and offer an API from which Auga draws its levels, modifier icons and evolve bar
  (started 2026-10-29 in the SLS repository); Auga then reads it from a class in `Auga/Compat/`. Auga's stars sit
  below the bar by default (`[EnemyHud] StarsBelowHealthBar`), and SLS's own star rows should do the same.
- **Star rows above level 3 with SLS:** Auga leaves those stars to SLS and Creature Level Control. Once the SLS API
  exists, decide whether Auga draws SLS's levels in its own rows instead.
- **README:** the Thunderstore page's compatibility list predates the port and names none of the mods that refuse to
  load next to Auga (MyLittleUI, ExtraSlots, AzuExtendedPlayerInventory, Almanac, and the ones Auga tags). Add them,
  and the tested rows as they are confirmed on 1.x.

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
