using UnityEngine;
namespace SlimeCoop.Prototype
{
    public sealed class PrototypeMovingRaft : MonoBehaviour
    {
        private PrototypeGame _game;
        private PrototypeStageObjective _objective;
        private Vector3 _destination;
        private bool _requested;
        private readonly System.Collections.Generic.List<PrototypeParticipant> _riders = new System.Collections.Generic.List<PrototypeParticipant>();
        public bool WaterLockReleased { get; private set; }
        public bool Arrived { get; private set; }
        public int RiderCount { get; private set; }
        public void Configure(PrototypeGame game, PrototypeStageObjective objective)
        { _game = game; _objective = objective; _destination = transform.position + Vector3.forward * 6; }
        public bool ReleaseWaterLock(PrototypeParticipant actor)
        {
            if (actor == null || !actor.CanAct || WaterLockReleased) return false;
            var p = actor.transform.position;
            if (Mathf.Abs(p.x) > 3.5f || p.z < 20 || p.z > 28 || p.y > .2f) return false;
            WaterLockReleased = true; _objective.ActivateRegion(0);
            _game.SetStatus(_game.HasNetworkPlayers
                ? "물속 잠금 해제. 전원이 직접 부유물에 올라탄 뒤 조타 장치를 사용하세요."
                : "물속 잠금 해제. T로 동료를 부유물에 배치하고, 전원이 올라탄 뒤 조타 장치를 사용하세요.");
            return true;
        }
        public bool RequestTravel(PrototypeParticipant actor)
        {
            if (actor == null || !actor.CanAct || !WaterLockReleased || Arrived || !OnBoard(actor)) return false;
            _requested = true; return true;
        }
        public bool OnBoard(PrototypeParticipant actor)
        {
            var delta = actor.transform.position - transform.position;
            return actor.CanAct && Mathf.Abs(delta.x) < 1.75f && Mathf.Abs(delta.z) < 1.5f && delta.y >= 0 && delta.y < .9f;
        }
        public Vector3 BoardingPoint(int id) => transform.position + new Vector3(id == 1 ? -.9f : .9f, .2f, id == 3 ? -.6f : .6f);
        private void Update()
        {
            if (_game == null || Arrived) return;
            _riders.Clear();
            foreach (var actor in _game.Participants) if (OnBoard(actor)) _riders.Add(actor);
            RiderCount = _riders.Count;
            if (!_requested || RiderCount != _objective.RequiredRoles) return;
            var next = Vector3.MoveTowards(transform.position, _destination, 1.5f * Time.deltaTime);
            var delta = next - transform.position;
            transform.position = next;
            foreach (var actor in _riders)
            {
                var controller = actor.GetComponent<CharacterController>();
                if (controller != null && controller.enabled) controller.Move(delta);
            }
            if (Vector3.Distance(next, _destination) < .02f)
            { Arrived = true; _objective.ActivateRegion(1); _game.SetStatus("전원 협동 운행 완료. 다음 방의 공동 장치로 이동하세요."); }
        }
    }
}
