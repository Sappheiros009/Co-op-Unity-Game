using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>
    /// Minimal local participant state used to exercise the stage-start roster and exit rules.
    /// It is not a network identity; server authority is a later implementation step.
    /// </summary>
    public sealed class PrototypeParticipant : MonoBehaviour
    {
        public int ParticipantId { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsAlive { get; private set; }
        public bool HasEnteredExit { get; private set; }

        public void Configure(int participantId, string displayName)
        {
            ParticipantId = participantId;
            DisplayName = displayName;
            ResetParticipant();
        }

        public void MarkEnteredExit()
        {
            HasEnteredExit = true;
        }

        public void MarkDown()
        {
            IsAlive = false;
        }

        public void ResetParticipant()
        {
            IsAlive = true;
            HasEnteredExit = false;
        }
    }
}
