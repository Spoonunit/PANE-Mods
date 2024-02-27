using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

//Author : InvisibleArrow, SpoonUnit
//Tested On :
//Tested By :
//Tested With PANE Version :

namespace PANE_Disable_Debt_Defeat
{
    [BepInPlugin(ModGUID, ModName, VersionString)]
    public class PANEDisableDebtDefeat : BaseUnityPlugin
    {
        private const string ModGUID = "PANE.FIX.DisableDebtDefeat";
        private const string ModName = "!Fix - Disable Debt Defeat";
        private const string VersionString = "0.0.1";
        private static ManualLogSource logger;

        // settings
        private static ConfigEntry<bool> enabled;

        private void Awake()
        {
            logger = Logger;
            logger.LogInfo($"Plugin {ModGUID} is loaded!");

            enabled = Config.Bind("Settings",
                                    "Disable Debt Defeat",
                                    true,
                                    "Disable Debt Defeat");

            Harmony.CreateAndPatchAll(typeof(PANEDisableDebtDefeat), null);
            PANEDisableDebtDefeat.logger.LogInfo($"{ModGUID} applied!");
        }

        [HarmonyPatch(typeof(MapGameplay), "ChangeTreasury")]
        [HarmonyPrefix]
        private static bool ChangeTreasuryPatch(MapGameplay __instance, int amount)
        {
            if (enabled.Value)
            {
                LevelMap level = GlobalAccessor.Level;
                UIManager instance = UIManager.Instance;
                if (amount < 0 && __instance.Treasury + amount < 0)
                {
                    if (!level.WinCondition.SandboxMode && __instance.Treasury + amount < -10000)
                    {
                        instance.RegisterDefeatNotification();
                    }
                    else if (__instance.Treasury >= 0)
                    {
                        if (GlobalAccessor.Level.DebtCount == 0 && level.RescueGift > 0)
                        {
                            level.Treasury += level.RescueGift;
                            instance.DisplayNotification(new NotificationContext
                            {
                                Title = "Events/#Notif_OutOfMoney_Title",
                                NotificationLocale = ((level.PharaohRank == PharaohRank.GreatPharaoh) ? "Events/#Notif_OutOfMoney_Content" : "Events/#Notif_OutOfMoney_Content_var"),
                                Type = NotificationType.Warning,
                                OverseerLink = Overseer.Treasury,
                                SourceType = SourceType.Banqueroute,
                                ShowFullscreen = true
                            });
                        }
                        else
                        {
                            instance.DisplayNotification(new NotificationContext
                            {
                                Title = "Events/#Notif_Debt_Title",
                                NotificationLocale = "Events/#Notif_Debt_Content",
                                Type = NotificationType.Warning,
                                OverseerLink = Overseer.Treasury,
                                SourceType = SourceType.Debt,
                                ShowFullscreen = true
                            });
                        }
                        level.DebtCount++;
                    }
                }
                level.Treasury += amount;
                level.ThisYearTreasuryData.Balance = __instance.Treasury;
                instance.DisplayMoney(__instance.Treasury);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(BriefingPanel), "DisplayBriefingPanel")]
        [HarmonyPostfix]
        private static void DisplayBriefingPanelPatch()
        {
            if (enabled.Value)
            {
                GameObject.Find("Canvas/UI/MapInfos/Briefing_Panel/Background/PanelContent/Content/SeparatorContainer").SetActive(false);
                GameObject.Find("Canvas/UI/MapInfos/Briefing_Panel/Background/PanelContent/Content/DefeatHeader (1)").SetActive(false);
                GameObject.Find("Canvas/UI/MapInfos/Briefing_Panel/Background/PanelContent/Content/DefeatText").SetActive(false);
                GameObject.Find("Canvas/UI/MapInfos/Briefing_Panel/Background/PanelContent/Content/Separator").SetActive(false);
            }
        }
    }
}