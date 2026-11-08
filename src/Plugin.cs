using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ShadowsOfDoubtCoop
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BasePlugin
    {
        public const string PluginGuid = "com.syojhin.shadowsofdoubt.coop";
        public const string PluginName = "Shadows of Doubt Co-op Suite";
        public const string PluginVersion = "1.0.0";

        public static new ManualLogSource Log { get; private set; } = null!;
        public static Plugin Instance { get; private set; } = null!;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            Log.LogInfo($"[+] Syojhin & Lara: {PluginName} v{PluginVersion} initializing...");

            // AddComponent automatically handles ClassInjector IL2CPP registration
            AddComponent<CoopNetworkManager>();

            // Apply Harmony detours
            var harmony = new Harmony(PluginGuid);
            harmony.PatchAll();

            var patched = harmony.GetPatchedMethods();
            int patchCount = 0;
            foreach (var m in patched)
            {
                patchCount++;
                Log.LogInfo($"[Harmony] Attached detour #{patchCount}: {m.DeclaringType?.Name}.{m.Name}");
            }

            Log.LogInfo($"[+] Co-op Suite successfully loaded into Unity 2021.3 IL2CPP runtime ({patchCount} active detours).");
        }
    }
}
