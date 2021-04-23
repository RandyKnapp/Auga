namespace AugaUnity
{
    public class CraftingTabButton : TabButton
    {
        public virtual void Update()
        {
            SetSelected(!Button.interactable);
        }
    }
}
