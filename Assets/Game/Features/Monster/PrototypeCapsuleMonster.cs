using UnityEngine;

namespace SlimeCoop.Prototype
{
    public enum PrototypeMonsterState { Patrol, Chase, Lured, AttackWarning, AttackCooldown, Disabled }

    /// <summary>No player attack and no kill score. Environment/lure are the available counters.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypeCapsuleMonster : MonoBehaviour
    {
        [System.Serializable]
        private sealed class MonsterParameters
        {
            public float maxHealth = 40f;
            public float normalDetectionRange = 13f;
            public float necklaceDetectionRange = 25f;
            public float attackRange = 1.7f;
            public float warningSeconds = .8f;
            public float attackCooldownSeconds = 1f;
            public float movementSpeed = 2.3f;
            public float gravity = 22f;
            public float patrolRadius = 3f;
        }

        [SerializeField] private MonsterParameters _parameters = new MonsterParameters();
        private PrototypeGame _game;
        private CharacterController _controller;
        private PrototypeParticipant _target;
        private Vector3 _spawn;
        private float _vertical, _health, _attackAt = -1, _cooldown;
        private PrototypeMonsterState _state;
        public string State { get; private set; } = "순찰";
        public long AttackWarningSequence { get; private set; }
        public bool AttackWarning => isActiveAndEnabled && _health > 0 && _attackAt > Time.time;
        public PrototypeMonsterState CurrentState => _state;

        public void Configure(PrototypeGame game, Vector3 spawn, Color color)
        {
            _game = game; _spawn = spawn;
            EnsureParameters();
            _controller = GetComponent<CharacterController>();
            _controller.radius = .45f;
            _controller.height = 1.9f;
            _controller.center = Vector3.up * .95f;
            if (transform.Find("MonsterBody") == null)
                PrototypeVisuals.CreateCapsule("MonsterBody", transform, transform.position + Vector3.up, color).transform.localPosition = Vector3.up;
            ResetForRun();
        }

        private void Update()
        {
            if (_game == null || _game.Player == null || _game.IsTransitioning || _health <= 0) return;
            if (_game.Map == null || _game.Map.Layout == null)
            {
                SetState(PrototypeMonsterState.Patrol);
                return;
            }

            var target = FindTarget(out var range);
            var lure = PrototypeLure.Active;
            var destination = _spawn + new Vector3(Mathf.Sin(Time.time * .3f) * SafePatrolRadius, 0, 0);
            var direction = Vector3.zero;

            if (lure != null)
            {
                _target = null; _attackAt = -1;
                SetState(PrototypeMonsterState.Lured);
                destination = lure.transform.position;
            }
            else if (target != null)
            {
                _target = target;
                destination = target.transform.position;
                if (range <= SafeAttackRange)
                {
                    direction = Vector3.zero;
                    if (_attackAt > 0)
                    {
                        if (Time.time < _attackAt) SetState(PrototypeMonsterState.AttackWarning);
                        else ResolveAttack(target);
                    }
                    else if (Time.time <= _cooldown)
                    {
                        SetState(PrototypeMonsterState.AttackCooldown);
                    }
                    else
                    {
                        StartAttackWarning();
                    }

                }
                else
                {
                    _attackAt = -1;
                    SetState(PrototypeMonsterState.Chase);
                }
            }
            else
            {
                _target = null; _attackAt = -1;
                SetState(PrototypeMonsterState.Patrol);
            }

            if (_state == PrototypeMonsterState.Chase || _state == PrototypeMonsterState.Lured || _state == PrototypeMonsterState.Patrol)
            {
                var waypoint = _game.Map.Layout.NextWaypoint(transform.position, destination);
                direction = waypoint - transform.position;
                direction.y = 0;
            }

            if (_controller == null || !_controller.enabled) return;
            if (_controller.isGrounded) _vertical = -2; else _vertical -= SafeGravity * Time.deltaTime;
            _controller.Move((direction.normalized * SafeMovementSpeed + Vector3.up * _vertical) * Time.deltaTime);
            if (direction.sqrMagnitude > .1f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5);
        }

        public void EnvironmentDamage(float amount)
        {
            if (_health <= 0 || !float.IsFinite(amount) || amount <= 0) return;
            _health = Mathf.Max(0, _health - amount);
            if (_health > 0) return;

            _target = null; _attackAt = -1;
            SetState(PrototypeMonsterState.Disabled);
            if (_controller != null) _controller.enabled = false;
            gameObject.SetActive(false);
        }

        public void ResetForRun()
        {
            EnsureParameters();
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (_controller == null) _controller = GetComponent<CharacterController>();
            _controller.enabled = false;
            transform.position = _spawn;
            _controller.enabled = true;
            _vertical = 0;
            _target = null;
            _health = SafeMaxHealth;
            _attackAt = -1;
            _cooldown = 0;
            SetState(PrototypeMonsterState.Patrol);
        }

        private PrototypeParticipant FindTarget(out float bestRange)
        {
            PrototypeParticipant best = null;
            bestRange = float.PositiveInfinity;
            var bestId = int.MaxValue;
            foreach (var participant in _game.Participants)
            {
                if (participant == null || !participant.CanAct) continue;
                var delta = participant.transform.position - transform.position;
                var distance = delta.magnitude;
                var player = participant.GetComponent<PrototypeCapsulePlayer>();
                var detectionRange = player != null && player.NecklaceActive
                    ? SafeNecklaceDetectionRange : SafeNormalDetectionRange;
                if (distance <= .001f || distance >= detectionRange) continue;
                if (distance > bestRange + .0001f ||
                    (Mathf.Abs(distance - bestRange) <= .0001f && participant.ParticipantId >= bestId)) continue;
                if (!HasLineOfSight(participant, delta, distance)) continue;
                best = participant; bestRange = distance; bestId = participant.ParticipantId;
            }
            return best;
        }

        private bool HasLineOfSight(PrototypeParticipant participant, Vector3 delta, float distance)
        {
            if (participant == null || distance <= .001f) return false;
            return !Physics.Raycast(transform.position + Vector3.up, delta / distance, out var hit, distance, ~0,
                QueryTriggerInteraction.Ignore) || hit.collider.GetComponentInParent<PrototypeParticipant>() == participant;
        }

        private void StartAttackWarning()
        {
            _attackAt = Time.time + SafeWarningSeconds;
            AttackWarningSequence++;
            SetState(PrototypeMonsterState.AttackWarning);
            PrototypeCues.Ping(transform.position);
        }

        private void ResolveAttack(PrototypeParticipant target)
        {
            if (target != null && target.CanAct)
            {
                var delta = target.transform.position - transform.position;
                if (delta.magnitude <= SafeAttackRange + .2f && HasLineOfSight(target, delta, delta.magnitude))
                    target.Damage(PrototypeTuning.Current.monsterDamage);
            }
            _attackAt = -1;
            _cooldown = Time.time + SafeCooldownSeconds;
            SetState(PrototypeMonsterState.AttackCooldown);
        }

        private void SetState(PrototypeMonsterState value)
        {
            _state = value;
            switch (value)
            {
                case PrototypeMonsterState.Chase: State = "추적"; break;
                case PrototypeMonsterState.Lured: State = "유인됨"; break;
                case PrototypeMonsterState.AttackWarning: State = "공격 예고"; break;
                case PrototypeMonsterState.AttackCooldown: State = "공격 대기"; break;
                case PrototypeMonsterState.Disabled: State = "환경 장치로 저지"; break;
                default: State = "순찰"; break;
            }
        }

        private void EnsureParameters()
        {
            if (_parameters == null) _parameters = new MonsterParameters();
        }

        private float SafeMaxHealth => Mathf.Max(1, _parameters.maxHealth);
        private float SafeNormalDetectionRange => Mathf.Max(.1f, _parameters.normalDetectionRange);
        private float SafeNecklaceDetectionRange => Mathf.Max(SafeNormalDetectionRange, _parameters.necklaceDetectionRange);
        private float SafeAttackRange => Mathf.Max(.1f, _parameters.attackRange);
        private float SafeWarningSeconds => Mathf.Max(.01f, _parameters.warningSeconds);
        private float SafeCooldownSeconds => Mathf.Max(0, _parameters.attackCooldownSeconds);
        private float SafeMovementSpeed => Mathf.Max(0, _parameters.movementSpeed);
        private float SafeGravity => Mathf.Max(0, _parameters.gravity);
        private float SafePatrolRadius => Mathf.Max(0, _parameters.patrolRadius);
    }
}

