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

            Enemy = GetComponent<EnemyComponents>();

            Enemy.Eid.dontCountAsKills = true;
            Enemy.PreDeath += PreDeath;
            Enemy.PostDeath += PostDeath;
            Enemy.AvoidHealthBasedSlowDown = true;

            GameObject rootGo = Enemy.RootGameObject;

            var colliders = rootGo.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.gameObject.GetOrAddComponent<IgnoreDeathZones>();
            }

            if (v2 != null && v2.secondEncounter)
            {
                string name = "V2... 2!";

                switch (UnityEngine.Random.Range(0, 100))
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

            garbage = GetComponentInChildren<GabrielBase>();

            prison = GetComponentInChildren<FleshPrison>();

            lev = GetComponent<LeviathanController>();

            sisyprime = GetComponent<SisyphusPrime>();
            minosP = GetComponent<MinosPrime>();
            minos = GetComponent<MinosBoss>();

            gery = GetComponent<Geryon>();

            if (garbage != null)
            {
                GabrielBaseBossVersionFA.SetValue(garbage, false);
                GetComponent<GabrielVoice>().Invoke("Taunt", UnityEngine.Random.Range(0.1f, 0.5f));
            }

            if (minosP != null)
            {
                MonoSingleton<SubtitleController>.Instance.DisplaySubtitle("WEAK");
                minosP.GetComponent<AudioSource>().clip = minosP.phaseChangeVoice;
                minosP.GetComponent<AudioSource>().SetPitch(1f);
                minosP.GetComponent<AudioSource>().Play(tracked: true);
            }

            if (sisyprime != null)
            {
                sisyprime.Taunt();
            }

            if (IsTundraAgony)
            {
                Assert.IsNotNull(Enemy);
                Assert.IsNotNull(Enemy.GetComponent<Enemy>());
                Assert.IsNotNull(Enemy.GetComponent<Enemy>().symbiote);
                Symbiote = Enemy.GetComponent<Enemy>().symbiote.GetComponent<EnemyComponents>();
                SymbioteSm = Symbiote.GetComponent<SwordsMachine>();
            }

            if (minos != null)
            {
                minos.blackHole = GameObject.Instantiate(minos.blackHole.gameObject, minos.blackHole.transform.parent);
                var modifier = minos.blackHole.AddComponent<BlackholeModifier>();
                modifier.RescaleOnStart = 0.4f;
                modifier.Damage = 50;
                modifier.KillThreshold = 5;
                _minosTargetPos = transform.position;
            }

            if (prison != null && prison.blackHole != null)
            {
                prison.blackHole = GameObject.Instantiate(prison.blackHole.gameObject, prison.blackHole.transform.parent);
                var modifier = prison.blackHole.AddComponent<BlackholeModifier>();
                modifier.RescaleOnStart = 1.0f;
                modifier.Damage = 50;
                modifier.KillThreshold = 5;
            }

            if (gery != null)
            {
                FieldAccess<Geryon, Transform> rotateAroundFA = new FieldAccess<Geryon, Transform>("rotateAround");
                //rotateAroundFA.SetValue(gery, CyberArena.Instance.FloorCenter);
                gery.gameObject.AddComponent<GeryonTweaks>();
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

            if (!Enemy.Eid.Dead && Enemy.transform.position.y < 5.0f && remainingBoostHelpers > 0 && lastBoostHelperTimestamp.TimeSince > 0.75 && garbage == null)
            {
                var rb = Enemy.GetComponent<Rigidbody>();
                if (rb != null && minosP == null && sisyprime == null)
                {
                    var pos = rb.transform.position;
                    var horizontalPos = pos;
                    horizontalPos.Scale(new Vector3(1.0f, 0.0f, 1.0f));
                    rb.velocity.Scale(new Vector3(1.0f, 0.0f, 1.0f));
                    /*
                    var dist = Vector3.Distance(CybergrindBosses.cgCenter, horizontalPos);
                    rb.velocity = Vector3.up * (dist * 2.15f) + ((CybergrindBosses.cgCenter - horizontalPos).normalized) * 1.5f * dist;
                    */
                    rb.velocity = Vector3.up * 80.0f + ((CyberArena.HorizontalCenter - horizontalPos).normalized) * 35.0f;
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

                var idolPrefab = EnemyPrefabDatabase.GetPrefab(EnemyType.Idol);
                var idol = idolPrefab.GetComponent<Idol>();
                FieldAccess<Idol, GameObject> deathParticleFA = new FieldAccess<Idol, GameObject>("deathParticle");
                var deathParticle = deathParticleFA.GetValue(idol);

                if (sisyprime != null)
                {
                    sisyprime.DisableGravity();
                    Enemy.Eid.SimpleDamage(10.0f);
                    var ouchieParticle = GameObject.Instantiate(deathParticle, transform.position, Quaternion.identity, EndlessGrid.Instance.transform);
                    ouchieParticle.SetActive(true);
                    sisyprime.transform.position = CyberArena.HorizontalCenter + Vector3.up * 100.0f;
                }
                else if (minosP != null)
                {
                    minosP.DisableGravity();
                    Enemy.Eid.SimpleDamage(7.0f);
                    var ouchieParticle = GameObject.Instantiate(deathParticle, transform.position, Quaternion.identity, EndlessGrid.Instance.transform);
                    ouchieParticle.SetActive(true);
                    minosP.transform.position = CyberArena.HorizontalCenter + Vector3.up * 100.0f;
                }
            }

            if (minos)

                if (Enemy.transform.position.y < -5.0f && IsTundraAgony)
                {
                    Enemy.Eid.InstaKill();
                }

            if (v2 != null && v2.isEnraged)
            {
                v2.UnEnrage();
            }

            if (v2 != null)
            {
                if (v2.transform.position.magnitude > 350.0f && !Enemy.Eid.Dead)
                {
                    v2.GetComponent<EnemyIdentifier>().InstaKill();
                    StyleHUD.Instance.AddPoints(10, "STRANDED");
                }
            }

            if (symbioteSaveStart.TimeSince < 4.0)
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

                if (symbioteSaveStart.TimeSince < 2.0f)
                {
                    SymbioteRb.transform.position += (Vector3.up * symbioteSavePhase1Speed * Time.fixedDeltaTime);
                    symbioteSavePhase2Speed = Vector3.Distance(SymbioteRb.transform.position, transform.position) * 2.75f;
                }
                else if (symbioteSaveStart.TimeSince > 3.0f)
                {
                    SymbioteRb.transform.position += (Vector3.Normalize(transform.position - SymbioteRb.transform.position) * symbioteSavePhase2Speed * Time.fixedDeltaTime);
                }

                var machine = GetComponent<Machine>();
                var healingFA = new FieldAccess<Machine, bool>("healing");
                if (healingFA.GetValue(machine))
                {
                    healingFA.SetValue(machine, false);
                }
            }

            if (lev != null)
            {
                if (RemainingEnemies <= 1)
                {
                    if (!lev.secondPhase && lev.readyForSecondPhase && _levSecondPhaseRequestTimestamp.TimeSince > 1.0)
                    {
                        lev.SubAttackOver();
                    }
                    else if (!lev.secondPhase && lev.readyForSecondPhase)
                    {
                        lev.transform.position += Vector3.down * 35.0f * Time.fixedDeltaTime;
                    }

                    LeviathanSecondPhase();
                }

                if (levSpawnHookPointsTimer > 0.0f)
                {
                    levSpawnHookPointsTimer -= Time.fixedDeltaTime;

                    if (levSpawnHookPointsTimer <= 0.0f)
                    {
                        levSpawnHookPointsTimer = -1.0f;

                        LeviathanSpawnHookPoints();
                        LevSecondPhaseLaunchPlayerUp();
                    }
                }

                if (levDestroyFloorTimer > 0.0f)
                {
                    levDestroyFloorTimer -= Time.fixedDeltaTime;

                    if (levDestroyFloorTimer <= 0.0f)
                    {
                        levDestroyFloorTimer = -1.0f;
                        DestroyFloor();
                    }
                }

                if (levDeathLaunchPlayerTimer > 0.0f)
                {
                    levDeathLaunchPlayerTimer -= Time.fixedDeltaTime;

                    if (levDeathLaunchPlayerTimer <= 0.0f)
                    {
                        levDeathLaunchPlayerTimer = -1.0f;

                        var colliders = lev.gameObject.GetComponentsInChildren<Collider>();

                        foreach (var col in colliders)
                        {
                            col.enabled = false;
                        }

                        LevDeathLaunchPlayer();
                        BigHarmlessExplosionAt(lev.head.transform.position);
                    }
                }
            }

            if (v2 != null)
            {
                v2.dontEnrage = true;
            }

            if (Enemy.transform.position.y < -15.0f && (Enemy.Eid.enemyType == EnemyType.Swordsmachine || Enemy.Eid.enemyType == EnemyType.V2 || Enemy.Eid.enemyType == EnemyType.V2Second))
            {
                Enemy.Eid.InstaKill();
            }

            if (Enemy.Eid.Dead && Enemy.Eid.enemyType == EnemyType.Minos && waitingToDestroyTimestamp.TimeSince > 0.2)
            {
                _verticalShiftVelocity -= Time.fixedDeltaTime * 60.0f;

                transform.position += Vector3.up * _verticalShiftVelocity;
            }

            if (waitingToDestroyTime > 0.0f && waitingToDestroyTimestamp.TimeSince > waitingToDestroyTime)
            {
                if (lev != null)
                {
                    BigHarmlessExplosionAt(lev.head.transform.position);
                }

                if (gery != null)
                {
                    var goreZone = GoreZone.ResolveGoreZone(base.transform);

                    for (int i = 0; i < 24; i++)
                    {
                        var blood = BloodsplatterManager.Instance.GetGore(GoreType.Head, false, false, false, GetComponent<EnemyIdentifier>());
                        if (!blood)
                        {
                            break;
                        }

                        blood.transform.position = transform.position + (UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(0.0f, 34.0f));
                        if (goreZone.goreZone != null)
                        {
                            blood.transform.SetParent(goreZone.goreZone, true);
                        }

                        blood.SetActive(value: true);
                        if (blood.TryGetComponent<Bloodsplatter>(out var splatter))
                        {
                            splatter.GetReady();
                        }
                    }
                }

                Enemy.InstaDestroy();
            }
        }

        private void BigHarmlessExplosionAt(Vector3 position)
        {
            var explosion = GameObject.Instantiate(NyxLib.Assets.ExplosionPrefab, position, Quaternion.identity);
            var eadd = explosion.GetComponent<ExplosionAdditions>();
            eadd.Harmless = true;
            eadd.ExplosionScale = 20.0f;
            eadd.ExplosionSpeedScale = 10.0f;
            eadd.ExplosionPushScale = 0.0f;
            explosion.SetActive(true);
            foreach (var audio in eadd.Audios)
            {
                audio.maxDistance *= 100.0f;
                audio.volume *= 1.2f;
            }
        }

        private bool _levAlreadySecondPhasing = false;
        private FixedTimeStamp _levSecondPhaseRequestTimestamp = new FixedTimeStamp();
        private void LeviathanSecondPhase()
        {
            if (_levAlreadySecondPhasing)
            {
                return;
            }

            _levSecondPhaseRequestTimestamp.UpdateToNow();

            _levAlreadySecondPhasing = true;

            lev.phaseChangeHealth = 10000.0f;
            lev.onEnterSecondPhase.onActivate.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                if (leviathanSecondPhaseEventCalled)
                {
                    return;
                }

                leviathanSecondPhaseEventCalled = true;

                lev.stat.health *= 0.5f;

                var explosion = GameObject.Instantiate(NyxLib.Assets.ExplosionPrefab, CyberArena.HorizontalCenter, Quaternion.identity);
                var eadd = explosion.GetComponent<ExplosionAdditions>();
                eadd.Harmless = true;
                eadd.ExplosionScale = 20.0f;
                eadd.ExplosionSpeedScale = 10.0f;
                eadd.ExplosionPushScale = 0.0f;
                explosion.SetActive(true);

                foreach (var audio in eadd.Audios)
                {
                    audio.maxDistance *= 100.0f;
                    audio.volume *= 1.2f;
                }

                levDestroyFloorTimer = 0.2f;
                levSpawnHookPointsTimer = 0.35f;
            }));
        }

        private static void DestroyFloor()
        {
            for (int i = 0; i < EndlessGrid.Instance.cubes.Length; i++)
            {
                EndlessCube[] cubeX = EndlessGrid.Instance.cubes[i];
                foreach (var cube in cubeX)
                {
                    cube.transform.position += Vector3.down * 150.0f;
                }
            }

            var combinedGridStaticObjectFA = new FieldAccess<EndlessGrid, GameObject>("combinedGridStaticObject");

            combinedGridStaticObjectFA.GetValue(EndlessGrid.Instance).SetActive(false);

            var jumpPadPoolFA = new FieldAccess<EndlessGrid, List<CyberPooledPrefab>>("jumpPadPool");

            var jumpPadPool = jumpPadPoolFA.GetValue(EndlessGrid.Instance);

            foreach (var jumpPad in jumpPadPool)
            {
                jumpPad.gameObject.SetActive(false);
            }
        }

        private static void LevSecondPhaseLaunchPlayerUp()
        {
            if (NewMovement.Instance.transform.position.y < 50.0f)
            {
                NewMovement.Instance.gc.heavyFall = false;
            }

            NewMovement.Instance.LaunchUp(75.0f);
        }

        private void LeviathanSpawnHookPoints()
        {
            Vector3 offset = Vector3.forward * 65.0f;
            int numHookPoints = 4;
            for (int i = 0; i < numHookPoints; i++)
            {
                offset.y = 60.0f;

                Vector3 currentOffset = (Quaternion.Euler(new Vector3(0.0f, Mathf.Lerp(0.0f, 360.0f, ((float)(i) + -0.5f) / numHookPoints), 0.0f)) * (offset));

                var hookPointGo = GameObject.Instantiate(NyxLib.Assets.HookPoints.SlingshotHookPoint, lev.head.transform.position + currentOffset, Quaternion.identity, EndlessGrid.Instance.transform);
                hookPointGo.SetActive(true);
                GameObjectsToDestroy.Add(hookPointGo);
            }
        }

        protected void OnEnable()
        {
            Cybergrind.PreCybergrindNextWave += OnNextWave;
        }

        protected void OnDisable()
        {
            Cybergrind.PreCybergrindNextWave -= OnNextWave;
        }

        protected void OnNextWave(EventMethodCanceler canceler, EndlessGrid endlessGrid)
        {
            if (Enemy.Eid.enemyType == EnemyType.Leviathan || Enemy.Eid.enemyType == EnemyType.Minos ||
                Enemy.Eid.enemyType == EnemyType.Leviathan || Enemy.Eid.enemyType == EnemyType.Leviathan)
            {
                if (Enemy.Eid.Dead)
                {
                    return;
                }
            }
            Enemy.InstaDestroy();
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
            symbioteSavePhase1Speed = Mathf.Abs((SymbioteRb.transform.position.y - (transform.position.y + 20.0f))) * 0.8f;
            SymbioteRb.isKinematic = true;
            sm?.Knockdown(false, false, true, false);
            symbioteSaveStart.UpdateToNow();
        }

        bool addDeadEnemyCalled = false;
        private SwordsMachine sm;
        private LeviathanController lev;
        private V2 v2;
        private MinosPrime minosP;
        private SisyphusPrime sisyprime;
        private GabrielBase garbage;
        private FleshPrison prison;
        private Geryon gery;
        private MinosBoss minos;
        FixedTimeStamp symbioteSaveStart;
        private FixedTimeStamp waitingToDestroyTimestamp;
        private float waitingToDestroyTime = -1.0f;
        private bool leviathanSecondPhaseEventCalled = false;
        private float levSpawnHookPointsTimer = -1.0f;
        private float levDestroyFloorTimer = -1.0f;
        private float levDeathLaunchPlayerTimer = -1.0f;
        private float _verticalShiftVelocity = 0.0f;
        private Vector3 _minosTargetPos;

        public bool IsTundraAgony { get; internal set; }
        public EnemyComponents Symbiote { get; private set; }
        public SwordsMachine SymbioteSm { get; private set; }
        public Rigidbody SymbioteRb { get; private set; }
        public List<GameObject> GameObjectsToDestroy { get; private set; } = new List<GameObject>();
        public int RemainingEnemies => EndlessGrid.Instance.enemyAmount - EndlessGrid.Instance.GetComponent<ActivateNextWave>().deadEnemies;

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
                levDeathLaunchPlayerTimer = 0.4f;
            }

            if (Enemy.Eid.enemyType == EnemyType.Minos)
            {
                waitingToDestroyTime = 1.75f;
                waitingToDestroyTimestamp.UpdateToNow();
            }

            if (sisyprime != null)
            {
                waitingToDestroyTime = 0.5f;
                waitingToDestroyTimestamp.UpdateToNow();
            }

            if (minosP != null)
            {
                waitingToDestroyTime = 0.5f;
                waitingToDestroyTimestamp.UpdateToNow();
            }

            if (gery != null)
            {
                waitingToDestroyTime = 0.5f;
                waitingToDestroyTimestamp.UpdateToNow();
            }
        }

        private void LevDeathLaunchPlayer()
        {
            if (NewMovement.Instance.transform.position.y < 75.0f && lev.secondPhase)
            {
                NewMovement.Instance.gc.heavyFall = false;
            }

            if (NewMovement.Instance.transform.position.y < 120.0f && lev.secondPhase)
            {
                var playerHorizontalPos = NewMovement.Instance.transform.position;
                playerHorizontalPos.Scale(new Vector3(1.0f, 0.0f, 1.0f));

                NewMovement.Instance.Launch((Vector3.up * 100.0f + (((CyberArena.HorizontalCenter - playerHorizontalPos)).normalized * (Vector3.Distance(CyberArena.HorizontalCenter, playerHorizontalPos) * 0.4f))).normalized, Vector3.Distance(CyberArena.HorizontalCenter, playerHorizontalPos) * 0.65f, true);
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