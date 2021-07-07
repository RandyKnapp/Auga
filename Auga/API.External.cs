using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Auga
{
    public static class API
    {
        private static readonly Assembly _targetAssembly;

        static API()
        {
            _targetAssembly = LoadAssembly();
            if (_targetAssembly != null)
            {
                var harmony = new Harmony("mods.randyknapp.auga.API");
                foreach (var method in typeof(API).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public).Where(m => m.Name != "IsLoaded" && m.Name != "LoadAssembly"))
                {
                    harmony.Patch(method, transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(API), nameof(Transpiler))));
                }
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _, MethodBase original)
        {
            var parameters = original.GetParameters().Select(p =>
            {
                var type = p.ParameterType;
                if (type.Assembly == Assembly.GetExecutingAssembly() && _targetAssembly != null)
                {
                    type = _targetAssembly.GetType(type.FullName ?? string.Empty);
                }
                return type;
            }).ToArray();
            MethodBase originalMethod = _targetAssembly.GetType("Auga.API").GetMethod(original.Name, parameters);

            for (var i = 0; i < parameters.Length; ++i)
            {
                yield return new CodeInstruction(OpCodes.Ldarg, i);
            }

            yield return new CodeInstruction(OpCodes.Call, originalMethod);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        public static Assembly LoadAssembly()
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "Auga");
        }

        public static bool IsLoaded()
        {
            return LoadAssembly() != null;
        }

        public static void TestMethod()
        {
        }
    }
}
