using UnityEngine;
using BepInEx;
using Nyxpiri.ULTRAKILL.NyxLib;
using System;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Nyxpiri.ULTRAKILL.NyxLib.EnemyTypes;
using Nyxpiri.Unity.Collections;

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

            if (_currentFakeFallDelay < 0)
            {
                _currentFakeFallDelay = UnityEngine.Random.Range(Options.ForcedFakeFallDelayMinWaves.Value, Options.ForcedFakeFallDelayMaxWaves.Value);
            }

            EnemyAmountToAdd = 0;

            TypesToSpawn.Clear();

            var wave = endlessGrid.currentWave;

            int allPoints = pointsFi.GetValue(endlessGrid);
            int maxPoints = Mathf.FloorToInt(allPoints * Options.PointsRatioAllocatedToBosses.Value);
            int points = maxPoints;
            int spawnCostBonus = 0;
            int spawnCostBonusSpent = 0;
            Dictionary<NyxLib.AEnemyType, int> individualSpawnCostBonuses = new Dictionary<NyxLib.AEnemyType, int>();

            Log.Debug($"---------- deciding bosses to spawn with {points} points (allPoints = {allPoints}) -------------");

            bool isFakeFall = false;
            bool canBeFakeFall = true;

            SpawnedLastWave.Clear();

            if (Options.UseForcedFakeFall.Value && wave >= Options.ForcedFakeFallMinWave.Value)
            {
                if (_wavesSinceFakeFall >= _currentFakeFallDelay)
                {
                    isFakeFall = true;
                    Log.Debug($"Forcing fake fall!");
                }
            }

            var enemyEntries = Options.EnemyEntries.ToList();
            HashSet<AEnemyType> denyList = new HashSet<AEnemyType>();

            for (int i = 0; i < Options.BossPickerIterations.Value; i++)
            {
                bool needToChoose = i >= Options.BossPickerIterations.Value / 2.0f;
                enemyEntries.Shuffle();
                foreach (var entryRaw in enemyEntries)
                {
                    var entry = entryRaw.Value;
                    Log.Debug($"{entryRaw.Key} being TESTED to spawn");

                    SpawnCostBoosts.TryAdd(entryRaw.Key, 0);
                    SpawnCooldowns.TryAdd(entryRaw.Key, 0);

                    if (!entry.Enabled.Value)
                    {
                        Log.Debug($"{entryRaw.Key} DENIED on the basis of being not enabled");
                        continue;
                    }

                    if (SpawnCooldowns[entryRaw.Key] > 0)
                    {
                        Log.Debug($"{entryRaw.Key} DENIED on the basis of wave cooldown {SpawnCooldowns[entryRaw.Key]}");
                        continue;
                    }

                    if (wave < entry.SpawnWave.Value)
                    {
                        Log.Debug($"{entryRaw.Key} DENIED on the basis of wave {wave} being less than {entry.SpawnWave.Value}");
                        continue;
                    }

                    if (!needToChoose)
                    {
                        float chooseOdds = Mathf.Clamp(NyxMath.InverseNormalizeToRange(SpawnCooldowns[entryRaw.Key], -10, 0), 0.2f, 1.0f);

                        if (UnityEngine.Random.Range(0.0f, 1.0f) > chooseOdds)
                        {
                            Log.Debug($"{entryRaw.Key} DENIED on the basis of choose odds {chooseOdds}, 'cooldown' {SpawnCooldowns[entryRaw.Key]}");
                            continue;
                        }
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

                    if (UnityEngine.Random.Range(0.0f, 1.0f) < boostPercentage || denyList.Contains(entryRaw.Key))
                    {
                        Log.Debug($"{entryRaw.Key} DENIED on the basis of {boostPercentage} rng roll not working out for it");
                        //denyList.Add(entryRaw.Key);
                        continue;
                    }

                    float spawnCostBonusToAdd = (int)(baseSpawnCost * entry.SpawnCostBonusScalar.Value);
                    float spawnCostRequirement = spawnCost;

                    spawnCostRequirement = spawnCostRequirement * entry.SpawnCostRequirementScalar.Value;

                    foreach (var type in TypesToSpawn)
                    {
                        var otherEntry = Options.EnemyEntries[type];
                        var otherBaseSpawnCost = otherEntry.SpawnCost.Value;
                        var otherSpawnCost = otherBaseSpawnCost + (spawnCostBonus + spawnCostBonusToAdd) + individualSpawnCostBonuses[type] + SpawnCostBoosts[type];
                        float otherSpawnCostRequirement = spawnCost * otherEntry.SpawnCostRequirementScalar.Value;

                        if (spawnCostRequirement < otherSpawnCostRequirement)
                        {
                            Log.Debug($"{entryRaw.Key} opting to use {otherSpawnCostRequirement} as the spawn cost requirement due to previously queued to be spawned type ({type})");
                        }

                        spawnCostRequirement = Math.Max(spawnCostRequirement, otherSpawnCostRequirement);
                    }

                    if (points < (spawnCostRequirement))
                    {
                        Log.Debug($"{entryRaw.Key} DENIED on the basis of {points} being less than {spawnCostRequirement}");
                        continue;
                    }

                    if (entryRaw.Key.VanillaEnumValue == EnemyType.FleshPanopticon || entryRaw.Key.VanillaEnumValue == EnemyType.FleshPrison)
                    {
                        if (TypesToSpawn.Contains(EnemyTypeDB.Instance.GetVanillaType(EnemyType.FleshPanopticon)) || TypesToSpawn.Contains(EnemyTypeDB.Instance.GetVanillaType(EnemyType.FleshPrison)))
                        {
                            Log.Debug($"{entryRaw.Key} DENIED on the basis of conflicting type being intended to spawn");
                            continue;
                        }
                    }

                    var attributes = Options.EnemiesAttributes.GetValueOrDefault(entryRaw.Key, null);

                    if (attributes == null)
                    {
                        attributes = Options.EnemiesAttributes[EnemyTypeDB.Instance.GetVanillaType(EnemyType.Filth)];
                        Log.Warning($"{entryRaw.Key} doesn't have an attributes entry, falling back to Filth attributes entry");
                    }

                    if (!attributes.CanSpawnInFakeFall.Value)
                    {
                        if (!isFakeFall && canBeFakeFall)
                        {
                            Log.Debug($"{entryRaw.Key} selected and canBeFakeFall is true yet isFakeFall is false, can no longer be fake fall.");
                            canBeFakeFall = false;
                        }
                        else if (isFakeFall)
                        {
                            continue;
                        }
                    }

                    if (entryRaw.Key == EnemyTypeDB.Instance.GetVanillaType(EnemyType.Geryon) && canBeFakeFall)
                    {
                        Log.Debug($"Geryon selected, we shall fake fall.");
                        isFakeFall = true;
                    }
                    else if (entryRaw.Key == EnemyTypeDB.Instance.GetVanillaType(EnemyType.Geryon) && !canBeFakeFall)
                    {
                        Log.Debug($"Geryon tried but we can't be fake fall, no fake fall");
                        continue;
                    }

                    SpawnCooldowns[entryRaw.Key] = entryRaw.Value.SpawnCooldown.Value;
                    SpawnedLastWave.Add(entryRaw.Key);
                    points -= spawnCostToSpend;
                    spawnCostBonus += (int)spawnCostBonusToAdd;
                    spawnCostBonusSpent += (int)((int)(baseSpawnCost * entry.SpawnCostBonusScalar.Value) * entry.SpawnCostBonusSpentScalar.Value);
                    individualSpawnCostBonuses[entryRaw.Key] += entry.IndividualCostIncreasePerSpawn.Value;
                    TypesToSpawn.Enqueue(entryRaw.Key);
                    EnemyAmountToAdd += 1;

                    if (entryRaw.Key == EnemyVariants.TundraAgonyType)
                    {
                        EnemyAmountToAdd += 1;
                    }

                    ShouldFakeFall = isFakeFall;

                    Log.Debug($"adding type {entryRaw.Key} to types to spawn for a cost of {spawnCostToSpend} leaving {points} points left (spawnCostBonus: {spawnCostBonus}, spawnCostBonusSpent: {spawnCostBonusSpent})");
                }
            }

            if (ShouldFakeFall)
            {
                Log.Debug($"ShouldFakeFall will == true, _wavesSinceFakeFall: {_wavesSinceFakeFall}, _currentFakeFallDelay: {_currentFakeFallDelay}");
                _currentFakeFallDelay = UnityEngine.Random.Range(Options.ForcedFakeFallDelayMinWaves.Value, Options.ForcedFakeFallDelayMaxWaves.Value);
                _wavesSinceFakeFall = 0;
                Log.Debug($"ShouldFakeFall == true, _wavesSinceFakeFall: {_wavesSinceFakeFall}, _currentFakeFallDelay: {_currentFakeFallDelay}");
            }

            foreach (var entry in TypesToSpawn)
            {
                SpawnCostBoosts[entry] += Options.EnemyEntries[entry].IndividualPersistentSpawnCostBoost.Value;
                SpawnCostBoosts[entry] = Math.Clamp(SpawnCostBoosts[entry], 0, Options.EnemyEntries[entry].IndividualPersistentSpawnCostBoostMax.Value);
            }

            pointsFi.SetValue(endlessGrid, allPoints - (maxPoints - points));
            Log.Debug($"--------- should spawn {TypesToSpawn.Count} bosses -------------");
        }

        internal void UpdateForceFakeFallCooldown()
        {
            _wavesSinceFakeFall += 1;
        }

        internal void UpdateBossCooldowns()
        {
            Dictionary<AEnemyType, int> newSpawnCooldowns = new Dictionary<AEnemyType, int>(SpawnCooldowns);

            foreach (var key in SpawnCooldowns.Keys)
            {
                if (SpawnedLastWave.Contains(key))
                {
                    continue;
                }

                newSpawnCooldowns[key] -= 1;
            }

            Dictionary<AEnemyType, int> newSpawnCostBoosts = new Dictionary<AEnemyType, int>(SpawnCostBoosts);
            foreach (var key in SpawnCostBoosts.Keys)
            {
                if (SpawnedLastWave.Contains(key) || SpawnCooldowns.GetValueOrDefault(key, 0) > 0)
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
        public bool ShouldFakeFall = false;
        internal static int EnemyAmountToAdd { get; set; } = 0;
        int _wavesSinceFakeFall = 0;
        int _currentFakeFallDelay = -10;
        private Dictionary<NyxLib.AEnemyType, int> SpawnCooldowns = new Dictionary<NyxLib.AEnemyType, int>();
        private Dictionary<NyxLib.AEnemyType, int> SpawnCostBoosts = new Dictionary<NyxLib.AEnemyType, int>();
    }
}