using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Collections;
using System;
using Debug = UnityEngine.Debug;
using Application = UnityEngine.Application;
using BepInEx.Configuration;

namespace PANE_Cityshot
{
    [BepInPlugin(ModGUID, ModName, VersionString)]
    public class PANECityshot : BaseUnityPlugin
    {
        private const string ModGUID = "PANE.ENH.Cityshot";
        private const string ModName = "_Enhancement - Cityshot";
        private const string VersionString = "1.0.0";
        static ManualLogSource logger;

        // settings
        private static ConfigEntry<bool> enabled;
        private static ConfigEntry<int> superRes;

        private void Awake()
        {
            logger = Logger;
            logger.LogInfo($"Plugin {ModGUID} is loaded!");

            // list of settings - these will be surfaced in configuration managed if the mod dll is present in bepinex/plugins

            enabled = Config.Bind("Settings",
                                    "Enable Cityshot",
                                    false,
                                    "Enable Cityshot");
            superRes = Config.Bind("Settings",
                                    "Super Resolution Multiplier",
                                    1,
                                    new ConfigDescription("Super Resolution Multiplier", new AcceptableValueRange<int>(1, 4)));

            Harmony.CreateAndPatchAll(typeof(PANECityshot));
            logger.LogInfo($"{ModName} applied!");
        }

        // specific mod content

        [HarmonyPatch(typeof(MapGameplay), "ProcessKeyBindings")]
        [HarmonyPrefix]
        public static bool ProcessKeyBindingsPatch(ref UIManager ____uiManager)
        {
            if (enabled.Value)
            {
                if (ControlsHelper.GetKeyPressed(KeyId.OverlayClose, false))
                {
                    int width = 3873 * superRes.Value;
                    int height = 1897 * superRes.Value;

                    GameObject mainCamera = GameObject.Find("Main Camera");
                    if (mainCamera == null)
                    {
                        Debug.LogError("Main Camera not found in the scene.");
                        return false;
                    }


                    Camera camera = Camera.main;
                    CameraManager cameraManager = mainCamera.GetComponent<CameraManager>();
                    cameraManager.enabled = false;
                    RenderTexture renderTexture = new RenderTexture(width, height, 24);
                    camera.targetTexture = renderTexture;
                    Texture2D texture2D = new Texture2D(width - (33 * superRes.Value), height - (33 * superRes.Value), TextureFormat.RGB24, false);

                    CameraManager.Instance.StartCoroutine(CaptureAtEndOfFrame(mainCamera, renderTexture, texture2D, camera, ____uiManager));
                    return false;
                }
            }
            return true;
        }

        private static IEnumerator CaptureAtEndOfFrame(GameObject mainCamera, RenderTexture renderTexture, Texture2D texture2D, Camera camera, UIManager ____uiManager)
        {
            yield return new WaitForEndOfFrame();

            bool cmncl = CheatManager.NoCameraLimit;

            CheatManager.NoCameraLimit = true;


            LevelMap level = GlobalAccessor.Level;
            logger.LogInfo($"level.Width:{level.Width}, level.Height:{level.Height}, cameraAspect:{CameraManager.Instance.ActiveCam.aspect}");
            var mapGameplay = AccessTools.FieldRefAccess<UIManager, MapGameplay>(____uiManager, "_mapGameplay");
            MethodInfo setUserInterfaceVisibility = mapGameplay.GetType().GetMethod("SetUserInterfaceVisibility", BindingFlags.NonPublic | BindingFlags.Instance);
            setUserInterfaceVisibility.Invoke(mapGameplay, new object[] { false });

            var startOrth = CameraManager.Instance.ActiveCam.orthographicSize;
            var localPosition = CameraManager.Instance.ActiveCam.transform.localPosition;
            CameraManager.Instance.ActiveCam.orthographicSize = ((float)level.Height) / (CameraManager.Instance.ActiveCam.aspect * 4f);
            CameraManager.Instance.ValidateCameraPosition();
            var currentPosition = CameraManager.Instance.ActiveCam.transform.localPosition;
            currentPosition.x = currentPosition.x + 1;
            CameraManager.Instance.ActiveCam.transform.localPosition = currentPosition;
            logger.LogInfo($"level.Width:{level.Width}, level.Height:{level.Height}, cameraAspect:{CameraManager.Instance.ActiveCam.aspect}, localPosition:{CameraManager.Instance.ActiveCam.transform.position.x}");

            camera.Render();
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.Destroy(renderTexture);

            byte[] bytes = texture2D.EncodeToPNG();
            string text = Application.dataPath + $"/../Screenshot_{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.png";
            File.WriteAllBytes(text, bytes);
            Debug.Log("High-resolution screenshot saved to: " + text);

            CameraManager cameraManager = mainCamera.GetComponent<CameraManager>();
            cameraManager.enabled = true;
            CameraManager.Instance.ActiveCam.orthographicSize = startOrth;
            CameraManager.Instance.ActiveCam.transform.localPosition = localPosition;
            setUserInterfaceVisibility.Invoke(mapGameplay, new object[] { true });

            CheatManager.NoCameraLimit = cmncl;
        }
    }
}