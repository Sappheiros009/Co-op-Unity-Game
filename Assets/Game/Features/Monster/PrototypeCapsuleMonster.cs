using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>No player attack and no kill score. Environment/lure are the available counters.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypeCapsuleMonster : MonoBehaviour
    {
        private PrototypeGame _game;
        private CharacterController _controller;
        private Vector3 _spawn;
        private float _vertical, _health = 40, _attackAt = -1, _cooldown;
        public string State { get; private set; } = "순찰";
        public void Configure(PrototypeGame game, Vector3 spawn, Color color)
        {
            _game = game; _spawn = spawn;
            _controller = GetComponent<CharacterController>(); _controller.radius = 0.45f;
            _controller.height = 1.9f; _controller.center = Vector3.up * 0.95f;
            PrototypeVisuals.CreateCapsule("MonsterBody", transform, transform.position + Vector3.up, color).transform.localPosition = Vector3.up;
            ResetForRun();
        }
        private void Update()
        {
            if (_game == null || _game.Player == null || _game.IsTransitioning || _health <= 0) return;
            PrototypeParticipant target = null; var range = float.PositiveInfinity;
            foreach (var p in _game.Participants)
            {
                if (!p.CanAct) continue;
                var delta = p.transform.position - transform.position; var d = delta.magnitude;
                var player = p.GetComponent<PrototypeCapsulePlayer>();
                var detectionRange = player != null && player.NecklaceActive ? 25f : 13f;
                if (d >= range || d >= detectionRange) continue;
                if (Physics.Raycast(transform.position + Vector3.up, delta.normalized, out var hit, d, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<PrototypeParticipant>() != p) continue;
                target = p; range = d;
            }
            var lure = PrototypeLure.Active;
            var destination = lure != null ? lure.transform.position : target != null ? target.transform.position : _spawn + new Vector3(Mathf.Sin(Time.time * 0.3f) * 3, 0, 0);
            var direction = _game.Map.Layout.NextWaypoint(transform.position, destination) - transform.position; direction.y = 0;
            State = lure != null ? "유인됨" : target != null ? "추적" : "순찰";
            if (target != null && lure == null && range <= 1.7f)
            {
                if (_attackAt < 0 && Time.time > _cooldown) { _attackAt = Time.time + 0.8f; PrototypeCues.Ping(transform.position); }
                if (_attackAt > 0)
                {
                    State = "공격 예고";
                    if (Time.time >= _attackAt) { target.Damage(PrototypeTuning.Current.monsterDamage); _attackAt = -1; _cooldown = Time.time + 1; }
                }
                direction = Vector3.zero;
            }
            else _attackAt = -1;
            if (_controller.isGrounded) _vertical = -2; else _vertical -= 22 * Time.deltaTime;
            _controller.Move((direction.normalized * 2.3f + Vector3.up * _vertical) * Time.deltaTime);
            if (direction.sqrMagnitude > 0.1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5);
        }
        public void EnvironmentDamage(float amount)
        {
            _health = Mathf.Max(0, _health - amount);
            if (_health > 0) return;
            State = "환경 장치로 저지"; _controller.enabled = false; gameObject.SetActive(false);
        }
        public void ResetForRun()
        {
            _controller.enabled = false; transform.position = _spawn; _controller.enabled = true;
            _health = 40; _attackAt = -1; _cooldown = 0;
        }
    }
}
