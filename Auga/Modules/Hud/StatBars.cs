using AugaUnity;

namespace Auga
{
    public enum StatBarTextDisplayMode { JustValue, ValueAndMax, ValueMaxPercent, JustPercent }
    public enum StatBarTextPosition { Off = -1, Above, Below, Center, Start, End };

    // Applies the [StatBars] settings to the health, stamina and eitr bars. Runs every frame (Auga.Update), so a
    // change in the Configuration Manager shows at once.
    public static class StatBars
    {
        public static void Apply()
        {
            if (Hud.instance != null)
            {
                var newHealthPanel = Hud.instance.transform.Find("hudroot/HealthBar");
                var newStaminaPanel = Hud.instance.transform.Find("hudroot/StaminaBar");
                var newEitrPanel = Hud.instance.transform.Find("hudroot/EitrBar");

                if (newHealthPanel != null && newHealthPanel.GetComponent<AugaHealthBar>() is AugaHealthBar healthBar)
                {
                    healthBar.Hide = !ValConfig.HealthBarShow.Value;
                    healthBar.FixedLength = ValConfig.HealthBarFixedSize.Value;
                    healthBar.TextDisplay = (AugaHealthBar.TextDisplayMode)ValConfig.HealthBarTextDisplay.Value;
                    healthBar.DisplayTextPosition = (AugaHealthBar.TextPosition)ValConfig.HealthBarTextPosition.Value;
                    healthBar.ShowTicks = ValConfig.HealthBarShowTicks.Value;
                }

                if (newStaminaPanel != null && newStaminaPanel.GetComponent<AugaHealthBar>() is AugaHealthBar staminaBar)
                {
                    staminaBar.Hide = !ValConfig.StaminaBarShow.Value;
                    staminaBar.FixedLength = ValConfig.StaminaBarFixedSize.Value;
                    staminaBar.TextDisplay = (AugaHealthBar.TextDisplayMode)ValConfig.StaminaBarTextDisplay.Value;
                    staminaBar.DisplayTextPosition = (AugaHealthBar.TextPosition)ValConfig.StaminaBarTextPosition.Value;
                    staminaBar.ShowTicks = ValConfig.StaminaBarShowTicks.Value;
                }

                if (newEitrPanel != null && newEitrPanel.GetComponent<AugaHealthBar>() is AugaHealthBar eitrBar)
                {
                    eitrBar.Hide = !ValConfig.EitrBarShow.Value;
                    eitrBar.FixedLength = ValConfig.EitrBarFixedSize.Value;
                    eitrBar.TextDisplay = (AugaHealthBar.TextDisplayMode)ValConfig.EitrBarTextDisplay.Value;
                    eitrBar.DisplayTextPosition = (AugaHealthBar.TextPosition)ValConfig.EitrBarTextPosition.Value;
                    eitrBar.ShowTicks = ValConfig.EitrBarShowTicks.Value;
                }
            }
        }
    }
}
