using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace PEAK_CozyCampfire;

// Here are some basic resources on code style and naming conventions to help
// you in your first CSharp plugin!
// https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
// https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces

// This BepInAutoPlugin attribute comes from the Hamunii.BepInEx.AutoPlugin
// NuGet package, and it will generate the BepInPlugin attribute for you!
// For more info, see https://github.com/Hamunii/BepInEx.AutoPlugin
[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;

    private static List<Campfire> campfireCache = [];

    private void Awake()
    {
        // BepInEx gives us a logger which we can use to log information.
        // See https://lethal.wiki/dev/fundamentals/logging
        Log = Logger;

        // BepInEx also gives us a config file for easy configuration.
        // See https://lethal.wiki/dev/intermediate/custom-configs

        // We can apply our hooks here.
        // See https://lethal.wiki/dev/fundamentals/patching-code
        Harmony val = new($"{Id}");
        val.PatchAll();

        // Log our awake here so we can see it in LogOutput.log file
        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    [HarmonyPatch(typeof(Campfire), "Awake")]
    private class Campfire_Awake_Patch
    {
        private static void Postfix(Campfire __instance)
        {
            __instance.burnsFor = float.PositiveInfinity;
            __instance.beenBurningFor = 0f;

            if (!campfireCache.Contains(__instance))
            {
                campfireCache.Add(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(CharacterAfflictions), "UpdateNormalStatuses")]
    private class Patch_UpdateNormalStatuses
    {
        private static readonly Dictionary<CharacterAfflictions, Character> playerCache = [];

        private static bool Prefix(CharacterAfflictions __instance)
        {
            if (!playerCache.TryGetValue(__instance, out var player))
            {
                player = __instance.GetComponent<Character>();
                if (player != null)
                {
                    playerCache[__instance] = player;
                }
            }

            if (player == null || !player.IsLocal || player.data.dead)
            {
                return true;
            }

            Vector3 playerPosition = player.Center;
            foreach (Campfire campfire in campfireCache)
            {
                Vector3 positionDiff = campfire.transform.position - playerPosition;
                float playerDistanceSqr = positionDiff.sqrMagnitude;
                float radiusSqr = campfire.moraleBoostRadius * campfire.moraleBoostRadius;
                if (playerDistanceSqr <= radiusSqr)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
