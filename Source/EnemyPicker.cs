using UnityEngine;
using BepInEx;
using Nyxpiri.ULTRAKILL.NyxLib;
using System;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Nyxpiri.ULTRAKILL.NyxLib.EnemyTypes;

namespace Nyxpiri.ULTRAKILL.CybergrindBosses
{
    public class BossPicker
    {
        private void Reset()
        {
            SpawnCooldowns.Clear();
            TypesToSpawn.Clear();
            EnemyAmountToAdd = 0;
        }

        public static FieldAccess<EndlessGrid, int> pointsFi = new FieldAccess<EndlessGrid, int>("points");

        public void SolveBossesToSpawn(EndlessGrid endlessGrid)
        {
            Assert.IsTrue(Cheats.Enabled);

            EnemyAmountToAdd = 0;

            TypesToSpawn.Clear();

            var wave = endlessGrid.currentWave;

            int allPoints = pointsFi.GetValue(endlessGrid);
            int maxPoints = Mathf.FloorToInt(allPoints * Options.PointsRatioAllocatedToBosses.Value);
            int points = maxPoints;
            int spawnCostBonus = 0;
            int spawnCostBonusSpent = 0;
            Dictionary<NyxLib.AEnemyType, int> individualSpawnCostBonuses = new Dictionary<NyxLib.AEnemyType, int>();

            Log.Debug($"deciding bosses to spawn with {points} points (allPoints = {allPoints})");

            HashSet<AEnemyType> deniedList = new HashSet<AEnemyType>();

            for (int i = 0; i < Options.BossSpawnIterations.Value && points > 0; i++)
            {
                var entryRaw = Options.EnemyEntries.ElementAt(UnityEngine.Random.Range(0, Options.EnemyEntries.Count));
                var entry = entryRaw.Value;

                SpawnCostBoosts.TryAdd(entryRaw.Key, 0);

                if (!entry.Enabled.Value)
                {
                    //Log.Debug($"{entryRaw.Key} DENIED on the basis of being not enabled");
                    continue;
                }

                if (SpawnCooldowns.GetValueOrDefault(entryRaw.Key, 0) > 0)
                {
                    continue;
                }

                if (wave < entry.SpawnWave.Value)
                {
                    //Log.Debug($"{entryRaw.Key} DENIED on the basis of wave {wave} being less than {entry.SpawnWave.Value}");
                    continue;
                }

                individualSpawnCostBonuses.TryAdd(entryRaw.Key, 0);

                var baseSpawnCost = entry.SpawnCost.Value;
                var spawnCost = baseSpawnCost + spawnCostBonus + individualSpawnCostBonuses[entryRaw.Key] + SpawnCostBoosts[entryRaw.Key];
                var boostPercentage = NyxMath.NormalizeToRange(SpawnCostBoosts[entryRaw.Key], 0, entryRaw.Value.IndividualPersistentSpawnCostBoostMax.Value);
                var spawnCostToSpend = (int)(baseSpawnCost * entry.SpawnCostSpentScalar.Value) + spawnCostBonusSpent;

                if (entryRaw.Value.IndividualPersistentSpawnCostBoostMax.Value == 0)
                {
                    boostPercentage = 0;
                }

                if (deniedList.Contains(entryRaw.Key))
                {
                    continue;
                }

                if (UnityEngine.Random.Range(0.0f, 1.0f) < boostPercentage)
                {
                    deniedList.Add(entryRaw.Key);
                    continue;
                }

                if (points < (spawnCost * entry.SpawnCostRequirementScalar.Value))
                {
                    //Log.Debug($"{entryRaw.Key} DENIED on the basis of {points} being less than {spawnCost * entry.SpawnCostRequirementScalar.Value}");
                    continue;
                }

                SpawnCooldowns[entryRaw.Key] = entryRaw.Value.SpawnCooldown.Value;

                if (entryRaw.Key.VanillaEnumValue == EnemyType.FleshPanopticon || entryRaw.Key.VanillaEnumValue == EnemyType.FleshPrison)
                {
                    if (TypesToSpawn.Contains(EnemyTypeDB.Instance.GetVanillaType(EnemyType.FleshPanopticon)) || TypesToSpawn.Contains(EnemyTypeDB.Instance.GetVanillaType(EnemyType.FleshPrison)))
                    {
                        continue;
                    }
                }

                SpawnedLastWave.Add(entryRaw.Key);
                points -= spawnCostToSpend;
                spawnCostBonus += (int)(baseSpawnCost * entry.SpawnCostBonusScalar.Value);
                spawnCostBonusSpent += (int)(baseSpawnCost * entry.SpawnCostBonusSpentScalar.Value);
                individualSpawnCostBonuses[entryRaw.Key] += entry.IndividualCostIncreasePerSpawn.Value;
                TypesToSpawn.Enqueue(entryRaw.Key);
                EnemyAmountToAdd += 1;

                if (entryRaw.Key == EnemyVariants.TundraAgonyType)
                {
                    EnemyAmountToAdd += 1;
                }

                Log.Debug($"adding type {entryRaw.Key} to types to spawn");
            }

            foreach (var entry in TypesToSpawn)
            {
                SpawnCostBoosts[entry] += Options.EnemyEntries[entry].IndividualPersistentSpawnCostBoost.Value;
                SpawnCostBoosts[entry] = Math.Clamp(SpawnCostBoosts[entry], 0, Options.EnemyEntries[entry].IndividualPersistentSpawnCostBoostMax.Value);
            }

            pointsFi.SetValue(endlessGrid, allPoints - (maxPoints - points));
            Log.Debug($"should spawn {TypesToSpawn.Count} types");
        }

        internal void UpdateBossCooldowns()
        {
            Dictionary<AEnemyType, int> newSpawnCooldowns = new Dictionary<AEnemyType, int>(SpawnCooldowns);

            foreach (var key in SpawnCooldowns.Keys)
            {
                newSpawnCooldowns[key] -= 1;
            }

            Dictionary<AEnemyType, int> newSpawnCostBoosts = new Dictionary<AEnemyType, int>(SpawnCostBoosts);
            foreach (var key in SpawnCostBoosts.Keys)
            {
                if (SpawnedLastWave.Contains(key))
                {
                    continue;
                }
                newSpawnCostBoosts[key] -= Options.EnemyEntries[key].IndividualPersistentSpawnCostBoostDecay.Value;
                newSpawnCostBoosts[key] = Math.Max(newSpawnCostBoosts[key], 0);
            }

            SpawnCooldowns = newSpawnCooldowns;
            SpawnCostBoosts = newSpawnCostBoosts;
        }

        public Queue<NyxLib.AEnemyType> TypesToSpawn { get; private set; } = new Queue<NyxLib.AEnemyType>();
        public HashSet<NyxLib.AEnemyType> SpawnedLastWave { get; private set; } = new HashSet<NyxLib.AEnemyType>();
        internal static int EnemyAmountToAdd { get; set; } = 0;
        private Dictionary<NyxLib.AEnemyType, int> SpawnCooldowns = new Dictionary<NyxLib.AEnemyType, int>();
        private Dictionary<NyxLib.AEnemyType, int> SpawnCostBoosts = new Dictionary<NyxLib.AEnemyType, int>();
    }
}