﻿using HarmonyLib;
using ProjectM.Gameplay.Systems;
using Unity.Entities;
using Unity.Collections;
using ProjectM.Network;
using ProjectM;
using OpenRPG.Systems;
using OpenRPG.Utils;
using System;

namespace OpenRPG.Hooks;

[HarmonyPatch(typeof(ArmorLevelSystem_Spawn), nameof(ArmorLevelSystem_Spawn.OnUpdate))]
public class ArmorLevelSystem_Spawn_Patch
{
    private static void Prefix(ArmorLevelSystem_Spawn __instance)
    {
        if (ExperienceSystem.isEXPActive)
        {
            EntityManager entityManager = __instance.EntityManager;
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities){
                Entity Owner = entityManager.GetComponentData<EntityOwner>(entity).Owner;
                ArmorLevel level = new ArmorLevel();
                level.Level = 0;
                try
                {
                    if (!entityManager.TryGetComponentData<ArmorLevel>(entity, out level))
                    {
                        throw new MemberNotFoundException("Armor Level Not Found");
                    }
                }
                catch(Exception e){
                    Plugin.LogInfo("AOT Error I think" + e.Message);
                }
                if (!ExperienceSystem.ShouldAllowGearLevel)
                {
                    level.Level = 0;
                }
                else
                {
                    Entity User = __instance.EntityManager.GetComponentData<PlayerCharacter>(Owner).UserEntity;
                    ulong SteamID = __instance.EntityManager.GetComponentData<User>(User).PlatformId;

                    float levelEfficiency = (level.Level / 10 - ExperienceSystem.getLevel(SteamID) / 3) / 2;
                    if (levelEfficiency > 0) level.Level = levelEfficiency * 10;
                }

                entityManager.SetComponentData(entity, level);
            }
        }
    }

}

[HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
public class WeaponLevelSystem_Spawn_Patch
{
    private static void Prefix(WeaponLevelSystem_Spawn __instance)
    {
        if (Helper.buffLogging) Plugin.LogInfo("Weapon System Patch Entry");
        if (ExperienceSystem.isEXPActive || WeaponMasterSystem.isMasteryEnabled)
        {
            EntityManager entityManager = __instance.EntityManager;
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                Entity Owner = entityManager.GetComponentData<EntityOwner>(entity).Owner;
                Entity User = __instance.EntityManager.GetComponentData<PlayerCharacter>(Owner).UserEntity;
                if (WeaponMasterSystem.isMasteryEnabled || ExperienceSystem.ShouldAllowGearLevel || ExperienceSystem.LevelRewardsOn)
                {
                    if (Helper.buffLogging) Plugin.LogInfo(" Applying Moose buff");
                    Helper.ApplyBuff(User, Owner, Helper.AppliedBuff);
                }
                if (ExperienceSystem.isEXPActive)
                {
                    WeaponLevel level = new WeaponLevel();
                    level.Level = 0;
                    try
                    {
                        if(!entityManager.TryGetComponentData<WeaponLevel>(entity, out level))
                        {
                            throw new MemberNotFoundException("Weapon Level Not Found");
                        }
                    }
                    catch(Exception e){
                        Plugin.LogInfo("AOT Error I think" + e.Message);
                    }
                    if (!ExperienceSystem.ShouldAllowGearLevel)
                    {
                        level.Level = 0;                            
                    }
                    else
                    {                            
                        ulong SteamID = __instance.EntityManager.GetComponentData<User>(User).PlatformId;

                        float levelEfficiency = (level.Level * .3f - ExperienceSystem.getLevel(SteamID) / 3) / 2;
                        if (levelEfficiency > 0) level.Level = levelEfficiency / .3f;

                        if (ExperienceSystem.ShouldAllowGearLevel)
                        {
                            if (!Cache.player_geartypedonned.ContainsKey(SteamID) || Cache.player_geartypedonned[SteamID] == null)
                                Cache.player_geartypedonned[SteamID] = new System.Collections.Generic.Dictionary<UnitStatType, float>();
                        }
                    }                      
                    entityManager.SetComponentData(entity, level);
                }
                if (WeaponMasterSystem.isMasteryEnabled)
                {                        
                    if (!entityManager.HasComponent<PlayerCharacter>(Owner)) continue;                       
                }
            }
        }
    }

}

[HarmonyPatch(typeof(WeaponLevelSystem_Destroy), nameof(WeaponLevelSystem_Destroy.OnUpdate))]
public class WeaponLevelSystem_Destroy_Patch
{
    private static void Prefix(WeaponLevelSystem_Destroy __instance)
    {
        if (ExperienceSystem.isEXPActive && (ExperienceSystem.LevelRewardsOn || ExperienceSystem.ShouldAllowGearLevel))
        {
            EntityManager entityManager = __instance.EntityManager;
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                Entity Owner = entityManager.GetComponentData<EntityOwner>(entity).Owner;
                Entity User = __instance.EntityManager.GetComponentData<PlayerCharacter>(Owner).UserEntity;
                ulong SteamID = __instance.EntityManager.GetComponentData<User>(User).PlatformId;
                if (ExperienceSystem.ShouldAllowGearLevel) //experiment with buffing for equipment based on level.
                {
                    if (!Cache.player_geartypedonned.ContainsKey(SteamID) || Cache.player_geartypedonned[SteamID] == null)
                        Cache.player_geartypedonned[SteamID] = new System.Collections.Generic.Dictionary<UnitStatType, float>();
                    //we can accomplish gear bonuses per level using a similar buffing system approach as ability point buffs for leveling.
                    //might need a better data structure...but should be fine in the cache only.
                }
                //reset buffs for being unarmed
                Helper.ApplyBuff(User, Owner, Helper.AppliedBuff);
            }
        }
    }
}

[HarmonyPatch(typeof(SpellLevelSystem_Spawn), nameof(SpellLevelSystem_Spawn.OnUpdate))]
public class SpellLevelSystem_Spawn_Patch
{
    private static void Prefix(SpellLevelSystem_Spawn __instance)
    {
        if (ExperienceSystem.isEXPActive)
        {
            EntityManager entityManager = __instance.EntityManager;
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                SpellLevel level = new SpellLevel();
                level.Level = 0;
                try
                {
                    if (!entityManager.TryGetComponentData<SpellLevel>(entity, out level))
                    {
                        throw new MemberNotFoundException("Spell Level Not Found");
                    }
                }
                catch (Exception e)
                {
                    Plugin.LogInfo("AOT Error I think" + e.Message);
                }
                level.Level = 0;
                entityManager.SetComponentData(entity, level);
            }
        }
    }

    private static void Postfix(SpellLevelSystem_Spawn __instance)
    {
        if (ExperienceSystem.isEXPActive)
        {
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                Entity Owner = __instance.EntityManager.GetComponentData<EntityOwner>(entity).Owner;
                if (!__instance.EntityManager.HasComponent<PlayerCharacter>(Owner)) return;
                if (ExperienceSystem.isEXPActive)
                {
                    Entity User = __instance.EntityManager.GetComponentData<PlayerCharacter>(Owner).UserEntity;
                    ulong SteamID = __instance.EntityManager.GetComponentData<User>(User).PlatformId;
                    ExperienceSystem.SetLevel(Owner, User, SteamID);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(SpellLevelSystem_Destroy), nameof(SpellLevelSystem_Destroy.OnUpdate))]
public class SpellLevelSystem_Destroy_Patch
{
    private static void Prefix(SpellLevelSystem_Destroy __instance)
    {

        if (ExperienceSystem.isEXPActive)
        {
            EntityManager entityManager = __instance.EntityManager;
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                SpellLevel level = new SpellLevel();
                level.Level = 0;
                try
                {
                    if (!entityManager.TryGetComponentData<SpellLevel>(entity, out level))
                    {
                        throw new MemberNotFoundException("Spell Level Not Found");
                    }
                }
                catch (Exception e)
                {
                    Plugin.LogInfo("AOT Error I think" + e.Message);
                }
                entityManager.SetComponentData(entity, level);
            }
        }
    }

    private static void Postfix(SpellLevelSystem_Destroy __instance)
    {
        if (ExperienceSystem.isEXPActive)
        {
            EntityManager entityManager = __instance.EntityManager;
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (!entityManager.HasComponent<LastTranslation>(entity))
                {
                    Entity Owner = entityManager.GetComponentData<EntityOwner>(entity).Owner;
                    if (entityManager.HasComponent<PlayerCharacter>(Owner))
                    {
                        Entity User = entityManager.GetComponentData<PlayerCharacter>(Owner).UserEntity;
                        ulong SteamID = entityManager.GetComponentData<User>(User).PlatformId;
                        ExperienceSystem.SetLevel(Owner, User, SteamID);
                    }
                }
            }
        }
    }
}