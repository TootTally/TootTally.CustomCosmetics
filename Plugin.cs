using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTally.Utils;

namespace TootTally.CustomCursor
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "CustomCursor.cfg";
        private const string CONFIG_FIELD = "CursorName";
        private const string DEFAULT_CURSORNAME = "TEMPLATE";
        private const TrailType DEFAULT_TRAIL = TrailType.None;

        public static string CURSORFOLDER_PATH = "CustomCursors/";
        public Options option;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get ; set; }
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;

            ModuleConfigEnabled = TootTally.Plugin.Instance.Config.Bind("Modules", "Custom Cursor", true, "Enable Custom Cursor Module");
            OptionalTrombSettings.Add(TootTally.Plugin.Instance.moduleSettings, ModuleConfigEnabled);
            TootTally.Plugin.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            option = new Options()
            {
                CursorName = config.Bind(CONFIG_FIELD, nameof(option.CursorName), DEFAULT_CURSORNAME),
                TrailType = config.Bind(CONFIG_FIELD, nameof(option.TrailType), DEFAULT_TRAIL),
            };

            CustomCursor.LoadCursorTexture();
            Harmony.CreateAndPatchAll(typeof(CustomCursorPatch), PluginInfo.PLUGIN_GUID);
            TootTally.Plugin.LogInfo($"Module {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public void UnloadModule()
        {
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            TootTally.Plugin.LogInfo($"Module {PluginInfo.PLUGIN_GUID} is unloaded!");
        }

        public static class CustomCursorPatch
        {
            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void PatchCustorTexture(GameController __instance)
            {
                CustomCursor.ApplyCustomTextureToCursor(__instance);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Update))]
            [HarmonyPostfix]
            public static void PatchCursorPositionWhilePlaying(GameController __instance)
            {
                CustomCursor.UpdateCursor();
            }

        }

        public class Options
        {
            public ConfigEntry<string> CursorName { get; set; }
            public ConfigEntry<TrailType> TrailType { get; set; }

        }
        public enum TrailType
        {
            None = 0,
            Short = 1,
            Long = 2,
        }
    }
}
