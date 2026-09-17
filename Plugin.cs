using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EikthyrDropDeerMeat
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class EikthyrDropDeerMeatPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.motikofus.eikthyrdropdeermeat";
        public const string PluginName = "Eikthyr Drop Deer Meat";
        public const string PluginVersion = "2.0.1";

        internal static BepInEx.Logging.ManualLogSource Log;

        internal const int TierCount = 6;
        internal static readonly ConfigEntry<int>[] Weights = new ConfigEntry<int>[TierCount];
        internal static readonly ConfigEntry<int>[] Quantities = new ConfigEntry<int>[TierCount];

        private static readonly int[] DefaultWeights = { 45, 22, 13, 9, 6, 5 };
        private static readonly int[] DefaultQuantities = { 5, 6, 7, 8, 9, 10 };

        // Prefab hashes resolved once at load: no hashing at runtime.
        internal static readonly int EikthyrHash = StableHash("Eikthyr");
        internal static readonly int DeerMeatHash = StableHash("DeerMeat");

        // ItemDrop.Save() is not public in the non-publicized assembly, so it is
        // bound once here instead of being called directly.
        private static MethodInfo itemDropSave;

        // Death positions queued by the ZDO watcher, consumed on the next frame.
        // Spawning directly inside the ZDOMan callback would mutate its collections
        // while it is iterating them.
        private static readonly List<Vector3> PendingSpawns = new List<Vector3>(2);

        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;

            for (int i = 0; i < TierCount; i++)
            {
                int tier = i + 1;

                Weights[i] = Config.Bind(
                    "Loot",
                    $"Weight{tier}",
                    DefaultWeights[i],
                    $"The chance and total amount of tier {tier} drops. " +
                    "Weights are relative: they are summed across all tiers, so they do not have to add up to 100. " +
                    "Set to 0 to disable this tier.");

                Quantities[i] = Config.Bind(
                    "Loot",
                    $"Weight{tier}Quantity",
                    DefaultQuantities[i],
                    $"Amount of deer meat dropped when tier {tier} is rolled.");
            }

            try
            {
                itemDropSave = AccessTools.Method(typeof(ItemDrop), "Save");
                if (itemDropSave == null)
                    Logger.LogWarning($"[{PluginName}] ItemDrop.Save() not found, falling back to writing the stack directly on the ZDO.");

                harmony = new Harmony(PluginGUID);

                // Patched manually so a signature change in a future Valheim update
                // produces a clear log line instead of breaking the whole plugin.
                MethodInfo target = AccessTools.Method(typeof(ZDOMan), "HandleDestroyedZDO", new[] { typeof(ZDOID) });
                if (target == null)
                {
                    Logger.LogError($"[{PluginName}] ZDOMan.HandleDestroyedZDO(ZDOID) not found. " +
                                    "This Valheim version is not supported, the mod will stay idle.");
                    return;
                }

                harmony.Patch(target, prefix: new HarmonyMethod(
                    AccessTools.Method(typeof(EikthyrDeathWatcher), nameof(EikthyrDeathWatcher.Prefix))));

                Logger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
                Logger.LogInfo($"[{PluginName}] Server-side only: install it on the dedicated server, or on the host " +
                               "in host & play. Clients (including consoles) need nothing.");
                Logger.LogInfo($"[{PluginName}] Config file: BepInEx/config/{PluginGUID}.cfg");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{PluginName}] Harmony patching failed: {ex}");
            }
        }

        private void OnDestroy()
        {
            if (harmony != null)
                harmony.UnpatchSelf();
        }

        internal static void QueueSpawn(Vector3 position)
        {
            PendingSpawns.Add(position);
        }

        private void Update()
        {
            // Costs a single int comparison per frame in the overwhelming majority of cases.
            if (PendingSpawns.Count == 0)
                return;

            for (int i = 0; i < PendingSpawns.Count; i++)
            {
                try
                {
                    SpawnDeerMeat(PendingSpawns[i]);
                }
                catch (Exception ex)
                {
                    Log?.LogError($"[{PluginName}] Failed to spawn bonus loot: {ex}");
                }
            }

            PendingSpawns.Clear();
        }

        private static void SpawnDeerMeat(Vector3 position)
        {
            if (ZNetScene.instance == null)
                return;

            int amount = RollDeerMeatAmount();
            if (amount <= 0)
            {
                Log?.LogInfo($"[{PluginName}] No valid tier configured, no bonus drop.");
                return;
            }

            GameObject prefab = ZNetScene.instance.GetPrefab(DeerMeatHash);
            if (prefab == null)
            {
                Log?.LogWarning($"[{PluginName}] Prefab 'DeerMeat' not found in ZNetScene.");
                return;
            }

            int maxStack = 1;
            ItemDrop prefabDrop = prefab.GetComponent<ItemDrop>();
            if (prefabDrop != null && prefabDrop.m_itemData != null && prefabDrop.m_itemData.m_shared != null)
                maxStack = Mathf.Max(1, prefabDrop.m_itemData.m_shared.m_maxStackSize);

            int spawned = 0;
            int remaining = amount;

            // Split into several stacks if someone configures a huge amount.
            while (remaining > 0)
            {
                int stack = Mathf.Min(remaining, maxStack);
                remaining -= stack;

                Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.6f;
                Vector3 spawnPos = position + new Vector3(offset.x, 0.75f, offset.y);

                GameObject go = UnityEngine.Object.Instantiate(prefab, spawnPos, Quaternion.identity);
                if (go == null)
                    continue;

                ZNetView nview = go.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid())
                    continue;

                ZDO zdo = nview.GetZDO();

                ItemDrop itemDrop = go.GetComponent<ItemDrop>();
                if (itemDrop != null && itemDrop.m_itemData != null)
                {
                    itemDrop.m_itemData.m_stack = stack;

                    if (itemDropSave != null)
                        itemDropSave.Invoke(itemDrop, null);
                    else if (zdo != null)
                        zdo.Set("stack", stack);
                }

                // Release ownership so the nearest player takes it over and simulates the
                // physics. Without this, the item would stay frozen in the air whenever the
                // authoritative machine is not loading that area (dedicated server).
                if (zdo != null)
                    zdo.SetOwner(0L);

                spawned += stack;
            }

            Log?.LogInfo($"[{PluginName}] Spawned {spawned} DeerMeat at {position} after Eikthyr's death.");
        }

        // Rolls a deer meat amount based on the configured weights.
        // Returns 0 if no valid tier is configured (all weights at 0).
        internal static int RollDeerMeatAmount()
        {
            int totalWeight = 0;
            for (int i = 0; i < TierCount; i++)
                totalWeight += Mathf.Max(0, Weights[i].Value);

            if (totalWeight <= 0)
                return 0;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < TierCount; i++)
            {
                int weight = Mathf.Max(0, Weights[i].Value);
                if (weight <= 0)
                    continue;

                cumulative += weight;
                if (roll < cumulative)
                    return Mathf.Max(0, Quantities[i].Value);
            }

            return Mathf.Max(0, Quantities[TierCount - 1].Value);
        }

        // Same algorithm as Valheim's own string hashing, reimplemented here so the
        // plugin does not need a reference to assembly_utils.dll.
        private static int StableHash(string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + hash2 * 1566083941;
            }
        }
    }

    // ---------------------------------------------------------------------
    // Death detection.
    //
    // Whoever kills Eikthyr (a modded host, or an unmodded PC/console client)
    // destroys its ZDO, and that destruction is routed to every machine,
    // including the authoritative one. Hooking it is the only way to react to
    // the kill without requiring the mod on the clients, and it hands us the
    // exact death position.
    // ---------------------------------------------------------------------
    public static class EikthyrDeathWatcher
    {
        // Guards against the same destruction being processed twice.
        private static ZDOID lastHandled = ZDOID.None;

        // __0 instead of the parameter name: survives a rename in a game update.
        public static void Prefix(ZDOID __0)
        {
            try
            {
                // Only the authoritative machine hands out the bonus loot, so it is
                // granted exactly once whatever the setup.
                if (ZNet.instance == null || !ZNet.instance.IsServer())
                    return;

                ZDOMan zdoMan = ZDOMan.instance;
                if (zdoMan == null)
                    return;

                ZDO zdo = zdoMan.GetZDO(__0);
                if (zdo == null || zdo.GetPrefab() != EikthyrDropDeerMeatPlugin.EikthyrHash)
                    return;

                if (__0 == lastHandled)
                    return;

                lastHandled = __0;
                EikthyrDropDeerMeatPlugin.QueueSpawn(zdo.GetPosition());
            }
            catch (Exception ex)
            {
                EikthyrDropDeerMeatPlugin.Log?.LogError(
                    $"[{EikthyrDropDeerMeatPlugin.PluginName}] Exception in EikthyrDeathWatcher: {ex}");
            }
        }
    }
}
