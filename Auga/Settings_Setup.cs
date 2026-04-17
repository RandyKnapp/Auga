using System;
using System.Collections.Generic;
using System.Reflection;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace Auga
{
    // --------------------------------------------------------------------------
    // Settings-патчи для совместимости Auga с Valheim 0.221 (новый Settings UI).
    //
    // Что изменилось в Valheim 0.221:
    //   - Settings полностью переработан: используется ISettingsTab interface
    //   - 7 реализаций ISettingsTab (AudioSettings, GraphicsSettings и т.д.)
    //     прикреплены как компоненты к страницам вкладок TabHandler
    //   - Auga-префаб Settings НИКОГДА не имел этих компонентов → InitializeTabs
    //     возвращает null-записи в SettingsTabs → все вызывающие места крашатся
    //
    // Стратегия:
    //   1. InitializeTabs Prefix: инициализируем SettingsTabs как ПУСТОЙ List
    //      (не null!) и пропускаем vanilla — так OnOk/OnBack безопасно итерируют
    //   2. OnOk Prefix: полностью заменяем ванильную логику — сохраняем настройки
    //      и закрываем панель через приватный CloseSettings()
    //   3. Финализеры на OnBack, Update, OnDestroy — защита от прочих NPE
    //   4. AugaBindingDisplay: патч SetBinding чтобы не крашилось на новых именах
    // --------------------------------------------------------------------------

    [HarmonyPatch(typeof(Settings), "InitializeTabs")]
    public static class Settings_InitializeTabs_Patch
    {
        private static readonly FieldInfo s_settingsTabsField =
            typeof(Settings).GetField("SettingsTabs",
                BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool Prefix(Settings __instance)
        {
            // Инициализируем SettingsTabs как пустой список, иначе OnOk/OnBack/
            // CloseSettings падают с NPE на SettingsTabs.Count или foreach.
            // Пустой список безопасен: m_tabsToSave = 0, foreach не итерирует.
            s_settingsTabsField?.SetValue(__instance, new List<ISettingsTab>());

            // Пытаемся найти TabHandler в Auga-префабе и подписаться на смену вкладок
            // (без ISettingsTab — просто чтобы TabHandler не крашил подписку)
            try
            {
                var tabHandlerField = typeof(Settings).GetField("m_tabHandler",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var tabHandler = tabHandlerField?.GetValue(__instance) as TabHandler;
                if (tabHandler == null)
                {
                    tabHandler = __instance.GetComponentInChildren<TabHandler>();
                    tabHandlerField?.SetValue(__instance, tabHandler);
                }
            }
            catch (Exception ex)
            {
                Auga.LogWarning($"Settings InitializeTabs: TabHandler setup failed: {ex.Message}");
            }

            return false; // пропускаем ванильный InitializeTabs
        }
    }

    // Awake вызывает InitializeTabs (уже безопасен) и OnInputLayoutChanged
    // (m_tabKeyHints может быть null в Auga-префабе).
    [HarmonyPatch(typeof(Settings), nameof(Settings.Awake))]
    public static class Settings_Awake_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    // OnOk: ванильная версия требует SettingsTabs.Count > 0, чтобы
    // TabSaved → ApplyAndClose сработали. У нас SettingsTabs пустой —
    // m_tabsToSave = 0, foreach пустой, TabSaved никогда не вызывается,
    // ApplyAndClose никогда не вызывается, настройки не сохраняются и
    // панель не закрывается. Заменяем полностью.
    [HarmonyPatch(typeof(Settings), nameof(Settings.OnOk))]
    public static class Settings_OnOk_Patch
    {
        private static readonly MethodInfo s_closeSettings =
            typeof(Settings).GetMethod("CloseSettings",
                BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool Prefix(Settings __instance)
        {
            try
            {
                // Сохраняем настройки (аналог ApplyAndClose без TabSaved)
                ZInput.instance?.Save();
                if (GameCamera.instance) GameCamera.instance.ApplySettings();
                if (MusicMan.instance) MusicMan.instance.ApplySettings();
                if (KeyHints.instance) KeyHints.instance.ApplySettings();
                PlatformPrefs.Save();

                // Отписываемся от TabHandler, если он был подключён
                var tabHandlerField = typeof(Settings).GetField("m_tabHandler",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var tabHandler = tabHandlerField?.GetValue(__instance) as TabHandler;
                if (tabHandler != null)
                {
                    var activeTabChanged = typeof(Settings).GetMethod("ActiveTabChanged",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    if (activeTabChanged != null)
                    {
                        var handler = Delegate.CreateDelegate(
                            typeof(Action<int>), __instance, activeTabChanged, false)
                            as Action<int>;
                        if (handler != null)
                            tabHandler.ActiveTabChanged -= handler;
                    }
                }
            }
            catch (Exception ex)
            {
                Auga.LogWarning($"Settings.OnOk save error: {ex.Message}");
            }

            try
            {
                // CloseSettings: итерирует SettingsTabs (пустой → OK),
                // вызывает SettingsClosed и Destroy(gameObject)
                s_closeSettings?.Invoke(__instance, null);
            }
            catch (Exception ex)
            {
                Auga.LogWarning($"Settings.OnOk close error: {ex.Message}");
                // Аварийное закрытие — уничтожаем GO напрямую
                UnityEngine.Object.Destroy(__instance.gameObject);
            }

            return false; // пропускаем ванильный OnOk
        }
    }

    // OnBack вызывает ResetTabSettings (итерирует SettingsTabs — безопасно с пустым
    // списком) и CloseSettings. Финализер на случай прочих NPE (m_tabHandler и т.д.)
    [HarmonyPatch(typeof(Settings), "OnBack")]
    public static class Settings_OnBack_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    // Update: ZInput.GetKeyDown → OnBack, обычно безопасно, но на всякий случай
    [HarmonyPatch(typeof(Settings), "Update")]
    public static class Settings_Update_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    // OnDestroy: m_tabHandler.ActiveTabChanged -= ActiveTabChanged; может упасть если
    // m_tabHandler не был инициализирован (например, TabHandler не нашли в Auga-префабе)
    [HarmonyPatch(typeof(Settings), "OnDestroy")]
    public static class Settings_OnDestroy_Patch
    {
        public static Exception Finalizer(Exception __exception)
            => __exception is NullReferenceException ? null : __exception;
    }

    // AugaBindingDisplay.SetBinding: оригинальный метод обращается к
    // ZInput.instance.m_buttons (private в Valheim 0.221) → FieldAccessException → SetText
    // никогда не вызывается → текст остаётся "W" (дефолт из префаба).
    //
    // Решение: полностью заменяем SetBinding через Prefix (return false всегда).
    // Вся логика повторена с рефлексией для m_buttons + прямым вызовом GetBoundKeyString
    // (публичный метод — обращается к m_buttons изнутри, через свой доступ).
    [HarmonyPatch(typeof(AugaBindingDisplay), nameof(AugaBindingDisplay.SetBinding))]
    public static class AugaBindingDisplay_SetBinding_Patch
    {
        private static readonly FieldInfo s_buttonsField =
            typeof(ZInput).GetField("m_buttons", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool Prefix(AugaBindingDisplay __instance, string keyName)
        {
            try
            {
                if (ZInput.instance == null) { __instance.SetText("?"); return false; }

                // m_buttons приватный — читаем через рефлексию
                var buttons = s_buttonsField?.GetValue(ZInput.instance)
                    as Dictionary<string, ZInput.ButtonDef>;

                if (buttons == null || !buttons.ContainsKey(keyName))
                {
                    __instance.SetText("?");
                    return false;
                }

                // GetBoundKeyString — публичный метод, обращается к m_buttons через
                // собственный доступ класса (не вызывает FieldAccessException).
                var key = Localization.instance.GetBoundKeyString(keyName);

                // Обнаружение кнопок мыши
                var showMouse = -1;
                if      (key == "Mouse0" || key == "LMB") showMouse = 0;
                else if (key == "Mouse1" || key == "RMB") showMouse = 1;
                else if (key == "Mouse2" || key == "MMB") showMouse = 2;
                else if (key == "Mouse3") showMouse = 3;
                else if (key == "Mouse4") showMouse = 4;
                else if (key == "Mouse5") showMouse = 5;
                else if (key == "Mouse6") showMouse = 6;

                // Нормализация строк (из оригинального SetBinding)
                switch (key)
                {
                    case "Equals":    key = "="; break;
                    case "BackQuote": key = "`"; break;
                }

                if (key.StartsWith("Keypad"))
                {
                    key = key.Replace("Keypad", "Num")
                             .Replace("Divide", "/")
                             .Replace("Minus", "-")
                             .Replace("Multiply", "*")
                             .Replace("Equals", "=")
                             .Replace("Period", ".")
                             .Replace("Plus", "+");
                }
                else if (key.StartsWith("Alpha"))
                {
                    key = key.Replace("Alpha", "");
                }
                else
                {
                    switch (key)
                    {
                        case "LeftArrow":  key = "←"; break;
                        case "RightArrow": key = "→"; break;
                        case "UpArrow":    key = "↑"; break;
                        case "DownArrow":  key = "↓"; break;
                    }
                }

                __instance.SetText(key, showMouse);
            }
            catch (Exception ex)
            {
                Auga.LogWarning($"[AugaBindingDisplay] SetBinding '{keyName}' failed: {ex.Message}");
                try { __instance.SetText("?"); } catch { }
            }
            return false; // всегда пропускаем оригинальный SetBinding
        }
    }

    // Старый Settings_Setup — комментированный код, оставлен для справки
    [HarmonyPatch]
    public static class Settings_Setup
    {
        // TODO (Valheim 0.221): Settings UI полностью переработан.
        // Для полноценной поддержки нужно реализовать ISettingsTab на каждой странице
        // Auga-вкладок (AudioSettings, GraphicsSettings, KeyboardMouseSettings и т.д.)
        // и добавить компоненты в asset bundle. До тех пор настройки сохраняются/
        // загружаются через PlatformPrefs напрямую (OnOk patch выше).
    }
}
