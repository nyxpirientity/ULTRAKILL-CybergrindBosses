using System;
using HarmonyLib;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nyxpiri.ULTRAKILL.CybergrindBosses
{
    public static class EnemyVariants
    {
        public static EnemyTypeData TundraAgonyType = new EnemyTypeData("SWORDSMACHINE \"AGONY\" AND \"TUNDRA\"", "TundraAndAgony", "cybergrind-bosses.tundra-and-agony", EnemyType.Swordsmachine);
        public static EnemyTypeData BloodTree = new EnemyTypeData("Blood Tree", "BloodTree", "cybergrind-bosses.blood-tree");
        public static LeviathanController LeviathanPrefab = null;
        public static GameObject CorpseOfKingMinosPrefab = null;
        public static GameObject CentaurSecurityPrefab = null;
        public static BloodFiller BloodTreePrefab = null;
        private static GameObject prefabHolder = null;
        public static Geryon GeryonPrefab = null;
        public static FakeFallZone FakeFallZone = null;
        public static GameObject FakeFallZoneHud = null;

        public static GameObject SpawnAgonyAndTundra(Vector3 position, Quaternion rotation, Transform parent)
        {
            var go = EnemyPrefabDatabase.TrySpawnAt(TundraAgonyType, position, rotation, parent, true);

            return go;
        }

        internal static void Initialize()
        {
            LevelQuickLoader.AddQuickLoadLevel("Level 2-4");
            LevelQuickLoader.AddQuickLoadLevel("Level 1-3");
            LevelQuickLoader.AddQuickLoadLevel("Level 5-4");
            LevelQuickLoader.AddQuickLoadLevel("Level 7-3");
            LevelQuickLoader.AddQuickLoadLevel("Level 7-4");
            LevelQuickLoader.AddQuickLoadLevel("Level 8-4");

            NyxLib.Assets.AssetPickingManager.AddAssetPicker<MinosBoss>((minos) =>
            {
                prefabHolder ??= new GameObject();
                GameObject.DontDestroyOnLoad(prefabHolder);
                prefabHolder.SetActive(false);

                CorpseOfKingMinosPrefab = GameObject.Instantiate(minos.gameObject, prefabHolder.transform);
                var pminos = CorpseOfKingMinosPrefab.GetComponent<MinosBoss>();
                pminos.parryChallenge = false;

                return true;
            });

            NyxLib.Assets.AssetPickingManager.AddAssetPicker<CombinedBossBar>((bb) =>
            {
                prefabHolder ??= new GameObject();
                GameObject.DontDestroyOnLoad(prefabHolder);
                prefabHolder.SetActive(false);

                if (bb.gameObject.name != "SecuritySystem")
                {
                    return false;
                }

                if (SceneHelper.CurrentScene != "Level 7-4")
                {
                    return false;
                }

                CentaurSecurityPrefab = GameObject.Instantiate(bb.gameObject, prefabHolder.transform);

                return true;
            });

            NyxLib.Assets.AssetPickingManager.AddAssetPicker<Geryon>((geryon) =>
            {
                prefabHolder ??= new GameObject();
                GameObject.DontDestroyOnLoad(prefabHolder);
                prefabHolder.SetActive(false);

                GeryonPrefab = GameObject.Instantiate(geryon, prefabHolder.transform);

                return true;
            });

            NyxLib.Assets.AssetPickingManager.AddAssetPicker<FakeFallZone>((ffz) =>
            {
                prefabHolder ??= new GameObject();
                GameObject.DontDestroyOnLoad(prefabHolder);
                prefabHolder.SetActive(false);

                FakeFallZone = GameObject.Instantiate(ffz, prefabHolder.transform);

                return true;
            });

            NyxLib.Assets.AssetPickingManager.AddAssetPicker<BloodFiller>((bf) =>
            {
                prefabHolder ??= new GameObject();
                GameObject.DontDestroyOnLoad(prefabHolder);
                prefabHolder.SetActive(false);

                if (!bf.gameObject.name.Contains("ideTree"))
                {
                    return false;
                }

                BloodTreePrefab = GameObject.Instantiate(bf, prefabHolder.transform);
                bf.onFullyFilled = new UltrakillEvent();
                BloodTreePrefab.gameObject.SetActive(false);

                return true;
            });

            NyxLib.Assets.AssetPickingManager.AddAssetPicker<LeviathanController>((leviathan) =>
            {
                prefabHolder ??= new GameObject();
                GameObject.DontDestroyOnLoad(prefabHolder);
                prefabHolder.SetActive(false);

                LeviathanPrefab = MonoBehaviour.Instantiate(leviathan, prefabHolder.transform);
                LeviathanPrefab.phaseChangeHealth = -10.0f;
                LeviathanPrefab.tailAddHealth = LeviathanPrefab.GetComponent<Enemy>().health * 0.5f;

                return true;
            });

            NyxLib.Assets.AssetPickingManager.AddAssetPicker<SwordsMachine>((sm) =>
            {
                var enemy = sm.GetComponent<Enemy>();

                if (enemy.symbiote == null)
                {
                    return false;
                }

                var tundraName = "SwordsMachine Tundra";
                var agonyName = "SwordsMachine Agony";
                var eid = enemy.GetComponent<EnemyIdentifier>();

                if (eid.name == tundraName || eid.name == agonyName)
                {
                    var prefab = new GameObject();
                    prefab.SetActive(false);

                    GameObject tundraGo = null;
                    GameObject agonyGo = null;

                    if (eid.name == tundraName)
                    {
                        tundraGo = GameObject.Instantiate(enemy.gameObject, prefab.transform);
                        agonyGo = GameObject.Instantiate(enemy.symbiote.gameObject, prefab.transform);
                    }
                    else if (eid.name == agonyName)
                    {
                        agonyGo = GameObject.Instantiate(enemy.gameObject, prefab.transform);
                        tundraGo = GameObject.Instantiate(enemy.symbiote.gameObject, prefab.transform);
                    }

                    tundraGo.transform.localPosition = Vector3.zero;
                    agonyGo.transform.localPosition = Vector3.zero;

                    tundraGo.GetComponent<Enemy>().symbiote = agonyGo.GetComponent<Enemy>();
                    agonyGo.GetComponent<Enemy>().symbiote = tundraGo.GetComponent<Enemy>();

                    tundraGo.SetActive(true);
                    agonyGo.SetActive(true);

                    GameObject.DontDestroyOnLoad(prefab);

                    EnemyPrefabDatabase.Instance.RegisterPrefab(TundraAgonyType, prefab);
                    return true;
                }

                return false;
            });
        }
    }
}