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
    [HarmonyPatch(typeof(FleshPrison), "SpawnInsignia")]
    public static class FleshPrisonSpawnInsigniaPatch
    {
        private static void SetLocalScaleReplacement(Transform transform, Vector3 value)
        {
            Action doItNormally = () =>
            {
                transform.localScale = value;
            };

            if (!Cheats.Enabled)
            {
                doItNormally();
                return;
            }

            if (value.y != 2f)
            {
                doItNormally();
                return;
            }

            if (!_isGrindBoss)
            {
                doItNormally();
                return;
            }

            transform.localScale = value * Options.FleshPrisonInsigniaSizeScalar.Value;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instr in instructions)
            {
                if (instr.Calls(typeof(Transform).GetProperty("localScale").SetMethod))
                {
                    instr.operand = typeof(FleshPrisonSpawnInsigniaPatch).GetMethod(nameof(SetLocalScaleReplacement), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                }

                yield return instr;
            }
        }

        private static bool _isGrindBoss = false;

        public static bool Prefix(FleshPrison __instance)
        {
            if (!Cheats.Enabled)
            {
                _isGrindBoss = false;
                return true;
            }

            _isGrindBoss = (__instance.GetComponent<GrindBoss>() != null);
            return true;
        }

        public static void Postfix(FleshPrison __instance)
        {
            _isGrindBoss = false;
        }
    }
}