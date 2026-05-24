using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Nyxpiri.ULTRAKILL.NyxLib;
using Nyxpiri.ULTRAKILL.NyxLib.EnemyTypes;

namespace Nyxpiri.ULTRAKILL.CybergrindBosses
{
    public static class Options
    {
        public class EnemyEntry
        {
            public ConfigEntry<bool> Enabled = null;
            public ConfigEntry<int> SpawnCost = null;
            public ConfigEntry<float> SpawnCostBonusScalar = null;
            public ConfigEntry<float> SpawnCostRequirementScalar = null;
            public ConfigEntry<float> SpawnCostSpentScalar = null;
            public ConfigEntry<int> SpawnWave = null;
            public ConfigEntry<float> HealthScalar = null;
            public ConfigEntry<int> SpawnCooldown = null;
            public ConfigEntry<int> IndividualCostIncreasePerSpawn = null;
            public ConfigEntry<int> IndividualPersistentSpawnCostBoostMax = null;
            public ConfigEntry<int> IndividualPersistentSpawnCostBoostDecay = null;
            public ConfigEntry<int> IndividualPersistentSpawnCostBoost = null;

            public ConfigEntry<bool> ShowBossBar { get; internal set; }
        }

        public static Dictionary<NyxLib.AEnemyType, EnemyEntry> EnemyEntries = new Dictionary<NyxLib.AEnemyType, EnemyEntry>();
        public static bool EnemyEntriesInitialized = false;

        public static ConfigEntry<float> PointsRatioAllocatedToBosses { get; private set; } = null;
        public static ConfigEntry<int> BossSpawnIterations { get; private set; } = null;
        public static ConfigEntry<int> BossWaveCooldownMin { get; private set; } = null;
        public static ConfigEntry<int> BossWaveCooldownMax { get; private set; }
        public static ConfigEntry<bool> OnlyCountBossWavesTowardsBossCooldowns { get; private set; }
        public static ConfigEntry<bool> UseBossWaveCooldown { get; private set; }

        public static ConfigEntry<float> BloodTreeEnemyCountFillSpeedBase = null;
        public static ConfigEntry<float> BloodTreeWaveHpFillSpeedBase = null;
        public static ConfigEntry<float> BloodTreeFillSpeedBlend = null;
        public static ConfigEntry<float> BloodTreeCatcherRadius = null;
        public static ConfigEntry<float> BloodTreeCatcherHeight = null;

        public static ConfigEntry<float> FleshPrisonInsigniaSizeScalar { get; private set; }

        internal static void Initialize(BaseUnityPlugin plugin)
        {
            Assert.IsNotNull(plugin);

            _config = plugin.Config;

            _configFileManager = plugin.gameObject.AddComponent<ConfigFileManager>();
            _configFileManager.Initialize(_config);
            _configFileManager.OnReload += Reload;

            PointsRatioAllocatedToBosses = _config.Bind("General", "PointsRatioAllocatedToBosses", 0.5f);
            BossSpawnIterations = _config.Bind("General", "BossSpawnIterations", 75);
            BossWaveCooldownMin = _config.Bind("General", "GlobalBossWaveCooldownMin", 2);
            BossWaveCooldownMax = _config.Bind("General", "GlobalBossWaveCooldownMax", 2);
            UseBossWaveCooldown = _config.Bind("General", "UseGlobalBossWaveCooldown", true);
            OnlyCountBossWavesTowardsBossCooldowns = _config.Bind("General", "OnlyCountBossWavesTowardsBossCooldowns", true);

            AddEnemyType(enemyType: EnemyType.Geryon,
                         enabled: false,
                         spawnCost: 1000,
                         spawnWave: 1,
                         showBossBar: true,
                         healthScalar: 0.5f,
                         spawnCooldown: 70,
                         spawnCostBonusScalar: 5.0f,
                         spawnCostRequirementScalar: 2.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 0,
                         individualPersistentSpawnCostBoostMax: 0,
                         individualPersistentSpawnCostBoostDecay: 0
                         );

            AddEnemyType(enemyType: EnemyType.CancerousRodent,
                         enabled: true,
                         spawnCost: 150,
                         spawnWave: 1,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 10,
                         spawnCostBonusScalar: 0.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 0.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 0,
                         individualPersistentSpawnCostBoostMax: 0,
                         individualPersistentSpawnCostBoostDecay: 0
                         );

            AddEnemyType(enemyType: EnemyType.VeryCancerousRodent,
                         enabled: true,
                         spawnCost: 250,
                         spawnWave: 30,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 15,
                         spawnCostBonusScalar: 0.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 0.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 0,
                         individualPersistentSpawnCostBoostMax: 0,
                         individualPersistentSpawnCostBoostDecay: 0
                         );

            AddEnemyType(enemyType: EnemyType.Mandalore,
                         enabled: true,
                         spawnCost: 350,
                         spawnWave: 40,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 30,
                         spawnCostBonusScalar: 0.25f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 0.5f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 0,
                         individualPersistentSpawnCostBoostMax: 0,
                         individualPersistentSpawnCostBoostDecay: 0
                         );

            AddEnemyType(enemyType: EnemyVariants.TundraAgonyType,
                         enabled: true,
                         spawnCost: 15,
                         spawnWave: 3,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 30,
                         individualPersistentSpawnCostBoostMax: 60,
                         individualPersistentSpawnCostBoostDecay: 15
                         );

            AddEnemyType(enemyType: EnemyVariants.BloodTree,
                         enabled: true,
                         spawnCost: 75,
                         spawnWave: 20,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 3,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 0.1f,
                         individualCostIncreasePerSpawn: 25,
                         individualPersistentSpawnCostBoost: 90,
                         individualPersistentSpawnCostBoostMax: 240,
                         individualPersistentSpawnCostBoostDecay: 30
                         );

            AddEnemyType(enemyType: EnemyType.V2,
                         enabled: true,
                         spawnCost: 20,
                         spawnWave: 5,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 60,
                         individualPersistentSpawnCostBoostMax: 440,
                         individualPersistentSpawnCostBoostDecay: 20
                         );

            AddEnemyType(enemyType: EnemyType.V2Second,
                         enabled: true,
                         spawnCost: 30,
                         spawnWave: 10,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 80,
                         individualPersistentSpawnCostBoostMax: 680,
                         individualPersistentSpawnCostBoostDecay: 60
                         );

            AddEnemyType(enemyType: EnemyType.Gabriel,
                         enabled: true,
                         spawnCost: 35,
                         spawnWave: 10,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 80,
                         individualPersistentSpawnCostBoostMax: 480,
                         individualPersistentSpawnCostBoostDecay: 60
                         );

            AddEnemyType(enemyType: EnemyType.GabrielSecond,
                         enabled: true,
                         spawnCost: 105,
                         spawnWave: 20,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 120,
                         individualPersistentSpawnCostBoostMax: 620,
                         individualPersistentSpawnCostBoostDecay: 100
                         );

            AddEnemyType(enemyType: EnemyType.Leviathan,
                         enabled: true,
                         spawnCost: 88,
                         spawnWave: 18,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 240,
                         individualPersistentSpawnCostBoostMax: 1000,
                         individualPersistentSpawnCostBoostDecay: 60
                         );

            AddEnemyType(enemyType: EnemyType.Minos,
                         enabled: true,
                         spawnCost: 60,
                         spawnWave: 15,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 120,
                         individualPersistentSpawnCostBoostMax: 1000,
                         individualPersistentSpawnCostBoostDecay: 30
                         );

            AddEnemyType(enemyType: EnemyType.Minotaur,
                         enabled: true,
                         spawnCost: 150,
                         spawnWave: 40,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 150,
                         individualPersistentSpawnCostBoostMax: 500,
                         individualPersistentSpawnCostBoostDecay: 50
                         );

            AddEnemyType(enemyType: EnemyType.FleshPrison,
                         enabled: true,
                         spawnCost: 175,
                         spawnWave: 40,
                         showBossBar: true,
                         healthScalar: 0.6f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 150,
                         individualPersistentSpawnCostBoostMax: 1500,
                         individualPersistentSpawnCostBoostDecay: 20
                         );

            AddEnemyType(enemyType: EnemyType.FleshPanopticon,
                         enabled: true,
                         spawnCost: 325,
                         spawnWave: 50,
                         showBossBar: true,
                         healthScalar: 0.6f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 200,
                         individualPersistentSpawnCostBoostMax: 1500,
                         individualPersistentSpawnCostBoostDecay: 20
                         );

            AddEnemyType(enemyType: EnemyType.MinosPrime,
                         enabled: true,
                         spawnCost: 225,
                         spawnWave: 40,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 100,
                         individualPersistentSpawnCostBoostMax: 500,
                         individualPersistentSpawnCostBoostDecay: 35
                         );

            AddEnemyType(enemyType: EnemyType.SisyphusPrime,
                         enabled: true,
                         spawnCost: 250,
                         spawnWave: 50,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 0,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 135,
                         individualPersistentSpawnCostBoostMax: 500,
                         individualPersistentSpawnCostBoostDecay: 45
                         );

            BloodTreeEnemyCountFillSpeedBase = _config.Bind("BloodTree", "EnemyCountFillSpeedBase", 1.75f);
            BloodTreeWaveHpFillSpeedBase = _config.Bind("BloodTree", "WaveHpFillSpeedBase", 85.0f);
            BloodTreeFillSpeedBlend = _config.Bind("BloodTree", "FillSpeedBlend", 0.65f);
            BloodTreeCatcherRadius = _config.Bind("BloodTree", "BloodCatcherRadius", 12.0f);
            BloodTreeCatcherHeight = _config.Bind("BloodTree", "BloodCatcherHeight", 46.0f);

            FleshPrisonInsigniaSizeScalar = _config.Bind("FleshPrisons", "InsigniaSizeScalar", 0.5f);
        }

        private static void AddEnemyType(EnemyType enemyType, bool enabled, int spawnCost, int spawnWave, bool showBossBar, float healthScalar, int spawnCooldown, float spawnCostBonusScalar, float spawnCostRequirementScalar, float spawnCostSpentScalar, int individualCostIncreasePerSpawn, int individualPersistentSpawnCostBoost, int individualPersistentSpawnCostBoostMax, int individualPersistentSpawnCostBoostDecay)
        {
            AddEnemyType(EnemyTypeDB.Instance.GetVanillaType(enemyType), enabled, spawnCost, spawnWave, showBossBar, healthScalar, spawnCooldown, spawnCostBonusScalar, spawnCostRequirementScalar, spawnCostSpentScalar, individualCostIncreasePerSpawn, individualPersistentSpawnCostBoost, individualPersistentSpawnCostBoostMax, individualPersistentSpawnCostBoostDecay);
        }

        private static void AddEnemyType(NyxLib.AEnemyType enemyType, bool enabled, int spawnCost, int spawnWave, bool showBossBar, float healthScalar, int spawnCooldown, float spawnCostBonusScalar, float spawnCostRequirementScalar, float spawnCostSpentScalar, int individualCostIncreasePerSpawn, int individualPersistentSpawnCostBoost, int individualPersistentSpawnCostBoostMax, int individualPersistentSpawnCostBoostDecay)
        {
            if (EnemyEntries.ContainsKey(enemyType))
            {
                return;
            }

            var entry = new EnemyEntry();

            entry.Enabled = _config.Bind($"{enemyType}", $"Enabled", enabled);
            entry.SpawnCost = _config.Bind($"{enemyType}", $"SpawnCost", spawnCost);
            entry.SpawnWave = _config.Bind($"{enemyType}", $"SpawnWave", spawnWave);
            entry.ShowBossBar = _config.Bind($"{enemyType}", $"ShowBossBar", showBossBar);
            entry.HealthScalar = _config.Bind($"{enemyType}", $"HealthScalar", healthScalar);
            entry.SpawnCooldown = _config.Bind($"{enemyType}", $"SpawnCooldown", spawnCooldown);
            entry.SpawnCostBonusScalar = _config.Bind($"{enemyType}", $"SpawnCostBonusScalar", spawnCostBonusScalar);
            entry.SpawnCostRequirementScalar = _config.Bind($"{enemyType}", $"SpawnCostRequirementScalar", spawnCostRequirementScalar);
            entry.SpawnCostSpentScalar = _config.Bind($"{enemyType}", $"SpawnCostSpentScalar", spawnCostSpentScalar);
            entry.IndividualCostIncreasePerSpawn = _config.Bind($"{enemyType}", $"IndividualCostIncreasePerSpawn", individualCostIncreasePerSpawn);
            entry.IndividualPersistentSpawnCostBoost = _config.Bind($"{enemyType}", $"IndividualPersistentSpawnCostBoost", individualPersistentSpawnCostBoost);
            entry.IndividualPersistentSpawnCostBoostMax = _config.Bind($"{enemyType}", $"IndividualPersistentSpawnCostBoostMax", individualPersistentSpawnCostBoostMax);
            entry.IndividualPersistentSpawnCostBoostDecay = _config.Bind($"{enemyType}", $"IndividualPersistentSpawnCostBoostDecay", individualPersistentSpawnCostBoostDecay);

            EnemyEntries.Add(enemyType, entry);
        }

        private static void Reload()
        {
        }

        private static ConfigFile _config = null;
        private static ConfigFileManager _configFileManager;
    }
}
