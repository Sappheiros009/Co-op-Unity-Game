using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    [Serializable] public sealed class PrototypeNetworkActorState
    {
        public int id, slot, selected;
        public string name, prompt;
        public Vector3 position, guidance;
        public float yaw, pitch, cameraHeight, health, stamina, rescue;
        public bool connected, alive, exited, crouching, necklace, climbing;
        public PrototypeItemKind[] items;
    }
    [Serializable] public struct PrototypeNetworkPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool active;
        public static PrototypeNetworkPose Read(Transform target) => new PrototypeNetworkPose
        { position = target.position, rotation = target.rotation, active = target.gameObject.activeSelf };
        public bool IsValid => Finite(position) && Finite(new Vector3(rotation.x, rotation.y, rotation.z))
            && float.IsFinite(rotation.w) && Mathf.Abs(Quaternion.Dot(rotation, rotation) - 1) < .02f;
        private static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
    }
    [Serializable] public sealed class PrototypeNetworkPickupState
    {
        public PrototypeItemKind kind;
        public PrototypeNetworkPose pose;
    }
    [Serializable] public sealed class PrototypeNetworkWorldState
    {
        public string runId, phase, objective, status;
        public int chapter, stage, seed, yourActor = -1, score, arrivals, roles, activeRoles;
        public long sequence, simulationTick;
        public float elapsed, exitRemaining;
        public bool settled, bossWarning, bossDanger, lureActive;
        public Vector3 lurePosition;
        public PrototypeNetworkActorState[] actors = Array.Empty<PrototypeNetworkActorState>();
        public PrototypeNetworkPose[] props = Array.Empty<PrototypeNetworkPose>();
        public PrototypeNetworkPickupState[] pickups = Array.Empty<PrototypeNetworkPickupState>();
        public string[] journal = Array.Empty<string>();
        public bool chapterCompleted;
        public PrototypeChapterCompletion completion;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(runId) || runId.Length > 64 || sequence < 1 || simulationTick < 0
                || chapter < 1 || chapter > 7 || stage < 1 || stage > 6 || actors == null || actors.Length < 2 || actors.Length > 4
                || yourActor < -1 || yourActor >= actors.Length || props == null || props.Length > 12
                || pickups == null || pickups.Length > 32 || journal == null || journal.Length > 32
                || (phase != "Playing" && phase != "Story" && phase != "Lobby" && phase != "WaitingRoom")
                || !Finite(elapsed) || elapsed < 0 || !Finite(exitRemaining) || exitRemaining < 0
                || score < 0 || arrivals < 0 || arrivals > actors.Length || roles != actors.Length
                || activeRoles < 0 || activeRoles > roles || !Finite(lurePosition)
                || (objective?.Length ?? 0) > 1024 || (status?.Length ?? 0) > 1024) return false;
            // Explicit discriminator avoids JsonUtility's null class -> empty object round-trip ambiguity.
            if (chapterCompleted && (completion == null || !completion.IsValid() || stage != 6 || !settled || arrivals < 1
                || (phase != "Story" && phase != "WaitingRoom") || completion.runId != runId || completion.chapter != chapter
                || completion.participants != actors.Length || completion.score != score || completion.seconds != elapsed)) return false;
            var slots = new HashSet<int>();
            for (var i = 0; i < actors.Length; i++)
            {
                var a = actors[i];
                if (a == null || a.id != i || a.slot < 0 || a.slot > 3 || !slots.Add(a.slot)
                    || string.IsNullOrWhiteSpace(a.name) || a.name.Length > 24 || (a.prompt?.Length ?? 0) > 512
                    || !Finite(a.position) || !Finite(a.guidance) || !Finite(a.yaw) || !Finite(a.pitch)
                    || a.yaw < 0 || a.yaw > 360 || a.pitch < -82 || a.pitch > 82
                    || !Finite(a.cameraHeight) || a.cameraHeight < .3f || a.cameraHeight > 2
                    || !Finite(a.health) || a.health < 0 || a.health > 100 || !Finite(a.stamina) || a.stamina < 0 || a.stamina > 100
                    || !Finite(a.rescue) || a.rescue < 0 || a.rescue > 1 || a.items == null || a.items.Length > 3
                    || a.selected < 0 || a.selected > 2) return false;
                foreach (var item in a.items) if (!Enum.IsDefined(typeof(PrototypeItemKind), item)) return false;
            }
            foreach (var pose in props) if (!pose.IsValid) return false;
            foreach (var pickup in pickups)
                if (pickup == null || !Enum.IsDefined(typeof(PrototypeItemKind), pickup.kind) || !pickup.pose.IsValid) return false;
            foreach (var entry in journal) if (entry == null || entry.Length > 1024) return false;
            return true;
        }
        private static bool Finite(float value) => float.IsFinite(value);
        private static bool Finite(Vector3 p) => Finite(p.x) && Finite(p.y) && Finite(p.z);

        // Only stateful prop roots are sent. Static seeded geometry and body-part animation are not streamed.
        public static Transform[] DynamicProps(PrototypeGame game)
        {
            var props = new List<Transform>();
            foreach (var p in game.GetComponentsInChildren<PrototypeCarryable>(true)) props.Add(p.transform);
            foreach (var p in game.GetComponentsInChildren<PrototypeCyclePlatform>(true)) props.Add(p.transform);
            foreach (var p in game.GetComponentsInChildren<PrototypeMovingRaft>(true)) props.Add(p.transform);
            foreach (var p in game.GetComponentsInChildren<PrototypeCapsuleMonster>(true)) props.Add(p.transform);
            return props.ToArray();
        }
    }
}
