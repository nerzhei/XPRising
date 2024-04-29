﻿using System;
using ProjectM;
using Unity.Entities;
using Unity.Mathematics;

namespace OpenRPG.Utils; 

public class SpawnUnit {
    private static Entity emptyEntity = new();
    public enum SpawnFaction {
        Default = 0,
        VampireHunters = 1,
    }
    // Encodes the unit level/faction into the lifetime
    // Set faction/level to 0 to spawn the unit with the default faction/level
    // Encoded as: 99F.LLCC
    // where:
    //    F = faction
    //   LL = level
    //   CC = checksum
    // Note: Due to floats only providing up to 7 digits of info, this is the most we can encode into this value.
    // Some edge cases occur, which are handled appropriately in the decode function.
    public static float EncodeLifetime(int lifetime, int level, SpawnFaction faction) {
        // Lifetime needs to be a multiple of 10 between 10 and 999 for the following code to work.
        lifetime = Math.Clamp((lifetime / 10) * 10, 10, 1000);
        // Faction needs to be between 0 and 9
        var factionAsInt = Math.Clamp((int)faction, 0, 9);
        // Level needs to be between 0 and 99
        level = Math.Clamp(level, 0, 99);
	
        // Adds level and level "checksum" - this assumes a level cap of 99
        var partFaction = factionAsInt; // section: 10X
        var partLevel = level / 1_00f; // section: 100.XX
        var partLevelChecksum = level / 1_00_00f; // section: 100.00XX
        return lifetime + partFaction + partLevel + partLevelChecksum;
    }

    // Decodes a unit level/faction out of the lifetime duration.
    // Returns whether true if the level decodes correctly
    public static bool DecodeLifetime(float lifetime, out int level, out SpawnFaction faction) {
        // Encoded as 99F.LLCC where:
        // F = faction
        // LL = level
        // CC = level checksum
	
        // Get 1 digits for the faction
        var encodedSection = lifetime % 10;
        var decoded = (int)encodedSection;
        faction = Enum.IsDefined(typeof(SpawnFaction), decoded) ? (SpawnFaction)decoded : SpawnFaction.Default;

        // Get 2 digits for the level
        encodedSection = (encodedSection % 1) * 100;
        decoded = (int)encodedSection;
        level = decoded;

        // Get 2 digits for the level check
        encodedSection = (encodedSection % 1) * 100;
        // Need to round this one, as float inaccuracies creep in :(
        var levelCheck = (int)Math.Round(encodedSection);
	
        // There are some edge cases that occur due to floating point implementation.
        // This will clean those edge cases up.
        if (levelCheck != level) {
            switch (level) {
                case 15:
                case 40:
                    levelCheck -= 1;
                    break;
                case 54:
                    levelCheck += 1;
                    break;
            }
        }

        return levelCheck == level;
    }

    public static void Spawn(Prefabs.Units type, float3 position, int count, float minRange, float maxRange, float lifetime) {
        Plugin.Server.GetExistingSystem<UnitSpawnerUpdateSystem>().SpawnUnit(
            emptyEntity, new PrefabGUID((int)type), position, count, minRange, maxRange, lifetime);
    }
}