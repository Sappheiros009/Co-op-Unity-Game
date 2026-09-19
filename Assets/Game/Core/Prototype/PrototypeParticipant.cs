using UnityEngine;

namespace SlimeCoop.Prototype
{
    // Authoritative state for the LOCAL simulator, not a Steam/network identity.
    public sealed class PrototypeParticipant : MonoBehaviour
    {
        public int ParticipantId { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsAlive { get; private set; }
        public bool HasEnteredExit { get; private set; }
        public bool IsConnected { get; private set; } = true;
        public bool IsCrouching { get; set; }
        public float Health { get; private set; } = 100f;
        public bool CanAct => IsAlive && IsConnected && !HasEnteredExit;
        public PrototypeSpecialty Specialty { get; private set; }
        public PrototypeInventory Inventory { get; private set; }
        private float _protectedUntil;

        public void Configure(int id, string displayName)
        {
            ParticipantId = id; DisplayName = displayName;
            Specialty = id == 0 ? PrototypeSession.SelectedSpecialty : (PrototypeSpecialty)(id % 3);
            Inventory = new PrototypeInventory(PrototypeTuning.Current.inventorySlots);
            IsConnected = PrototypeSession.IsConnected(id);
            ResetParticipant();
        }
        public void SetConnected(bool value) { IsConnected = value; }
        public bool ApplyReplicaStatus(PrototypeNetworkActorState state)
        {
            var player = GetComponent<PrototypeCapsulePlayer>();
            if (player == null || player.Control != PrototypePlayerControl.Replica || state == null || state.id != ParticipantId) return false;
            Health = state.health; IsAlive = state.alive; IsConnected = state.connected;
            HasEnteredExit = state.exited; IsCrouching = state.crouching; return true;
        }
        public void MarkEnteredExit() { HasEnteredExit = true; }
        public void MarkDown() { if (HasEnteredExit) return; IsAlive = false; Health = 0; }
        public void Damage(float amount)
        {
            if (!CanAct || Time.time < _protectedUntil || !float.IsFinite(amount) || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            if (Health == 0) MarkDown();
        }
        public void Heal(float amount) { if (CanAct && float.IsFinite(amount) && amount > 0) Health = Mathf.Min(Health + amount, 100); }
        public bool Revive()
        {
            if (IsAlive || !IsConnected) return false;
            IsAlive = true;
            Health = PrototypeTuning.Current.reviveHealth;
            _protectedUntil = Time.time + PrototypeTuning.Current.reviveProtection;
            return true;
        }
        public void ResetParticipant()
        {
            IsAlive = true; HasEnteredExit = false; Health = 100;
            IsCrouching = false; _protectedUntil = 0;
        }
    }
}
