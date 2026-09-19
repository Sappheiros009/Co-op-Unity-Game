using UnityEngine;

namespace SlimeCoop.Prototype
{
    public enum PrototypeBotOrder { Follow, Assist, Hold, Crouch, RegionAssist }
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypeTeammate : MonoBehaviour
    {
        private PrototypeGame _game;
        private CharacterController _controller;
        private Vector3 _spawn, _offset, _exit;
        private float _vertical, _rescueTime;
        private PrototypeParticipant _rescuing;
        public PrototypeParticipant Participant { get; private set; }
        public PrototypeBotOrder Order { get; private set; }
        public void Configure(PrototypeGame game, int id, string displayName, Vector3 spawn, Vector3 offset, Vector3 exit, Color color)
        {
            _game = game; _spawn = spawn; _offset = offset; _exit = exit;
            Participant = GetComponent<PrototypeParticipant>() ?? gameObject.AddComponent<PrototypeParticipant>();
            Participant.Configure(id, displayName); Participant.Inventory.Add(PrototypeItemKind.Medkit);
            _controller = GetComponent<CharacterController>(); _controller.radius = 0.4f;
            _controller.height = 1.8f; _controller.center = Vector3.up * 0.9f; _controller.stepOffset = 0.3f;
            var visuals = new GameObject("CharacterParts"); visuals.transform.SetParent(transform, false);
            visuals.AddComponent<PrototypeSlimeBody>().Configure(Participant, color, false);
            ResetForRun();
        }
        public void SetAssist(bool assist) { SetOrder(assist ? PrototypeBotOrder.Assist : PrototypeBotOrder.Follow); }
        public void SetOrder(PrototypeBotOrder order) { Order = order; Participant.IsCrouching = order == PrototypeBotOrder.Crouch; }
        private void Update()
        {
            if (_game == null || _game.Player == null || Participant == null || !Participant.CanAct || _game.IsTransitioning) return;
            Vector3 target;
            var rescue = FindRescue();
            if (rescue != null)
            {
                target = rescue.transform.position;
                if (Vector3.Distance(transform.position, target) < 2.2f)
                {
                    if (_rescuing != rescue) { _rescuing = rescue; _rescueTime = 0; }
                    _rescueTime += Time.deltaTime;
                    var hasKit = Participant.Inventory.Has(PrototypeItemKind.Medkit);
                    if (_rescueTime >= (hasKit ? PrototypeTuning.Current.reviveSeconds : PrototypeTuning.Current.healerReviveSeconds))
                    {
                        if (rescue.Revive() && hasKit) Participant.Inventory.Consume(PrototypeItemKind.Medkit);
                        _game.SetStatus("로컬 시험 동료가 구조했습니다."); _rescueTime = 0;
                    }
                    target = transform.position;
                }
                else { _rescuing = null; _rescueTime = 0; }
            }
            else
            {
                _rescuing = null; _rescueTime = 0;
                if (Order == PrototypeBotOrder.Hold || Order == PrototypeBotOrder.Crouch) target = transform.position;
                else if (_game.Exit.IsGateOpen && (_game.Exit.HasStartedSettlement || Vector3.Distance(_game.Player.transform.position, _exit) < 10))
                    target = _exit + new Vector3((Participant.ParticipantId - 2) * 0.65f, 0, -0.3f);
                else if (Order == PrototypeBotOrder.RegionAssist && _game.Map.Raft != null && !_game.Objective.RegionalReady)
                    target = _game.Map.Raft.BoardingPoint(Participant.ParticipantId);
                else if ((Order == PrototypeBotOrder.Assist || Order == PrototypeBotOrder.RegionAssist) && _game.Objective != null)
                {
                    target = _game.Objective.Pads[Mathf.Min(Participant.ParticipantId, _game.Objective.Pads.Count - 1)].position;
                    if (_game.Objective.BossDanger || _game.Objective.BossWarning) target.x = target.x < 0 ? -8 : 8;
                }
                else target = _game.Player.transform.position + _offset;
            }
            MoveTo(target);
            _game.Exit.RegisterArrival(Participant);
            if (transform.position.y < -8) { Participant.Damage(25); Teleport(_spawn); }
        }
        private PrototypeParticipant FindRescue()
        {
            if (!Participant.Inventory.Has(PrototypeItemKind.Medkit) && Participant.Specialty != PrototypeSpecialty.Healer) return null;
            PrototypeParticipant nearest = null; var distance = 25f;
            foreach (var p in _game.Participants)
            {
                if (p == Participant || p.IsAlive || !p.IsConnected || p.HasEnteredExit) continue;
                var d = Vector3.Distance(transform.position, p.transform.position);
                if (d < distance) { nearest = p; distance = d; }
            }
            return nearest;
        }
        private void MoveTo(Vector3 target)
        {
            target = _game.Map.Layout.NextWaypoint(transform.position, target);
            var direction = target - transform.position; direction.y = 0;
            if (direction.magnitude < 0.35f) direction = Vector3.zero; else direction.Normalize();
            if (direction != Vector3.zero && Physics.SphereCast(transform.position + Vector3.up * 0.9f, 0.32f, direction, out var hit, 0.75f, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponent<PrototypeParticipant>() == null)
            {
                var side = Vector3.Cross(Vector3.up, hit.normal);
                if (Vector3.Dot(side, direction) < 0) side = -side;
                direction = (side + hit.normal * 0.15f).normalized;
            }
            if (_controller.isGrounded) _vertical = -2; else _vertical -= 22 * Time.deltaTime;
            _controller.Move((direction * 4.2f + Vector3.up * _vertical) * Time.deltaTime);
            if (direction != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8);
        }
        public void Teleport(Vector3 p) { _controller.enabled = false; transform.position = p; _controller.enabled = true; _vertical = 0; }
        public void ResetForRun() { Teleport(_spawn); Participant.ResetParticipant(); Order = PrototypeBotOrder.Follow; }
    }
}
