using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace TootTally.CustomCursor
{
    public static class CustomCursor
    {
        private const string NOTETARGET_PATH = "GameplayCanvas/GameSpace/TargetNote";
        private const string NOTEDOT_PATH = "GameplayCanvas/GameSpace/TargetNote/note-dot";
        private const string NOTEDOTGLOW_PATH = "GameplayCanvas/GameSpace/TargetNote/note-dot-glow";
        private const string NOTEDOTGLOW1_PATH = "GameplayCanvas/GameSpace/TargetNote/note-dot-glow/note-dot-glow (1)";

        private static Texture2D _noteTargetTexture, _noteDotTexture, _noteDotGlowTexture, _noteDotGlow1Texture;
        private static RectTransform _targetNoteRectangle;
        private static string _lastCursorName;

        public static void LoadCursorTexture(GameController __instance, string CursorName)
        {
            //If textures are already set, skip
            if (AreAllTexturesLoaded() && !ConfigCursorNameChanged()) return;

            string folderPath = Path.Combine(Paths.BepInExRootPath, Plugin.CURSORFOLDER_PATH, CursorName);

            //Dont know which will request will finish first...
            Plugin.Instance.StartCoroutine(LoadCursorTexture(folderPath + "/TargetNote.png", texture =>
            {
                _noteTargetTexture = texture;
                Plugin.Instance.LogInfo("Target Texture Loaded.");
                if (AreAllTexturesLoaded() && __instance != null)
                    OnAllTextureLoadedAfterConfigChange(__instance);
            }));
            Plugin.Instance.StartCoroutine(LoadCursorTexture(folderPath + "/note-dot.png", texture =>
            {
                _noteDotTexture = texture;
                Plugin.Instance.LogInfo("Dot Texture Loaded.");
                if (AreAllTexturesLoaded() && __instance != null) 
                    OnAllTextureLoadedAfterConfigChange(__instance);
            }));
            Plugin.Instance.StartCoroutine(LoadCursorTexture(folderPath + "/note-dot-glow.png", texture =>
            {
                _noteDotGlowTexture = texture;
                Plugin.Instance.LogInfo("Dot Glow Texture Loaded.");
                if (AreAllTexturesLoaded() && __instance != null)
                    OnAllTextureLoadedAfterConfigChange(__instance);
            }));
            Plugin.Instance.StartCoroutine(LoadCursorTexture(folderPath + "/note-dot-glow (1).png", texture =>
            {
                _noteDotGlow1Texture = texture;
                Plugin.Instance.LogInfo("Dot Glow1 Texture Loaded.");
                if (AreAllTexturesLoaded() && __instance != null) 
                    OnAllTextureLoadedAfterConfigChange(__instance);
            }));
        }

        public static void UnloadTextures()
        {
            Texture2D.DestroyImmediate(_noteTargetTexture);
            Texture2D.DestroyImmediate(_noteDotTexture);
            Texture2D.DestroyImmediate(_noteDotGlowTexture);
            Texture2D.DestroyImmediate(_noteDotGlow1Texture);
            Plugin.Instance.LogInfo("Custom Cursor Textures Destroyed.");
        }

        public static void OnAllTextureLoadedAfterConfigChange(GameController __instance)
        {
            ApplyCustomTextureToCursor(__instance);
            _lastCursorName = Plugin.Instance.option.CursorName.Value;

        }

        public static bool AreAllTexturesLoaded() => _noteTargetTexture != null && _noteDotTexture != null && _noteDotGlowTexture != null && _noteDotGlow1Texture != null;

        public static bool ConfigCursorNameChanged() => Plugin.Instance.option.CursorName.Value != _lastCursorName;

        public static IEnumerator<UnityWebRequestAsyncOperation> LoadCursorTexture(string filePath, Action<Texture2D> callback)
        {
            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(filePath);
            yield return webRequest.SendWebRequest();

            if (!webRequest.isNetworkError && !webRequest.isHttpError)
                callback(DownloadHandlerTexture.GetContent(webRequest));
            else
                Plugin.Instance.LogInfo("Cursor does not exist or have the wrong format.");
        }

        public static void ApplyCustomTextureToCursor(GameController __instance)
        {
            if (!AreAllTexturesLoaded()) return;

            Plugin.Instance.LogInfo("Applying Custom Textures to cursor.");
            GameObject noteTarget = GameObject.Find(NOTETARGET_PATH).gameObject;
            GameObject noteDot = GameObject.Find(NOTEDOT_PATH).gameObject;
            GameObject noteDotGlow = GameObject.Find(NOTEDOTGLOW_PATH).gameObject;
            GameObject noteDotGlow1 = GameObject.Find(NOTEDOTGLOW1_PATH).gameObject;

            _targetNoteRectangle = noteTarget.GetComponent<RectTransform>();

            noteTarget.GetComponent<Image>().sprite = Sprite.Create(_noteTargetTexture, new Rect(0, 0, _noteTargetTexture.width, _noteTargetTexture.height), Vector2.one);
            _targetNoteRectangle.sizeDelta = new Vector2(_noteTargetTexture.width, _noteTargetTexture.height) / 2;
            noteDot.GetComponent<Image>().sprite = Sprite.Create(_noteDotTexture, new Rect(0, 0, _noteDotTexture.width, _noteDotTexture.height), Vector2.zero);
            noteDot.GetComponent<RectTransform>().sizeDelta = new Vector2(_noteDotTexture.width, _noteDotTexture.height) / 2;
            noteDotGlow.GetComponent<Image>().sprite = Sprite.Create(_noteDotGlowTexture, new Rect(0, 0, _noteDotGlowTexture.width, _noteDotGlowTexture.height), Vector2.zero);
            noteDotGlow.GetComponent<RectTransform>().sizeDelta = new Vector2(_noteDotGlowTexture.width, _noteDotGlowTexture.height) / 2;
            noteDotGlow1.GetComponent<Image>().sprite = Sprite.Create(_noteDotGlow1Texture, new Rect(0, 0, _noteDotGlow1Texture.width, _noteDotGlow1Texture.height), Vector2.zero);
            noteDotGlow1.GetComponent<RectTransform>().sizeDelta = new Vector2(_noteDotGlow1Texture.width, _noteDotGlow1Texture.height) / 2;
        }

        public static void UpdateCursor()
        {
            if (_targetNoteRectangle != null)
                _targetNoteRectangle.anchoredPosition = new Vector3((_targetNoteRectangle.sizeDelta.x / 60 * -30) + 60, _targetNoteRectangle.anchoredPosition.y);
        }

    }
}
