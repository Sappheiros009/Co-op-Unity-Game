using UnityEngine;

namespace SlimeCoop.Prototype
{
    // Fixed rooms, randomized links. Pure data makes seed and reachability regression-testable.
    public sealed class PrototypeRoomLayout
    {
        public readonly float[] CorridorX = new float[2];
        public readonly Vector3[] ItemPositions = new Vector3[3];
        private readonly Vector3[] _monsterSpawnCandidates;
        public readonly int Seed;
        public System.Collections.Generic.IReadOnlyList<Vector3> MonsterSpawnCandidates => _monsterSpawnCandidates;

        public PrototypeRoomLayout(int seed)
        {
            Seed = seed;
            var random = new System.Random(seed);
            for (var i = 0; i < 2; i++) CorridorX[i] = random.Next(2) == 0 ? -6 : 6;
            var candidates = new[] { new Vector3(-3,.6f,-2), new Vector3(3,.6f,-2), new Vector3(4,.6f,4),
                new Vector3(-4,.6f,4), new Vector3(-6,.6f,-4), new Vector3(0,.6f,4) };
            for (var i = 0; i < candidates.Length; i++)
            { var pick = random.Next(i, candidates.Length); (candidates[i], candidates[pick]) = (candidates[pick], candidates[i]); }
            System.Array.Copy(candidates, ItemPositions, ItemPositions.Length);

            // The candidate order is part of the generated result. MapGeneration selects the
            // first candidate that is clear for the current chapter's world geometry.
            _monsterSpawnCandidates = new[]
            {
                new Vector3(CorridorX[0], .15f, 20),
                new Vector3(0, .15f, 28),
                new Vector3(CorridorX[1], .15f, 44)
            };
        }

        public bool Validate()
        {
            if (CorridorX == null || CorridorX.Length != 2 ||
                !float.IsFinite(CorridorX[0]) || !float.IsFinite(CorridorX[1]) ||
                Mathf.Abs(CorridorX[0]) > 6 || Mathf.Abs(CorridorX[1]) > 6) return false;

            for (var i = 0; i < ItemPositions.Length; i++)
            {
                if (!IsFinite(ItemPositions[i]) || Mathf.Abs(ItemPositions[i].x) > 8 || Mathf.Abs(ItemPositions[i].z) > 6) return false;
                for (var j = i + 1; j < ItemPositions.Length; j++) if (Vector3.Distance(ItemPositions[i], ItemPositions[j]) < 1) return false;
                if (!CanReach(new Vector3(0, .15f, -5), ItemPositions[i])) return false;
            }

            for (var i = 0; i < _monsterSpawnCandidates.Length; i++)
            {
                var candidate = _monsterSpawnCandidates[i];
                if (!IsFinite(candidate) || !IsInsidePlayableBounds(candidate, .45f)) return false;
                for (var j = i + 1; j < _monsterSpawnCandidates.Length; j++)
                    if (Vector3.Distance(candidate, _monsterSpawnCandidates[j]) < 1) return false;
                if (!CanReach(candidate, new Vector3(0, .15f, 53))) return false;
            }

            if (!CanReach(new Vector3(0, .15f, -5), new Vector3(0, .15f, 53))) return false;
            return true;
        }

        public bool IsInsidePlayableBounds(Vector3 position, float radius)
        {
            if (!IsFinite(position) || !float.IsFinite(radius) || radius < 0) return false;
            return position.x >= -10 + radius && position.x <= 10 - radius
                && position.z >= -8 + radius && position.z <= 56 - radius;
        }

        public bool CanReach(Vector3 from, Vector3 target)
        {
            if (!IsInsidePlayableBounds(from, .1f) || !IsInsidePlayableBounds(target, .1f)) return false;

            var current = from;
            for (var step = 0; step < 8; step++)
            {
                if (HorizontalDistance(current, target) <= 1.1f) return true;
                var waypoint = NextWaypoint(current, target);
                if (HorizontalDistance(current, waypoint) < .01f) return false;
                current = new Vector3(waypoint.x, from.y, waypoint.z);
            }
            return HorizontalDistance(current, target) <= 1.1f;
        }

        public Vector3 NextWaypoint(Vector3 from, Vector3 target)
        {
            // Cross each room boundary on its actual corridor, never straight through a wall.
            for (var i = 0; i < 2; i++)
            {
                var start = i * 24f + 7f; var end = i * 24f + 17f;
                if (from.z < end && target.z > end)
                {
                    if (from.z < start - 0.7f) return new Vector3(CorridorX[i], 0, start);
                    return new Vector3(CorridorX[i], 0, end + 0.8f);
                }
            }
            for (var i = 1; i >= 0; i--)
            {
                var start = i * 24f + 7f; var end = i * 24f + 17f;
                if (from.z > start && target.z < start)
                {
                    if (from.z > end + 0.7f) return new Vector3(CorridorX[i], 0, end);
                    return new Vector3(CorridorX[i], 0, start - 0.8f);
                }
            }
            return target;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            var delta = a - b; delta.y = 0;
            return delta.magnitude;
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }
    }
}

