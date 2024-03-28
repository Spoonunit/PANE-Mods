using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PANETooltipPosition
{
    [BepInPlugin(ModGUID, ModName, VersionString)]
    public class PANETooltipPosition : BaseUnityPlugin
    {
        private const string ModGUID = "Tooltip.Position.Fix";
        private const string ModName = "!Fix - Tooltip Position";
        private const string VersionString = "1.0.2";
        static ManualLogSource logger;

        static ConfigEntry<bool> conf_tooltip;
        private void Awake()
        {
            logger = Logger;
            logger.LogInfo($"Plugin {ModName} is loaded!");

            conf_tooltip = Config.Bind("UI / UX", "Adjust tooltip position on ultra-wide", true, "Enabling this setting will fix the tooltip position for ultra-wide displays");

            Harmony.CreateAndPatchAll(typeof(PANETooltipPosition));
            logger.LogInfo($"{ModName} applied!");
        }
        [HarmonyPatch(typeof(CameraManager), "GetRatioedMousePos")]
        [HarmonyPrefix]
        private static bool GetRatioedMousePosPatch(ref Vector2 offset, ref CameraManager __instance, ref Vector2 __result)
        {
            if (conf_tooltip.Value)
            {
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                Vector2 scaleFactor = new Vector2(1080f / screenSize.y, 1080f / screenSize.y);
                Vector2 cameraRectSize = __instance.ActiveCam.rect.size * screenSize;
                Vector2 aspectRatio = new Vector2(screenSize.x / cameraRectSize.x, screenSize.y / cameraRectSize.y);
                Vector2 letterboxOffset = new Vector2((screenSize.x - cameraRectSize.x) / 2f, (screenSize.y - cameraRectSize.y) / 2f);
                //logger.LogInfo($"Screen Size: {screenSize}, Camera Rect Size: {cameraRectSize}, Aspect Ratio: {aspectRatio}, Letter Box offset: {letterboxOffset}, Offset: {offset}");
                //logger.LogInfo($"Mouse Position x: {Input.mousePosition.x}, Mouse Position y: {Input.mousePosition.y}");
                __result = (new Vector2(Input.mousePosition.x, Input.mousePosition.y) - letterboxOffset + offset) * aspectRatio * scaleFactor;
                //logger.LogInfo($"Result: {__result}");
                return false;
            }
            return true;
        }
    }
}
