using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Intent only. Position, health, inventory, score and simulation time never come from a client.</summary>
    [System.Serializable]
    public struct PrototypePlayerInput
    {
        public bool hasControl;
        public PrototypeMotorInput motor;
        public PrototypeInteractionInput interaction;
        public float yaw, pitch;
        public bool necklacePressed, pingPressed;

        public bool IsValid => float.IsFinite(motor.move.x) && float.IsFinite(motor.move.y)
            && Mathf.Abs(motor.move.x) <= 1 && Mathf.Abs(motor.move.y) <= 1
            && float.IsFinite(yaw) && yaw >= 0 && yaw <= 360
            && float.IsFinite(pitch) && pitch >= -82 && pitch <= 82
            && interaction.selectedSlot >= -1 && interaction.selectedSlot <= 2;

        public void ClearEdges()
        {
            motor.jump = false;
            interaction.interactPressed = interaction.dropPressed = interaction.usePressed = false;
            interaction.selectedSlot = -1;
            necklacePressed = pingPressed = false;
        }

        public static PrototypePlayerInput Neutral(float yaw = 0, float pitch = 0) => new PrototypePlayerInput
        { yaw = yaw, pitch = pitch, interaction = new PrototypeInteractionInput { selectedSlot = -1 } };
    }
}
