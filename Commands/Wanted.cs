﻿using ProjectM;
using ProjectM.Network;
using RPGMods.Systems;
using RPGMods.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using Color = RPGMods.Utils.Color;
using Faction = RPGMods.Utils.Faction;
using Random = System.Random;

namespace RPGMods.Commands
{
    [CommandGroup("wanted", "w")]
    public static class Wanted{
        private static EntityManager entityManager = Plugin.Server.EntityManager;
        
        private static Random generate = new();
        
        [Command("get","g", "", "Shows your current wanted level", adminOnly: false)]
        public static void GetWanted(ChatCommandContext ctx){
            if (!HunterHuntedSystem.isActive){
                ctx.Reply("HunterHunted system is not enabled.");
                return;
            }
            var user = ctx.Event.User;
            var userEntity = ctx.Event.SenderUserEntity;
            
            var heatData = HunterHuntedSystem.GetPlayerHeat(userEntity);

            foreach (Faction.Type faction in FactionHeat.ActiveFactions) {
                var heat = heatData.heat[faction];
                Output.SendLore(userEntity, FactionHeat.GetFactionStatus(faction, heat.level));
                
                if (user.IsAdmin)
                {
                    var sinceAmbush = DateTime.Now - heat.lastAmbushed;
                    var nextAmbush = Math.Max((int)(HunterHuntedSystem.ambush_interval - sinceAmbush.TotalSeconds), 0);
                    Output.SendLore(userEntity, $"Level: {Color.White(heat.level.ToString())} Possible ambush in {Color.White(nextAmbush.ToString())}s Chance: {Color.White(HunterHuntedSystem.ambush_chance.ToString())}%");
                }
            }
        }

        [Command("set","s", "[name, faction, value]", "Sets the current wanted level", adminOnly: true)]
        public static void SetWanted(ChatCommandContext ctx, string name, string faction, int value) {
            bool isAllowed = ctx.Event.User.IsAdmin || PermissionSystem.PermissionCheck(ctx.Event.User.PlatformId, "wanted_args");
            if (isAllowed){
                var user = ctx.Event.User;
                var SteamID = user.PlatformId;
                var userEntity = ctx.Event.SenderUserEntity;
                
                if (Helper.FindPlayer(name, true, out var targetEntity, out var targetUserEntity))
                {
                    SteamID = entityManager.GetComponentData<User>(targetUserEntity).PlatformId;
                    userEntity = targetUserEntity;
                }
                else
                {
                    ctx.Reply($"Could not find specified player \"{name}\".");
                    return;
                }

                if (!Enum.TryParse(faction, true, out Faction.Type heatFaction)) {
                    ctx.Reply("Faction not yet supported");
                    return;
                }

                // Set wanted level and reset last ambushed so the user can be ambushed from now (ie, greater than ambush_interval seconds ago) 
                var updatedHeatData = HunterHuntedSystem.SetPlayerHeat(
                    userEntity,
                    heatFaction,
                    value,
                    DateTime.Now - TimeSpan.FromSeconds(HunterHuntedSystem.ambush_interval + 1));
                
                ctx.Reply($"Player \"{name}\" wanted value changed.");
                ctx.Reply(updatedHeatData.ToString());
            }
        }
        
        [Command("spawn","sp", "[name, faction]", "Spawns the current wanted level on the user", adminOnly: true)]
        public static void SpawnFaction(ChatCommandContext ctx, string name, string faction) {
            var isAllowed = ctx.Event.User.IsAdmin || PermissionSystem.PermissionCheck(ctx.Event.User.PlatformId, "wanted_args");
            if (!isAllowed) return;
            
            var userEntity = ctx.Event.SenderUserEntity;
                
            if (!Helper.FindPlayer(name, true, out var targetEntity, out var targetUserEntity))
            {
                ctx.Reply($"Could not find specified player \"{name}\".");
                return;
            }

            if (!Enum.TryParse(faction, true, out Faction.Type heatFaction)) {
                ctx.Reply("Faction not yet supported");
                return;
            }

            var heatData = HunterHuntedSystem.GetPlayerHeat(userEntity);

            var heat = heatData.heat[heatFaction];
            // Update faction spawn time (or else as soon as they are in combat it might spawn more)
            HunterHuntedSystem.SetPlayerHeat(userEntity, heatFaction, heat.level, DateTime.Now);

            FactionHeat.Ambush(userEntity, targetUserEntity, heatFaction, heat.level, generate);
        }
    }
}
