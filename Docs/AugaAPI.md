# Auga API

Auga's API lets another mod check whether Auga is installed, build widgets in Auga's style, add to Auga's tooltips,
add tabs and links to the player panel, add tabs to the workbench, and give its own panels the Auga look. Everything
is static on `Auga.API`, in [`Auga/API/API.cs`](../Auga/API/API.cs); the types it hands out are in
[`Auga/API/API.Common.cs`](../Auga/API/API.Common.cs). The API version is 1.7.0.

## Setting up your mod

Your mod carries a stub copy of the API. In the stub every method does nothing and returns `null` or `false`, and
`IsLoaded()` returns `false`. When Auga is installed, it redirects your calls on the stub to the real methods while
your mod loads. Calling the API is therefore safe whether or not Auga is installed.

1. **Put the stub inside your own assembly.** Either compile the two API files into your mod with the `API` constant
   defined, as [`AugaApiExample`](../AugaApiExample/AugaApiExample.csproj) does:

   ```xml
   <PropertyGroup>
     <DefineConstants>$(DefineConstants);API</DefineConstants>
   </PropertyGroup>
   <ItemGroup>
     <Compile Include="path\to\Auga\API\API.cs" Link="AugaAPI\API.cs" />
     <Compile Include="path\to\Auga\API\API.Common.cs" Link="AugaAPI\API.Common.cs" />
   </ItemGroup>
   ```

   or reference `AugaAPI.dll` (built by the `AugaAPI` project in this repository) and merge it into your mod with
   ILRepack and `Internalize`. Never ship `AugaAPI.dll` as a file of its own. The stub compiles against the game's
   assemblies (Unity UI, TextMeshPro and `gui_framework` among them), which JotunnLib's references cover.
2. **Declare a soft dependency on Auga.** This is required: Auga only redirects the calls of mods that list its GUID.

   ```csharp
   [BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
   ```

3. **Check `Auga.API.IsLoaded()` before you call anything else.**

The player panel, workbench, requirements panel and crafting calls go through Auga's inventory screen, which only
exists once `InventoryGui.Awake` has run; before that they return `null` or do nothing. Make those calls from an
`InventoryGui.Awake` postfix:

```csharp
[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
public static class InventoryGui_Awake_Patch
{
    public static void Postfix()
    {
        if (!Auga.API.IsLoaded() || Auga.API.PlayerPanel_HasTab("MyModTab"))
            return;

        var tab = Auga.API.PlayerPanel_AddTab("MyModTab", null, "My Mod", index => Debug.Log("My Mod tab selected"));
        var button = Auga.API.SmallButton_Create(tab.ContentGO.transform, "MyModButton", "Do something");
        button.onClick.AddListener(() => Debug.Log("Clicked"));
    }
}
```

Methods marked *new in 1.x* were added during the port to Valheim 1.x. When an older Auga without them is installed,
calls to them stay on the stub and do nothing.

## Colours

Hex strings for Auga's palette, for `ColorUtility.TryParseHtmlString` or rich text such as `<color=#EAA800>`.

| Field | Value | Field | Value |
| --- | --- | --- | --- |
| `Brown1` | `#EAE1D9` | `Gold` | `#B98A12` |
| `Brown2` | `#D1C9C2` | `BrightGold` | `#EAA800` |
| `Brown3` | `#A39689` | `BrightestGold` | `#FFBF1B` |
| `Brown4` | `#706457` | `GoldDark` | `#755608` |
| `Brown5` | `#2E2620` | `Blue` | `#216388` |
| `Brown6` | `#EAE1D9` | `LightBlue` | `#1AACEF` |
| `Brown7` | `#181410` | `Green` | `#1B9B37` |
| `Red` | `#AD1616` | `RedText` | `#CD2121` |

## Fonts and sprites

| Method | Returns |
| --- | --- |
| `GetBoldFontAsset()`, `GetSemiBoldFontAsset()`, `GetRegularFontAsset()` | Source Sans Pro as a `TMP_FontAsset`. Valheim's UI is TextMeshPro, so use these. *New in 1.x.* |
| `GetBoldFont()`, `GetSemiBoldFont()`, `GetRegularFont()` | The same fonts as legacy `UnityEngine.Font`s, for `UnityEngine.UI.Text` |
| `GetItemBackgroundSprite()` | The background sprite of Auga's item slots |

## Restyling your own panels

*New in 1.x.* These give a panel your mod built from vanilla parts the Auga look, without rebuilding it from Auga
widgets. Texts, sliders and toggles are restyled in place. `Panel_Restyle` replaces buttons and scrollbars with new
Auga ones: it carries their listeners over and re-points the references held by components under the same root
object, but a reference kept anywhere else, such as a static field, still points at the destroyed original, so look
those up again afterwards.

| Method | What it does |
| --- | --- |
| `Panel_Restyle(GameObject panel, bool replaceBackground = true, bool replaceButtons = true, bool replaceScrollbars = true, bool restyleTexts = true)` | Restyles a whole panel: the background becomes an Auga panel, buttons become Auga buttons (keeping their listeners, labels and gamepad hints), scrollbars, sliders and toggles take the Auga look, and texts get Auga's fonts and colours. Turn off the parts you want to keep. |
| `Text_Restyle(TMP_Text text, bool bold = false, string colorHex = null)` | Auga's bold or regular font on one text, and a colour when one is given |
| `Scrollbar_Restyle(Scrollbar scrollbar, bool vertical = true)` | Auga's scrollbar look |
| `Slider_Restyle(Slider slider)` | Auga's slider look |
| `Toggle_Restyle(Toggle toggle)` | Auga's checkbox look |

## Panels, buttons and dividers

Labels are localized, so `$` tokens work.

| Method | What it does |
| --- | --- |
| `Panel_Create(Transform parent, Vector2 size, string name, bool withCornerDecoration)` | An Auga panel of the given size, centred in `parent` |
| `SmallButton_Create`, `MediumButton_Create`, `FancyButton_Create`, `SettingsButton_Create` `(Transform parent, string name, string labelText)` | An Auga button with a label. Returns the `Button`. |
| `DiamondButton_Create(Transform parent, string name, Sprite icon)` | A diamond-shaped icon button. A `null` icon leaves it empty. |
| `Divider_CreateSmall(Transform parent, string name, float width = -1)` | A divider line. A negative width keeps the prefab's width. |
| `Divider_CreateMedium`, `Divider_CreateLarge` `(Transform parent, string name, float width = -1)` | A heading divider. Returns `(divider, content)`; put your heading text under `content`. |
| `Button_SetTextColors(Button button, Color normal, Color highlighted, Color pressed, Color selected, Color disabled, Color baseTextColor)` | The label colour of an Auga button for each of its states |
| `Button_OverrideTextColor(Button button, Color color)` | One fixed label colour, replacing the per-state colours |

## Tooltips

### Adding an Auga tooltip to an object

| Method | What it does |
| --- | --- |
| `Tooltip_MakeSimpleTooltip(GameObject obj)` | A plain text tooltip. Set its text with vanilla's `UITooltip.Set(topic, text)` on the same object. |
| `Tooltip_MakeItemTooltip(GameObject obj, ItemDrop.ItemData item)` | Auga's full item tooltip |
| `Tooltip_MakeFoodTooltip(GameObject obj, Player.Food food)` | Auga's food tooltip |
| `Tooltip_MakeStatusEffectTooltip(GameObject obj, StatusEffect statusEffect)` | Auga's status effect tooltip |
| `Tooltip_MakeSkillTooltip(GameObject obj, Skills.Skill skill)` | Auga's skill tooltip |

### Adding to Auga's tooltips

Auga's item, food, status effect and skill tooltips are complex tooltips: a header (icon, topic, subtitle,
description) above a list of text boxes. A listener runs each time Auga fills one, wherever it appears, and receives
the tooltip's `GameObject`. Pass that object to the `ComplexTooltip_*` methods below.

```csharp
Auga.API.ComplexTooltip_AddItemTooltipCreatedListener((tooltip, item) =>
{
    var box = Auga.API.ComplexTooltip_AddTwoColumnTextBox(tooltip);
    Auga.API.TooltipTextBox_AddLine(box, "$mymod_bonus", "+10%");
});
```

| Method | What it does |
| --- | --- |
| `ComplexTooltip_AddItemTooltipCreatedListener(Action<GameObject, ItemDrop.ItemData>)` | Runs after Auga fills an item tooltip, each time the item it shows changes |
| `ComplexTooltip_AddFoodTooltipCreatedListener(Action<GameObject, Player.Food>)` | The same for food |
| `ComplexTooltip_AddStatusEffectTooltipCreatedListener(Action<GameObject, StatusEffect>)` | The same for status effects |
| `ComplexTooltip_AddSkillTooltipCreatedListener(Action<GameObject, Skills.Skill>)` | The same for skills |
| `ComplexTooltip_AddItemStatPreprocessor(Func<ItemDrop.ItemData, string, string, Tuple<string, string>>)` | Rewrites the stat lines of item tooltips (see below) |

`ComplexTooltip_AddItemStatPreprocessor` is called with `(item, label, value)` for every stat line Auga writes into
an item tooltip; `value` is `null` for a line with no value. Return a new `Tuple<string, string>(label, value)` to
change the line, the same values to keep it, or `null` to hide it. Preprocessors from every mod run in the order they
were added, each on the previous one's result; once one returns `null`, the rest are skipped.

### Working on a complex tooltip

| Method | What it does |
| --- | --- |
| `ComplexTooltip_ClearTextBoxes(GameObject tooltip)` | Removes every text box and divider below the header |
| `ComplexTooltip_AddTwoColumnTextBox(GameObject tooltip)` | Adds a box of label and value lines. Returns the box. |
| `ComplexTooltip_AddCenteredTextBox(GameObject tooltip)` | Adds a box of centred lines |
| `ComplexTooltip_AddUpgradeLabels`, `ComplexTooltip_AddUpgradeTwoColumnTextBox`, `ComplexTooltip_AddCheckBoxTextBox` `(GameObject tooltip)` | Adds the boxes the workbench uses for upgrades and checklists. Only tooltips whose prefab has these templates support them. |
| `ComplexTooltip_AddDivider(GameObject tooltip)` | Adds a divider line |
| `ComplexTooltip_SetTopic(GameObject tooltip, string topic)` | The title |
| `ComplexTooltip_SetSubtitle(GameObject tooltip, string subtitle)` | The line under the title; empty hides it |
| `ComplexTooltip_SetDescription(GameObject tooltip, string desc)` | The description; empty hides it |
| `ComplexTooltip_EnableDescription(GameObject tooltip, bool enabled)` | Shows or hides the description and its divider |
| `ComplexTooltip_SetIcon(GameObject tooltip, Sprite icon)` | The icon |
| `ComplexTooltip_GenerateItemSubtext(GameObject tooltip, ItemDrop.ItemData item)` | The subtitle Auga writes under an item's name, as unlocalized `$` tokens |
| `ComplexTooltip_SetItem(GameObject tooltip, ItemDrop.ItemData item, int quality = -1, int variant = -1)` | Fills the whole tooltip for an item, as Auga does. `-1` means the item's own quality or variant. |
| `ComplexTooltip_SetItemNoTextBoxes(GameObject tooltip, ItemDrop.ItemData item, int quality = -1, int variant = -1)` | Fills only the header, so you can add your own boxes |
| `ComplexTooltip_SetFood`, `ComplexTooltip_SetStatusEffect`, `ComplexTooltip_SetSkill` | Fill the whole tooltip for a food, status effect or skill |

### Writing into a text box

Each call adds a line to a box returned by one of the `ComplexTooltip_Add*` methods. Values can be any object and are
converted with `ToString()`. With `localize` on, the default, `$` tokens are translated.

| Method | Adds |
| --- | --- |
| `TooltipTextBox_AddLine(GameObject box, object a, bool localize = true)` | A line of text |
| `TooltipTextBox_AddLine(GameObject box, object a, object b, bool localize = true)` | A label and a value |
| `TooltipTextBox_AddLine(GameObject box, object a, object b, object parenthetical, bool localize = true)` | A label and a value followed by a grey `(parenthetical)` |
| `TooltipTextBox_AddUpgradeLine(GameObject box, object label, object value1, object value2, string color2, bool localize = true)` | A label, the current value and the upgraded value, the last in `color2` (a hex string) when given |
| `TooltipTextBox_AddLine(GameObject box, Text t, object s, bool localize = true)` | A line in one particular text of the box. Kept for older mods: `t` is matched to the TextMeshPro text on the same object, or else the box's main text. |

Every one of these also has an overload that ends in `bool localize, bool overwrite`. With `overwrite` set, the line
replaces the box's text instead of being added below it.

## Player panel tabs

| Method | What it does |
| --- | --- |
| `PlayerPanel_AddTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected)` | Adds a tab to the player panel. Returns a `PlayerPanelTabData`, or `null` before `InventoryGui.Awake` or when the ID is taken. |
| `PlayerPanel_HasTab(string tabID)` | Whether a tab with that ID exists |
| `PlayerPanel_IsTabActive(GameObject tabButton)` | Whether that tab is the selected one |
| `PlayerPanel_GetTabButton(int index)` | The button of the tab at that index |

The tab ID becomes the name of the tab's button and must be unique. Adding an ID that already exists adds nothing and
returns `null`. Auga 1.3.12 and older throw instead, so check `PlayerPanel_HasTab` first if your mod supports them.
The title is localized; a `null` icon keeps the default one. `onTabSelected` receives the
tab's index each time the tab is selected. Put your UI under `ContentGO`, an empty 564 × 780 object that is shown
while the tab is selected.

## Player panel links

Vanilla's inventory has a row of round buttons (compendium, trophies, skills, PvP) that many mods add their own
button to. Auga replaces that row with the row of diamond buttons under the player name, the player panel's tab row.
A link is a button in that row that calls your action when clicked instead of opening a tab, such as Clans' button
that opens its window. (API 1.7.0)

| Method | What it does |
| --- | --- |
| `PlayerPanel_AddLink(string linkID, Sprite icon, string tooltip, Action onClick)` | Adds a link, or updates the icon, tooltip and action of the link with that ID. Returns `false` only for an empty ID, and on the stub. |
| `PlayerPanel_HasLink(string linkID)` | Whether a link with that ID is registered |
| `PlayerPanel_SetLinkVisible(string linkID, bool visible)` | Shows or hides the link. Returns `false` when there is no such link. |
| `PlayerPanel_RemoveLink(string linkID)` | Removes the link |
| `PlayerPanel_GetLink(string linkID)` | The link's button in the current inventory screen, or `null` before that screen exists. Use it to place a popup next to the link. |

Auga keeps your links and builds them into every inventory screen (the game makes a new one for each world), so
register once, at any time after Auga has loaded: in your plugin's `Awake` or in an `InventoryGui.Awake` postfix.
Adding an ID again only updates it and keeps its visibility, so registering from each `InventoryGui.Awake` is
harmless. A new link is visible. Links always come after the tabs, in the order they were first added.

The icon is fitted inside the diamond and tinted with the tab icons' colours, so a light glyph on a transparent
background looks best; a `null` icon keeps the default one. The tooltip is localized when it is shown. Like the tabs, the row is hidden
while the player stands at a crafting station. A link that throws is logged with its ID.

```csharp
[BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
public class MyMod : BaseUnityPlugin
{
    private void Awake()
    {
        if (Auga.API.IsLoaded())
            Auga.API.PlayerPanel_AddLink("MyMod_Window", myIcon, "$mymod_open_window", MyWindow.Toggle);
    }
}
```

## Workbench tabs

| Method | What it does |
| --- | --- |
| `Workbench_AddWorkbenchTab(string tabID, Sprite tabIcon, string tabTitleText, Action<int> onTabSelected)` | Adds a tab to the workbench panel. Returns a `WorkbenchTabData`, or `null` before `InventoryGui.Awake` or when the ID is taken. |
| `Workbench_AddVanillaWorkbenchTab(...)` | The same, for a tab driven by vanilla's recipe list: while it is selected, the item info shows vanilla's recipe name and description. SimpleRecycling's RECYCLE tab works this way. |
| `Workbench_HasWorkbenchTab(string tabID)` | Whether a tab with that ID exists |
| `Workbench_IsTabActive(GameObject tabButton)` | Whether that tab is the selected one |
| `Workbench_GetCraftingTabButton()`, `Workbench_GetUpgradeTabButton()` | The buttons of the built-in Craft and Upgrade tabs |
| `Workbench_CreateNewResultsPanel()` | A new copy of the crafting results panel inside the crafting panel |

The tab ID rules are the same as for player panel tabs, with `Workbench_HasWorkbenchTab` for the check. Every added tab
shows the same generic requirements panel (`RequirementsPanelGO`) and the crafting panel's item info (`ItemInfoGO`,
a complex tooltip), so fill them when your tab is selected. Use the `RequirementsPanel_*` methods on the first and the
`ComplexTooltip_*` methods on the second.

### Requirements panel

| Method | What it does |
| --- | --- |
| `RequirementsPanel_GetIcon(GameObject panel)` | The `Image` that shows the item being made |
| `RequirementsPanel_RequirementList(GameObject panel)` | The requirement slots. While the panel is shown, vanilla's `InventoryGui.m_recipeRequirementList` points at them, and vanilla's `InventoryGui.SetupRequirement` can fill one. |
| `RequirementsPanel_SetWires(GameObject panel, RequirementWireState[] wireStates, bool canCraft)` | Colours the wires from the slots to the item: one state per slot (`Absent`, `Have` or `DontHave`), and the last wire as met when `canCraft` is true |

### Custom variant panel

The workbench has a button for choosing an item's style. A mod can put its own button there, which opens a dialog of
its own.

| Method | What it does |
| --- | --- |
| `CustomVariantPanel_Enable(string buttonLabel, Action<bool> onShow)` | Replaces the style button with yours. Clicking it opens or closes the dialog and calls `onShow` with whether it is now open. Returns the dialog's text, for you to fill. |
| `CustomVariantPanel_SetButtonLabel(string buttonLabel)` | Changes the button's label |
| `CustomVariantPanel_Disable()` | Closes the dialog and brings the style button back |

### Crafting controls

`GetCraftingControls()` returns the controls under the workbench's craft button, as a `CraftingControls`: the craft
button, the multicraft group with its plus and minus buttons, the amount text and its background, and the amount
input field. Its fields are `null` until the crafting panel exists; the stub returns `null` itself. (API 1.6.0)

## Types

| Type | Fields |
| --- | --- |
| `PlayerPanelTabData` | `Index`, `TabTitle` (`TMP_Text`), `TabButtonGO`, `ContentGO` |
| `WorkbenchTabData` | `Index`, `TabTitle` (`TMP_Text`), `TabButtonGO`, `RequirementsPanelGO`, `ItemInfoGO` |
| `CraftingControls` | `CraftButton`, `Multicraft`, `PlusButton`, `MinusButton`, `CraftAmountText`, `CraftAmountBG`, `Amount`, `InputAmount` (`GuiInputField`), `InputText` |
| `RequirementWireState` | `Absent`, `Have`, `DontHave` |

## Compatibility between versions

Your calls are matched to Auga by type name and method signature, so a signature in the API never changes once it is
published; new behaviour arrives as new methods or overloads. A mod built against an older stub keeps working with a
newer Auga. A mod built against a newer stub still loads with an older Auga, and the calls Auga does not have yet
stay on the stub.
