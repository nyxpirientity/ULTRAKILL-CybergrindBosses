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
    [BepInDependency("nyxpiri.ultrakill.nyxlib", BepInDependency.DependencyFlags.HardDependency)]
    public class CybergrindBosses : BaseUnityPlugin
    {
        protected void Awake()
        {
            Log.Initialize(Logger);
            Options.Initialize(this);
            Assets.Initialize();
            EnemyVariants.Initialize();

            NyxLib.Cheats.ReadyForCheatRegistration += RegisterCheats;
            NyxLib.ScenesEvents.OnSceneWasLoaded += (UnityEngine.SceneManagement.Scene scene, string levelName, string unitySceneName) =>
            {
                if (EndlessGrid.Instance != null)
                {
                    EndlessGrid.Instance.gameObject.GetOrAddComponent<CyberArena>();
                    EndlessGrid.Instance.gameObject.GetOrAddComponent<BossSpawner>();
                }
            };

            Harmony.CreateAndPatchAll(GetType().Assembly);
        }

        public const string CheatID = "nyxpiri.cybergrind-bosses";

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
            __instance.tempEnemyAmount += BossPicker.EnemyAmountToAdd;
            BossPicker.EnemyAmountToAdd = 0;
        }
    }
}
