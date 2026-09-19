using UnityEngine;

namespace SlimeCoop.Prototype.Tests
{
    // Test-only tick adapter. Executes in FixedUpdate, like the dedicated server world.
    public sealed class PrototypeHazardServerMotorProbe : MonoBehaviour
    {
        public PrototypeCapsulePlayer Player;
        private void FixedUpdate()
        { if(Player!=null) Player.StepServerInput(PrototypePlayerInput.Neutral(),Time.fixedDeltaTime); }
    }
}
