using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Auga;

public class HudUpdatePatch
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    public static class Hud_Update_Patch
    {
        public static void HudUpdate(Hud instance)
        {
            float deltaTime = Time.deltaTime;
            if (instance.m_savingTriggered | ((UnityEngine.Object) ZNet.instance != (UnityEngine.Object) null && ZNet.instance.IsSaving()) || (double) instance.m_saveIconTimer > 0.0)
            {
                instance.m_saveIcon.SetActive(true);
                instance.m_saveIconTimer -= Time.unscaledDeltaTime;
                //Color color = instance.m_saveIconImage.color;
                float a = Mathf.PingPong(instance.m_saveIconTimer * 2f, 1f);
                //instance.m_saveIconImage.color = new Color(color.r, color.g, color.b, a);
                instance.m_badConnectionIcon.SetActive(false);
            }
            else
            {
                instance.m_saveIcon.SetActive(false);
                instance.m_badConnectionIcon.SetActive((UnityEngine.Object) ZNet.instance != (UnityEngine.Object) null && ZNet.instance.HasBadConnection() && (double) Mathf.Sin(Time.time * 10f) > 0.0);
            }
            Player localPlayer = Player.m_localPlayer;
            instance.UpdateDamageFlash(deltaTime);
            if (!(bool) (UnityEngine.Object) localPlayer)
                return;
            if (Input.GetKeyDown(KeyCode.F3) && Input.GetKey(KeyCode.LeftControl))
                instance.m_userHidden = !instance.m_userHidden;
            instance.SetVisible(!instance.m_userHidden && !localPlayer.InCutscene());
            instance.UpdateBuild(localPlayer, false);
            instance.m_tempStatusEffects.Clear();
            localPlayer.GetSEMan().GetHUDStatusEffects(instance.m_tempStatusEffects);
            instance.UpdateStatusEffects(instance.m_tempStatusEffects);
            instance.UpdateGuardianPower(localPlayer);
            float attackDrawPercentage = localPlayer.GetAttackDrawPercentage();
            instance.UpdateFood(localPlayer);
            instance.UpdateHealth(localPlayer);
            instance.UpdateStamina(localPlayer, deltaTime);
            instance.UpdateEitr(localPlayer, deltaTime);
            instance.UpdateStealth(localPlayer, attackDrawPercentage);
            instance.UpdateCrosshair(localPlayer, attackDrawPercentage);
            instance.UpdateEvent(localPlayer);
            instance.UpdateActionProgress(localPlayer);
            instance.UpdateStagger(localPlayer, deltaTime);
            instance.UpdateMount(localPlayer, deltaTime);        }
            
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instrs = instructions.ToList();

            var counter = 0;

            CodeInstruction LogMessage(CodeInstruction instruction)
            {
                //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                return instruction;
            }

            for (int i = 0; i < instrs.Count; ++i)
            {
                if (i == 0)
                {
                    yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                    counter++;

                    yield return LogMessage(new CodeInstruction(OpCodes.Call,AccessTools.DeclaredMethod(typeof(Hud_Update_Patch), nameof(HudUpdate))));
                    counter++;

                    yield return LogMessage(new CodeInstruction(OpCodes.Ret));
                    counter++;

                }
            }
        }
    }
    
}