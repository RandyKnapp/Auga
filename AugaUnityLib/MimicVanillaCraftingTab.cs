using JetBrains.Annotations;
using UnityEngine;

namespace AugaUnity
{
    public class MimicVanillaCraftingTab : MonoBehaviour
    {
        public ComplexTooltip OutputTooltip;

        protected TooltipTextBox DescriptionTextBox;

        [UsedImplicitly]
        public void Update()
        {
            var inventoryGui = InventoryGui.instance;
            if (inventoryGui == null)
                return;

            if (OutputTooltip != null)
            {
                OutputTooltip.ClearTextBoxes();
                DescriptionTextBox = OutputTooltip.AddTextBox(OutputTooltip.TwoColumnTextBoxPrefab);

                OutputTooltip.SetTopic(inventoryGui.m_recipeName.text);
                var text = inventoryGui.m_recipeDecription.text.Trim('\n', '\t', ' ');

                // This is specifically for Simple Recycling
                text = text.Replace("<size=10>", "");
                text = text.Replace("</size>", "");
                DescriptionTextBox.Text.text = text;
            }
        }
    }
}
