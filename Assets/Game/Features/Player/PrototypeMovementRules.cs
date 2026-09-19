using UnityEngine;
namespace SlimeCoop.Prototype
{
    // Shared motor input, used by the keyboard adapter and deterministic physics tests.
    [System.Serializable]
    public struct PrototypeMotorInput
    {
        public Vector2 move;
        public bool sprint, crouch, jump, climb;
    }
    public static class PrototypeMovementRules
    {
        public static bool CanSprint(Vector2 input, bool requested, bool crouched, float stamina) => requested && input.y > 0 && !crouched && stamina > 1;
        public static Vector3 ClimbRight(Vector3 surfaceNormal) => Vector3.Cross(surfaceNormal, Vector3.up).normalized;
    }
}
