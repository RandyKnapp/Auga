using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCustomizaton : MonoBehaviour
{
	public static PlayerCustomizaton m_barberInstance;

	private static string m_lastHair;

	private static string m_lastBeard;

	private static Vector3 m_lastHairColor;

	public Color m_skinColor0 = Color.white;

	public Color m_skinColor1 = Color.white;

	public Color m_hairColor0 = Color.white;

	public Color m_hairColor1 = Color.white;

	public float m_hairMaxLevel = 1f;

	public float m_hairMinLevel = 0.1f;

	public TMP_Text m_selectedBeard;

	public TMP_Text m_selectedHair;

	public Slider m_skinHue;

	public Slider m_hairLevel;

	public Slider m_hairTone;

	public RectTransform m_beardPanel;

	public Toggle m_maleToggle;

	public Toggle m_femaleToggle;

	public ItemDrop m_noHair;

	public ItemDrop m_noBeard;

	public GameObject m_rootPanel;

	public Button m_apply;

	public Button m_cancel;

	public PlayerCustomizaton m_barberGui;

	public int m_hairToolTier;

	private List<ItemDrop> m_beards;

	private List<ItemDrop> m_hairs;

	private static bool m_barberWasHidden;

	private void OnEnable()
	{
		if ((bool)m_barberGui)
		{
			m_barberInstance = m_barberGui;
			m_rootPanel.gameObject.SetActive(value: false);
		}
		if ((bool)m_maleToggle)
		{
			m_maleToggle.isOn = true;
		}
		if ((bool)m_femaleToggle)
		{
			m_femaleToggle.isOn = false;
		}
		m_beardPanel.gameObject.SetActive(value: true);
	}

	private bool LoadHair()
	{
		if (m_hairs == null && (bool)ObjectDB.instance)
		{
			m_beards = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Beard");
			m_hairs = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Hair");
			m_beards.RemoveAll((ItemDrop x) => x.name.Contains("_"));
			m_hairs.RemoveAll((ItemDrop x) => x.name.Contains("_"));
			m_beards.Sort((ItemDrop x, ItemDrop y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name)));
			m_hairs.Sort((ItemDrop x, ItemDrop y) => Localization.instance.Localize(x.m_itemData.m_shared.m_name).CompareTo(Localization.instance.Localize(y.m_itemData.m_shared.m_name)));
			m_beards.Remove(m_noBeard);
			m_beards.Insert(0, m_noBeard);
			m_hairs.Remove(m_noHair);
			m_hairs.Insert(0, m_noHair);
			return true;
		}
		return m_hairs != null;
	}

	private void Update()
	{
		if (!LoadHair() || ((bool)m_rootPanel && !m_rootPanel.activeInHierarchy) || GetPlayer() == null)
		{
			return;
		}
		m_selectedHair.text = Localization.instance.Localize(GetHair());
		m_selectedBeard.text = Localization.instance.Localize(GetBeard());
		if ((bool)m_skinHue)
		{
			Color c = Color.Lerp(m_skinColor0, m_skinColor1, m_skinHue.value);
			GetPlayer().SetSkinColor(Utils.ColorToVec3(c));
		}
		if ((bool)m_hairTone)
		{
			Color c2 = Color.Lerp(m_hairColor0, m_hairColor1, m_hairTone.value) * Mathf.Lerp(m_hairMinLevel, m_hairMaxLevel, m_hairLevel.value);
			GetPlayer().SetHairColor(Utils.ColorToVec3(c2));
		}
		if (IsBarberGuiVisible())
		{
			if (InventoryGui.IsVisible() || Minimap.IsOpen() || Game.IsPaused())
			{
				HideBarberGui();
			}
			if (ZInput.GetKeyDown(KeyCode.Escape) || Player.m_localPlayer.IsDead())
			{
				OnCancel();
			}
		}
	}

	private Player GetPlayer()
	{
		if ((bool)Player.m_localPlayer)
		{
			return Player.m_localPlayer;
		}
		return GetComponentInParent<FejdStartup>()?.GetPreviewPlayer();
	}

	public void OnHairHueChange(float v)
	{
	}

	public void OnSkinHueChange(float v)
	{
	}

	public void SetPlayerModel(int index)
	{
		Player player = GetPlayer();
		if (!(player == null))
		{
			player.SetPlayerModel(index);
			if (index == 1)
			{
				ResetBeard();
			}
		}
	}

	public void OnHairLeft()
	{
		int num = GetHairIndex();
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			if (m_hairs[num2].m_itemData.m_shared.m_toolTier <= m_hairToolTier)
			{
				num = num2;
				break;
			}
		}
		SetHair(num);
	}

	public void OnHairRight()
	{
		int num = GetHairIndex();
		for (int i = num + 1; i < m_hairs.Count; i++)
		{
			if (m_hairs[i].m_itemData.m_shared.m_toolTier <= m_hairToolTier)
			{
				num = i;
				break;
			}
		}
		SetHair(num);
	}

	public void OnBeardLeft()
	{
		if (GetPlayer().GetPlayerModel() == 1)
		{
			return;
		}
		int num = GetBeardIndex();
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			if (m_beards[num2].m_itemData.m_shared.m_toolTier <= m_hairToolTier)
			{
				num = num2;
				break;
			}
		}
		SetBeard(num);
	}

	public void OnBeardRight()
	{
		if (GetPlayer().GetPlayerModel() == 1)
		{
			return;
		}
		int num = GetBeardIndex();
		for (int i = num + 1; i < m_beards.Count; i++)
		{
			if (m_beards[i].m_itemData.m_shared.m_toolTier <= m_hairToolTier)
			{
				num = i;
				break;
			}
		}
		SetBeard(num);
	}

	public void OnApply()
	{
		m_barberInstance.m_rootPanel.gameObject.SetActive(value: false);
		Player.m_localPlayer.ResetAttachCameraPoint();
		Player.m_localPlayer.AttachStop();
		m_barberWasHidden = false;
	}

	public void OnCancel()
	{
		GetPlayer().SetHair(m_lastHair);
		GetPlayer().SetBeard(m_lastBeard);
		GetPlayer().SetHairColor(m_lastHairColor);
		m_barberInstance.m_rootPanel.gameObject.SetActive(value: false);
		Player.m_localPlayer.ResetAttachCameraPoint();
		Player.m_localPlayer.AttachStop();
		m_barberWasHidden = false;
	}

	private void ResetBeard()
	{
		GetPlayer().SetBeard(m_noBeard.gameObject.name);
	}

	private void SetBeard(int index)
	{
		if (index >= 0 && index < m_beards.Count)
		{
			GetPlayer().SetBeard(m_beards[index].gameObject.name);
		}
	}

	private void SetHair(int index)
	{
		ZLog.Log("Set hair " + index);
		if (index >= 0 && index < m_hairs.Count)
		{
			GetPlayer().SetHair(m_hairs[index].gameObject.name);
		}
	}

	private int GetBeardIndex()
	{
		string beard = GetPlayer().GetBeard();
		for (int i = 0; i < m_beards.Count; i++)
		{
			if (m_beards[i].gameObject.name == beard)
			{
				return i;
			}
		}
		return 0;
	}

	private int GetHairIndex()
	{
		string hair = GetPlayer().GetHair();
		for (int i = 0; i < m_hairs.Count; i++)
		{
			if (m_hairs[i].gameObject.name == hair)
			{
				return i;
			}
		}
		return 0;
	}

	private string GetHair()
	{
		return m_hairs[GetHairIndex()].m_itemData.m_shared.m_name;
	}

	private string GetBeard()
	{
		return m_beards[GetBeardIndex()].m_itemData.m_shared.m_name;
	}

	public static bool IsBarberGuiVisible()
	{
		if ((bool)m_barberInstance && (bool)m_barberInstance.m_rootPanel)
		{
			return m_barberInstance.m_rootPanel.gameObject.activeInHierarchy;
		}
		return false;
	}

	public static void ShowBarberGui()
	{
		if (!m_barberInstance)
		{
			return;
		}
		if (!m_barberWasHidden)
		{
			m_lastHair = m_barberInstance.GetPlayer().GetHair();
			m_lastBeard = m_barberInstance.GetPlayer().GetBeard();
			m_lastHairColor = m_barberInstance.GetPlayer().GetHairColor();
			Player.m_localPlayer.HideHandItems();
			Vector3 hairColor = m_barberInstance.GetPlayer().GetHairColor();
			float value = 0f;
			float value2 = 0f;
			float num = 100f;
			for (float num2 = 0f; num2 < 1f; num2 += 0.02f)
			{
				for (float num3 = m_barberInstance.m_hairMinLevel; num3 < m_barberInstance.m_hairMaxLevel; num3 += 0.02f)
				{
					Vector3 vector = Utils.ColorToVec3(Color.Lerp(m_barberInstance.m_hairColor0, m_barberInstance.m_hairColor1, num2) * Mathf.Lerp(m_barberInstance.m_hairMinLevel, m_barberInstance.m_hairMaxLevel, num3));
					float num4 = Mathf.Abs(vector.x - hairColor.x) + Mathf.Abs(vector.y - hairColor.y) + Mathf.Abs(vector.z - hairColor.z);
					if (num4 < num)
					{
						num = num4;
						value = num2;
						value2 = num3;
					}
				}
			}
			m_barberInstance.m_hairTone.value = value;
			m_barberInstance.m_hairLevel.value = value2;
		}
		m_barberInstance.m_rootPanel.gameObject.SetActive(value: true);
		m_barberInstance.m_apply.Select();
	}

	public static void HideBarberGui()
	{
		m_barberWasHidden = true;
		m_barberInstance.m_rootPanel.gameObject.SetActive(value: false);
	}

	public static bool BarberBlocksLook()
	{
		if (IsBarberGuiVisible() && !ZInput.GetKey(KeyCode.Mouse1))
		{
			return !ZInput.IsGamepadActive();
		}
		return false;
	}
}
