using System;
using HarmonyLib;
using Nyxpiri.ULTRAKILL.NyxLib;
using UnityEngine;

namespace Nyxpiri.ULTRAKILL.CybergrindBosses
{
    public class BlackholeModifier : MonoBehaviour
    {
        public BlackHoleProjectile BlackHole { get; private set; } = null;

        public int KillThreshold = 10;
        public int Damage = 99;
        public float RescaleOnStart = 1.0f;
        public float SpeedScalar = 1.0f;

        protected void Start()
        {
            BlackHole = GetComponent<BlackHoleProjectile>();
            transform.localScale *= RescaleOnStart;
            BlackHole.speed *= SpeedScalar;
        }

        [HarmonyPatch(typeof(BlackHoleProjectile), "OnTriggerEnter")]
        public static class BlackHoleProjectileOnTriggerEnterPatch
        {
            public static bool Prefix(BlackHoleProjectile __instance, Collider other)
            {
                if (!NyxLib.Cheats.Enabled)
                {
                    return true;
                }

                var modifier = __instance.GetComponent<BlackholeModifier>();

                if (modifier == null)
                {
                    return true;
                }

                if (!__instance.enemy || __instance.target == null)
                {
                    return true;
                }

                if (!__instance.target.IsTargetTransform(other.gameObject.transform))
                {
                    return true;
                }

                __instance.Explode();

                NewMovement player = NewMovement.Instance;

                if (player.hp > modifier.KillThreshold)
                {
                    var damage = Mathf.Min(modifier.Damage, player.hp - 1);
                    player.GetHurt(damage, true);
                    player.ForceAntiHP(100 - player.hp);
                }
                else
                {
                    player.GetHurt(modifier.KillThreshold, true);
                }

                return false;
            }

            public static void Postfix(BlackHoleProjectile __instance, Collider other)
            {

            }
        }
    }
}