﻿using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using ProjectM.Gameplay.Systems;
using OpenRPG.Utils;
using ProjectM;
using OpenRPG.Systems;

namespace OpenRPG.Hooks
{
    [HarmonyPatch(typeof(HandleGameplayEventsBase), nameof(HandleGameplayEventsBase.OnUpdate))]
    public class HandleGameplayEventsBase_Patch
    {
        private static string InitialErrorMessage =
            "System.InvalidOperationException: GetSingleton<ProjectM.DayNightCycle>() requires that exactly one ProjectM.DayNightCycle exist that match this query, but there are 0.";
        private static byte CurrentDay = 0;
        private static bool isDNInitialized = false;
        private static void Postfix(HandleGameplayEventsBase __instance)
        {
            //-- Day Cycle Tracking
            if (Plugin.isInitialized)
            {
                try
                {
                    var DNCycle = Plugin.Server.GetExistingSystem<DayNightCycleSystem>().GetSingleton<DayNightCycle>();

                    if (CurrentDay != DNCycle.GameDateTimeNow.Day)
                    {
                        if (!isDNInitialized)
                        {
                            CurrentDay = DNCycle.GameDateTimeNow.Day;
                            isDNInitialized = true;
                        }
                        else
                        {
                            CurrentDay = DNCycle.GameDateTimeNow.Day;
                            if (WorldDynamicsSystem.isFactionDynamic) WorldDynamicsSystem.OnDayCycle();
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.Message.StartsWith(InitialErrorMessage))
                    {
                        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, $"DNC singleton not yet ready. This error should only appear on the initial creation/start-up of the server.");
                    }
                    else
                    {
                        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Error, $"DayNightCycle: {e.Message}");
                    }
                }
            }
            //-- ------------------

            //-- Spawn Custom NPC Task
            if (Cache.spawnNPC_Listen.Count > 0)
            {
                foreach (var item in Cache.spawnNPC_Listen)
                {
                    if (item.Value.Process == false) continue;

                    var entity = item.Value.getEntity();
                    var Option = item.Value.Options;

                    if (Option.ModifyBlood)
                    {
                        if (__instance.EntityManager.HasComponent<BloodConsumeSource>(entity))
                        {
                            var BloodSource = __instance.EntityManager.GetComponentData<BloodConsumeSource>(entity);
                            BloodSource.UnitBloodType = Option.BloodType;
                            BloodSource.BloodQuality = Option.BloodQuality;
                            BloodSource.CanBeConsumed = Option.BloodConsumeable;
                            __instance.EntityManager.SetComponentData(entity, BloodSource);
                        }
                    }

                    if (Option.ModifyStats)
                    {
                        __instance.EntityManager.SetComponentData(entity, Option.UnitStats);
                    }

                    if (item.Value.Duration < 0)
                    {
                        __instance.EntityManager.SetComponentData(entity, new LifeTime()
                        {
                            Duration = 0,
                            EndAction = LifeTimeEndAction.None
                        });
                    }
                    else
                    {
                        __instance.EntityManager.SetComponentData(entity, new LifeTime()
                        {
                            Duration = item.Value.Duration,
                            EndAction = LifeTimeEndAction.Destroy
                        });
                    }

                    Cache.spawnNPC_Listen.Remove(item.Key);
                }
            }
        }
    }
}