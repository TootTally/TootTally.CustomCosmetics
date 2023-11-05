using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using System.Net;
using Random = System.Random;
using static Mono.Math.BigInteger;

namespace TootTally.CustomCosmetics
{
    public static class CustomNote
    {
        private static Sprite _noteOriginalInTexture, _noteOriginalOutTexture;

        private static Sprite _noteStartOutTexture, _noteEndOutTexture;
        private static Sprite _noteStartInTexture, _noteEndInTexture;
        private static string _lastNoteName;
        private static readonly Random _rdm = new Random();

        public static void LoadNoteTexture(GameController __instance, string NoteName)
        {
            //If textures are already set, skip
            if (AreAllTexturesLoaded() && !ConfigNotesNameChanged()) return;

            string folderPath = Path.Combine(Paths.BepInExRootPath, Plugin.NOTES_FOLDER_PATH, NoteName);

            //Dont know which will request will finish first...
            Plugin.Instance.StartCoroutine(LoadNoteTexture(folderPath + "/NoteStartOutline.png", texture =>
            {
                _noteStartOutTexture = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2f);
                Plugin.Instance.LogInfo("NoteStartOutline Texture Loaded.");
                if (AreAllTexturesLoaded() && __instance != null)
                    OnAllTextureLoadedAfterConfigChange(__instance);
            }));
            Plugin.Instance.StartCoroutine(LoadNoteTexture(folderPath + "/NoteEndOutline.png", texture =>
            {
                _noteEndOutTexture = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2f);
                Plugin.Instance.LogInfo("NoteEndOutline Texture Loaded.");
                if (AreAllTexturesLoaded() && __instance != null)
                    OnAllTextureLoadedAfterConfigChange(__instance);
            }));
            Plugin.Instance.StartCoroutine(LoadNoteTexture(folderPath + "/NoteStartInline.png", texture =>
            {
                _noteStartInTexture = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2f);
                Plugin.Instance.LogInfo("NoteStartInline Texture Loaded.");
                if (AreAllTexturesLoaded() && __instance != null)
                    OnAllTextureLoadedAfterConfigChange(__instance);
            }));
            Plugin.Instance.StartCoroutine(LoadNoteTexture(folderPath + "/NoteEndInline.png", texture =>
            {
                _noteEndInTexture = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2f);
                Plugin.Instance.LogInfo("NoteEndInline Texture Loaded.");
                if (AreAllTexturesLoaded() && __instance != null)
                    OnAllTextureLoadedAfterConfigChange(__instance);
            }));
        }

        public static void UnloadTextures()
        {
            Texture2D.DestroyImmediate(_noteStartOutTexture);
            Texture2D.DestroyImmediate(_noteEndOutTexture);
            Texture2D.DestroyImmediate(_noteStartInTexture);
            Texture2D.DestroyImmediate(_noteEndInTexture);
            Plugin.Instance.LogInfo("Custom Notes Textures Destroyed.");
        }

        public static void OnAllTextureLoadedAfterConfigChange(GameController __instance)
        {
            ApplyCustomTextureToNotes(__instance);
            _lastNoteName = Plugin.Instance.option.NoteName.Value;

        }

        public static bool AreAllTexturesLoaded() => _noteStartOutTexture != null && _noteEndOutTexture != null && _noteStartInTexture != null && _noteEndInTexture != null;

        public static bool ConfigNotesNameChanged() => Plugin.Instance.option.NoteName.Value != _lastNoteName;

        public static IEnumerator<UnityWebRequestAsyncOperation> LoadNoteTexture(string filePath, Action<Texture2D> callback)
        {
            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(filePath);
            yield return webRequest.SendWebRequest();

            if (!webRequest.isNetworkError && !webRequest.isHttpError)
                callback(DownloadHandlerTexture.GetContent(webRequest));
            else
                Plugin.Instance.LogInfo("Custom Note does not exist or have the wrong format.");
        }

        public static void ResolvePresets(GameController __instance)
        {
            if ((!AreAllTexturesLoaded() || __instance == null) && Plugin.Instance.option.NoteName.Value != Plugin.DEFAULT_NOTENAME)
            {
                Plugin.Instance.LogInfo($"[{Plugin.Instance.option.NoteName.Value}] preset loading...");
                LoadNoteTexture(__instance, Plugin.Instance.option.NoteName.Value);
            }
            else if (Plugin.Instance.option.NoteName.Value != Plugin.DEFAULT_NOTENAME)
                ApplyCustomTextureToNotes(__instance);
            else if (__instance != null)
            {
                SetToOriginalTexture(__instance);
                Plugin.Instance.LogInfo("[Default] preset selected. Not loading any Custom Notes.");
            }
        }

        public static void ApplyCustomTextureToNotes(GameController __instance)
        {
            if (!AreAllTexturesLoaded()) return;
            Plugin.Instance.LogInfo("Applying Custom Textures to notes.");

            var design = __instance.singlenote.GetComponent<NoteDesigner>();
            if (_noteOriginalInTexture == null || _noteOriginalOutTexture == null)
            {
                _noteOriginalInTexture = design.startdot.sprite;
                _noteOriginalOutTexture = __instance.singlenote.transform.Find("StartPoint").GetComponent<Image>().sprite;
            }

            __instance.singlenote.transform.Find("StartPoint").GetComponent<Image>().sprite = _noteStartOutTexture;
            __instance.singlenote.transform.Find("EndPoint").GetComponent<Image>().sprite = _noteEndOutTexture;
            design.startdot.sprite = _noteStartInTexture;
            design.enddot.sprite = _noteEndInTexture;

        }

        public static void SetToOriginalTexture(GameController __instance)
        {
            if (_noteOriginalInTexture == null || _noteOriginalOutTexture == null) return;

            var design = __instance.singlenote.GetComponent<NoteDesigner>();
            __instance.singlenote.transform.Find("StartPoint").GetComponent<Image>().sprite = _noteOriginalOutTexture;
            __instance.singlenote.transform.Find("EndPoint").GetComponent<Image>().sprite = design.startdot.sprite = design.enddot.sprite = _noteOriginalInTexture;
        }


        public static void ApplyColor(GameController __instance)
        {
            Color c = Plugin.Instance.option.NoteColorStart.Value;
            Color c2 = Plugin.Instance.option.NoteColorEnd.Value;
            __instance.note_c_start = new float[] { c.r, c.g, c.b };
            __instance.note_c_end = new float[] { c2.r, c2.g, c2.b };
        }

        public static void ApplyRandomColor(ref float col_r, ref float col_g, ref float col_b, ref float col_r2, ref float col_g2, ref float col_b2)
        {
            col_r = (float)_rdm.NextDouble();
            col_g = (float)_rdm.NextDouble();
            col_b = (float)_rdm.NextDouble();
            col_r2 = (float)_rdm.NextDouble();
            col_g2 = (float)_rdm.NextDouble();
            col_b2 = (float)_rdm.NextDouble();
        }

        public static void ApplyNoteResize(GameController __instance)
        {
            var startRect = __instance.singlenote.transform.Find("StartPoint").GetComponent<RectTransform>();
            var endRect = __instance.singlenote.transform.Find("EndPoint").GetComponent<RectTransform>();

            startRect.sizeDelta = Plugin.Instance.option.NoteHeadSize.Value * Vector2.one * 40f;
            endRect.sizeDelta = Plugin.Instance.option.NoteHeadSize.Value * Vector2.one * 17f;
            startRect.pivot = endRect.pivot = Vector2.one / 2f;
            startRect.anchoredPosition = Vector2.zero;

            __instance.singlenote.transform.Find("StartPoint/StartPointColor").GetComponent<RectTransform>().sizeDelta = Plugin.Instance.option.NoteHeadSize.Value * Vector2.one * 16f;
            __instance.singlenote.transform.Find("EndPoint/EndPointColor").GetComponent<RectTransform>().sizeDelta = Plugin.Instance.option.NoteHeadSize.Value * Vector2.one * 10f;

            __instance.singlenote.transform.Find("Line").GetComponent<LineRenderer>().widthMultiplier = Plugin.Instance.option.NoteBodySize.Value * 7;
            __instance.singlenote.transform.Find("OutlineLine").GetComponent<LineRenderer>().widthMultiplier = Plugin.Instance.option.NoteBodySize.Value * 12;
        }

        //The fact I have to do that is bullshit
        public static void FixNoteEndPosition(GameController __instance)
        {
            foreach (GameObject note in __instance.allnotes)
            {
                var rect = note.transform.transform.Find("EndPoint").GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(1, .5f);
                rect.pivot = new Vector2(0.34f, 0.5f);
            }
        }

        public static class CustomNotePatch
        {
            [HarmonyPatch(typeof(HomeController), nameof(HomeController.tryToSaveSettings))]
            [HarmonyPostfix]
            public static void OnSettingsChange()
            {
                ResolvePresets(null);
            }

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
            [HarmonyPostfix]
            public static void OnHomeStartLoadTexture()
            {
                ResolvePresets(null);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.buildNotes))]
            [HarmonyPrefix]
            public static void PatchCustorTexture(GameController __instance)
            {
                ResolvePresets(__instance);
                ApplyNoteResize(__instance);

                if (Plugin.Instance.option.OverwriteNoteColor.Value)
                    ApplyColor(__instance);
            }

            [HarmonyPatch(typeof(NoteDesigner), nameof(NoteDesigner.setColorScheme))]
            [HarmonyPrefix]
            public static void PatchCursorColorRandom(ref float col_r, ref float col_g, ref float col_b, ref float col_r2, ref float col_g2, ref float col_b2)
            {
                if (Plugin.Instance.option.RandomNoteColor.Value)
                    ApplyRandomColor(ref col_r, ref col_g, ref col_b, ref col_r2, ref col_g2, ref col_b2);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.buildNotes))]
            [HarmonyPostfix]
            public static void FixEndNotePosition(GameController __instance)
            {
                FixNoteEndPosition(__instance);
            }
        }

    }
}
