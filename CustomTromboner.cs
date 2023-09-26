using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TootTally.Utils;
using TootTally.Utils.TootTallySettings;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;

namespace TootTally.CustomCosmetics
{
    public static class CustomTromboner
    {
        public const string CUSTOM_TROMBONER_FOLDER = "CustomTromboners";

        private static Dictionary<string, AssetBundle> _bonerDict;
        private static AssetBundle _currentBundle;
        private static TootTallySettingLabel _label;
        private static TootTallySettingDropdown _dropdown;

        public static string[] GetBonerNames => _bonerDict.Keys.ToArray();

        public static void LoadAssetBundles()
        {
            Plugin.Instance.LogInfo("New Custom Tromboner detected, reloading CustomTromboners...");

            if (_bonerDict != null)
                foreach (string key in _bonerDict.Keys)
                    _bonerDict[key].Unload(true);

            _bonerDict = new Dictionary<string, AssetBundle>();

            var path = Path.Combine(Paths.BepInExRootPath, CUSTOM_TROMBONER_FOLDER);
            var files = FileHelper.GetAllBonerFilesFromDirectory(path);
            files.ForEach(AddToAssetBundle);
            Plugin.Instance.LogInfo("Custom Tromboners Loaded.");
        }

        public static void AddToAssetBundle(FileInfo file)
        {
            Plugin.Instance.LogInfo($"Would add {file.Name} using {file.FullName}");
            try
            {
                _bonerDict.Add(file.Name.Replace(FileHelper.BONER_FILE_EXT, ""), AssetBundle.LoadFromFile(file.FullName));
            }
            catch (Exception ex)
            {
                Plugin.Instance.LogError(ex.Message);
                Plugin.Instance.LogError(ex.StackTrace);
            }
        }

        public static void ResolveCurrentBundle()
        {
            var bonerName = Plugin.Instance.option.BonerName.Value;
            if (bonerName != Plugin.DEFAULT_BONER && _bonerDict.ContainsKey(bonerName))
            {
                _currentBundle = _bonerDict[bonerName];
                _currentBundle.GetAllAssetNames().ToList().ForEach(Plugin.Instance.LogInfo);
                Plugin.Instance.LogInfo($"Boner bundle {_currentBundle.name} loaded.");
            }
            else if (_currentBundle != null && bonerName == Plugin.DEFAULT_BONER)
            {
                Plugin.Instance.LogInfo($"No bundle loaded.");
                _currentBundle = null;
            }
        }

        public static Material GetMaterial(string name) => _currentBundle.LoadAsset<Material>(name);
        public static Texture GetTexture(string name) => _currentBundle.LoadAsset<Texture>(name);


        public static class CustomBonerPatch
        {
            public static GameObject _customPuppet, _customPuppetPrefab;
            [HarmonyPatch(typeof(HomeController), nameof(HomeController.tryToSaveSettings))]
            [HarmonyPostfix]
            public static void OnSettingsChange()
            {
                ResolveCurrentBundle();
            }

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
            [HarmonyPostfix]
            public static void OnHomeControllerStart()
            {
                var path = Path.Combine(Paths.BepInExRootPath, CUSTOM_TROMBONER_FOLDER);
                if (_bonerDict == null || Directory.GetFiles(path).Any(x => !_bonerDict.ContainsKey(x.Replace(FileHelper.BONER_FILE_EXT, ""))))
                    LoadAssetBundles();
                ResolveCurrentBundle();
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPrefix]
            public static void SetPuppetIDOnStartPrefix(GameController __instance)
            {
                if (_currentBundle == null) return;
                _customPuppetPrefab = _currentBundle.LoadAsset<GameObject>("puppet.prefab");
                if (_customPuppetPrefab != null)
                {
                    _customPuppet = GameObject.Instantiate(_customPuppetPrefab, __instance.modelparent.transform);
                    _customPuppet.transform.localPosition = new Vector3(0.7f, -0.4f, 1.3f);
                }
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void OverwriteHumanPuppetBody(GameController __instance)
            {
                if (_customPuppetPrefab != null)
                {
                    __instance.puppet_human.transform.localScale = Vector3.zero;
                    return;
                }
                if (_currentBundle == null)
                    return;
                Plugin.Instance.LogInfo("Applying Custom Boner...");
                var puppetController = __instance.puppet_humanc;
                puppetController.head_oob = GetMaterial("head_oob.mat");
                puppetController.head_def = GetMaterial("head_def.mat");
                puppetController.head_def_es = GetMaterial("head_def_es.mat");
                puppetController.head_act = GetMaterial("head_act.mat");
                var mats = puppetController.bodymesh.materials;
                mats[0] = GetMaterial("body.mat");
                puppetController.bodymesh.materials = mats;
            }
        }

        public static int GetPuppetIDFromName(string PuppetName) => PuppetName.ToLower().Replace(" ", "") switch
        {
            "beezerly" or "kazyleii" => 0,
            "appaloosa" or "trixiebell" => 2,
            "hornlord" or "soda" => 4,
            "jermajesty" or "meldor" => 6,
            _ => -1
        };

        internal static class FileHelper
        {
            public const string BONER_FILE_EXT = ".boner";
            public static FileInfo[] GetFilesFromDirectory(string directory) => GetOrCreateDirectory(directory).GetFiles();

            public static DirectoryInfo GetOrCreateDirectory(string directory)
            {
                if (!Directory.Exists(directory))
                    return Directory.CreateDirectory(directory);
                return new DirectoryInfo(directory);
            }

            public static List<FileInfo> GetAllBonerFilesFromDirectory(string diretory) => GetFilesFromDirectory(diretory).Where(x => x.FullName.Contains(BONER_FILE_EXT)).ToList();
        }

        [Serializable]
        private class BonerInfo : MonoBehaviour
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Author { get; set; }
            public string PuppetName { get; set; }
        }
    }
}
