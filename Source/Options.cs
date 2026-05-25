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
            public ConfigEntry<float> SpawnCostBonusSpentScalar = null;
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

        public class EnemyAttributes
        {
            public ConfigEntry<bool> CanSpawnInFakeFall = null;
            public ConfigEntry<int> FakeFallSpawnCost = null;
            public ConfigEntry<int> FakeFallSpawnCostIncreasePerSpawn = null;
            public ConfigEntry<int> FakeFallDespawnValue = null;
        }

        public static Dictionary<NyxLib.AEnemyType, EnemyEntry> EnemyEntries = new Dictionary<NyxLib.AEnemyType, EnemyEntry>();
        public static Dictionary<NyxLib.AEnemyType, EnemyAttributes> EnemiesAttributes = new Dictionary<NyxLib.AEnemyType, EnemyAttributes>();
        public static bool EnemyEntriesInitialized = false;

        public static ConfigEntry<float> PointsRatioAllocatedToBosses { get; private set; } = null;
        public static ConfigEntry<int> BossWaveCooldownMin { get; private set; } = null;
        public static ConfigEntry<int> BossWaveCooldownMax { get; private set; }
        public static ConfigEntry<bool> OnlyCountBossWavesTowardsBossCooldowns { get; private set; }
        public static ConfigEntry<bool> UseBossWaveCooldown { get; private set; }

        public static ConfigEntry<float> BloodTreeEnemyCountFillSpeedBase = null;
        public static ConfigEntry<float> BloodTreeWaveHpFillSpeedBase = null;
        public static ConfigEntry<float> BloodTreeFillSpeedBlend = null;
        public static ConfigEntry<float> BloodTreeCatcherRadius = null;
        public static ConfigEntry<float> BloodTreeCatcherHeight = null;

        public static ConfigEntry<bool> UseForcedFakeFall = null;
        public static ConfigEntry<int> ForcedFakeFallMinWave = null;
        public static ConfigEntry<int> ForcedFakeFallDelayMinWaves = null;
        public static ConfigEntry<int> ForcedFakeFallDelayMaxWaves = null;


        public static ConfigEntry<float> FleshPrisonInsigniaSizeScalar { get; private set; }

        internal static void Initialize(BaseUnityPlugin plugin)
        {
            Assert.IsNotNull(plugin);

            _config = plugin.Config;

            _configFileManager = plugin.gameObject.AddComponent<ConfigFileManager>();
            _configFileManager.Initialize(_config);
            _configFileManager.OnReload += Reload;

            UseForcedFakeFall = _config.Bind("FakeFall", "UseForcedFakeFall", true);
            ForcedFakeFallMinWave = _config.Bind("FakeFall", "ForcedFakeFallMinWave", 30);
            ForcedFakeFallDelayMinWaves = _config.Bind("FakeFall", "ForcedFakeFallDelayMinWaves", 9);
            ForcedFakeFallDelayMaxWaves = _config.Bind("FakeFall", "ForcedFakeFallDelayMaxWaves", 12);

            PointsRatioAllocatedToBosses = _config.Bind("General", "PointsRatioAllocatedToBosses", 0.5f);
            BossWaveCooldownMin = _config.Bind("General", "GlobalBossWaveCooldownMin", 1);
            BossWaveCooldownMax = _config.Bind("General", "GlobalBossWaveCooldownMax", 1);
            UseBossWaveCooldown = _config.Bind("General", "UseGlobalBossWaveCooldown", true);
            OnlyCountBossWavesTowardsBossCooldowns = _config.Bind("General", "OnlyCountBossWavesTowardsBossCooldowns", true);

            AddEnemyType(enemyType: EnemyType.Geryon,
                         enabled: true,
                         spawnCost: 150,
                         spawnWave: 30,
                         showBossBar: true,
                         healthScalar: 0.5f,
                         spawnCooldown: 5,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostBonusSpentScalar: 0.75f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 350,
                         individualPersistentSpawnCostBoostMax: 350,
                         individualPersistentSpawnCostBoostDecay: 40
                         );

            AddEnemyType(enemyType: EnemyType.CancerousRodent,
                         enabled: true,
                         spawnCost: 250,
                         spawnWave: 1,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 10,
                         spawnCostBonusScalar: 0.0f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 0.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 0,
                         individualPersistentSpawnCostBoostMax: 0,
                         individualPersistentSpawnCostBoostDecay: 0
                         );

            AddEnemyType(enemyType: EnemyType.VeryCancerousRodent,
                         enabled: true,
                         spawnCost: 350,
                         spawnWave: 30,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 15,
                         spawnCostBonusScalar: 0.0f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 0.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 0,
                         individualPersistentSpawnCostBoostMax: 0,
                         individualPersistentSpawnCostBoostDecay: 0
                         );

            AddEnemyType(enemyType: EnemyType.Mandalore,
                         enabled: true,
                         spawnCost: 450,
                         spawnWave: 40,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 30,
                         spawnCostBonusScalar: 0.25f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 0.5f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 0,
                         individualPersistentSpawnCostBoostMax: 0,
                         individualPersistentSpawnCostBoostDecay: 0
                         );

            AddEnemyType(enemyType: EnemyVariants.TundraAgonyType,
                         enabled: true,
                         spawnCost: 20,
                         spawnWave: 3,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 2,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 30,
                         individualPersistentSpawnCostBoostMax: 60,
                         individualPersistentSpawnCostBoostDecay: 15
                         );

            AddEnemyType(enemyType: EnemyVariants.BloodTree,
                         enabled: true,
                         spawnCost: 120,
                         spawnWave: 20,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 1,
                         spawnCostBonusScalar: 0.25f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 25,
                         individualPersistentSpawnCostBoost: 90,
                         individualPersistentSpawnCostBoostMax: 300,
                         individualPersistentSpawnCostBoostDecay: 30
                         );

            AddEnemyType(enemyType: EnemyType.V2,
                         enabled: true,
                         spawnCost: 35,
                         spawnWave: 5,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 3,
                         spawnCostBonusScalar: 0.75f,
                         spawnCostBonusSpentScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 60,
                         individualPersistentSpawnCostBoostMax: 220,
                         individualPersistentSpawnCostBoostDecay: 20
                         );

            AddEnemyType(enemyType: EnemyType.V2Second,
                         enabled: true,
                         spawnCost: 50,
                         spawnWave: 10,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 3,
                         spawnCostBonusScalar: 0.75f,
                         spawnCostBonusSpentScalar: 0.5f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 80,
                         individualPersistentSpawnCostBoostMax: 680,
                         individualPersistentSpawnCostBoostDecay: 60
                         );

            AddEnemyType(enemyType: EnemyType.Gabriel,
                         enabled: true,
                         spawnCost: 65,
                         spawnWave: 10,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 3,
                         spawnCostBonusScalar: 0.3f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 180,
                         individualPersistentSpawnCostBoostMax: 180,
                         individualPersistentSpawnCostBoostDecay: 50
                         );

            AddEnemyType(enemyType: EnemyType.GabrielSecond,
                         enabled: true,
                         spawnCost: 145,
                         spawnWave: 20,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 3,
                         spawnCostBonusScalar: 0.35f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 220,
                         individualPersistentSpawnCostBoostMax: 220,
                         individualPersistentSpawnCostBoostDecay: 100
                         );

            AddEnemyType(enemyType: EnemyType.Leviathan,
                         enabled: true,
                         spawnCost: 130,
                         spawnWave: 18,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 2,
                         spawnCostBonusScalar: 0.4f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 240,
                         individualPersistentSpawnCostBoostMax: 240,
                         individualPersistentSpawnCostBoostDecay: 50
                         );

            AddEnemyType(enemyType: EnemyType.Minos,
                         enabled: true,
                         spawnCost: 140,
                         spawnWave: 15,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 5,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 120,
                         individualPersistentSpawnCostBoostMax: 120,
                         individualPersistentSpawnCostBoostDecay: 20
                         );

            AddEnemyType(enemyType: EnemyType.Minotaur,
                         enabled: true,
                         spawnCost: 150,
                         spawnWave: 35,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 3,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 300,
                         individualPersistentSpawnCostBoostMax: 300,
                         individualPersistentSpawnCostBoostDecay: 75
                         );

            AddEnemyType(enemyType: EnemyType.FleshPrison,
                         enabled: true,
                         spawnCost: 250,
                         spawnWave: 40,
                         showBossBar: true,
                         healthScalar: 0.6f,
                         spawnCooldown: 5,
                         spawnCostBonusScalar: 0.4f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 250,
                         individualPersistentSpawnCostBoostMax: 300,
                         individualPersistentSpawnCostBoostDecay: 70
                         );

            AddEnemyType(enemyType: EnemyType.FleshPanopticon,
                         enabled: true,
                         spawnCost: 425,
                         spawnWave: 50,
                         showBossBar: true,
                         healthScalar: 0.6f,
                         spawnCooldown: 6,
                         spawnCostBonusScalar: 0.55f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 240,
                         individualPersistentSpawnCostBoostMax: 300,
                         individualPersistentSpawnCostBoostDecay: 40
                         );

            AddEnemyType(enemyType: EnemyType.MinosPrime,
                         enabled: true,
                         spawnCost: 340,
                         spawnWave: 40,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 3,
                         spawnCostBonusScalar: 0.75f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 350,
                         individualPersistentSpawnCostBoostMax: 350,
                         individualPersistentSpawnCostBoostDecay: 85
                         );

            AddEnemyType(enemyType: EnemyType.SisyphusPrime,
                         enabled: true,
                         spawnCost: 310,
                         spawnWave: 50,
                         showBossBar: true,
                         healthScalar: 1.0f,
                         spawnCooldown: 3,
                         spawnCostBonusScalar: 0.5f,
                         spawnCostBonusSpentScalar: 1.0f,
                         spawnCostRequirementScalar: 1.0f,
                         spawnCostSpentScalar: 1.0f,
                         individualCostIncreasePerSpawn: 10000,
                         individualPersistentSpawnCostBoost: 350,
                         individualPersistentSpawnCostBoostMax: 350,
                         individualPersistentSpawnCostBoostDecay: 100
                         );

            BloodTreeEnemyCountFillSpeedBase = _config.Bind("BloodTree", "EnemyCountFillSpeedBase", 1.75f);
            BloodTreeWaveHpFillSpeedBase = _config.Bind("BloodTree", "WaveHpFillSpeedBase", 85.0f);
            BloodTreeFillSpeedBlend = _config.Bind("BloodTree", "FillSpeedBlend", 0.65f);
            BloodTreeCatcherRadius = _config.Bind("BloodTree", "BloodCatcherRadius", 12.0f);
            BloodTreeCatcherHeight = _config.Bind("BloodTree", "BloodCatcherHeight", 46.0f);

            FleshPrisonInsigniaSizeScalar = _config.Bind("FleshPrisons", "InsigniaSizeScalar", 0.5f);

            AddEnemyAttribs(EnemyVariants.TundraAgonyType, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyVariants.BloodTree, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);

            AddEnemyAttribs(EnemyType.BigJohnator, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Centaur, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.CancerousRodent, canSpawnInFakeFall: true, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Cerberus, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 50);
            AddEnemyAttribs(EnemyType.Deathcatcher, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 50);
            AddEnemyAttribs(EnemyType.Drone, canSpawnInFakeFall: true, fakeFallSpawnCost: 10, fakeFallSpawnCostIncreasePerSpawn: 5, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Ferryman, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 100);
            AddEnemyAttribs(EnemyType.Filth, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 2);
            AddEnemyAttribs(EnemyType.FleshPanopticon, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.FleshPrison, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Gabriel, canSpawnInFakeFall: true, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.GabrielSecond, canSpawnInFakeFall: true, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Geryon, canSpawnInFakeFall: true, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Gutterman, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 60);
            AddEnemyAttribs(EnemyType.Guttertank, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 50);
            AddEnemyAttribs(EnemyType.HideousMass, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 80);
            AddEnemyAttribs(EnemyType.Idol, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 50);
            AddEnemyAttribs(EnemyType.Leviathan, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.MaliciousFace, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 25);
            AddEnemyAttribs(EnemyType.Mandalore, canSpawnInFakeFall: true, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Mannequin, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 20);
            AddEnemyAttribs(EnemyType.Mindflayer, canSpawnInFakeFall: true, fakeFallSpawnCost: 65, fakeFallSpawnCostIncreasePerSpawn: 65, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Minos, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.MinosPrime, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Minotaur, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.MirrorReaper, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Power, canSpawnInFakeFall: true, fakeFallSpawnCost: 80, fakeFallSpawnCostIncreasePerSpawn: 80, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Providence, canSpawnInFakeFall: true, fakeFallSpawnCost: 70, fakeFallSpawnCostIncreasePerSpawn: 70, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Puppet, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Schism, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 20);
            AddEnemyAttribs(EnemyType.Sisyphus, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.SisyphusPrime, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Soldier, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 30);
            AddEnemyAttribs(EnemyType.Stalker, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 50);
            AddEnemyAttribs(EnemyType.Stray, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 3);
            AddEnemyAttribs(EnemyType.Streetcleaner, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 5);
            AddEnemyAttribs(EnemyType.Swordsmachine, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 15);
            AddEnemyAttribs(EnemyType.Turret, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 50);
            AddEnemyAttribs(EnemyType.V2, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.V2Second, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.VeryCancerousRodent, canSpawnInFakeFall: true, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Virtue, canSpawnInFakeFall: true, fakeFallSpawnCost: 50, fakeFallSpawnCostIncreasePerSpawn: 25, fakeFallDespawnValue: 0);
            AddEnemyAttribs(EnemyType.Wicked, canSpawnInFakeFall: false, fakeFallSpawnCost: 0, fakeFallSpawnCostIncreasePerSpawn: 0, fakeFallDespawnValue: 0);
        }

        private static void AddEnemyAttribs(EnemyType enemyType, bool canSpawnInFakeFall, int fakeFallSpawnCost, int fakeFallSpawnCostIncreasePerSpawn, int fakeFallDespawnValue)
        {
            AddEnemyAttribs(EnemyTypeDB.Instance.GetVanillaType(enemyType), canSpawnInFakeFall, fakeFallSpawnCost, fakeFallSpawnCostIncreasePerSpawn, fakeFallDespawnValue);
        }

        private static void AddEnemyAttribs(AEnemyType enemyType, bool canSpawnInFakeFall, int fakeFallSpawnCost, int fakeFallSpawnCostIncreasePerSpawn, int fakeFallDespawnValue)
        {
            var attribs = new EnemyAttributes();
            attribs.CanSpawnInFakeFall = _config.Bind($"{enemyType.Name}", "CanSpawnInFakeFall", canSpawnInFakeFall);
            attribs.FakeFallSpawnCost = _config.Bind($"{enemyType.Name}", "FakeFallSpawnCost", fakeFallSpawnCost);
            attribs.FakeFallDespawnValue = _config.Bind($"{enemyType.Name}", "FakeFallDespawnValue", fakeFallDespawnValue);
            attribs.FakeFallSpawnCostIncreasePerSpawn = _config.Bind($"{enemyType.Name}", "FakeFallSpawnCostIncreasePerSpawn", fakeFallSpawnCostIncreasePerSpawn);
            EnemiesAttributes.TryAdd(enemyType, attribs);
        }

        private static void AddEnemyType(EnemyType enemyType, bool enabled, int spawnCost, int spawnWave, bool showBossBar, float healthScalar, int spawnCooldown, float spawnCostBonusScalar, float spawnCostBonusSpentScalar, float spawnCostRequirementScalar, float spawnCostSpentScalar, int individualCostIncreasePerSpawn, int individualPersistentSpawnCostBoost, int individualPersistentSpawnCostBoostMax, int individualPersistentSpawnCostBoostDecay)
        {
            AddEnemyType(EnemyTypeDB.Instance.GetVanillaType(enemyType), enabled, spawnCost, spawnWave, showBossBar, healthScalar, spawnCooldown, spawnCostBonusScalar, spawnCostRequirementScalar, spawnCostSpentScalar, spawnCostBonusSpentScalar, individualCostIncreasePerSpawn, individualPersistentSpawnCostBoost, individualPersistentSpawnCostBoostMax, individualPersistentSpawnCostBoostDecay);
        }

        private static void AddEnemyType(NyxLib.AEnemyType enemyType, bool enabled, int spawnCost, int spawnWave, bool showBossBar, float healthScalar, int spawnCooldown, float spawnCostBonusScalar, float spawnCostRequirementScalar, float spawnCostSpentScalar, float spawnCostBonusSpentScalar, int individualCostIncreasePerSpawn, int individualPersistentSpawnCostBoost, int individualPersistentSpawnCostBoostMax, int individualPersistentSpawnCostBoostDecay)
        {
            if (EnemyEntries.ContainsKey(enemyType))
            {
                return;
            }

            var entry = new EnemyEntry();

            entry.Enabled = _config.Bind($"{enemyType}", $"Enabled", enabled, $"Sets whether {enemyType} bosses are enabled and will spawn in the Cybergrind with the cheat active.");
            entry.SpawnCost = _config.Bind($"{enemyType}", $"SpawnCost", spawnCost, $"Sets the base amount of spawn points {enemyType} costs");
            entry.SpawnWave = _config.Bind($"{enemyType}", $"SpawnWave", spawnWave, $"Sets the minimum wave that must be reached before {enemyType} can spawn.");
            entry.ShowBossBar = _config.Bind($"{enemyType}", $"ShowBossBar", showBossBar, $"Toggles if {enemyType} has a bossbar. if applicable");
            entry.HealthScalar = _config.Bind($"{enemyType}", $"HealthScalar", healthScalar, $"Scales (multiplies) {enemyType} base health by this value when spawned");
            entry.SpawnCooldown = _config.Bind($"{enemyType}", $"SpawnCooldown", spawnCooldown, $"A cooldown in the form of a number of waves before {enemyType} can spawn again");
            entry.SpawnCostBonusScalar = _config.Bind($"{enemyType}", $"SpawnCostBonusScalar", spawnCostBonusScalar, $"When {enemyType} is picked to spawn in a wave, their base cost scaled by this value will be added as required cost for every other boss tried to be spawned that wave");
            entry.SpawnCostBonusSpentScalar = _config.Bind($"{enemyType}", $"SpawnCostBonusSpentScalar", spawnCostBonusSpentScalar, $"Scalar for the amount of bonus cost will be added to the full bonus spent cost pool for this wave. This effects how many extra points after {enemyType} will actually be spent, regardless of the base requirement. (using 'SpawnCost * SpawnCostBonusSpentScalar' as a base) ");
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
