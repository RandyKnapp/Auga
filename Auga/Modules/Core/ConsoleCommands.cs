using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Auga
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class Terminal_InitTerminal_Patch
    {
        public static void Postfix()
        {
            // Forgets the biomes the local player has discovered, so the "new biome" messages can be seen again.
            new Terminal.ConsoleCommand("resetbiomes", "", args =>
            {
                var t = typeof(Player).GetField(nameof(Player.m_knownBiome),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                t.SetValue(Player.m_localPlayer,new HashSet<Heightmap.Biome>());
            });
        }
    }
}
