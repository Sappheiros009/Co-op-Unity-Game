using System;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    // Values are replaceable experiment data, never approved release balance.
    [Serializable]
    public sealed class PrototypeTuning
    {
        public string rulesVersion = "prototype-experiment-v2";
        public float walkSpeed = 4.5f, sprintSpeed = 7f, jumpHeight = 1.4f, gravity = 22f;
        public float staminaMaximum = 100f, sprintDrain = 16f, climbDrain = 22f, staminaRecovery = 25f;
        public float climbSpeed = 2.7f, exhaustionGrace = 0.7f, reviveSeconds = 3f, healerReviveSeconds = 6f;
        public float reviveHealth = 35f, reviveProtection = 2f, interactionRange = 3.2f;
        public int inventorySlots = 3, speedScoreMaximum = 1000, arrivalBonusPerExtra = 250;
        public float speedScoreDecay = 10f, puzzleHoldSeconds = 2f, monsterDamage = 14f;
        private static PrototypeTuning _current;
        public static PrototypeTuning Current
        {
            get
            {
                if (_current != null) return _current;
                var data = Resources.Load<TextAsset>("PrototypeBalance");
                _current = data == null ? new PrototypeTuning() : JsonUtility.FromJson<PrototypeTuning>(data.text);
                return _current;
            }
        }
    }
}
