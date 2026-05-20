using System;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.CybergrindBosses
{
    public class GrindTree : MonoBehaviour
    {
        public BloodFiller Bf { get; private set; } = null;
        public EndlessGrid Eg { get; private set; } = null;

        public ActivateNextWave Anw { get; private set; } = null;

        private GoreZone _goreZone;

        public EnemyComponents DeathcatcherEnemy { get; private set; } = null;
        public Deathcatcher Deathcatcher { get; private set; } = null;

        public int NumEnemies => (Eg.enemyAmount - Anw.deadEnemies);

        protected void Awake()
        {
            Bf = GetComponent<BloodFiller>();
            Eg = EndlessGrid.Instance;
            Anw = Eg.GetComponent<ActivateNextWave>();
        }

        protected void Start()
        {
            _goreZone = GoreZone.ResolveGoreZone(base.transform);

            var capsules = GetComponentsInChildren<CapsuleCollider>();
            foreach (var capsule in capsules)
            {
                if (capsule.gameObject.name != "Bloodcatcher")
                {
                    continue;
                }

                capsule.radius = Options.BloodTreeCatcherRadius.Value;
                capsule.height = Options.BloodTreeCatcherHeight.Value;
            }

            DeathcatcherEnemy = EnemyPrefabDatabase.TrySpawnAt(EnemyType.Deathcatcher, transform.position, Quaternion.identity, transform, true).GetComponent<EnemyComponents>();
            Deathcatcher = DeathcatcherEnemy.GetComponent<Deathcatcher>();
            DeathcatcherEnemy.Eid.Bless();
            DeathcatcherEnemy.Eid.dontCountAsKills = true;
            Deathcatcher.gameObject.transform.localScale = Vector3.one * 0.00001f;

            Bf.onFullyFilled.onActivate.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                OnFilled();
            }));

            DeathcatcherEnemy.gameObject.SetActive(true);
            DeathcatcherEnemy.PostDeath += (cancelInfo, instaKill) =>
            {
                OnFilled(trueFill: false);
            };

            StartTimestamp.UpdateToNow();
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
            Destroy(gameObject);
        }

        private bool _beenFilled = false;
        private void OnFilled(bool trueFill = true)
        {
            if (_beenFilled)
            {
                return;
            }

            _beenFilled = true;

            EndlessGrid.Instance.GetComponent<ActivateNextWave>().deadEnemies += 1;

            if (DeathcatcherEnemy != null)
            {
                FieldAccess<Deathcatcher, GameObject> deathParticleFA = new FieldAccess<Deathcatcher, GameObject>("deathParticle");
                var deathParticle = UnityEngine.Object.Instantiate(deathParticleFA.GetValue(Deathcatcher), transform.position + Vector3.up * 8.0f, Quaternion.identity, _goreZone.gibZone);
                deathParticle.SetActive(true);
                deathParticle.transform.localScale *= 2.0f;
                var explosion = deathParticle.GetComponentInChildren<Explosion>();
                explosion.pushForceMultiplier = 0.0f;
                DeathcatcherEnemy.InstaDestroy();
            }

            if (trueFill)
            {
                StyleHUD.Instance.AddPoints(100, $"MERCY");
            }

            if (_failSafeFilth != null && !_failSafeFilth.Eid.Dead)
            {
                _failSafeFilth.Eid.InstaKill();
            }

            Destroy(gameObject);
        }

        protected void FixedUpdate()
        {
            Bf.fillSpeed = Options.BloodTreeFillSpeedBase.Value / Mathf.Max(EndlessGrid.Instance.enemyAmount, 5.0f);
            if (NumEnemies <= 3)
            {
                if (SpawnFailsafeFilthTimestamp.TimeSince >= Mathf.Clamp(30.0f / ((float)StartTimestamp.TimeSince + 1.0f), 0.5f, 40.0f))
                {
                    TrySpawnFailsafeFilth();
                }
            }
        }

        private void TrySpawnFailsafeFilth()
        {
            if (_failSafeFilth != null && !_failSafeFilth.Eid.Dead)
            {
                return;
            }

            var filthGo = EnemyPrefabDatabase.TrySpawnAt(EnemyType.Filth, transform.position + Vector3.up, Quaternion.identity, EndlessGrid.Instance.transform, true);
            var filth = filthGo.GetComponent<EnemyComponents>();
            filth.Eid.dontCountAsKills = true;
            filth.Eid.PuppetSpawn();
            SpawnFailsafeFilthTimestamp.UpdateToNow();
            _failSafeFilth = filth;
        }

        FixedTimeStamp StartTimestamp = new FixedTimeStamp();
        FixedTimeStamp SpawnFailsafeFilthTimestamp = new FixedTimeStamp();
        private EnemyComponents _failSafeFilth = null;
    }
}