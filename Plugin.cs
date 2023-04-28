using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Runtime.CompilerServices;
using TootTally.Utils;
using TrombSettings;
using UnityEngine.UIElements;

namespace TootTally.CustomCursor
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "CustomCursor.cfg";
        private const string CONFIG_FIELD = "CursorName";
        private const string DEFAULT_CURSORNAME = "TEMPLATE";
        private const string SETTINGS_PAGE_NAME = "CustomCursor";
        private const TrailType DEFAULT_TRAIL = TrailType.None;

        public static string CURSORFOLDER_PATH = "CustomCursors/";
        public Options option;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public ConfigEntry<string> CursorName { get; set; }
        public bool IsConfigInitialized { get; set; }
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }

        public ManualLogSource GetLogger => Logger;

        public void LogInfo(string msg) => Logger.LogInfo(msg);
        public void LogError(string msg) => Logger.LogError(msg);

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
                PresetNames = config.Bind(CONFIG_FIELD, nameof(option.PresetNames), Presets.Default)
            };

            string targetFolderPath = Path.Combine(Paths.BepInExRootPath, "CustomCursors");
            if (!Directory.Exists(targetFolderPath))
            {
                string sourceFolderPath = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "CustomCursors");
                LogInfo("CustomCursors folder not found. Attempting to move folder from " + sourceFolderPath + " to " + targetFolderPath);
                if (Directory.Exists(sourceFolderPath))
                    Directory.Move(sourceFolderPath, targetFolderPath);
                else
                {
                    LogError("Source CustomCursors Folder Not Found. Cannot Create CustomCursors Folder. Download the module again to fix the issue.");
                    return;
                }
            }

            /*if (Directory.Exists(targetFolderPath))
            {
                var files = Directory.EnumerateDirectories(targetFolderPath);
                var settingPage = OptionalTrombSettings.GetConfigPage(SETTINGS_PAGE_NAME);
                foreach (string f in files)
                    CursorName.SetSerializedValue(f);
                OptionalTrombSettings.Add(settingPage, CursorName);
            }*/
            var settingPage = OptionalTrombSettings.GetConfigPage(SETTINGS_PAGE_NAME);
            OptionalTrombSettings.Add(settingPage, option.PresetNames);

            //Preload textures so you don't have to at the start of every songs
            Harmony.CreateAndPatchAll(typeof(CustomCursorPatch), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            CustomCursor.UnloadTextures();
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            LogInfo($"Module unloaded!");
        }

        public static class CustomCursorPatch
        {
            [HarmonyPatch(typeof(HomeController), nameof(HomeController.tryToSaveSettings))]
            [HarmonyPostfix]
            public static void OnSettingsChange()
            {
                ResolvePresets(null);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void PatchCustorTexture(GameController __instance)
            {
                ResolvePresets(__instance);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Update))]
            [HarmonyPostfix]
            public static void PatchCursorPositionWhilePlaying()
            {
                CustomCursor.UpdateCursor();
            }

        }

        public static void ResolvePresets(GameController __instance)
        {
            if ((!CustomCursor.AreAllTexturesLoaded() || __instance == null) && Instance.option.PresetNames.Value != Presets.Default)
                if (Instance.option.PresetNames.Value == Presets.Custom)
                {
                    Plugin.Instance.LogInfo("[Custom] preset selected. Loading config file CursorName.");
                    CustomCursor.LoadCursorTexture(__instance, Instance.option.CursorName.Value);
                }
                else
                {
                    Plugin.Instance.LogInfo($"[{Instance.option.PresetNames.Value.ToString()}] preset loaded.");
                    CustomCursor.LoadCursorTexture(__instance, Instance.option.PresetNames.Value.ToString());
                }
            else if (Instance.option.PresetNames.Value != Presets.Default)
                CustomCursor.ApplyCustomTextureToCursor(__instance);
            else
                Plugin.Instance.LogInfo("[Default] preset selected. Not loading any Custom Cursor.");
        }

        public class Options
        {
            public ConfigEntry<string> CursorName { get; set; }
            public ConfigEntry<TrailType> TrailType { get; set; }
            public ConfigEntry<Presets> PresetNames { get; set; }
        }

        public enum Presets
        {
            Default,
            Custom,
            ElectrostatsCursor50,
            OsuBlue,
            Star
        }
        public enum TrailType
        {
            None = 0,
            Short = 1,
            Long = 2,
        }
    }
}
