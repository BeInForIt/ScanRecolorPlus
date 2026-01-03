using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using hoppinhauler.ScanRecolorRework;

namespace hoppinhauler.ScanRecolorRework
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "HoppinHauler.ScanRecolorPlus";
        public const string PluginName = "ScanRecolorPlus";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;
        internal static Plugin Instance;

        private Harmony _harmony;

        internal static ConfigFile BepInExConfig()
        {
            return Instance != null ? Instance.Config : null;
        }

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            ModConfig.Bind(Config);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(HUDManagerPatch));

            SceneHooks.Install();

            Log.LogInfo(PluginName + " v" + PluginVersion + " loaded.");
        }

        private void OnDestroy()
        {
            SceneHooks.Uninstall();
            try { _harmony.UnpatchSelf(); } catch { /* ignored */ }
        }
    }
}
