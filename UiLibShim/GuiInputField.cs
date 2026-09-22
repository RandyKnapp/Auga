namespace Fishlabs
{
    /// <summary>
    /// Stand-in for the pre-1.0 Valheim input field script referenced by the Auga prefabs (assembly ui_lib,
    /// class Fishlabs.GuiInputField). It derives from the current game widget so the serialized fields
    /// (TMP_InputField fields, VirtualKeyboardTitle, CaretOnGamepadUsage, OnInputSubmit, ...) load unchanged and
    /// GetComponent&lt;GUIFramework.GuiInputField&gt;() keeps working.
    /// </summary>
    public class GuiInputField : GUIFramework.GuiInputField
    {
    }
}
