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
    [ConfigureSingleton(SingletonFlags.NoAutoInstance)]
    public class BossSpawner : MonoSingleton<BossSpawner>
    {
        public bool IsBossWave => NyxLib.Cheats.IsCheatEnabled(CybergrindBosses.CheatID) && (_bossWaveCooldown <= 0 || !Options.UseBossWaveCooldown.Value);
        private int _bossWaveCooldown = 0;
        BossPicker _bossPicker = new BossPicker();
        float spawnTimer = 0.0f;

        protected void OnEnable()
        {
            Cybergrind.PostCybergrindNextWave += NextWave;
        }

        protected void OnDisable()
        {
            Cybergrind.PostCybergrindNextWave -= NextWave;
        }

        private void NextWave(EventMethodCancelInfo cancelInfo, EndlessGrid endlessGrid)
        {
            if (Cheats.IsCheatDisabled(CybergrindBosses.CheatID))
            {
                return;
            }

            spawnTimer = 0.3f;

            if (IsBossWave)
            {
                _bossPicker.SolveBossesToSpawn(endlessGrid);
                _bossWaveCooldown = UnityEngine.Random.Range(Options.BossWaveCooldownMin.Value, Options.BossWaveCooldownMax.Value + 1);
            }
            else
            {
                _bossPicker.TypesToSpawn.Clear();
            }

            if (IsBossWave || !Options.OnlyCountBossWavesTowardsBossCooldowns.Value)
            {
                _bossPicker.UpdateBossCooldowns();
            }

            _bossWaveCooldown -= 1;
        }

        protected void FixedUpdate()
        {
            if (Cheats.IsCheatDisabled(CybergrindBosses.CheatID))
            {
                return;
            }

            if (!CyberArena.Instance.GenerationFinished)
            {
                return;
            }

            spawnTimer -= Time.fixedDeltaTime;

            Vector3 spawnPos = Vector3.zero;

            if (spawnTimer <= 0.0f)
            {
                spawnTimer = 0.25f;

                if (_bossPicker.TypesToSpawn.Count == 0)
                {
                    return;
                }

                var typeToSpawn = _bossPicker.TypesToSpawn.Dequeue();
                GameObject prefab = null;

                switch (typeToSpawn.VanillaEnumValue)
                {
                    case EnemyType.Gabriel:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.Gabriel);
                        spawnPos = CyberArena.Instance.FloorCenter + (Vector3.up * 8.0f);
                        break;
                    case EnemyType.GabrielSecond:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.GabrielSecond);
                        spawnPos = CyberArena.Instance.FloorCenter + (Vector3.up * 16.0f);
                        break;
                    case EnemyType.V2:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.V2);
                        spawnPos = CyberArena.RandomOneByOne.GetValueOrDefault(Vector3.zero);
                        break;
                    case EnemyType.V2Second:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.V2Second);
                        spawnPos = CyberArena.RandomOneByOne.GetValueOrDefault(Vector3.zero);
                        break;
                    case EnemyType.FleshPrison:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.FleshPrison);
                        break;
                    case EnemyType.FleshPanopticon:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.FleshPanopticon);
                        break;
                    case EnemyType.MinosPrime:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.MinosPrime);
                        spawnPos = CyberArena.RandomOneByOne.GetValueOrDefault(Vector3.zero);
                        break;
                    case EnemyType.Minos:
                        prefab = EnemyVariants.CorpseOfKingMinosPrefab;
                        break;
                    case EnemyType.Leviathan:
                        prefab = EnemyVariants.LeviathanPrefab.gameObject;
                        break;
                    case EnemyType.SisyphusPrime:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.SisyphusPrime);
                        spawnPos = CyberArena.RandomTwoByTwo.GetValueOrDefault(Vector3.zero);
                        break;
                    case EnemyType.CancerousRodent:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.CancerousRodent);
                        spawnPos = CyberArena.RandomOneByOne.GetValueOrDefault(Vector3.zero);
                        break;
                    case EnemyType.Minotaur:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.Minotaur);
                        spawnPos = CyberArena.RandomThreeByThree.GetValueOrDefault(Vector3.zero);
                        break;
                    case EnemyType.VeryCancerousRodent:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.VeryCancerousRodent);
                        spawnPos = CyberArena.RandomTwoByTwo.GetValueOrDefault(Vector3.zero);
                        break;
                    case EnemyType.Geryon:
                        prefab = EnemyVariants.GeryonPrefab.gameObject;
                        break;
                    case EnemyType.Mandalore:
                        prefab = NyxLib.EnemyPrefabDatabase.GetPrefab(EnemyType.Mandalore);
                        spawnPos = CyberArena.RandomTwoByTwo.GetValueOrDefault(Vector3.zero);
                        break;
                    default:
                        break;
                }

                GameObject enemyGo = null;

                spawnPos = spawnPos == Vector3.zero ? CyberArena.Instance.FloorCenter + (Vector3.up * 10.0f) + (Vector3.forward * 10.0f) : spawnPos;
                var bossBar = Options.EnemyEntries[typeToSpawn].ShowBossBar.Value;

                if (prefab != null)
                {
                    enemyGo = GameObject.Instantiate(prefab, EndlessGrid.Instance.gameObject.transform);
                    Log.Debug($"done spawning prefab for type {typeToSpawn}");
                    var enemy = enemyGo.GetComponentInChildren<EnemyComponents>();
                    enemy.gameObject.AddComponent<GrindBoss>();
                    enemyGo.SetActive(true);
                    var eid = enemy.Eid;

                    enemy.Health = enemy.Health * Options.EnemyEntries[typeToSpawn].HealthScalar.Value;

                    eid.BossBar(bossBar);

                    if (typeToSpawn.VanillaEnumValue == EnemyType.FleshPrison || typeToSpawn.VanillaEnumValue == EnemyType.FleshPanopticon)
                    {
                        enemyGo.transform.position = CyberArena.Instance.FloorCenter;
                    }
                    else if (typeToSpawn.VanillaEnumValue == EnemyType.Minos)
                    {
                        enemyGo.transform.position = CyberArena.Instance.FloorCenter + (Vector3.right * 175.0f) + (Vector3.down * 600.0f);
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
                    else if (typeToSpawn.VanillaEnumValue == EnemyType.Geryon)
                    {
                        enemyGo.transform.position = spawnPos + Vector3.back * 125.0f + Vector3.up * 40.0f;
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
                    }
                    else if (typeToSpawn == EnemyVariants.BloodTree)
                    {
                        var bf = GameObject.Instantiate(EnemyVariants.BloodTreePrefab, CyberArena.RandomOneByOne.GetValueOrDefault(CyberArena.Instance.FloorCenter), Quaternion.Euler(-90.0f, 0.0f, 0.0f), EndlessGrid.Instance.transform);
                        bf.gameObject.AddComponent<GrindTree>();
                        bf.gameObject.SetActive(true);
                    }
                    else
                    {

                    }
                }
            }
        }
    }
}