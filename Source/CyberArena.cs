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
    public class CyberArena : MonoSingleton<CyberArena>
    {
        public static Vector3 HorizontalCenter => new Vector3(0.0f, 0.0f, 62.5f);
        public Vector3 FloorCenter { get; private set; } = HorizontalCenter;
        public bool GenerationFinished { get; private set; } = false;

        public delegate void GenerationFinishedEventHandler(CyberArena arena);
        public event GenerationFinishedEventHandler OnGenerationFinished;

        public EndlessGrid Grid { get; private set; } = null;

        public static FieldAccess<EndlessGrid, int> incompletePrefabsFA = new FieldAccess<EndlessGrid, int>("incompletePrefabs");
        public static FieldAccess<EndlessGrid, int> incompleteBlocksFA = new FieldAccess<EndlessGrid, int>("incompleteBlocks");

        public static IReadOnlyList<Vector3> ThreeByThrees => CyberArena.Instance._threeByThrees;
        public static IReadOnlyList<Vector3> TwoByTwos => CyberArena.Instance._twoByTwos;
        public static IReadOnlyList<Vector3> OneByOnes => CyberArena.Instance._oneByOnes;

        private List<Vector3> _threeByThrees = new List<Vector3>();
        private List<Vector3> _twoByTwos = new List<Vector3>();
        private List<Vector3> _oneByOnes = new List<Vector3>();

        private bool _initialSpawn;

        public static Vector3? RandomOneByOne
        {
            get
            {
                return ((OneByOnes.Count > 0) ? new Vector3?(OneByOnes[UnityEngine.Random.Range(0, OneByOnes.Count)]) : null);
            }
        }

        public static Vector3? RandomTwoByTwo
        {
            get
            {
                return ((TwoByTwos.Count > 0) ? new Vector3?(TwoByTwos[UnityEngine.Random.Range(0, TwoByTwos.Count)]) : null);
            }
        }

        public static Vector3? RandomThreeByThree
        {
            get
            {
                return ((ThreeByThrees.Count > 0) ? new Vector3?(ThreeByThrees[UnityEngine.Random.Range(0, ThreeByThrees.Count)]) : null);
            }
        }

        protected void Awake()
        {
            Grid = GetComponent<EndlessGrid>();
        }

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
            _initialSpawn = true;
            GenerationFinished = false;
        }

        protected void FixedUpdate()
        {
            if (NyxLib.Cheats.IsCheatDisabled(CybergrindBosses.CheatID))
            {
                return;
            }

            if (!Cybergrind.IsActive)
            {
                return;
            }

            var incompletePrefabs = incompletePrefabsFA.GetValue(EndlessGrid.Instance);
            var incompleteBlocks = incompleteBlocksFA.GetValue(EndlessGrid.Instance);

            if (incompleteBlocks > 0 || incompletePrefabs > 0)
            {
                return;
            }

            if (_initialSpawn)
            {
                _initialSpawn = false;

                Physics.SphereCast(HorizontalCenter + Vector3.up * 200.0f, 2.0f, Vector3.down, out var hit, float.PositiveInfinity, 16777216);
                FloorCenter = hit.collider != null ? (hit.point) : HorizontalCenter;

                UpdateSpawnPositions();

                GenerationFinished = true;
                OnGenerationFinished?.Invoke(this);
            }
        }

        private void UpdateSpawnPositions()
        {
            _oneByOnes.Clear();
            _twoByTwos.Clear();
            _threeByThrees.Clear();

            for (int i = 0; i < Grid.cubes.Length; i++)
            {
                for (int j = 0; j < Grid.cubes[i].Length; j++)
                {
                    if (Grid.cubes[i][j].blockedByPrefab)
                    {
                        continue;
                    }

                    _oneByOnes.Add(GetCubeTopCenter(Grid.cubes[i][j]));

                    Vector3? twoByTwo = TestForSpawnPosition(sizeI: 2, sizeJ: 2, iStart: i, jStart: j);

                    if (!twoByTwo.HasValue)
                    {
                        continue;
                    }

                    _twoByTwos.Add(twoByTwo.Value);

                    Vector3? threeByThree = TestForSpawnPosition(sizeI: 3, sizeJ: 3, iStart: i, jStart: j);

                    if (!threeByThree.HasValue)
                    {
                        continue;
                    }

                    _threeByThrees.Add(threeByThree.Value);
                }
            }
        }

        private Vector3? TestForSpawnPosition(int sizeI, int sizeJ, int iStart, int jStart)
        {
            List<Vector3> positions = new List<Vector3>(sizeI * sizeJ);
            for (int i = iStart; i < iStart + sizeI; i++)
            {
                for (int j = jStart; j < jStart + sizeJ; j++)
                {
                    if (Grid.cubes.Length <= i)
                    {
                        return null;
                    }

                    if (Grid.cubes[i].Length <= j)
                    {
                        return null;
                    }

                    if (Grid.cubes[i][j].blockedByPrefab)
                    {
                        return null;
                    }

                    positions.Add(GetCubeTopCenter(Grid.cubes[i][j]));
                }
            }

            Vector3 center = Vector3.zero;
            float expectedY = positions[0].y;

            foreach (var position in positions)
            {
                center += position;

                if (Mathf.Abs(position.y - expectedY) > 0.5f)
                {
                    return null;
                }
            }

            center /= positions.Count;

            // technically average not center but I think in the case of a uniform grid like this 
            // average always == center... right? didn't feel like doing bounding box min/max stuff
            return center;
        }

        private Vector3 GetCubeTopCenter(EndlessCube endlessCube)
        {
            Vector3 topCenter = endlessCube.transform.position + new Vector3(0.0f, 25.0f, 0.0f); ;
            return topCenter;
        }
    }
}