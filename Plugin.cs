using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TootTally.Utils;
using TootTally.Utils.TootTallySettings;
using UnityEngine;

namespace TootTally.CustomCosmetics
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "CustomCosmetics.cfg";
        private const string CURSOR_CONFIG_FIELD = "CustomCursor";
        private const string NOTE_CONFIG_FIELD = "CustomCursor";
        public const string DEFAULT_CURSORNAME = "Default";
        public const string DEFAULT_NOTENAME = "Default";
        private const string SETTINGS_PAGE_NAME = "CustomCosmetics";
        private const TrailType DEFAULT_TRAIL = TrailType.None;

        public static string CURSORS_FOLDER_PATH = "CustomCursors";
        public static string NOTES_FOLDER_PATH = "CustomNotes";
        public Options option;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public ConfigEntry<string> CursorName { get; set; }
        public bool IsConfigInitialized { get; set; }
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }
        public static TootTallySettingPage settingPage;
        public ManualLogSource GetLogger => Logger;

        public void LogInfo(string msg) => Logger.LogInfo(msg);
        public void LogError(string msg) => Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;

            ModuleConfigEnabled = TootTally.Plugin.Instance.Config.Bind("Modules", "Custom Cursor", true, "Enable Custom Cursor Module");
            TootTally.Plugin.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            option = new Options()
            {
                CursorName = config.Bind(CURSOR_CONFIG_FIELD, nameof(option.CursorName), DEFAULT_CURSORNAME),
                TrailType = config.Bind(CURSOR_CONFIG_FIELD, nameof(option.TrailType), DEFAULT_TRAIL),
                NoteName = config.Bind(NOTE_CONFIG_FIELD, nameof(option.NoteName), DEFAULT_NOTENAME),
                NoteHeadSize = config.Bind(NOTE_CONFIG_FIELD, nameof(option.NoteHeadSize), 1f),
                NoteBodySize = config.Bind(NOTE_CONFIG_FIELD, nameof(option.NoteBodySize), 1f),
                OverwriteNoteColor = config.Bind(NOTE_CONFIG_FIELD, nameof(option.OverwriteNoteColor), false),
                NoteColorStart = config.Bind(NOTE_CONFIG_FIELD, nameof(option.NoteColorStart), Color.white),
                NoteColorEnd = config.Bind(NOTE_CONFIG_FIELD, nameof(option.NoteColorEnd), Color.black),

            };

            settingPage = TootTallySettingsManager.AddNewPage(SETTINGS_PAGE_NAME, "Custom Cosmetics", 40, new UnityEngine.Color(.1f, .1f, .1f, .1f));


            TryMigrateFolder("CustomCursors");
            TryMigrateFolder("CustomNotes");

            CreateDropdownFromFolder(CURSORS_FOLDER_PATH, option.CursorName, DEFAULT_CURSORNAME);
            CreateDropdownFromFolder(NOTES_FOLDER_PATH, option.NoteName, DEFAULT_NOTENAME);

            var headSlider = settingPage.AddSlider("NoteHeadSizeSlider", 0f, 5f, 250f, "Note Head Size", option.NoteHeadSize, false);
            var bodySlider = settingPage.AddSlider("NoteBodySizeSlider", 0f, 5f, 250f, "Note Body Size", option.NoteBodySize, false);

            settingPage.AddButton("ResetSliders", new Vector2(160, 80), "Reset", () =>
            {
                headSlider.slider.value = 1f;
                bodySlider.slider.value = 1f;
            });

            settingPage.AddToggle("OverwriteNoteColor", option.OverwriteNoteColor, OnToggleValueChange);
            if (option.OverwriteNoteColor.Value) OnToggleValueChange(true);

            //Preload textures so you don't have to at the start of every songs
            Harmony.CreateAndPatchAll(typeof(CustomCursorPatch), PluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(CustomNotePatch), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        public void OnToggleValueChange(bool value)
        {
            if (value)
            {
                settingPage.AddLabel("Note Start Color");
                settingPage.AddColorSliders("NoteStart", "Note Start Color", option.NoteColorStart);
                settingPage.AddLabel("Note End Color");
                settingPage.AddColorSliders("NoteEnd", "Note End Color", option.NoteColorEnd);
            }
            else
            {
                settingPage.RemoveSettingObjectFromList("Note Start Color");
                settingPage.RemoveSettingObjectFromList("NoteStart");
                settingPage.RemoveSettingObjectFromList("Note End Color");
                settingPage.RemoveSettingObjectFromList("NoteEnd");
            }

        }

        public void TryMigrateFolder(string folderName)
        {
            string targetFolderPath = Path.Combine(Paths.BepInExRootPath, folderName);
            if (!Directory.Exists(targetFolderPath))
            {
                string sourceFolderPath = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), folderName);
                LogInfo($"{folderName} folder not found. Attempting to move folder from " + sourceFolderPath + " to " + targetFolderPath);
                if (Directory.Exists(sourceFolderPath))
                    Directory.Move(sourceFolderPath, targetFolderPath);
                else
                {
                    LogError($"Source {folderName} Folder Not Found. Cannot Create {folderName} Folder. Download the module again to fix the issue.");
                    return;
                }
            }
        }

        public void CreateDropdownFromFolder(string folderName, ConfigEntry<string> config, string defaultValue)
        {
            var folderNames = new List<string> { defaultValue };
            var folderPath = Path.Combine(Paths.BepInExRootPath, folderName);
            if (Directory.Exists(folderPath))
            {
                var directories = Directory.GetDirectories(folderPath);
                directories.ToList().ForEach(d => folderNames.Add(Path.GetFileNameWithoutExtension(d)));
            }
            settingPage.AddLabel(folderName, folderName, 24, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);
            settingPage.AddDropdown($"{folderName}Dropdown", config, folderNames.ToArray());
        }

        public void UnloadModule()
        {
            CustomCursor.UnloadTextures();
            CustomNote.UnloadTextures();
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            settingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        public static class CustomCursorPatch
        {
            [HarmonyPatch(typeof(HomeController), nameof(HomeController.tryToSaveSettings))]
            [HarmonyPostfix]
            public static void OnSettingsChange()
            {
                CustomCursor.ResolvePresets(null);
            }

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
            [HarmonyPostfix]
            public static void OnHomeStartLoadTexture()
            {
                CustomCursor.ResolvePresets(null);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void PatchCustorTexture(GameController __instance)
            {
                CustomCursor.ResolvePresets(__instance);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Update))]
            [HarmonyPostfix]
            public static void PatchCursorPositionWhilePlaying()
            {
                CustomCursor.UpdateCursor();
            }

        }

        public static class CustomNotePatch
        {
            [HarmonyPatch(typeof(HomeController), nameof(HomeController.tryToSaveSettings))]
            [HarmonyPostfix]
            public static void OnSettingsChange()
            {
                CustomNote.ResolvePresets(null);
            }

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
            [HarmonyPostfix]
            public static void OnHomeStartLoadTexture()
            {
                CustomNote.ResolvePresets(null);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.buildNotes))]
            [HarmonyPrefix]
            public static void PatchCustorTexture(GameController __instance)
            {
                CustomNote.ResolvePresets(__instance);
                CustomNote.ApplyNoteResize(__instance);
            }

            [HarmonyPatch(typeof(NoteDesigner), nameof(NoteDesigner.setColorScheme))]
            [HarmonyPrefix]
            public static bool OverwriteSetColorScheme(NoteDesigner __instance)
            {
                if (!Instance.option.OverwriteNoteColor.Value) return true;
                CustomNote.ApplyColor(__instance);

                return false;
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.buildNotes))]
            [HarmonyPostfix]
            public static void FixEndNotePosition(GameController __instance)
            {
                CustomNote.FixNoteEndPosition(__instance);
            }
        }

        public class Options
        {
            public ConfigEntry<string> CursorName { get; set; }
            public ConfigEntry<TrailType> TrailType { get; set; }

            public ConfigEntry<string> NoteName { get; set; }
            public ConfigEntry<float> NoteHeadSize { get; set; }
            public ConfigEntry<float> NoteBodySize { get; set; }
            public ConfigEntry<bool> OverwriteNoteColor { get; set; }
            public ConfigEntry<Color> NoteColorStart { get; set; }
            public ConfigEntry<Color> NoteColorEnd { get; set; }
        }

        public enum TrailType
        {
            None = 0,
            Short = 1,
            Long = 2,
        }
    }
}
