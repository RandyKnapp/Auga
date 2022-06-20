# Project Auga
##### by RandyKnapp / n4

Project Auga is a completely re-imagined, modder-friendly UI-overhaul for Valheim. Every last piece of UI was considered and reworked from the ground-up to create a more helpful and immersive player experience, all while remaining familiar to Valheim veterans.

Join the Discord: [Project Auga Discord﻿](https://discord.gg/ZNhYeavv3C)
Visit the Website: [ProjectAuga.com](https://projectauga.com/)

## What's Changed?

Basically everything:
  * New Player HUD
  * Redesigned Player & Container inventories
  * New Consolidated Player Panels
  * Improved Crafting Panels
  * Expanded Character Select & New Character Screens
  * Overhauled Loading Screens
  * Auga-Style EVERYTHING

SEE SCREENSHOTS HERE: https://github.com/RandyKnapp/Auga/tree/main/Auga/Screenshots

## How to Install

### GitHub/Nexus Mods
  1. Install [BepInEx for Valheim﻿](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
  1. Download the zip file from the Nexus or the GitHub Release
  1. Extract the contents of the zip file to <Your Valheim Installation Directory>\BepInEx\plugins\Auga

### Thunderstore
  1. Install [BepInEx for Valheim﻿](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
  1. Download the zip file from here on Thunderstore
  1. Install with your mod manager
  1. OR - Extract the contents of the `files` folder inside the zip file to <Your Valheim Installation Directory>\BepInEx\plugins\Auga

## Mod Compatibility

Does it work with...

  * Controller: **LIMITED** (using a controller is not supported in all menus yet)
  * EpicLoot: **YES**
  * Equipment & Quick Slots: **YES**
  * Valheim+: **YES (LIMITED)** (Not all features work, please set the following config options or the HUD will break:)
	* displayStaminaValue = false
	* displayBowAmmoCounts = 0
	* Changing inventory size works, but it is slightly too small and always scrolls
	* GameClock: enabled = false
  * [JotunnBackpacks](https://www.nexusmods.com/valheim/mods/1416): **YES**
  * Better Wards: **YES**
  * Creature Level & Loot Control: **YES**
  * BetterTrader: **YES**
  * BuildExpansion: **NO (but kinda YES)** (But it now supports extra build pieces, BuildExpansion is no longer required when using Auga)
  * _Message me if your mod is compatible, I'll add it to this list! - RandyKnapp_

Project Auga drastically changes many parts of the Valheim UI. It will most likely not be compatible with other mods that modify the UI.

Please report bugs and mod conflicts on the [GitHub Issues Page](https://github.com/RandyKnapp/Auga/issues)﻿!

## For Modders

Project Auga comes with an API that allows other mods to easily access its features and create UI elements in the Auga style. It's also open-source on GitHub.

Auga API: https://github.com/RandyKnapp/Auga/wiki/Auga-API
Source: https://github.com/RandyKnapp/Auga

## Changelog

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