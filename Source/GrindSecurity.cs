using System;
using System.Collections.Generic;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.CybergrindBosses
{
    public class GrindSecurity : MonoBehaviour
    {
        public EnemyComponents IdolEnemy { get; private set; } = null;
        public EnemyComponents[] enemies = new EnemyComponents[0];
        public EnemyIdentifier[] eids = new EnemyIdentifier[0];
        public BossIdentifier bid = null;
        public Idol Idol { get; private set; } = null;

        protected void Awake()
        {
            foreach (var enemy in enemies)
            {
                enemy.Health *= Options.EnemyEntries[EnemyTypeDB.Instance.GetVanillaType(EnemyType.Centaur)].HealthScalar.Value;
            }
        }

        protected void Start()
        {
            if (!Cheats.Enabled)
            {
                return;
            }
            //gameObject.DebugPrintChildren();

            enemies = GetComponentsInChildren<EnemyComponents>(true);
            eids = GetComponentsInChildren<EnemyIdentifier>(true);
            bid = GetComponentInChildren<BossIdentifier>(true);
            IdolEnemy = EnemyPrefabDatabase.TrySpawnAt(EnemyType.Idol, transform.position, Quaternion.identity, transform, true).GetComponent<EnemyComponents>();
            Idol = IdolEnemy.GetComponent<Idol>();
            IdolEnemy.Eid.Bless();
            IdolEnemy.Eid.dontCountAsKills = true;
            Idol.gameObject.transform.localScale = Vector3.one * 0.00001f;
            var idolCols = Idol.GetComponentsInChildren<Collider>();
            foreach (var col in idolCols)
            {
                col.enabled = false;
            }

            IdolEnemy.gameObject.SetActive(true);
            IdolEnemy.PostDeath += (cancelInfo, instaKill) =>
            {
                Defeated();
            };

            var prevPos = bid.transform.parent.position;
            var offset = transform.position - prevPos;
            bid.transform.parent.position += offset;
            var corePos = bid.transform.parent.position + (Vector3.right * 1.75f);

            foreach (var eid in eids)
            {
                eid.onDeath.AddListener(new UnityEngine.Events.UnityAction(() =>
                {
                    CheckIfDefeated();
                }));

                if (!eid.gameObject.transform.IsChildOf(bid.transform.parent))
                {
                    eid.gameObject.transform.position += offset;
                }

                if (!eid.gameObject.name.Contains("Mainframe"))
                {
                    eid.transform.position = corePos + Vector3.Scale((eid.transform.position - corePos), new Vector3(-1.0f, 1.0f, 0.0f));
                    eid.transform.position = Vector3.Lerp(eid.gameObject.transform.position, corePos, 0.35f);
                    float prevY = eid.transform.position.y;
                    eid.transform.position = Vector3.Lerp(eid.gameObject.transform.position, corePos, 0.5f);
                    var tempPos = eid.transform.position;
                    tempPos.y = prevY;
                    eid.transform.position = tempPos;
                }

                eid.dontCountAsKills = true;
            }

            transform.position += (Vector3.right * 0.0f);

            var beam = GetComponentInChildren<ContinuousBeam>();
            Idol.gameObject.transform.position = corePos + Vector3.up * 12.0f;
        }

        protected void FixedUpdate()
        {
            if (!Cheats.Enabled)
            {
                return;
            }

            foreach (var eid in eids)
            {
                if (eid.blessed)
                {
                    eid.Unbless();
                }
            }
        }

        private void CheckIfDefeated()
        {
            foreach (var enemy in enemies)
            {
                if (!enemy.Eid.Dead)
                {
                    return;
                }
            }

            Defeated();
        }

        private void Defeated()
        {
            EndlessGrid.Instance.GetComponent<ActivateNextWave>().AddDeadEnemy();
            Destroy(gameObject);
        }
    }
}