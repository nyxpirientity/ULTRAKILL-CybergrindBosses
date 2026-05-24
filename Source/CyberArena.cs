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

        private bool _initialSpawn;

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

                GenerationFinished = true;
                OnGenerationFinished?.Invoke(this);
            }
        }
    }
}