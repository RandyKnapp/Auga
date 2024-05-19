using Fishlabs;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaCraftingControls : MonoBehaviour
    {
        [Header("Crafting Controls")]
        public Button craftButton;
        
        [Header("MultiCraft Objects")] 
        public GameObject multicraft;
        public Button plusButton;
        public Button minusButton;
        public TMP_Text craftAmountText;
        public GameObject craftAmountBg;
        public GameObject aaa;
        public GuiInputField inputAmount;
        public TMP_Text inputText;

        public static AugaCraftingControls Instance => _instance;

        private static AugaCraftingControls _instance;
        
        private void Awake()
        {
            _instance = this;
            Debug.LogWarning($"ACP is Awake.");
            Debug.LogWarning($"AAA is null: {aaa == null}");
            Debug.LogWarning($"InputAmount is null: {inputAmount == null}");
            Debug.LogWarning($"CraftButton is null: {craftButton == null}");
        }
    }
}