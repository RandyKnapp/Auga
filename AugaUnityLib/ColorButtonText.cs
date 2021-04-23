using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class ColorButtonText : Button
    {
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            var allValueSets = GetComponents<ColorButtonTextValues>();
            foreach (var values in allValueSets)
            {
                var targetColor =
                    state == SelectionState.Disabled ? values.TextColors.disabledColor :
                    state == SelectionState.Highlighted ? values.TextColors.highlightedColor :
                    state == SelectionState.Normal ? values.TextColors.normalColor :
                    state == SelectionState.Pressed ? values.TextColors.pressedColor :
                    state == SelectionState.Selected ? values.TextColors.selectedColor : Color.white;

                if (values.Text != null)
                {
                    values.Text.CrossFadeColor(targetColor, instant ? 0 : values.TextColors.fadeDuration, true, true);
                }
            }

            base.DoStateTransition(state, instant);
        }
    }

    public class ColorButtonTextValues : MonoBehaviour
    {
        public Text Text;
        public ColorBlock TextColors = ColorBlock.defaultColorBlock;
    }
}
