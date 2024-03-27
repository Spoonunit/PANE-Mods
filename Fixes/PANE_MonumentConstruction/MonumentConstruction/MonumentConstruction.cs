using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Pathfinding;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

//Author : InvisibleArrow, Danie!
//Tested By : InvisibleArrow, Danie!
//Tested With PANE Version : 1.5.3

namespace MonumentConstruction
{
    [BepInPlugin(ModGUID, ModName, VersionString)]
    public class MonumentConstruction : BaseUnityPlugin
    {
        private const string ModGUID = "Monument.Construction";
        private const string ModName = "!Fix - Monument Construction";
        private const string VersionString = "1.0.7";
        static ManualLogSource logger;

        static ConfigEntry<bool> conf_noNeededRoads;
        static ConfigEntry<bool> conf_drawBricklayers;

        private void Awake()
        {
            logger = Logger;
            logger.LogInfo($"Plugin {ModName} is loaded!");

            conf_noNeededRoads =    Config.Bind("Settings", "Allow Monument without road", true, "Enabling Access to monument without road. (To apply correctly reload save is needed)");
            conf_drawBricklayers =  Config.Bind("Settings", "Show Bricklayers on Mastabas", true, "Enable show and position Bricklayers correctly on Mastabas (This works only for new created Bricklayers).");
            
            Harmony.CreateAndPatchAll(typeof(MonumentConstruction));
            logger.LogInfo($"{ModName} applied!");
        }
        [HarmonyPatch(typeof(LevelMap), "AreRoadsConnected", new Type[] { typeof(CellCoord), typeof(CellCoord), typeof(bool) })]
        [HarmonyPrefix]
        private static bool ApplyAreRoadsConnectedPatch(LevelMap __instance, ref CellCoord roadA, ref CellCoord roadB, ref bool __result)
        {
            try
            {

                if (conf_noNeededRoads.Value)
                {
                    Cell cellA = __instance.GetCell(roadA);
                    Cell cellB = __instance.GetCell(roadB);

                    if ((cellA != null && cellA.Building != null && cellA.Building.Category == Sector.Monuments) ||
                        (cellB != null && cellB.Building != null && cellB.Building.Category == Sector.Monuments))
                    {
                        __result = true;
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Unable to load {MethodBase.GetCurrentMethod().Name}\n{ex}");
                return true;
            }
        }
        // Get rid of setting walker count variable, because technically these are not storage building walkers, but originating from work camps!
        // They will unnecessarily interfere with the storage buildings other delivery requests which is undesirable.
        // Add ability to walk cross country
        private static GameObject monumentRockPusherPrefab;
        private static GameObject monumentDeliveryManPrefab;

        private static void SetMonumentSeekerFlags(Seeker seeker)
        {
            if (conf_noNeededRoads.Value)
            {
                seeker.traversableTags = 1091;
                seeker.graphMask = 2;
            }
            else
            {
                seeker.traversableTags = 1090;
                seeker.graphMask = 1;

            }
        }

        [HarmonyPatch(typeof(LevelManager), "OnMapReadyCallback")]
        [HarmonyPostfix]
        private static void OnMapReadyCallbackPatch()
        {
            logger.LogInfo("OnMapReadyCallback: Modifying prefabs for monument walkers");
            monumentRockPusherPrefab = GameObject.Find("ModedRockPusherPrefab");
            monumentDeliveryManPrefab = GameObject.Find("ModedDeliveryManPrefab");
            if (monumentRockPusherPrefab == null || monumentDeliveryManPrefab == null)
            {
                monumentRockPusherPrefab = Instantiate(LevelManager.Instance.RockPusherPrefab);
                monumentDeliveryManPrefab = Instantiate(LevelManager.Instance.DeliveryManPrefab);

                monumentRockPusherPrefab.name = "ModedRockPusherPrefab";
                monumentDeliveryManPrefab.name = "ModedDeliveryManPrefab";

            }
            SetMonumentSeekerFlags(GlobalAccessor.GameplaySettings.MonumentLaborerPrefab.GetComponent<Seeker>());
            SetMonumentSeekerFlags(monumentRockPusherPrefab.GetComponent<Seeker>());
            SetMonumentSeekerFlags(monumentDeliveryManPrefab.GetComponent<Seeker>());
        }
        [HarmonyPatch(typeof(StorageBuilding), "SpawnDeliveryForMonument")]
        [HarmonyPrefix]
        private static bool SpawnDeliveryForMonumentPatch(StorageBuilding __instance, MonumentWorkingTicketDelivery a_ticket, Merchandise a_delivery)
        {
            try
            {
                if (conf_noNeededRoads.Value)
                {
                    GameObject gameObject = (a_delivery.IsStone() ? monumentRockPusherPrefab : monumentDeliveryManPrefab);
                    if (a_ticket.Quantity == 0f)
                    {
                        return false;
                    }
                    __instance.Remove(a_delivery.Type, (int)a_delivery.Quantity);
                    WalkerSpawner.SpawnWalker(new WalkerSpawner.Context
                    {
                        WalkerPrefab = gameObject,
                        OriginCoord = __instance.SpawnPoint,
                        OriginBuilding = __instance,
                        WalkerSpawnedCallback = delegate (WalkerBehavior w)
                        {
                            __instance.DeliveryWalker = (CarrierBehavior)w;
                            __instance.DeliveryWalker.Delivery = a_delivery;
                            __instance.DeliveryWalker.Ticket = a_ticket;
                        },
                        WalkerSpawnFailureCallback = delegate
                        {
                        }
                    }.SetDestinationSpawnPoint(a_ticket.Monument), null);
                }
                return false;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Unable to load {MethodBase.GetCurrentMethod().Name}\n{ex}");
                return true;
            }
        }
        [HarmonyPatch(typeof(GuildBuilding), "SpawnMonumentLaborer")]
        [HarmonyPrefix]
        private static bool ApplySpawnMonumentLaborerPatch(ref MonumentWorkingTicketLaborer a_ticket, ref GameObject a_walkerModel)
        {
            try
            {
                if (a_walkerModel != null)
                {
                    Seeker seeker = a_walkerModel.GetComponent<Seeker>();
                    if (seeker != null)
                    {
                        SetMonumentSeekerFlags(seeker);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Unable to load {MethodBase.GetCurrentMethod().Name}\n{ex}");
                return true;
            }
        }
        [HarmonyPatch(typeof(MonumentWorkerBehavior<MonumentWorkingTicket>), "UpdatePosition")]
        [HarmonyPostfix]
        private static void ApplyMonumentWorkerBehaviorrPatch(MonumentWorkerBehavior<MonumentWorkingTicket> __instance, ref int ____cellPathIndex, ref float ____cellPathProgress)
        {
            try
            {
                if (conf_drawBricklayers.Value)
                {
                    FieldInfo movingStateField = __instance.GetType().GetField("_movingState", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (movingStateField != null)
                    {
                        object movingStateValue = movingStateField.GetValue(__instance);

                        if (movingStateValue.ToString() == "Monument")
                        {
                            Monument monument = __instance.Ticket?.Monument;
                            Vector3 localPosition = ((Component)__instance).transform.localPosition;

                            if (monument != null && new[] { BuildingType.MastabaSmall, BuildingType.MastabaMedium, BuildingType.MastabaLarge }.Contains(monument.Type))
                            {
                                Vector3 oldPosition = localPosition;

                                localPosition.z -= monument.CurrentStepIndex;
                                localPosition.y += (float)((monument.CurrentStepIndex - 1) * 0.2);

                                if (____cellPathIndex == 0)
                                {
                                    localPosition = Vector3.LerpUnclamped(oldPosition, localPosition, ____cellPathProgress);
                                }
                                ((Component)__instance).transform.localPosition = localPosition;
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Unable to load {MethodBase.GetCurrentMethod().Name}\n{ex}");
            }
        }
    }
}