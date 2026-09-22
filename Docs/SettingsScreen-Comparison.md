# Settings screen: Auga prefab vs Valheim 1.0.15

Comparison of Auga's `AugaSettings` prefab with the vanilla settings screen, as a guide for rebuilding the Auga
prefab for the tabbed settings.

Sources (all captured on 2026-09-21 against Valheim 1.0.15):

* vanilla `Settings` prefab hierarchy dumped in game, plus every label with its localization token
* the decompiled `Valheim.SettingsGui.*` tab classes (the serialized fields each tab drives)
* Auga's built `AugaSettings` prefab as it is in the shipped `augaassets` bundle
* the Unity source `AugaUnity/Assets/Prefabs/AugaSettings.prefab`, which is already mid-edit (see the end)

Legend used in the tables: **present** = Auga has an equivalent control on the same tab; **moved** = Auga has it,
vanilla put it on another tab; **missing** = vanilla has it, Auga has nothing for it; **removed** = Auga has it,
vanilla no longer does; **changed** = both have it but the control type differs.

## 1. Screen structure

| | Vanilla 1.0.15 | Auga (bundle) |
| --- | --- | --- |
| Root | `Settings` [Settings, UIGroupHandler, Localize, RadialDataInitializer], a root canvas of its own | `AugaSettings` [Settings, UIGroupHandler, Localize], no Canvas |
| Frame | `Panel` with `Title` ($menu_settings), `HeaderLine`, `Back` and `Ok` (GuiButton + UIGamePad, key hints) | `panel` with art (Darken, Background), `PlayerPanelTitle`, `DividerSmall`, `Back` ($menu_back) and `Ok` ($settings_apply) as ColorButtonText buttons |
| Tabs | `TabButtons` [TabHandler, HorizontalLayoutGroup]: Gameplay, KeyboardMouse, Gamepad, Graphics, Audio, Accessibility. Each button has `Label`, `Selected/LabelSelected`, the first and last a gamepad `KeyHint` | `TabButtons` [HorizontalDividerFitter, TabHandler] / `Tabs`: Controls, Audio, Graphics, Misc |
| Tab pages | `TabContent`: KeyboardMouse, Gamepad, Graphics, Audio, Accessibility, Gameplay, RadialTab (7 pages; RadialTab has no tab button and is opened from Gameplay). Each page carries its own component (`KeyboardMouseSettings`, `GamepadSettings`, ...), a `UIGroupHandler`, mostly a `Localize` and a `SettingsTooltip` | `Tabs`: Controls, Audio, Graphics, Misc (4 pages, `UIGroupHandler` + `CanvasGroup` only, no tab components) |
| Dialogs | `KeyboardMouse/BindDialog` ($settings_presskey, $settings_clearkeyinfo), `Graphics/ResolutionDialog` (list of `ResListElement`), `Graphics/ResolutionSwitchDialog` (Yes/No GuiButtons, countdown) | `BindDialog` ($press_a_key), `SwitchDialog` (ConfirmDialog with ButtonYes, ButtonNo, Countdown), `DummyObjects` (DummyResDialog, DummyResElement, DummyScrollbar, DummyText) |

The biggest structural change: the vanilla `Settings` class no longer owns any control. It only holds
`m_settingsPanel`, `m_tabHandler`, `m_backButton`, `m_okButton` and `m_tabKeyHints`, and collects one `ISettingsTab`
per `TabHandler` page (`tab.m_page.GetComponent<ISettingsTab>()`). Every control lives in the page component. The
old Auga wiring (`Settings_Setup.cs` at the Vapok tip, which set `m_resButtonText`, `m_language`, `m_keys`,
`m_gamepadRoot`, `m_alternativeGlyphs` on `Settings`) has nothing to attach to anymore.

## 2. Where the old Auga tabs went

| Auga tab | Vanilla tab(s) that now hold its controls |
| --- | --- |
| Controls | KeyboardMouse (mouse, bindings) and Gamepad (everything controller related) |
| Audio | Audio (unchanged) |
| Graphics | Graphics (resolution, quality) and Accessibility (motion blur, depth of field are mirrored there) |
| Misc | Gameplay (language, tutorials, key hints, backups, background usage) and Accessibility (GUI scale, camera shake, ship camera, flashing lights); quick select went to KeyboardMouse |

## 3. Tab by tab

### 3.1 Gameplay (`GameplaySettings`)

| Vanilla row | Token | Field | Auga | Status |
| --- | --- | --- | --- | --- |
| Language stepper (`Language/Stepper`, GuiStepper) + `CommunityTranslation` text | $settings_language, $settings_langchange_community | `m_language` (TMP_Text), `m_communityTranslation` | Misc/WidgetsTop `Language` (UI Dropdown) | changed: dropdown became a left/right stepper; no community translation text |
| ToggleSprint | $settings_autorun | `m_toggleRun` | Controls `ToggleAutoRun` ($settings_autorun) | moved |
| ToggleAttackTowardsLookDir | $settings_toggle_attack_towards_look_direction | `m_toggleAttackTowardsPlayerLookDir` | none | missing |
| KeyHints | $settings_showkeyhints | `m_showKeyHints` | Misc/WidgetsRight `ShowKeyHints` | present |
| Tutorial/Tutorial | $settings_tutorialsenabled | `m_tutorialsEnabled` | Misc/WidgetsRight `ShowTutorials` | present |
| Tutorial/Reset | $menu_resettutorial | `m_resetTutorial` | Misc/WidgetsRight `ResetTutorialsButton` | present |
| ReduceBackgroundUsage | $settings_reducebg | `m_reduceBGUsage` | Misc/WidgetsLeft `ReduceBackgroundPerformance` | present |
| EnableConsole | $settings_console | `m_enableConsole` | none | missing |
| EnableShowBuildPieceAuthor | $settings_enable_show_build_piece_author | `m_showBuildPieceAuthor` | none | missing |
| Skip intro cinematic | $settings_skip_intro_cinematic | `m_skipIntroCinematic` | none | missing |
| AutoBackups slider + `CloudStorageWarning` text | $settings_autobackup, $settings_cloud_storage_warning | `m_autoBackups`, `m_autoBackupsText`, `m_cloudStorageWarning` | Misc/WidgetsLeft `AutobackupsSlider` | present (no warning text) |
| BottomButtons/DeletePlayfabAccount | $settings_deleteplayfabaccount | `m_deleteAccount` | none | missing |
| BottomButtons/Radial Settings | $settings_radial | `m_radialSettingsButton` | none | missing (opens the Radial page) |
| BottomButtons/Blocked Player List | $settings_blocked_player_list | `m_blockedPlayerList`, `m_playerListPrefab` | none | missing |

Auga controls on Misc that belong elsewhere now: `GuiScale`, `CameraShake`, `ImmersiveShipCamera`,
`ReduceFlashingLights` (Accessibility), `RightClickBuildSelection` (KeyboardMouse), `RenderScale` (Graphics, see
3.4).

### 3.2 Keyboard / Mouse (`KeyboardMouseSettings`)

| Vanilla row | Token | Field | Auga (Controls) | Status |
| --- | --- | --- | --- | --- |
| MouseSensitivity slider | $settings_mousesens | `m_mouseSensitivitySlider`, `m_mouseSensitivityText` | `MouseSensitivity` | present (no value text) |
| Settings/InvertMouse | $settings_invertmouse | `m_invertMouse` | `InvertMouse` | present |
| Settings/QuickPieceSelect | $settings_quickselect | `m_quickPieceSelect` | Misc `RightClickBuildSelection` | moved |
| Settings/ResetControls | $settings_resetcontrols | (onClick) | `ResetControlsButton` | present |
| Bindings scroll list | | `m_keys` (List of `KeySetting`), `m_scrollRectVisibilityManager` | `ItemListBkg/.../KeyBindings` | present, see rows below |
| BindDialog | $settings_presskey, $settings_clearkeyinfo | `m_bindDialog` | root `BindDialog` ($press_a_key) | present; vanilla adds the "clear key" hint |
| gamepad navigation helpers | | `m_bottomLeftKeyButton`, `m_bottomRightKeyButton` | none | missing (gamepad only) |

Binding rows. Vanilla lists 40 `KeySetting` entries (each row: `Label` + `Button` with GuiButton, the key text is
written into the button). Auga has 26 rows built from `LabeledKeybind` (an `AugaBindingDisplay` per row; Forward/Left
and Backward/Right share a row).

| Vanilla key | Token | Auga row |
| --- | --- | --- |
| Attack, SecondaryAttack, Block, Use, Jump, Run, Crouch, Sit | $settings_attack ... | same names |
| Walk | $settings_walk | ToggleWalk |
| Hide | $settings_hide | HideShowWeapons |
| GP | $settings_gp | ForsakenPower |
| AutoRun | $settings_autorun: | AutoRun |
| AutoPickup | $settings_autopickup | ToggleAutoPickup |
| Forward, Left, Backward, Right | $settings_forward ... | Forward (+Left), Backward (+Right) |
| Inventory, Map, BuildMenu | | same names |
| ZoomOut, ZoomIn | $settings_zoomout, $settings_zoomin | MapZoomOut, MapZoomIn |
| Remove | $settings_remove | Deconstruct |
| AltPlace | $settings_altplace | AltPlacement ($settings_altplace_short) |
| BuildPrev, BuildNext | $settings_buildprev, $settings_buildnext | PrevBuildCategory, NextBuildCategory |
| AltDodge | $settings_altdodge | **missing** |
| EnableConsole | $settings_console | **missing** |
| Hotbar1 ... Hotbar8 | $settings_hotbar1 ... | **missing** (8 rows) |
| OpenRadialWheel | $settings_open_radial_wheel | **missing** |
| OpenEmoteWheel | $settings_open_emote_wheel | **missing** |
| CamZoomOut, CamZoomIn | $settings_camzoomout, $settings_camzoomin | **missing** (inactive in vanilla too) |

No Auga binding row was removed by vanilla.

### 3.3 Gamepad (`GamepadSettings`) - new tab

| Vanilla row | Token | Field | Auga (Controls) | Status |
| --- | --- | --- | --- | --- |
| Toggles1/GamepadEnabled | $settings_gamepadenabled | `m_gamepadEnabled` | `GamepadEnabled` | moved |
| Toggles1/SwapTriggers | $settings_swap_triggers | `m_swapTriggers` | `SwapTriggers` | moved |
| Toggles1/UseTriggerFeedback (inactive) | $settings_controller_use_adaptive_triggers | `m_useAdaptiveTriggers` | none | missing (console) |
| Toggles2/InvertCameraX, InvertCameraY | $settings_invert_camera_x/_y | `m_controllerInvertCameraX/Y` | none | missing |
| Toggles2/Glyphs (GuiDropdown Xbox/Playstation) | $settings_glyphs | `m_glyphs`, `m_glyphOptions` | `AlternativeGlyphs` toggle ($settings_glyphs_ps) | changed: toggle became a dropdown |
| Vibration/VibrationStrength slider | $settings_controller_vibration_strength | `m_vibrationStrengthSlider` + fill image, label, percent text | none | missing |
| MotionSensorX/Y, ControllerSpeakerVolume (inactive) | $settings_controller_motion_sensor_*, $settings_controller_speaker_volume | `m_motionSensitivity*`, `m_controllerSpeaker*` | none | missing (console) |
| ControllerSensitivity/GamepadSensitivity | $settings_controller_sensitivity | `m_gamepadSensitivitySlider`, `m_gamepadSensitivityText`, `m_gamepadSensitivityRoot` | `GamepadSensitivity` ($settings_joysens) | moved |
| InputLayout stepper (LabelLeft + GUIStepper) | $settings_controller_layout | `m_leftControllerLayoutButton`, `m_rightControllerLayoutButton`, `m_controllerLayoutText` | none | missing (Default / Alternative 1 / Alternative 2 layouts) |
| SubtabsGroup/Tabs (TabHandler): Controller, Mouse | $switch_settings_controller, $switch_settings_mouse_tab | `m_tabs`, `m_controllerSettingsRoot`, `m_mouseSettingsRoot` | none | missing |
| SwitchMouseSettings: SwitchMouseEnabled, InvertCameraY, MouseSensitivity, InputLayout | $switch_settings_* | `m_switchMouseEnabled`, `m_invertMouse`, `m_mouseSensitivitySlider/Text`, `m_mouseLayoutText`, `m_left/rightMouseLayoutButton` | none | missing (Switch mouse mode) |
| GamepadMap (controller diagram, `GamepadMapController`) | | driven by `Menu`/`GamepadMap` classes | `GamepadDiagram` / `GamepadImage` | present in spirit; the Auga diagram predates the layout stepper |

### 3.4 Graphics (`GraphicsSettings`)

| Vanilla row | Token | Field | Auga (Graphics) | Status |
| --- | --- | --- | --- | --- |
| Resolution/Anchor/Resolutions (GuiDropdown) | $settings_res | `m_resolutionRoot`, `m_resolutionDropdown` | `Resolution` (UnityEngine.UI Dropdown filled by old Auga code) | changed: GuiDropdown driven by the game |
| Resolution/Anchor/Fullscreen | $settings_fullscreen | `m_fullscreenToggle` | `Fullscreen` | present |
| Resolution/Anchor/Test | $settings_test | `m_testResolutionButton` (GuiButton) | `TestResolutionButton` | present |
| "Upscaled 3D resolutions": render scale + upscaling algorithm (two GuiDropdowns) | $settings_3dresolutionlimit, $settings_upscalingalgorithm | `m_upscalingOptionsRoot`, `m_renderScaleDropdown`, `m_upscalingAlgorithmDropdown` | Misc `RenderScale` slider ($settings_renderscale) | changed: slider became two dropdowns and moved to Graphics |
| FPSLimit slider | $settings_fpslimit | `m_fpsLimitSlider`, `m_fpsLimitText` | `FramerateLimit` | present |
| VSync | $settings_vsync | `m_vsyncToggle` | `VSYNC` | present |
| GraphicsMode stepper + description | $settings_graphics_preset | `m_graphicPresetsRoot`, `m_graphicsMode`, `m_graphicPresetLeft/Right`, `m_graphicsModeDescr` | none | missing (quality presets) |
| QualitySlider template (inactive) | one per `GraphicsSettingInt`: Vegetation, LOD ($settings_lod2), Lights, ShadowQuality, PointLights, PointLightShadows, SSAO, ClothQuality, SimulationDistance | `m_qualitySliderPrefab`, instantiated at runtime | `Vegitation`, `DrawDistance`, `ParticleLights`, `ShadowQuality`, `PointLights`, `PointLightsShadows` (static sliders) | changed: generated from a template; SSAO is a slider now; ClothQuality and SimulationDistance missing |
| QualityToggles/Grid/QualityToggle template (inactive) | one per `GraphicsSettingBool`: DistantShadows, Tesselation, Bloom, DepthOfField, MotionBlur, ChromaticAberration, SunShafts, SoftParticles, AntiAliasing | `m_qualityTogglePrefab`, instantiated at runtime | `DistantShadows`, `Tessellation`, `Bloom`, `DepthOfField`, `MotionBlur`, `ChromaticAbberation`, `SunShafts`, `SoftParticles`, `AntiAliasing` (static toggles) | changed: generated from a template |
| SSAO toggle | | (now `GraphicsSettingInt.SSAO`) | `SSAO` toggle | removed as a toggle |
| SettingsList (ScrollRect + ScrollRectEnsureVisible) + Scrollbar | | `m_listRoot`, `m_settingsListScrollRectEnsureVisible`, `m_dropdownScrollRectEnsureVisible` | two fixed columns | missing: the list scrolls |
| ResolutionDialog (ResListElement list, ResolutionScroll) | | `m_resolutionDialog`, `m_resolutionListElement`, `m_resolutionListRoot`, `m_resolutionListScroll` | `DummyObjects/DummyResDialog`, `DummyResElement`, `DummyScrollbar` | missing (Auga only has placeholders) |
| ResolutionSwitchDialog (SwitchTopic, SwitchCountdown, No, Yes) | $settings_resok, $menu_no, $menu_yes | `m_resolutionSwitchDialog`, `m_resolutionOk` (GuiButton) | `SwitchDialog/ConfirmDialog` (ButtonYes, ButtonNo inactive, Countdown) | present; needs a GuiButton for Ok |
| Dev build texts (inactive) | | `m_devBuildSettingsText`, `m_devBuildSettingFramePrefab`, `m_devGraphicsModeValuesText`, `m_devPlayerPrefsValuesText` | none | missing (dev builds only, still dereferenced: keep hidden dummies) |

The quality slider template needs children `Label` (TMP_Text), `Info/Value` (TMP_Text) and `Info/Warning` (Image +
SettingsTooltip); the toggle template needs a `GuiToggle` with a `Label` child. `FpsLimit` is excluded from the
generated sliders (it has its own row), `Vsync` from the generated toggles, `AnisotropicTextures` is hidden.

### 3.5 Audio (`AudioSettings`)

| Vanilla row | Token | Field | Auga (Audio) | Status |
| --- | --- | --- | --- | --- |
| MasterVolume | $settings_mastervol | `m_volumeSlider`, `m_volumeText` | `MasterVolume` | present (no value text) |
| SfxVolume | $settings_sfxvol | `m_sfxVolumeSlider`, `m_sfxVolumeText` | `EffectVolume` | present (no value text) |
| MusicVolume | $settings_musicvol | `m_musicVolumeSlider`, `m_musicVolumeText` | `MusicVolume` | present (no value text) |
| ContinuosMusic | $settings_continousmusic | `m_continousMusic` | `ContinuousMusic` | present |

Audio is complete apart from the percentage texts next to each slider.

### 3.6 Accessibility (`AccessibilitySettings`) - new tab

| Vanilla row | Token | Field | Auga | Status |
| --- | --- | --- | --- | --- |
| GuiScaling slider | $settings_guiscale | `m_guiScaleSlider`, `m_guiScaleText` | Misc/WidgetsTop `GuiScale` | moved |
| ToggleSprint | $settings_autorun | `m_toggleRun` | Controls `ToggleAutoRun` | moved (vanilla shows it on Gameplay too) |
| ImmersiveCamera | $settings_cameratilt | `m_immersiveCamera` | Misc `ImmersiveShipCamera` ($settings_shipcameratilt) | moved; token changed |
| CameraShake | $settings_camerashake | `m_cameraShake` | Misc `CameraShake` | moved |
| ReduceFlashingLights | $settings_reduceflashinglights | `m_reduceFlashingLights` | Misc `ReduceFlashingLights` | moved |
| MotionBlur | $settings_motionblur | `m_motionblurToggle` | Graphics `MotionBlur` | moved (vanilla mirrors it on Graphics) |
| ToggleBlock | $settings_toggleblock | `m_toggleBlockToggle` | none | missing |
| DepthOfField | $settings_dof | `m_depthOfFieldToggle` | Graphics `DepthOfField` | moved (mirrored) |
| ClosedCaptions (inactive) | $settings_closedcaptions | `m_closedCaptionsToggle` | none | missing |
| DirectionalIndicators (inactive) | $settings_directionalindicators | `m_soundIndicatorsToggle` | none | missing |

### 3.7 Radial (`RadialSettings`) - new page, no tab button

Opened by the Gameplay "Radial Settings" button; the `Back` row returns. Auga has nothing for it.

| Vanilla row | Token | Field | Control |
| --- | --- | --- | --- |
| Back | $menu_back | `m_back` | GuiButton |
| Radial Size | $settings_radialsize | `m_radialSize` | GuiDropdown |
| Hover Speed | $settings_hoverselectspd | `m_hoverSelect` | GuiDropdown |
| PersistentBackButton (inactive) | $settings_persistentbackbtn | `m_persistentBackBtn` | GuiToggle |
| Animate | $settings_animateradial | `m_animateRadial` | GuiToggle |
| Spiral | $settings_spiraleffect | `m_spiralEffect` | GuiDropdown |
| DoubleTap | $settings_double_tap | `m_doubleTap` | GuiToggle |
| FlickSelect | $settings_flick_interact | `m_flick` | GuiToggle |
| ReleaseToUseMode (inactive) | $settings_release_to_use_mode | `m_releaseToUse` | GuiToggle |

## 4. Summary

**Moved** (Auga has the control, vanilla filed it elsewhere):
Gamepad enabled, swap triggers, gamepad sensitivity and the controller diagram (Controls to Gamepad); quick select
(Misc to KeyboardMouse); GUI scale, camera shake, ship camera tilt, reduce flashing lights (Misc to Accessibility);
auto-run toggle (Controls to Gameplay and Accessibility); render scale (Misc to Graphics, as dropdowns); motion blur
and depth of field are mirrored on Accessibility.

**Missing** (vanilla has it, Auga has no control):
Gameplay: attack towards look direction, enable console, show build piece author, skip intro cinematic, delete
PlayFab account, blocked player list, radial settings button, community translation and cloud storage texts.
Keyboard/Mouse: 14 binding rows (AltDodge, EnableConsole, Hotbar1 to Hotbar8, OpenRadialWheel, OpenEmoteWheel,
CamZoomOut, CamZoomIn), the clear-key hint in the bind dialog. Gamepad: invert camera X/Y, vibration strength,
controller input layout stepper, Controller/Mouse sub tabs, Switch mouse settings, adaptive triggers, motion sensor
and speaker sliders. Graphics: graphics preset stepper with description, upscaling algorithm dropdown, cloth quality,
simulation distance, the scrolling settings list, a real resolution list dialog. Accessibility: toggle block, closed
captions, directional indicators. Radial: the whole page. Everywhere: the value texts next to sliders and the gamepad
key hints on Back, Ok and the outer tab buttons.

**Removed** (Auga has it, vanilla dropped it):
The SSAO toggle (SSAO is a quality slider now), the alternative-glyphs toggle (a glyph dropdown now), the render
scale slider (dropdowns now), the resolution dropdown filled by mod code (the game fills a GuiDropdown itself), the
Controls/Misc tab split, and the `DummyObjects` placeholders that fed the old `Settings` fields.

**Changed control type**: language (dropdown to stepper), glyphs (toggle to dropdown), render scale (slider to
dropdowns), resolution (UI Dropdown to GuiDropdown), quality sliders/toggles (static to generated from templates).

## 5. What a rebuilt prefab has to provide

Vanilla builds every control on `GuiToggle`, `GuiSlider`, `GuiDropdown`, `GuiButton` and `GuiStepper`
(gui_framework), all with gamepad navigation. The tab page components are the contract:

| Component (page) | Serialized fields that must be assigned |
| --- | --- |
| `Settings` (root) | `m_settingsPanel`, `m_tabHandler`, `m_backButton`, `m_okButton`, `m_tabKeyHints` |
| `GameplaySettings` | `m_language`, `m_communityTranslation`, `m_cloudStorageWarning`, `m_toggleRun`, `m_toggleAttackTowardsPlayerLookDir`, `m_showKeyHints`, `m_tutorialsEnabled`, `m_resetTutorial`, `m_reduceBGUsage`, `m_enableConsole`, `m_showBuildPieceAuthor`, `m_skipIntroCinematic`, `m_autoBackups`, `m_autoBackupsText`, `m_deleteAccount`, `m_blockedPlayerList`, `m_radialSettingsButton`, `m_playerListPrefab` |
| `KeyboardMouseSettings` | `m_mouseSensitivitySlider`, `m_mouseSensitivityText`, `m_invertMouse`, `m_quickPieceSelect`, `m_bindDialog`, `m_keys` (40 `KeySetting`: `m_keyName`, `m_keyTransform`, `m_blockedButtons`), `m_bottomLeftKeyButton`, `m_bottomRightKeyButton`, `m_scrollRectVisibilityManager` |
| `GamepadSettings` | `m_tabs`, `m_controllerSettingsRoot`, `m_mouseSettingsRoot`, `m_gamepadEnabled`, `m_swapTriggers`, `m_glyphs`, `m_controllerTogglesRoot`, `m_controllerInvertCameraX/Y`, `m_useAdaptiveTriggers`, `m_left/rightControllerLayoutButton`, `m_controllerLayoutText`, `m_gamepadSensitivityRoot/Slider/Text`, `m_vibrationStrengthSlider` (+ fill image, label, percent text), motion sensor and speaker groups, `m_switchMouseEnabled`, `m_invertMouse`, `m_mouseSensitivitySlider/Text`, `m_mouseLayoutText`, `m_left/rightMouseLayoutButton`, `m_backButton`, `m_okButton` |
| `GraphicsSettings` | `m_listRoot`, `m_settingsListScrollRectEnsureVisible`, `m_resolutionRoot`, `m_resolutionDropdown`, `m_upscalingOptionsRoot`, `m_renderScaleDropdown`, `m_upscalingAlgorithmDropdown`, `m_fullscreenToggle`, `m_testResolutionButton`, `m_resolutionDialog`, `m_resolutionListElement`, `m_resolutionListRoot`, `m_resolutionListScroll`, `m_resolutionSwitchDialog`, `m_resolutionOk`, `m_fpsLimitSlider`, `m_fpsLimitText`, `m_vsyncToggle`, `m_graphicPresetsRoot`, `m_graphicsMode`, `m_graphicPresetLeft/Right`, `m_graphicsModeDescr`, `m_qualitySliderPrefab`, `m_qualityTogglePrefab`, `m_dropdownScrollRectEnsureVisible`, the four dev texts |
| `AudioSettings` | `m_volumeSlider/Text`, `m_sfxVolumeSlider/Text`, `m_musicVolumeSlider/Text`, `m_continousMusic` |
| `AccessibilitySettings` | `m_guiScaleSlider/Text`, `m_toggleRun`, `m_immersiveCamera`, `m_cameraShake`, `m_reduceFlashingLights`, `m_motionblurToggle`, `m_depthOfFieldToggle`, `m_closedCaptionsToggle`, `m_soundIndicatorsToggle`, `m_toggleBlockToggle` |
| `RadialSettings` | `m_back`, `m_radialSize`, `m_hoverSelect`, `m_persistentBackBtn`, `m_animateRadial`, `m_spiralEffect`, `m_doubleTap`, `m_flick`, `m_releaseToUse` |

All of these are private `[SerializeField]` members, so they can be assigned in the Unity inspector once the page
component is on the Auga page object (the game DLLs are referenced by the `valheim` package in `AugaUnity`). Each
page also needs a `UIGroupHandler` (the tab handler activates it) and should keep a `Localize` component so a
language change re-localizes it.

## 6. Current runtime state in the mod

* The Auga settings prefab is not used: `FejdStartup.m_settingsPrefab` and `Menu.m_settingsPrefab` stay vanilla
  (`MainMenu_Setup.cs`, `PauseMenu_Setup.cs`).
* `Settings_Setup.cs` keeps two patches that are effectively idle on the vanilla prefab: the language dropdown hook
  only acts when `m_language` sits inside a UI `Dropdown` (vanilla uses a stepper), and the `UpdateBindings` prefix
  only adds `AugaBindingDisplay` support on top of the vanilla key text.
* The vanilla screen is a root canvas; a replacement root needs `SetupHelper.EnsureRootCanvas` like the other
  screens.

## 7. Notes on the Unity source prefab (mid-edit)

`AugaUnity/Assets/Prefabs/AugaSettings.prefab` already differs from the shipped bundle: the tab buttons are named
`Gameplay`, `KeyboardMouse`, `Contgroller` (sic, should be `Gamepad`), `Graphics`, `Audio`, `Accessibility`, and a
`Gameplay` page exists next to `Misc` (a copy of it). Two copy-and-paste labels to fix while there: the `RenderScale`
row on Misc and Gameplay carries the `$settings_fpslimit` label, and the audio `EffectVolume` row is the old sfx
slider. The `LabeledKeybind` rows and `AugaBindingDisplay` still work with the current game (the mod's
`UpdateBindings` prefix feeds them), so the bindings list can be kept and extended by the 14 missing rows.
