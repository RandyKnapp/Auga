using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

public class Settings : MonoBehaviour
{
	private static Settings m_instance;

	private static bool m_startUp = true;

	public static bool ReduceBackgroundUsage = false;

	public static bool ContinousMusic = true;

	public static bool ReduceFlashingLights = false;

	public static bool ClosedCaptions = false;

	public static bool DirectionalSoundIndicators = false;

	public static AssetMemoryUsagePolicy AssetMemoryUsagePolicy = AssetMemoryUsagePolicy.KeepSynchronousOnlyLoaded;

	[SerializeField]
	private GameObject[] m_tabKeyHints;

	[SerializeField]
	private GameObject m_settingsPanel;

	[SerializeField]
	private TabHandler m_tabHandler;

	[SerializeField]
	private Button m_backButton;

	[SerializeField]
	private Button m_okButton;

	private bool m_navigationBlocked;

	private List<ISettingsTab> SettingsTabs;

	private int m_tabsToSave;

	public Action SettingsClosed;

	public static Settings instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		InitializeTabs();
		ZInput.OnInputLayoutChanged += OnInputLayoutChanged;
		OnInputLayoutChanged();
	}

	private void Update()
	{
		if (!m_navigationBlocked && ZInput.GetKeyDown(KeyCode.Escape))
		{
			OnBack();
		}
	}

	private void SetAvailableTabs()
	{
		SettingsTabs = new List<ISettingsTab>();
		foreach (TabHandler.Tab tab in m_tabHandler.m_tabs)
		{
			SettingsTabs.Add(tab.m_page.gameObject.GetComponent<ISettingsTab>());
		}
	}

	private void OnInputLayoutChanged()
	{
		GameObject[] tabKeyHints = m_tabKeyHints;
		for (int i = 0; i < tabKeyHints.Length; i++)
		{
			tabKeyHints[i].SetActive(ZInput.GamepadActive);
		}
	}

	private void ActiveTabChanged(int index)
	{
		SettingsTabs[index].OnTabOpen(m_backButton, m_okButton);
	}

	private void InitializeTabs()
	{
		m_tabHandler = GetComponentInChildren<TabHandler>();
		SetAvailableTabs();
		foreach (ISettingsTab settingsTab in SettingsTabs)
		{
			settingsTab.Initialize();
			settingsTab.SharedSettingChanged += OnSharedSettingChanged;
		}
		m_tabHandler.ActiveTabChanged += ActiveTabChanged;
	}

	private void OnSharedSettingChanged(string setting, int value)
	{
		foreach (ISettingsTab settingsTab in SettingsTabs)
		{
			settingsTab.OnSharedSettingChanged(setting, value);
		}
	}

	private void ResetTabSettings()
	{
		foreach (ISettingsTab settingsTab in SettingsTabs)
		{
			settingsTab.OnBack();
		}
		ZInput.instance.Save();
	}

	private void ApplyAndClose()
	{
		ZInput.instance.Save();
		if ((bool)GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if ((bool)MusicMan.instance)
		{
			MusicMan.instance.ApplySettings();
		}
		if ((bool)KeyHints.instance)
		{
			KeyHints.instance.ApplySettings();
		}
		PlatformPrefs.Save();
		m_tabHandler.ActiveTabChanged -= ActiveTabChanged;
		CloseSettings();
	}

	private void TabSaved()
	{
		if (--m_tabsToSave <= 0)
		{
			ApplyAndClose();
		}
	}

	private void OnDestroy()
	{
		ZInput.OnInputLayoutChanged -= OnInputLayoutChanged;
		m_tabHandler.ActiveTabChanged -= ActiveTabChanged;
		m_instance = null;
	}

	public void OnBack()
	{
		ResetTabSettings();
		CloseSettings();
	}

	private void CloseSettings()
	{
		foreach (ISettingsTab settingsTab in SettingsTabs)
		{
			settingsTab.SharedSettingChanged -= OnSharedSettingChanged;
			settingsTab.Terminate();
		}
		SettingsClosed?.Invoke();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void OnOk()
	{
		m_tabsToSave = SettingsTabs.Count;
		foreach (ISettingsTab settingsTab in SettingsTabs)
		{
			settingsTab.OnOkAsync(TabSaved);
		}
	}

	public void BlockNavigation(bool block)
	{
		m_navigationBlocked = block;
		m_okButton.gameObject.SetActive(!block);
		m_backButton.gameObject.SetActive(!block);
		m_tabHandler.m_gamepadInput = !block;
		m_tabHandler.m_keybaordInput = !block;
		m_tabHandler.m_tabKeyInput = !block;
	}

	public static bool IsSteamRunningOnSteamDeck()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("SteamDeck");
		if (!string.IsNullOrEmpty(environmentVariable))
		{
			return environmentVariable != "0";
		}
		return false;
	}

	public static void ApplyStartupSettings()
	{
		ReduceBackgroundUsage = PlatformPrefs.GetInt("ReduceBackgroundUsage") == 1;
		ContinousMusic = PlatformPrefs.GetInt("ContinousMusic", 1) == 1;
		ReduceFlashingLights = PlatformPrefs.GetInt("ReduceFlashingLights") == 1;
		Raven.m_tutorialsEnabled = PlatformPrefs.GetInt("TutorialsEnabled", 1) == 1;
		ClosedCaptions = PlatformPrefs.GetInt("ClosedCaptions") == 1;
		DirectionalSoundIndicators = PlatformPrefs.GetInt("DirectionalSoundIndicators") == 1;
	}
}
