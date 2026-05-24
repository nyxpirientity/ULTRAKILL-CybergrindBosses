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

            Log.Debug($"deciding bosses to spawn with {points} points (allPoints = {allPoints})");

            for (int i = 0; i < Options.BossSpawnIterations.Value && points > 0; i++)
            {
                var entryRaw = Options.EnemyEntries.ElementAt(UnityEngine.Random.Range(0, Options.EnemyEntries.Count));
                var entry = entryRaw.Value;

                if (!entry.Enabled.Value)
                {
                    Log.Debug($"{entryRaw.Key} DENIED on the basis of being not enabled");
                    continue;
                }

                if (SpawnCooldowns.GetValueOrDefault(entryRaw.Key, 0) > 0)
                {
                    continue;
                }

                if (wave < entry.SpawnWave.Value)
                {
                    Log.Debug($"{entryRaw.Key} DENIED on the basis of wave {wave} being less than {entry.SpawnWave.Value}");
                    continue;
                }

                var baseSpawnCost = entry.SpawnCost.Value;
                var spawnCost = baseSpawnCost + spawnCostBonus;

                if (TypesToSpawn.Contains(entryRaw.Key))
                {
                    Log.Debug($"{entryRaw.Key} DENIED on the basis of already being intended to be spawned");
                    continue;
                }

                if (points < (spawnCost * entry.SpawnCostRequirementScalar.Value))
                {
                    Log.Debug($"{entryRaw.Key} DENIED on the basis of {points} being less than {spawnCost * entry.SpawnCostRequirementScalar.Value}");
                    continue;
                }

                SpawnCooldowns[entryRaw.Key] = entryRaw.Value.SpawnCooldown.Value;

                if (entryRaw.Key.VanillaEnumValue == EnemyType.FleshPanopticon || entryRaw.Key.VanillaEnumValue == EnemyType.FleshPrison || entryRaw.Key == EnemyVariants.BloodTree)
                {
                    if (TypesToSpawn.Contains(EnemyTypeDB.Instance.GetVanillaType(EnemyType.FleshPanopticon)) || TypesToSpawn.Contains(EnemyTypeDB.Instance.GetVanillaType(EnemyType.FleshPrison)) || TypesToSpawn.Contains(EnemyVariants.BloodTree))
                    {
                        continue;
                    }
                }

                spawnCostBonus += (int)(baseSpawnCost * entry.SpawnCostBonusScalar.Value);
                points -= (int)(spawnCost * entry.SpawnCostSpentScalar.Value);
                TypesToSpawn.Enqueue(entryRaw.Key);
                EnemyAmountToAdd += 1;

                if (entryRaw.Key == EnemyVariants.TundraAgonyType)
                {
                    EnemyAmountToAdd += 1;
                }

                Log.Debug($"adding type {entryRaw.Key} to types to spawn");
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

            SpawnCooldowns = newSpawnCooldowns;
        }

        public Queue<NyxLib.AEnemyType> TypesToSpawn { get; private set; } = new Queue<NyxLib.AEnemyType>();
        internal static int EnemyAmountToAdd { get; set; } = 0;
        private Dictionary<NyxLib.AEnemyType, int> SpawnCooldowns = new Dictionary<NyxLib.AEnemyType, int>();
    }
}