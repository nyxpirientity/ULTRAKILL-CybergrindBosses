using UnityEngine;
using BepInEx;
using Nyxpiri.ULTRAKILL.NyxLib;
using System;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using Sandbox;
using System.Reflection;

namespace Nyxpiri.ULTRAKILL.CybergrindBosses
{
    public class GeryonTweaks : MonoBehaviour
    {
        private Geryon _gery = null;
        private GameObject rotateAroundGo = null;
        private EnemyIdentifier eid = null;

        protected void Awake()
        {
            _gery = GetComponent<Geryon>();
            eid = GetComponent<EnemyIdentifier>();
            var bruteForceColliders = playerBlockerShieldFA.GetValue(_gery).GetComponentsInChildren<Collider>();
            foreach (var col in bruteForceColliders)
            {
                col.enabled = false;
            }

            minimumAroundDistanceFA.SetValue(_gery, 82.0f);
            maximumAroundDistanceFA.SetValue(_gery, 92.0f);
            originalHealthFA.SetValue(_gery, eid.health);
            rotateAroundGo = new GameObject();
            rotateAroundGo.transform.parent = transform;
            rotateAroundFA.SetValue(_gery, rotateAroundGo.transform);
        }

        protected void Start()
        {
            originalHealthFA.SetValue(_gery, eid.health);
        }

        FieldAccess<Geryon, bool> inActionFA = new FieldAccess<Geryon, bool>("inAction");
        FieldAccess<Geryon, float> originalHealthFA = new FieldAccess<Geryon, float>("originalHealth");
        FieldAccess<Geryon, float> playerPushBackerCooldownFA = new FieldAccess<Geryon, float>("playerPushBackerCooldown");
        FieldAccess<Geryon, float> cooldownFA = new FieldAccess<Geryon, float>("cooldown");
        FieldAccess<Geryon, float> minimumAroundDistanceFA = new FieldAccess<Geryon, float>("minimumAroundDistance");
        FieldAccess<Geryon, float> maximumAroundDistanceFA = new FieldAccess<Geryon, float>("maximumAroundDistance");
        FieldAccess<Geryon, GameObject> playerBlockerShieldFA = new FieldAccess<Geryon, GameObject>("playerBlockerShield");
        FieldAccess<Geryon, Transform> rotateAroundFA = new FieldAccess<Geryon, Transform>("rotateAround");

        MethodInfo WaveClap = typeof(Geryon).GetMethod("WaveClap", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo BowForward = typeof(Geryon).GetMethod("BowForward", BindingFlags.Instance | BindingFlags.NonPublic);
        FixedTimeStamp _distancePreventionWaveClapTimestamp = new FixedTimeStamp();

        protected void FixedUpdate()
        {
            playerPushBackerCooldownFA.SetValue(_gery, 99f);

            var rotateAroundTarget = NewMovement.Instance.transform.position;
            rotateAroundTarget.Scale(new Vector3(1.0f, 0.0f, 1.0f));
            rotateAroundGo.transform.position = NyxMath.EaseInterpTo(rotateAroundGo.transform.position, rotateAroundTarget, 2.0f, Time.fixedDeltaTime);
        }

        private bool PrePickAttack()
        {
            if (!inActionFA.GetValue(_gery) && cooldownFA.GetValue(_gery) <= 0f)
            {
                var inScawyDistance = Vector3.Distance(playerBlockerShieldFA.GetValue(_gery).transform.position, eid.target.position) < 22f;

                if (inScawyDistance && _distancePreventionWaveClapTimestamp.TimeSince < 4.0f)
                {
                    WaveClap.Invoke(_gery, null);
                    _distancePreventionWaveClapTimestamp.UpdateToNow();
                    return false;
                }
                else if (inScawyDistance)
                {
                    BowForward.Invoke(_gery, null);
                    _distancePreventionWaveClapTimestamp.UpdateToNow();
                    return false;
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(Geryon), "PickAttack")]
        public static class GeryonPickAttackPatch
        {
            public static bool Prefix(Geryon __instance)
            {
                if (!NyxLib.Cheats.Enabled)
                {
                    return true;
                }

                var tweaks = __instance.GetComponent<GeryonTweaks>();

                if (tweaks != null)
                {
                    return tweaks.PrePickAttack();
                }

                return true;
            }

            public static void Postfix(Geryon __instance)
            {

            }
        }
    }
}