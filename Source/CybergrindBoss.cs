using UnityEngine;
using BepInEx;
using Nyxpiri.ULTRAKILL.NyxLib;
using System;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using Sandbox;

namespace Nyxpiri.ULTRAKILL.CybergrindBosses
{
    public class GrindBoss : MonoBehaviour
    {
        public class AutoDeactivate : MonoBehaviour
        {
            protected void OnEnable()
            {
                gameObject.SetActive(false);
            }
        }

        EnemyComponents Enemy = null;

        protected void Awake()
        {
            v2 = GetComponentInChildren<V2>();
            sm = GetComponent<SwordsMachine>();

            if (v2 != null && v2.secondEncounter)
            {
                string name = "V2... 2!";
                
                switch (UnityEngine.Random.Range(0, 3))
                {
                    case 0:
                    break;
                    case 1:
                    break;
                    default:
                    break;
                }
                
                OverrideFullNameFA.SetValue(v2.gameObject.GetComponent<EnemyIdentifier>(), name);
            }
        }

        protected void Start()
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }
            
            Enemy = GetComponent<EnemyComponents>();
            Enemy.Eid.dontCountAsKills = true;
            Enemy.PreDeath += PreDeath;
            Enemy.PostDeath += PostDeath;
            Enemy.AvoidHealthBasedSlowDown = true;
            
            garbage = GetComponentInChildren<GabrielBase>();

            prison = GetComponentInChildren<FleshPrison>();

            lev = GetComponent<LeviathanController>();
                        
            if (garbage != null)
            {
                GabrielBaseBossVersionFA.SetValue(garbage, false);
            }

            GameObject rootGo = Enemy.RootGameObject;

            var colliders = Enemy.RootGameObject.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.gameObject.GetOrAddComponent<IgnoreDeathZones>();
            }
                        
            if (IsTundraAgony)
            {
                Assert.IsNotNull(Enemy);
                Assert.IsNotNull(Enemy.GetComponent<Enemy>());
                Assert.IsNotNull(Enemy.GetComponent<Enemy>().symbiote);
                Symbiote = Enemy.GetComponent<Enemy>().symbiote.GetComponent<EnemyComponents>();
                SymbioteSm = Symbiote.GetComponent<SwordsMachine>();
            }
        }

        FieldAccess<V2, float> V2DistancePatienceFA = new FieldAccess<V2, float>("distancePatience");
        FieldAccess<EnemyIdentifier, string> OverrideFullNameFA = new FieldAccess<EnemyIdentifier, string>("overrideFullName");
        FieldAccess<GabrielBase, bool> GabrielBaseBossVersionFA = new FieldAccess<GabrielBase, bool>("bossVersion");
        int remainingBoostHelpers = 7;
        FixedTimeStamp lastBoostHelperTimestamp = new FixedTimeStamp();

        protected void FixedUpdate()
        {
            if (!NyxLib.Cheats.Enabled)
            {
                return;
            }

            if (v2 != null)
            {
                var distPatience = V2DistancePatienceFA.GetValue(v2);

                distPatience = Mathf.Min(distPatience, 4.5f);

                V2DistancePatienceFA.SetValue(v2, distPatience);
            }

            Enemy.Eid.dontCountAsKills = true;

            if (Enemy.transform.position.y < 5.0f && remainingBoostHelpers > 0 && lastBoostHelperTimestamp.TimeSince > 0.75 && garbage == null)
            {
                var rb = Enemy.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    var pos = rb.transform.position;
                    var horizontalPos = pos;
                    horizontalPos.Scale(new Vector3(1.0f, 0.0f, 1.0f));
                    rb.velocity.Scale(new Vector3(1.0f, 0.0f, 1.0f));
                    /*
                    var dist = Vector3.Distance(CybergrindBosses.cgCenter, horizontalPos);
                    rb.velocity = Vector3.up * (dist * 2.15f) + ((CybergrindBosses.cgCenter - horizontalPos).normalized) * 1.5f * dist;
                    */
                    rb.velocity = Vector3.up * 80.0f + ((CybergrindBosses.cgCenter - horizontalPos).normalized) * 25.0f;
                    lastBoostHelperTimestamp.UpdateToNow();
                    if (v2 != null)
                    {
                        var enemy = v2.GetComponent<EnemyComponents>();
                        enemy.Eid.ApplyDamage(Vector3.zero, enemy.Eid.transform.position, enemy.InitialHealth / 9.0f, 1.0f, null, true);
                        var explosionGo = GameObject.Instantiate(NyxLib.Assets.ExplosionPrefab, transform.parent);
                        explosionGo.transform.position = transform.position + Vector3.down;
                        var explosion = explosionGo.GetComponentInChildren<Explosion>();
                        explosion.ignite = false;
                        explosion.harmless = true;
                        explosion.damage = 0;
                        explosion.pushForceMultiplier = 0.0f;
                        explosion.friendlyFire = true;
                        explosionGo.SetActive(true);
                    }
                    else if (IsTundraAgony && Symbiote != null && !Symbiote.Eid.Dead)
                    {
                        Symbiote.GetComponent<GrindBoss>().RequestSymbioteSave();
                    }
                    else
                    {
                        remainingBoostHelpers -= 1;
                    }
                }

                if (Enemy.transform.position.y < -5.0f && IsTundraAgony)
                {
                    Enemy.Eid.InstaKill();                    
                }

                if (v2 != null && v2.isEnraged)
                {
                    v2.UnEnrage();
                }
            }
            
            if (symbioteSaveStart.TimeSince < 1.5)
            {
                if (Enemy.Eid.Dead || Symbiote == null || Symbiote.Eid.Dead)
                {
                    return;
                }
                
                if (!((sm?.downed).GetValueOrDefault(true)))
                {
                    sm.Knockdown();
                }
                
                if (!((SymbioteSm?.downed).GetValueOrDefault(true)))
                {
                    SymbioteSm.Knockdown();
                }

                if (Enemy.Health <= 0.5f)
                {
                    Enemy.Eid.InstaKill();
                }

                if (symbioteSaveStart.TimeSince < 0.75)
                {
                    SymbioteRb.transform.position += (Vector3.up * symbioteSavePhase1Speed * Time.fixedDeltaTime);
                    symbioteSavePhase2Speed = Vector3.Distance(SymbioteRb.transform.position, transform.position) * 2.5f;
                }
                else if (symbioteSaveStart.TimeSince > 1.0)
                {
                    SymbioteRb.transform.position += (Vector3.Normalize(transform.position - SymbioteRb.transform.position) * symbioteSavePhase2Speed * Time.fixedDeltaTime);
                }
            }

            if (lev != null)
            {
                if (EndlessGrid.Instance.enemyAmount - EndlessGrid.Instance.GetComponent<ActivateNextWave>().deadEnemies <= 1)
                {
                    lev.phaseChangeHealth = 10000.0f;
                    lev.onEnterSecondPhase.onActivate.AddListener(new UnityEngine.Events.UnityAction(() =>
                    {
                        if (leviathanSecondPhaseEventCalled)
                        {
                            return;
                        }

                        leviathanSecondPhaseEventCalled = true;
                        lev.transform.position += Vector3.down * 20.0f;

                        for (int i = 0; i < EndlessGrid.Instance.cubes.Length; i++)
                        {
                            EndlessCube[] cubeX = EndlessGrid.Instance.cubes[i];
                            foreach (var cube in cubeX)
                            {
                                cube.transform.position += Vector3.down * 150.0f;
                            }
                        }

                        lev.stat.health *= 0.5f;

                        var combinedGridStaticObjectFA = new FieldAccess<EndlessGrid, GameObject>("combinedGridStaticObject");

                        combinedGridStaticObjectFA.GetValue(EndlessGrid.Instance).SetActive(false);
                        var explosion = GameObject.Instantiate(NyxLib.Assets.ExplosionPrefab, CybergrindBosses.cgCenter, Quaternion.identity);
                        var eadd = explosion.GetComponent<ExplosionAdditions>();
                        eadd.Harmless = true;
                        eadd.ExplosionScale = 20.0f;
                        eadd.ExplosionSpeedScale = 5.0f;
                        eadd.ExplosionPushScale = 0.0f;
                        explosion.SetActive(true);
                        foreach (var audio in eadd.Audios)
                        {
                            audio.maxDistance *= 100.0f;
                            audio.volume *= 1.2f;
                        }

                        levSpawnHookPointsTimer = 1.0f;

                        if (NewMovement.Instance.transform.position.y < 50.0f)
                        {
                            NewMovement.Instance.gc.heavyFall = false;
                        }
                        
                        NewMovement.Instance.LaunchUp(75.0f);
                    }));
                }

                if (levSpawnHookPointsTimer > 0.0f)
                {
                    levSpawnHookPointsTimer -= Time.fixedDeltaTime;

                    if (levSpawnHookPointsTimer <= 0.0f)
                    {
                        levSpawnHookPointsTimer = -1.0f;

                        LeviathanSpawnHookPoints();
                    }
                }
            }

            if (v2 != null)
            {
                v2.dontEnrage = true;
            }

            if (Enemy.transform.position.y < -30.0f && remainingBoostHelpers <= 0 && Enemy.Eid.enemyType != EnemyType.Minos)
            {
                Enemy.Eid.InstaKill();
            }
            
            if (waitingToDestroyTime > 0.0f && waitingToDestroyTimestamp.TimeSince > waitingToDestroyTime)
            {
                if (lev != null)
                {
                        var explosion = GameObject.Instantiate(NyxLib.Assets.ExplosionPrefab, lev.head.transform.position, Quaternion.identity);
                        var eadd = explosion.GetComponent<ExplosionAdditions>();
                        eadd.Harmless = true;
                        eadd.ExplosionScale = 20.0f;
                        eadd.ExplosionSpeedScale = 8.0f;
                        eadd.ExplosionPushScale = 0.0f;
                        explosion.SetActive(true);
                        foreach (var audio in eadd.Audios)
                        {
                            audio.maxDistance *= 100.0f;
                            audio.volume *= 1.2f;
                        }
                }

                Enemy.InstaDestroy();
            }
        }

        private void LeviathanSpawnHookPoints()
        {
            Vector3 offset = Vector3.forward * 80.0f;
            int numHookPoints = 5;
            for (int i = 0; i < numHookPoints; i++)
            {
                offset.y = 80.0f;  

                Vector3 currentOffset = (Quaternion.Euler(new Vector3(0.0f, Mathf.Lerp(0.0f, 360.0f, ((float)(i) + -0.5f) / numHookPoints), 0.0f)) * (offset)); 

                var hookPointGo = GameObject.Instantiate(NyxLib.Assets.HookPoints.SlingshotHookPoint, lev.head.transform.position + currentOffset, Quaternion.identity, EndlessGrid.Instance.transform);
                hookPointGo.SetActive(true);
                GameObjectsToDestroy.Add(hookPointGo);
            }
        }

        float symbioteSavePhase1Speed = 0.0f;
        float symbioteSavePhase2Speed = 0.0f;

        private void RequestSymbioteSave()
        {
            if (Enemy.Eid.Dead)
            {
                return;
            }

            if (symbioteSaveStart.TimeSince < 3.0)
            {
                return;
            }

            SymbioteRb = SymbioteSm.GetComponent<Rigidbody>();
            symbioteSavePhase1Speed = Mathf.Abs((SymbioteRb.transform.position.y - (transform.position.y + 20.0f))) * 1.75f;
            SymbioteRb.isKinematic = true;
            sm?.Knockdown(false, false, true, false);
            symbioteSaveStart.UpdateToNow();
        }

        bool addDeadEnemyCalled = false;
        private SwordsMachine sm;
        private LeviathanController lev;
        private V2 v2;
        private GabrielBase garbage;
        private FleshPrison prison;
        FixedTimeStamp symbioteSaveStart;
        private FixedTimeStamp waitingToDestroyTimestamp;
        private float waitingToDestroyTime = -1.0f;
        private bool leviathanSecondPhaseEventCalled = false;
        private float levSpawnHookPointsTimer = -1.0f;

        public bool IsTundraAgony { get; internal set; }
        public EnemyComponents Symbiote { get; private set; }
        public SwordsMachine SymbioteSm { get; private set; }
        public Rigidbody SymbioteRb { get; private set; }
        public List<GameObject> GameObjectsToDestroy { get; private set; } = new List<GameObject>();

        protected void OnDestroy()
        {
            foreach (var go in GameObjectsToDestroy)
            {
                Destroy(go);
            }
        }

        private void PreDeath(EventMethodCanceler canceler, bool instakill)
        {
            if (Enemy.Eid.enemyType == EnemyType.Leviathan)
            {
                waitingToDestroyTime = 3.5f;
                waitingToDestroyTimestamp.UpdateToNow();
                if (NewMovement.Instance.transform.position.y < 15.0f && lev.secondPhase)
                {
                    NewMovement.Instance.gc.heavyFall = false;
                    NewMovement.Instance.LaunchUp(75.0f);
                }
            }
        }

        private void PostDeath(EventMethodCancelInfo cancelInfo, bool instakill)
        {
            if (addDeadEnemyCalled)
            {
                return;
            }

            addDeadEnemyCalled = true;
            EndlessGrid.Instance.GetComponent<ActivateNextWave>().AddDeadEnemy();
        }
    }
}