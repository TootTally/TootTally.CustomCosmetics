using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TootTally.Utils;
using TootTally.Utils.TootTallySettings;
using UnityEngine;
using UnityEngine.UI;

namespace TootTally.CustomCosmetics
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "CustomCosmetics.cfg";
        private const string CURSOR_CONFIG_FIELD = "CustomCursor";
        private const string NOTE_CONFIG_FIELD = "CustomCursor";
        private const string BONER_CONFIG_FIELD = "CustomBoner";
        public const string DEFAULT_CURSORNAME = "Default";
        public const string DEFAULT_NOTENAME = "Default";
        public const string DEFAULT_BONER = "None";
        private const string SETTINGS_PAGE_NAME = "CustomCosmetics";

        public static string CURSORS_FOLDER_PATH = "CustomCursors";
        public static string NOTES_FOLDER_PATH = "CustomNotes";
        public Options option;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public ConfigEntry<string> CursorName { get; set; }
        public bool IsConfigInitialized { get; set; }
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }
        public static TootTallySettingPage SettingPage;
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
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true) { SaveOnConfigSet = true };
            option = new Options()
            {
                CursorName = config.Bind(CURSOR_CONFIG_FIELD, nameof(option.CursorName), DEFAULT_CURSORNAME),

                CursorTrail = config.Bind(CURSOR_CONFIG_FIELD, nameof(option.CursorTrail), false),
                TrailSize = config.Bind(CURSOR_CONFIG_FIELD, nameof(option.TrailSize), .5f),
                TrailLength = config.Bind(CURSOR_CONFIG_FIELD, nameof(option.TrailLength), .1f),
                TrailSpeed = config.Bind(CURSOR_CONFIG_FIELD, nameof(option.TrailSpeed), 15f),
                TrailStartColor = config.Bind(CURSOR_CONFIG_FIELD, nameof(option.TrailStartColor), Color.white),
                TrailEndColor = config.Bind(CURSOR_CONFIG_FIELD, nameof(option.TrailEndColor), Color.white),

                NoteName = config.Bind(NOTE_CONFIG_FIELD, nameof(option.NoteName), DEFAULT_NOTENAME),
                NoteHeadSize = config.Bind(NOTE_CONFIG_FIELD, nameof(option.NoteHeadSize), 1f, "Size of the start and end note circles"),
                NoteBodySize = config.Bind(NOTE_CONFIG_FIELD, nameof(option.NoteBodySize), 1f, "Size of the note line"),
                RandomNoteColor = config.Bind(NOTE_CONFIG_FIELD, nameof(option.RandomNoteColor), false, "Randomize all the colors of the notes"),
                RGBNoteColor = config.Bind(NOTE_CONFIG_FIELD, nameof(option.RGBNoteColor), false, "High notes will be red, Mid will be yellow and low notes will be blue"),
                OverwriteNoteColor = config.Bind(NOTE_CONFIG_FIELD, nameof(option.OverwriteNoteColor), false, "Make the note color consistent"),
                NoteColorStart = config.Bind(NOTE_CONFIG_FIELD, nameof(option.NoteColorStart), Color.white),
                NoteColorEnd = config.Bind(NOTE_CONFIG_FIELD, nameof(option.NoteColorEnd), Color.black),
                
                BonerName = config.Bind(BONER_CONFIG_FIELD, nameof(option.BonerName), DEFAULT_BONER),

            };

            SettingPage = TootTallySettingsManager.AddNewPage(SETTINGS_PAGE_NAME, "Custom Cosmetics", 40, new UnityEngine.Color(.1f, .1f, .1f, .1f));


            TryMigrateFolder("CustomCursors");
            TryMigrateFolder("CustomNotes");
            TryMigrateFolder("CustomTromboners");

            CreateDropdownFromFolder(CURSORS_FOLDER_PATH, option.CursorName, DEFAULT_CURSORNAME);
            SettingPage.AddLabel("CustomTrailLabel", "Custom Trail", 24, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);
            SettingPage.AddToggle("CursorTrail", option.CursorTrail);
            SettingPage.AddSlider("Trail Size", 0, 1, option.TrailSize, false);
            SettingPage.AddSlider("Trail Length", 0, 1, option.TrailLength, false);
            SettingPage.AddSlider("Trail Speed", 0, 100, option.TrailSpeed, false);
            SettingPage.AddLabel("Trail Start Color");
            SettingPage.AddColorSliders("Trail Start Color", "Trail Start Color", option.TrailStartColor);
            SettingPage.AddLabel("Trail End Color");
            SettingPage.AddColorSliders("Trail End Color", "Trail End Color", option.TrailEndColor);
            CreateDropdownFromFolder(NOTES_FOLDER_PATH, option.NoteName, DEFAULT_NOTENAME);

            var headSlider = SettingPage.AddSlider("NoteHeadSizeSlider", 0f, 5f, 250f, "Note Head Size", option.NoteHeadSize, false);
            var bodySlider = SettingPage.AddSlider("NoteBodySizeSlider", 0f, 5f, 250f, "Note Body Size", option.NoteBodySize, false);

            SettingPage.AddButton("ResetSliders", new Vector2(160, 80), "Reset", () =>
            {
                headSlider.slider.value = 1f;
                bodySlider.slider.value = 1f;
            });

            SettingPage.AddToggle("RandomNoteColor", option.RandomNoteColor);
            //SettingPage.AddToggle("RGBNoteColor", option.RGBNoteColor);
            SettingPage.AddToggle("OverwriteNoteColor", option.OverwriteNoteColor, OnToggleValueChange);
            if (option.OverwriteNoteColor.Value) OnToggleValueChange(true);

            SettingPage.AddLabel("BonerLabel", "Custom Tromboners", 24, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);

            CustomTromboner.LoadAssetBundles();

            List<string> folderNames = new() { Plugin.DEFAULT_BONER };
            folderNames.AddRange(CustomTromboner.GetBonerNames);
            SettingPage.AddDropdown($"BonerDropdown", option.BonerName, folderNames.ToArray());

            //Preload textures so you don't have to at the start of every songs
            Harmony.CreateAndPatchAll(typeof(CustomCursor.CustomCursorPatch), PluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(CustomNote.CustomNotePatch), PluginInfo.PLUGIN_GUID);
            Harmony.CreateAndPatchAll(typeof(CustomTromboner.CustomBonerPatch), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        public void OnToggleValueChange(bool value)
        {
            if (value)
            {
                SettingPage.AddLabel("Note Start Color");
                SettingPage.AddColorSliders("NoteStart", "Note Start Color", option.NoteColorStart);
                SettingPage.AddLabel("Note End Color");
                SettingPage.AddColorSliders("NoteEnd", "Note End Color", option.NoteColorEnd);
            }
            else
            {
                SettingPage.RemoveSettingObjectFromList("Note Start Color");
                SettingPage.RemoveSettingObjectFromList("NoteStart");
                SettingPage.RemoveSettingObjectFromList("Note End Color");
                SettingPage.RemoveSettingObjectFromList("NoteEnd");
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
                var directories = Directory.GetDirectories(folderPath).ToList();
                directories.ForEach(d =>
                {
                    if (!d.Contains("TEMPALTE"))
                        folderNames.Add(Path.GetFileNameWithoutExtension(d));
                });
            }
            SettingPage.AddLabel(folderName, folderName, 24, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);
            SettingPage.AddDropdown($"{folderName}Dropdown", config, folderNames.ToArray());
        }

        public void UnloadModule()
        {
            CustomCursor.UnloadTextures();
            CustomNote.UnloadTextures();
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            SettingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        public class Options
        {
            public ConfigEntry<string> CursorName { get; set; }
            public ConfigEntry<bool> CursorTrail { get; set; }
            public ConfigEntry<float> TrailSize { get; set; }
            public ConfigEntry<float> TrailLength { get; set; }
            public ConfigEntry<float> TrailSpeed { get; set; }
            public ConfigEntry<Color> TrailStartColor { get; set; }
            public ConfigEntry<Color> TrailEndColor { get; set; }
            public ConfigEntry<string> NoteName { get; set; }
            public ConfigEntry<string> BonerName { get; set; }
            public ConfigEntry<float> NoteHeadSize { get; set; }
            public ConfigEntry<float> NoteBodySize { get; set; }
            public ConfigEntry<bool> RandomNoteColor { get; set; }
            public ConfigEntry<bool> RGBNoteColor { get; set; }
            public ConfigEntry<bool> OverwriteNoteColor { get; set; }
            public ConfigEntry<Color> NoteColorStart { get; set; }
            public ConfigEntry<Color> NoteColorEnd { get; set; }
        }

    }
}
