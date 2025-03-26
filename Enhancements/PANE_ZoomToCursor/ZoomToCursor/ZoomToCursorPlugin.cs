using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

//Author : SpoonUnit, Danie!
//Tested By : SpoonUnit, Danie!
//Tested With PANE Version : 1.5.3

namespace ZoomToCursor
{
    [BepInPlugin(ModGUID, ModName, VersionString)]
    public class ZoomToCursorPlugin : BaseUnityPlugin
    {
        private const string ModGUID = "PANE.ENH.ZoomToCursor";
        private const string ModName = "Enhancement - Zoom To Cursor";
        private const string VersionString = "1.0.0";
        static ManualLogSource logger;

        static ConfigEntry<bool> conf_zoomToCursor;
        private void Awake()
        {
            logger = Logger;
            logger.LogInfo($"Plugin {ModName} is loaded!");

            conf_zoomToCursor = Config.Bind("Camera", "Allow zoom to cursor", false, "Enabling zoom to cursor position instead to center of actual view.");

            Harmony.CreateAndPatchAll(typeof(ZoomToCursorPlugin));
            logger.LogInfo($"{ModName} applied!");
        }
        [HarmonyPatch(typeof(CameraManager), "Update")]
        [HarmonyPostfix]
        public static void ZoomToMousePatch(ref CameraManager __instance, ref float ____zoomRangeMin, ref float ____zoomStep)
        {
            if (conf_zoomToCursor.Value)
            {
                float axis = Input.GetAxis("Mouse ScrollWheel");

                if (!ControlsHelper.GetKeyHeld(KeyId.BuildingPicker) && !EventSystem.current.IsPointerOverGameObject() && !Mathf.Approximately(axis, 0f) && !MouseHelper.IsMouseOutOfWindow())
                {
                    Vector2 mousePosition = Input.mousePosition;
                    Vector3 mouseWorldPosition = __instance.ActiveCam.ScreenToWorldPoint(mousePosition);
                    Vector3 cameraToMouse = __instance.ActiveCam.transform.position - mouseWorldPosition;

                    float zoomDirection = Mathf.Sign(axis);
                    float zoomAmount = zoomDirection * ____zoomStep;

                    float newZoom = __instance.ActiveCam.orthographicSize - zoomAmount;
                    newZoom = Mathf.Clamp(newZoom, ____zoomRangeMin, __instance.ZoomMax);

                    float scaleFactor = newZoom / __instance.ActiveCam.orthographicSize;
                    cameraToMouse *= scaleFactor;

                    __instance.ActiveCam.transform.position = mouseWorldPosition + cameraToMouse;
                }
            }
        }
    }
}
