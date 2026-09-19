using System;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    [Serializable] public sealed class PrototypeNetworkWaitingInput
    {
        public long epoch, sequence;
        public PrototypePlayerInput input;
    }
    [Serializable] public sealed class PrototypeNetworkWaitingPose
    {
        public int slot;
        public long acknowledged;
        public Vector3 position;
        public float yaw, pitch, cameraHeight;
        public bool crouching;
        public bool IsValid => slot >= 0 && slot < 4 && acknowledged >= 0 && float.IsFinite(position.x) && float.IsFinite(position.y)
            && float.IsFinite(position.z) && Mathf.Abs(position.x) <= 14 && position.y >= -3 && position.y <= 5 && Mathf.Abs(position.z) <= 11
            && float.IsFinite(yaw) && yaw >= 0 && yaw <= 360 && float.IsFinite(pitch) && Mathf.Abs(pitch) <= 82
            && float.IsFinite(cameraHeight) && cameraHeight >= .8f && cameraHeight <= 1.6f;
    }
    [Serializable] public sealed class PrototypeNetworkWaitingState
    {
        public long epoch, tick;
        public PrototypeNetworkWaitingPose[] actors;
        public bool IsValid()
        {
            if (epoch < 1 || tick < 0 || actors == null || actors.Length > 4) return false;
            var seen = 0;
            foreach (var actor in actors)
            {
                if (actor == null || !actor.IsValid || (seen & (1 << actor.slot)) != 0) return false;
                seen |= 1 << actor.slot;
            }
            return true;
        }
    }
}
