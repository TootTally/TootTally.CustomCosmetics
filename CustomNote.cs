using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace TootTally.CustomCosmetics
{
    public static class CustomNote
    {
        private static Sprite _noteStartOutTexture, _noteEndOutTexture;
        private static Sprite _noteStartInTexture, _noteEndInTexture;
        private static string _lastNoteName;

        public static void LoadNoteTexture(GameController __instance, string NoteName)
        {
            //If textures are already set, skip
            if (AreAllTexturesLoaded() && !ConfigNotesNameChanged()) return;

            string folderPath = Path.Combine(Paths.BepInExRootPath, Plugin.NOTES_FOLDER_PATH, NoteName);

            //Dont know which will request will finish first...
            Plugin.Instance.StartCoroutine(LoadNoteTexture(folderPath + "/NoteStartOutline.png", texture =>
            {
                _noteStartOutTexture = Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), Vector2.one/2f);
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
            else
                Plugin.Instance.LogInfo("[Default] preset selected. Not loading any Custom Notes.");
        }

        public static void ApplyCustomTextureToNotes(GameController __instance)
        {
            if (!AreAllTexturesLoaded()) return;

            Plugin.Instance.LogInfo("Applying Custom Textures to notes.");
            var design = __instance.singlenote.GetComponent<NoteDesigner>();
            __instance.singlenote.transform.Find("StartPoint").GetComponent<Image>().sprite = _noteStartOutTexture;
            __instance.singlenote.transform.Find("EndPoint").GetComponent<Image>().sprite = _noteEndOutTexture;

            design.startdot.sprite = _noteStartInTexture;
            design.enddot.sprite = _noteEndInTexture;

        }

    }
}
