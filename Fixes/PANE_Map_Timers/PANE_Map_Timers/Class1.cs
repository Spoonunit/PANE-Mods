using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Text.Json;
using TMPro;
//using UnityEngine;

namespace PANE_Map_Timers
{
    public class TimerData
    {
        public Dictionary<string, MapTimerData> Timers { get; set; }
    }

    public class MapTimerData
    {
        public string Type { get; set; }
        public int Length { get; set; }
    }

    [BepInPlugin(modGUID, modName, modVersion)]
    public class PANEMapTimers : BaseUnityPlugin
    {
        private const string modGUID = "PANE.ENH.Map.Timers";
        private const string modName = "Enhancement - Map Timers";
        private const string modVersion = "0.1.0";
        static ManualLogSource logger;

        static TimerData timerData;
        static TextMeshProUGUI cityLabel;
        static string initialCityLabelText;

        // settings
        static ConfigEntry<string> conf_Timers;
        static ConfigEntry<bool> conf_TimersLogging;
        static ConfigEntry<bool> enabled;

        private void Awake()
        {
            logger = Logger;
            logger.LogInfo($"Plugin {modGUID} is loaded!");

            enabled = Config.Bind(
                "Settings",
                "Enable Timers",
                false,
                "Enable Timers"
            );

            conf_Timers = Config.Bind(
                "Timers", 
                "Timer Configuration",
                "" +
                "   {\"Timers\":" +
                "       {" +
                "       \"ValleyKings02-Tut\":" +
                "           {\"Type\":\"L\"," +
                "            \"Length\":8}," +
                "       \"RamsesII02-Qadesh\":" +
                "           {\"Type\":\"L\"," +
                "            \"Length\":6}," +
                "       \"CleopatrasCapitol01-Alexandria\":" +
                "           {\"Type\":\"L\"," +
                "            \"Length\":12}," +
                "       \"Custom6-Enkomi\":" +
                "           {\"Type\":\"L\"," +
                "            \"Length\":18}," +
                "       \"AncientConquerors02-Migdol\":" +
                "           {\"Type\":\"S\"," +
                "            \"Length\":7}," +
                "       \"AncientConquerors03-Tanis\":" +
                "           {\"Type\":\"S\"," +
                "            \"Length\":10}," +
                "       \"CleopatrasCapitol03-Maritis\":" +
                "           {\"Type\":\"S\"" +
                "            ,\"Length\":4}," +
                "       \"CleopatrasCapitol05-Actium\":" +
                "           {\"Type\":\"S\"," +
                "            \"Length\":6}," +
                "       \"Custom32-Warfare\":" +
                "           {\"Type\":\"S\"," +
                "            \"Length\":50}}" +
                "   }",
                "Timer Configuration"
            );
            conf_TimersLogging = Config.Bind(
                "Timers",
                "Extra Logging",
                false,
                "Extra Logging"
            );

            timerData = JsonSerializer.Deserialize<TimerData>(conf_Timers.Value);
            //timerData = JsonUtility.FromJson<TimerData>(conf_Timers.Value);

            Harmony.CreateAndPatchAll(typeof(PANEMapTimers));
            logger.LogInfo($"{modName} applied!");
        }

        // implement map timer logic

        [HarmonyPatch(typeof(LevelManager), "CheckEndingConditions")]
        [HarmonyPrefix]
        private static bool CheckEndingConditionsPatch(ref LevelManager __instance, ref MapGameplay ____mapGameplay)
        {

            logger.LogInfo(cityLabel.text);
            if (conf_TimersLogging.Value)
                logger.LogInfo($"Day:{____mapGameplay.Level.CurrentDay}, Month:{____mapGameplay.Level.CurrentMonth}, Year:{____mapGameplay.Level.CurrentYear}, Level: {____mapGameplay.Level.MapName}, TimerData: {conf_Timers.Value}, TimerData: {timerData.Timers}");

            MapTimerData mapTimerData;
            string label;
            if (timerData.Timers.TryGetValue(____mapGameplay.Level.MapName, out mapTimerData))
            {
                label = $"{initialCityLabelText} : {mapTimerData.Type} : {mapTimerData.Length - (____mapGameplay.Level.CurrentYear - ____mapGameplay.Level.StartingYear)} YRS";
                cityLabel.text = label;
                if (conf_TimersLogging.Value)
                {
                    logger.LogInfo($"This map ({____mapGameplay.Level.MapName}) has configured timer data. Type:{mapTimerData.Type}, Length:{mapTimerData.Length}");
                    logger.LogInfo($"MapLabel:{label}");
                }
                if (mapTimerData.Type == "S" && (____mapGameplay.Level.CurrentYear - ____mapGameplay.Level.StartingYear) < mapTimerData.Length)
                {
                    return false;
                }
                if (mapTimerData.Type == "L" && (____mapGameplay.Level.CurrentYear - ____mapGameplay.Level.StartingYear) >= mapTimerData.Length)
                {
                    UIManager.Instance.RegisterDefeatNotification();
                }
            }
            else
            {
                if (conf_TimersLogging.Value)
                    logger.LogInfo("No Timer data for this map");
            }
            return true;
        }

        [HarmonyPatch(typeof(TopBar), "Init")]
        [HarmonyPostfix]
        private static void TopBarInitPostfix(ref TopBar __instance, ref TextMeshProUGUI ____cityNameLabel)
        {
            cityLabel = ____cityNameLabel;
            initialCityLabelText = cityLabel.text;
        }


    }
}