### 1.2.16
* Fixed Password Dialogue Box
* Fixed Console Issue
* Fixed Chat Input positioning issue
* Removed Blackbox from under Keybind in Hover Text's
* Added Support for Comfy's Chatter Mod
* Added Additional Support for Comfy's Sears Catalog
### 1.2.15
* Hildir's Request 0.217.14 Update
* Known Issue: The chat input box is in the middle of the box.  Minor issue. Not game breaking.
### 1.2.14
* Fixed the TextMeshPro Blurry Fonts (thanks to Azumatt).
* Put NPC Text back into a smaller box so that the text wraps appropriately.
* Fixed Outline around Biome Name
### 1.2.13
* Password dialogue now hides password.
* Auga API has been updated to allow TooltipTextBox AddLine to overwrite instead of add.
* Fixed (again) Enemy Nameplates to be clear.
* Added Outlines on some HUD TMP Text boxes that were missing
### 1.2.12
* Fixing Password, Portal, Signs, and Tamable Inputs
* Removed some left over debugging
### 1.2.11
* Mini-map pins were not working.
  * Now have mini-map pins working.
* Chat Window text now wraps
* NPC Dialog now wraps
### 1.2.10
* Updates Valheim 0.216.9
* Adds in additional fonts to hopfully fix blurry text on unit frames.
### 1.2.9
* Hotfix for Blurry Text
* Added in Chinese, Japanese, Korean, Russian, and other languages to fonts.
  * This should now make most languages appear correctly.
  * If you are still seeing boxes, please report that to the Discord.
### 1.2.8
* Build Menu has been rebuilt to work with other mods that add hammers/categories.
  * Any mod using Jotunn 2.11.4 or higher to add categories will now work in build menu
  * This includes Odin Architect and ValheimRaft to name a few.
* Added in Chinese, Japanese, Korean, Russian, and other languages to fonts.
  * This should now make most languages appear correctly.
  * If you are still seeing boxes, please report that to the Discord.
### 1.2.7
* Fixes random loading issues with camera and UI lock out.
* Completely redesigned how StoreGui is attached to Auga.
* Better Trader and Knarr the Trader both now work together
### 1.2.6
* Better Trader now loads fully, and has been tested for compatibilty.
* Knarr the Trader compatibility has been set.
  * Known Issue: Both Knarr and Better Trader currently don't work at the same time with Auga
* Additional tweaks to Build Menu Controller in order to support Jotunn and HammerTime Compatibility
### 1.2.5
* Build Menu now respects other mods changes to Categories
* Build menu now has pagination of categories when needed.
* Repair Icon is now activated and visible when in Debug/No Cost mode.
* Store Gui has been reconfigured to allow other mods to utilize the Store/Trader
### 1.2.4
* All Inventories now display Quality Diamond Correctly.
### 1.2.3
* Updating Map to show Pin Labels
* Updating Minimap Biome Label
* Updating Inventory to load item Quality Diamond correctly.
* Updating TextInput dialog boxes and providing Cancel and OK buttons
* Added Build Menu Toggle Configuration setting for turning off the Auga Build Menu
  * This is for mod compatibility where otherwise the build menu would break
  * Setting requires a game relog/restart.
### 1.2.2
* Fixed Chat Box
* Fixed resolution settings from resetting everytime settings are changed.
* All Player HUD Elements have been activated.
* Build Menu has been restored.
* Minimap has been restored.
* Enemy Hud Restored
* All Features of Auga should now be working.
### 1.2.1
* Now Compatible with 0.214.300.5 of Valheim (latest branch)
* 1.2.0 was one version behind and the latest version changed a field name breaking the Compendium.
* NOTE:
  * All Menu's, Compendium, Settings, Inventory, and Crafting Interactions SHOULD be working without error.
  * All HUD Elements, like status bars, have been disabled, and the vanilla versions should be displayed.
    * This is temporary as we update the rest of the mod.
### 1.2.0
* Initial Compatibility for Valheim 0.214.300 Update
* All Menu's, Compendium, Settings, Inventory, and Crafting Interactions SHOULD be working without error.
* All HUD Elements, like status bars, have been disabled, and the vanilla versions should be displayed.
    * This is temporary as we update the rest of the mod.
* Adding in DiamondButton to Asset Bundle
* Fixing Compendium Scroll Bar so that it will scroll all entries.
### 1.1.3
  * Hotfix for new settings from new Valheim version
  * Fix for store item tooltips
### 1.1.2
  * Fixed an issue with Lore Compendium not populating
  * Build Hud, Selected Piece, Top Left Message, Center Message, and Chat are all movable
  * Eitr stats correctly visible in tooltips
  * Upgrade item icon correctly displays on the crafting panel
  * Added two new loading screen art pieces from the official Valheim press kit
  * Minor scrollbar fixes in Compendium and Crafting panel
### 1.1.1
  * Fixed animating pause menu buttons
  * All HUD elements are now freely movable and scalable, use the config
  * Health bars are customizable: fixed size, text position, text display options
### 1.1.0
  * Updated for Mistlands!
  * Mistlands specific UI and tooltips added
  * Compatibility with Simple Recycling Fixed by remmiz
### 1.0.12
  * Temporary fix for Valheim v0.211 - disabled auga in main menu until I have time to build the new save management menus
  * Settings menu fixed
### 1.0.11
  * Hotfix for Valheim v0.209.8
### 1.0.10
  * Hotfix for Valheim v0.208.1
### 1.0.9
  * Fixed bugs with ZInput preventing Auga from running with the new Valheim update
  * Restored the vanilla logo in the main menu
  * Added support for [MultiCraft](https://www.nexusmods.com/valheim/mods/263)!
### 1.0.8
  * Fixed Minimap/Map
  * Fixed Settings
  * Fixed issue with custom build menus (Odin's Architect, Clutter, Buildit, Planit)
  * Added ComplexTooltip callback to crafting menu
  * Updated API with callbacks for food, status effect, and skill tooltips as well 
### 1.0.7
  * Fixed issue with tower shield tooltips
  * Fixed cartography table map issue
  * Fixed various screen alignment/resolution issues
### 1.0.6
  * Reupload with correct files
### 1.0.5
  * Fixed build HUD selector
  * Fixed some screen alignment/resolution issues
  * Hooked up Last IP Joined
### 1.0.4
  * Updated for H&H
  * Implemented Auga-style Stagger Bar
### 1.0.3
  * BetterTrader bugfix
  * Extended Item Data Framework compatibility (please update EIDF to 1.0.8)
  * Added BuildExpansion-like support
  * Fixed white square on store buy button
  * Added support for more skills on the skills page
  * Added trash support (like TrashItems), enable it in the config
### 1.0.2
  * Fixed overlapping names and health bars for enemies when using CLLC
### 1.0.1
  * Valheim+ Compatibility