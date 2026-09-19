using UnityEngine;

namespace SlimeCoop.Prototype
{
    // Fixed rooms, randomized links. Pure data makes seed and reachability regression-testable.
    public sealed class PrototypeRoomLayout
    {
        public readonly float[] CorridorX = new float[2];
        public readonly Vector3[] ItemPositions = new Vector3[3];
        public readonly int Seed;
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
        }
        public bool Validate()
        {
            if (Mathf.Abs(CorridorX[0]) > 6 || Mathf.Abs(CorridorX[1]) > 6) return false;
            for (var i = 0; i < ItemPositions.Length; i++)
            {
                if (Mathf.Abs(ItemPositions[i].x) > 8 || Mathf.Abs(ItemPositions[i].z) > 6) return false;
                for (var j = i + 1; j < ItemPositions.Length; j++) if (Vector3.Distance(ItemPositions[i], ItemPositions[j]) < 1) return false;
            }
            return true;
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
    }
}
