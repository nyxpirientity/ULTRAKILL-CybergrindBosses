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
    [BepInPlugin("nyxpiri.ultrakill.cybergrind-bosses", "Cybergrind Bosses", "0.0.0")]
    [BepInProcess("ULTRAKILL.exe")]
    public class CybergrindBosses : BaseUnityPlugin
    {
        protected void Awake()
        {
            Log.Initialize(Logger);
            Options.Initialize(this);
            Assets.Initialize();
            EnemyVariants.Initialize();

            Cybergrind.PostCybergrindNextWave += NextWave;
            NyxLib.Cheats.ReadyForCheatRegistration += RegisterCheats;
            NyxLib.ScenesEvents.OnSceneWasLoaded += (UnityEngine.SceneManagement.Scene scene, string levelName, string unitySceneName) =>
            {
                SpawnCooldowns.Clear();
                TypesToSpawn.Clear();
                EnemyAmountToAdd = 0;
            };

            Harmony.CreateAndPatchAll(GetType().Assembly);
        }

        public const string CheatID = "nyxpiri.cybergrind-bosses";
        FieldAccess<EndlessGrid, int> pointsFi = new FieldAccess<EndlessGrid, int>("points");

        public HashSet<NyxLib.AEnemyType> TypesToSpawn { get; private set; } = new HashSet<NyxLib.AEnemyType>();
        internal static int EnemyAmountToAdd { get; set; } = 0;
        public static Vector3 cgCenter = new Vector3(0.0f, 0.0f, 62.5f);
        private Dictionary<NyxLib.AEnemyType, int> SpawnCooldowns = new Dictionary<NyxLib.AEnemyType, int>();

        private void NextWave(EventMethodCancelInfo cancelInfo, EndlessGrid endlessGrid)
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            if (NyxLib.Cheats.IsCheatDisabled(CheatID))
            {
                return;
            }

            var wave = endlessGrid.currentWave;

            EnemyAmountToAdd = 0;

            int allPoints = pointsFi.GetValue(endlessGrid);
            int maxPoints = Mathf.FloorToInt(allPoints * Options.PointsRatioAllocatedToBosses.Value);
            int points = maxPoints;
            int spawnCostBonus = 0;
            Log.Debug($"deciding bosses to spawn with {points} points (allPoints = {allPoints})");

            TypesToSpawn.Clear();

            for (int i = 0; i < 25 && points > 0; i++)
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

                if (points < spawnCost)
                {
                    Log.Debug($"{entryRaw.Key} DENIED on the basis of {points} being less than {spawnCost}");
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

                spawnCostBonus += (int)(baseSpawnCost * entry.SpawnCostBonusScalar.Value);
                points -= spawnCost;
                TypesToSpawn.Add(entryRaw.Key);
                EnemyAmountToAdd += 1;

                if (entryRaw.Key == EnemyVariants.TundraAgonyType)
                {
                    EnemyAmountToAdd += 1;
                }

                Log.Debug($"adding type {entryRaw.Key} to types to spawn");
            }
            
            Dictionary<AEnemyType, int> newSpawnCooldowns = new Dictionary<AEnemyType, int>(SpawnCooldowns);
            
            foreach (var key in SpawnCooldowns.Keys)
            {
                newSpawnCooldowns[key] -= 1;
            }

            SpawnCooldowns = newSpawnCooldowns;

            pointsFi.SetValue(endlessGrid, allPoints - (maxPoints - points));
            spawnTimer = 2.5f;
            initialSpawn = true;
            Log.Debug($"should spawn {TypesToSpawn.Count} types");
        }

        bool initialSpawn = false;
        float spawnTimer = 0.0f;
        Vector3 centerFloorPos = Vector3.zero;

        private void RegisterCheats(CheatsManager cheatsManager)
        {
            cheatsManager.RegisterCheat(new ToggleCheat("Cybergrind Bosses", CheatID, 
            (cheat) =>
            {
                
            },
            (cheat, cheatsManager) =>
            {

            }), "CYBERGRIND");
        }

        protected void Start()
        {
        }

        protected void FixedUpdate()
        {
            if (NyxLib.Cheats.IsCheatDisabled(CheatID))
            {
                return;   
            }

            spawnTimer -= Time.fixedDeltaTime;

            if (spawnTimer <= 0.0f)
            {
                if (initialSpawn)
                {
                    initialSpawn = false;

                    Physics.SphereCast(cgCenter + Vector3.up * 100.0f, 2.0f, Vector3.down, out RaycastHit hit, 200.0f);
                    centerFloorPos = hit.collider != null ? hit.point : cgCenter;
                }

                spawnTimer = 0.4f;
                
                if (TypesToSpawn.Count == 0)
                {
                    return;
                }

                var typeToSpawn = TypesToSpawn.FirstOrDefault();
                GameObject prefab = null;
                
                switch (typeToSpawn.VanillaEnumValue)
                {
                    case EnemyType.Gabriel:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.Gabriel);
                        break;
                    case EnemyType.GabrielSecond:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.GabrielSecond);
                        break;
                    case EnemyType.V2:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.V2);
                        break;
                    case EnemyType.V2Second:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.V2Second);
                        break;
                    case EnemyType.FleshPrison:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.FleshPrison);
                        break;
                    case EnemyType.FleshPanopticon:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.FleshPanopticon);
                        break;
                    case EnemyType.MinosPrime:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.MinosPrime);
                        break;
                    case EnemyType.Minos:
                        prefab = EnemyVariants.CorpseOfKingMinosPrefab;
                        break;
                    case EnemyType.Leviathan:
                        prefab = EnemyVariants.LeviathanPrefab.gameObject;
                        break;
                    case EnemyType.SisyphusPrime:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.SisyphusPrime);
                        break;
                    case EnemyType.CancerousRodent:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.CancerousRodent);
                        break;
                    default:
                        break;
                }   

                GameObject enemyGo = null;

                var spawnPos = centerFloorPos + (Vector3.up * 10.0f) + (Vector3.forward * 10.0f);
                var bossBar = Options.EnemyEntries[typeToSpawn].ShowBossBar.Value;
                
                if (prefab != null)
                {
                    enemyGo = GameObject.Instantiate(prefab, EndlessGrid.Instance.gameObject.transform);
                    enemyGo.SetActive(true);
                    Log.Debug($"done spawning prefab for type {typeToSpawn}");
                    TypesToSpawn.Remove(typeToSpawn);
                    var enemy = enemyGo.GetComponentInChildren<EnemyComponents>();
                    var eid = enemy.Eid;
                    eid.gameObject.AddComponent<GrindBoss>();

                    enemy.Health = enemy.Health * Options.EnemyEntries[typeToSpawn].HealthScalar.Value;
                    
                    eid.BossBar(bossBar);
                    
                    if (typeToSpawn.VanillaEnumValue == EnemyType.FleshPrison || typeToSpawn.VanillaEnumValue == EnemyType.FleshPanopticon)
                    {
                        enemyGo.transform.position = centerFloorPos;
                    }
                    else if (typeToSpawn.VanillaEnumValue == EnemyType.Minos)
                    {
                        enemyGo.transform.position = centerFloorPos + (Vector3.right * 175.0f) + (Vector3.down * 600.0f);
                        var bcs = enemyGo.GetComponentsInChildren<BoxCollider>();

                        foreach (var col in bcs)
                        {
                            col.enabled = col.GetComponent<Rigidbody>() != null;
                        }

                        var colliders = enemyGo.GetComponentsInChildren<Collider>(true);
                        foreach (var col in colliders)
                        {
                            col.gameObject.GetOrAddComponent<IgnoreDeathZones>();
                        }
                        
                    }
                    else if (typeToSpawn.VanillaEnumValue == EnemyType.Leviathan)
                    {
                        enemyGo.transform.position = spawnPos;

                        var colliders = enemyGo.GetComponentsInChildren<Collider>(true);
                        foreach (var col in colliders)
                        {
                            col.gameObject.GetOrAddComponent<IgnoreDeathZones>();
                        }

                        var lev = enemyGo.GetComponent<LeviathanController>();
                    }
                    else
                    {
                        enemyGo.transform.position = spawnPos;
                    }
                }
                else
                {
                    if (typeToSpawn == EnemyVariants.TundraAgonyType)
                    {
                        var agonyAndTundra = EnemyVariants.SpawnAgonyAndTundra(spawnPos, Quaternion.identity, EndlessGrid.Instance.transform);

                        var enemies = agonyAndTundra.GetComponentsInChildren<EnemyComponents>();

                        foreach (var enemy in enemies)
                        {
                            enemy.Health = enemy.Health * Options.EnemyEntries[typeToSpawn].HealthScalar.Value;
                            var boss = enemy.Eid.gameObject.AddComponent<GrindBoss>();
                            enemy.ResetHealthInfo();

                            enemy.Eid.BossBar(bossBar);

                            boss.IsTundraAgony = true;
                        }

                        Transform[] childrenToDetach = new Transform[agonyAndTundra.transform.childCount];

                        for (int i = 0; i < agonyAndTundra.transform.childCount; i++)
                        {
                            childrenToDetach[i] = agonyAndTundra.transform.GetChild(i);
                        } 

                        foreach (var child in childrenToDetach)
                        {
                            child.parent = EndlessGrid.Instance.transform;
                        }

                        GameObject.Destroy(agonyAndTundra);
 
                        TypesToSpawn.Remove(typeToSpawn);
                    }
                    else
                    {
                        return;
                    }
                }

                /*
                    GameObject.Instantiate(prefab, EndlessGrid.Instance.gameObject.transform); // GEE I WONDER WHY THIS DIDNT WORK?
                    prefab.transform.position = new Vector3(0.0f, 50.0f, 62.5f);
                    prefab.SetActive(true);
                    Log.Message($"done spawning prefab for type {typeToSpawn}");
                    TypesToSpawn.Remove(typeToSpawn);
                    Cybergrind.EndlessGrid.enemyAmount += 1;
                    prefab.GetComponentInChildren<EnemyIdentifier>().gameObject.AddComponent<GrindBoss>();
                */
            }
        }

        protected void Update()
        {

        }

        protected void LateUpdate()
        {

        }
    }

    [HarmonyPatch(typeof(EndlessGrid), "GetEnemies")]
    public static class EndlessGridGetEnemiesPatch
    {
        public static bool Prefix(EndlessGrid __instance)
        {
            return true;
        }
        
        public static void Postfix(EndlessGrid __instance)
        {
            __instance.tempEnemyAmount += CybergrindBosses.EnemyAmountToAdd;
            CybergrindBosses.EnemyAmountToAdd = 0;
        }
    }
}
