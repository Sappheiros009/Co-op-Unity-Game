using UnityEngine;

namespace SlimeCoop.Prototype
{
    public enum PrototypeInteractionKind { Pickup, Region, KeySocket, Record, WaterValve, RaftWheel }
    public sealed class PrototypeInteractable : MonoBehaviour
    {
        public PrototypeInteractionKind Kind;
        public PrototypeItemKind Item;
        public string Label;
        public int DeviceId;
        public PrototypeStageObjective Objective;
        public PrototypeMovingRaft Raft;
        private bool _used;
        private Vector3 _origin;
        public bool IsUsed => _used;
        private bool _hasRecoveryOrigin;
        public Vector3 RecoveryOrigin => _origin;
        public void SetRecoveryOrigin(Vector3 point) { _origin = point; _hasRecoveryOrigin = true; }
        private void Start() { if (!_hasRecoveryOrigin) _origin = transform.position; }
        public void RestorePickup()
        {
            if (Kind != PrototypeInteractionKind.Pickup) return;
            _used = false; transform.position = _origin; gameObject.SetActive(true);
            var body = GetComponent<Rigidbody>(); if (body != null) body.linearVelocity = Vector3.zero;
        }
        public bool TryUse(PrototypeParticipant actor)
        {
            if (_used || actor == null || !actor.CanAct) return false;
            if (!PrototypeSession.ValidateRequest(System.Guid.NewGuid().ToString("N"), actor.ParticipantId, actor,
                transform.position, PrototypeTuning.Current.interactionRange + 1, out _)) return false;
            if (Kind == PrototypeInteractionKind.Pickup)
            {
                if (!actor.Inventory.Add(Item)) return false;
                _used = true; gameObject.SetActive(false); return true;
            }
            if (Kind == PrototypeInteractionKind.KeySocket) return Objective.InsertKey(actor);
            if (Kind == PrototypeInteractionKind.WaterValve)
            { var released = Raft != null && Raft.ReleaseWaterLock(actor); if (released) _used = true; return released; }
            if (Kind == PrototypeInteractionKind.RaftWheel) return Raft != null && Raft.RequestTravel(actor);
            if (!Objective.ActivateRegion(DeviceId)) return false;
            _used = true;
            if (Kind == PrototypeInteractionKind.Record)
                PrototypeSession.Journal.Add("광장 기록: 고향의 시계는 멈췄지만, 귀향패가 가리키는 길은 아직 남아 있다. (임시 문장)");
            Label += " · 완료";
            return true;
        }
        private void Update()
        {
            if (transform.position.y < -10) RestorePickup();
        }
    }
}
