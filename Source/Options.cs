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

            public ConfigEntry<bool> ShowBossBar { get; internal set; }
        }

        public static Dictionary<NyxLib.AEnemyType, EnemyEntry> EnemyEntries = new Dictionary<NyxLib.AEnemyType, EnemyEntry>();
        public static bool EnemyEntriesInitialized = false;

        public static ConfigEntry<float> PointsRatioAllocatedToBosses { get; private set; } = null;
        public static ConfigEntry<int> BossSpawnIterations { get; private set; } = null;

        public static ConfigEntry<float> BloodTreeFillSpeedBase = null;
        public static ConfigEntry<float> BloodTreeCatcherRadius = null;
        public static ConfigEntry<float> BloodTreeCatcherHeight = null;

        internal static void Initialize(BaseUnityPlugin plugin)
        {
            Assert.IsNotNull(plugin);

            _config = plugin.Config;

            _configFileManager = plugin.gameObject.AddComponent<ConfigFileManager>();
            _configFileManager.Initialize(_config);
            _configFileManager.OnReload += Reload;

            PointsRatioAllocatedToBosses = _config.Bind("General", "PointsRatioAllocatedToBosses", 0.5f);
            BossSpawnIterations = _config.Bind("General", "BossSpawnIterations", 50);

            // AddEnemyType(EnemyType.Geryon, true, 1, 1, true, 1.0f, 0, 0.0f, 1.0f, 0.0f);
            AddEnemyType(EnemyType.CancerousRodent, true, 150, 1, true, 1.0f, 10, 0.0f, 1.0f, 0.0f);
            AddEnemyType(EnemyType.VeryCancerousRodent, true, 250, 30, true, 1.0f, 15, 0.0f, 1.0f, 0.0f);
            AddEnemyType(EnemyVariants.TundraAgonyType, true, 15, 3, true, 1.0f, 3, 0.5f, 1.0f, 1.0f);
            AddEnemyType(EnemyVariants.BloodTree, true, 100, 30, true, 1.0f, 4, 0.5f, 1.0f, 0.1f);
            AddEnemyType(EnemyType.V2, true, 20, 5, true, 1.0f, 3, 0.5f, 1.0f, 1.0f);
            AddEnemyType(EnemyType.V2Second, true, 30, 10, true, 1.0f, 3, 0.5f, 1.0f, 1.0f);
            AddEnemyType(EnemyType.Gabriel, true, 35, 10, true, 1.0f, 4, 0.5f, 1.0f, 1.0f);
            AddEnemyType(EnemyType.GabrielSecond, true, 105, 20, true, 1.0f, 4, 0.5f, 1.0f, 1.0f);
            AddEnemyType(EnemyType.Leviathan, true, 88, 18, true, 1.0f, 6, 0.5f, 1.0f, 1.0f);
            AddEnemyType(EnemyType.Minos, true, 60, 15, true, 1.0f, 8, 0.5f, 1.0f, 1.0f);
            AddEnemyType(EnemyType.Minotaur, true, 150, 40, true, 1.0f, 3, 0.5f, 1.0f, 1.0f);
            AddEnemyType(EnemyType.FleshPrison, true, 175, 40, true, 0.6f, 5, 0.5f, 1.0f, 1.0f);
            AddEnemyType(EnemyType.FleshPanopticon, true, 325, 50, true, 0.6f, 5, 0.5f, 1.0f, 1.0f);
            AddEnemyType(EnemyType.MinosPrime, true, 225, 40, true, 1.0f, 5, 0.5f, 1.0f, 1.0f);
            AddEnemyType(EnemyType.SisyphusPrime, true, 250, 50, true, 1.0f, 6, 0.5f, 1.0f, 1.0f);

            BloodTreeFillSpeedBase = _config.Bind("BloodTree", "FillSpeedBase", 1.75f);
            BloodTreeCatcherRadius = _config.Bind("BloodTree", "BloodCatcherRadius", 14.0f);
            BloodTreeCatcherHeight = _config.Bind("BloodTree", "BloodCatcherHeight", 30.0f);
        }

        private static void AddEnemyType(EnemyType enemyType, bool enabled, int spawnCost, int spawnWave, bool showBossBar, float healthScalar, int spawnCooldown, float spawnCostBonusScalar, float spawnCostRequirementScalar, float spawnCostSpentScalar)
        {
            AddEnemyType(EnemyTypeDB.Instance.GetVanillaType(enemyType), enabled, spawnCost, spawnWave, showBossBar, healthScalar, spawnCooldown, spawnCostBonusScalar, spawnCostRequirementScalar, spawnCostSpentScalar);
        }

        private static void AddEnemyType(NyxLib.AEnemyType enemyType, bool enabled, int spawnCost, int spawnWave, bool showBossBar, float healthScalar, int spawnCooldown, float spawnCostBonusScalar, float spawnCostRequirementScalar, float spawnCostSpentScalar)
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

            EnemyEntries.Add(enemyType, entry);
        }

        private static void Reload()
        {
        }

        private static ConfigFile _config = null;
        private static ConfigFileManager _configFileManager;
    }
}
