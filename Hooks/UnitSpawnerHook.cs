﻿using ProjectM;
using HarmonyLib;
using RPGMods.Utils;
using RPGMods.Systems;

namespace RPGMods.Hooks
{
    [HarmonyPatch(typeof(UnitSpawnerReactSystem), nameof(UnitSpawnerReactSystem.OnUpdate))]
    public static class UnitSpawnerReactSystem_Patch
    {
        public static bool listen = false;
        public static void Prefix(UnitSpawnerReactSystem __instance)
        {
            //if (__instance.__OnUpdate_LambdaJob0_entityQuery != null)
            {
                var entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach (var entity in entities)
                {
                    if (!__instance.EntityManager.HasComponent<LifeTime>(entity)) return;

                    var Duration = __instance.EntityManager.GetComponentData<LifeTime>(entity).Duration;
                    if (Duration == HunterHuntedSystem.ambush_despawn_timer)
                    {
                        // Change faction to Vampire Hunters for spawned units
                        var Faction = __instance.EntityManager.GetComponentData<FactionReference>(entity);
                        Faction.FactionGuid = ModifiablePrefabGUID.Create(entity, __instance.EntityManager, new PrefabGUID((int)Utils.Faction.Type.VampireHunters));
                        __instance.EntityManager.SetComponentData(entity, Faction);
                    }

                    if (listen)
                    {
                        if (Cache.spawnNPC_Listen.TryGetValue(Duration, out var Content))
                        {
                            Content.EntityIndex = entity.Index;
                            Content.EntityVersion = entity.Version;
                            if (Content.Options.Process) Content.Process = true;

                            Cache.spawnNPC_Listen[Duration] = Content;
                            listen = false;
                        }
                    }
                }
            }
        }
    }
}
