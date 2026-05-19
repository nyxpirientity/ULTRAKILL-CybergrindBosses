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
            public ConfigEntry<int> SpawnWave = null;
            public ConfigEntry<float> HealthScalar = null;
            public ConfigEntry<int> SpawnCooldown = null;

            public ConfigEntry<bool> ShowBossBar { get; internal set; }
        }

        public static Dictionary<NyxLib.AEnemyType, EnemyEntry> EnemyEntries = new Dictionary<NyxLib.AEnemyType, EnemyEntry>();
        public static bool EnemyEntriesInitialized = false;

        public static ConfigEntry<float> PointsRatioAllocatedToBosses = null;

        internal static void Initialize(BaseUnityPlugin plugin)
        {
            Assert.IsNotNull(plugin);

            _config = plugin.Config;

            _configFileManager = plugin.gameObject.AddComponent<ConfigFileManager>();
            _configFileManager.Initialize(_config);
            _configFileManager.OnReload += Reload;

            PointsRatioAllocatedToBosses = _config.Bind("General", "PointsRatioAllocatedToBosses", 0.5f);

            AddEnemyType(EnemyType.CancerousRodent, true, 50, 1, true, 1.0f, 10, -1.0f);
            AddEnemyType(EnemyVariants.TundraAgonyType, true, 15, 3, true, 1.0f, 3, 0.5f);
            AddEnemyType(EnemyType.V2, true, 20, 5, true, 1.0f, 3, 0.5f);
            AddEnemyType(EnemyType.V2Second, true, 30, 10, true, 1.0f, 3, 0.5f);
            AddEnemyType(EnemyType.Gabriel, true, 40, 10, true, 1.0f, 4, 0.5f);
            AddEnemyType(EnemyType.GabrielSecond, true, 125, 20, true, 1.0f, 6, 0.5f);
            AddEnemyType(EnemyType.Leviathan, true, 1, 1, true, 1.0f, 8, 0.5f);
            AddEnemyType(EnemyType.Minos, true, 75, 15, true, 1.0f, 8, 0.5f);
            AddEnemyType(EnemyType.FleshPrison, true, 175, 40, true, 0.6f, 5, 0.5f);
            AddEnemyType(EnemyType.FleshPanopticon, true, 750, 50, true, 0.6f, 5, 0.5f);
            AddEnemyType(EnemyType.MinosPrime, true, 400, 40, true, 1.0f, 5, 0.5f);
            AddEnemyType(EnemyType.SisyphusPrime, true, 400, 50, true, 1.0f, 6, 0.5f);
        }
        
        private static void AddEnemyType(EnemyType enemyType, bool enabled, int spawnCost, int spawnWave, bool showBossBar, float healthScalar, int spawnCooldown, float spawnCostBonusScalar)
        {
            AddEnemyType(EnemyTypeDB.Instance.GetVanillaType(enemyType), enabled, spawnCost, spawnWave, showBossBar, healthScalar, spawnCooldown, spawnCostBonusScalar);
        }

        private static void AddEnemyType(NyxLib.AEnemyType enemyType, bool enabled, int spawnCost, int spawnWave, bool showBossBar, float healthScalar, int spawnCooldown, float spawnCostBonusScalar)
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

            EnemyEntries.Add(enemyType, entry);
        }

        private static void Reload()
        {
        }

        private static ConfigFile _config = null;
        private static ConfigFileManager _configFileManager;
    }
}
