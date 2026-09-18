using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>
    /// Primitive capsule monster. It chases the local player and applies contact damage;
    /// it deliberately grants no score because the planning rule excludes monster-kill points.
    /// </summary>
    public sealed class PrototypeCapsuleMonster : MonoBehaviour
    {
        [SerializeField] private float detectionRange = 18f;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float attackRange = 1.4f;
        [SerializeField] private float damagePerSecond = 12f;

        private PrototypeGame _game;
        private Vector3 _spawnPoint;
        private float _damageCooldown;

        public void Configure(PrototypeGame game, Vector3 spawnPoint, Color bodyColor)
        {
            _game = game;
            _spawnPoint = spawnPoint;
            _damageCooldown = 0f;

            var collider = GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CapsuleCollider>();
            }

            collider.height = 2f;
            collider.radius = 0.5f;
            collider.center = new Vector3(0f, 1f, 0f);
            collider.isTrigger = true;

            var body = transform.Find("Monster Capsule Body");
            if (body == null)
            {
                PrototypeVisuals.CreateCapsule(
                    "Monster Capsule Body",
                    transform,
                    transform.position + Vector3.up,
                    bodyColor,
                    false).transform.localPosition = Vector3.up;
            }

            ResetForRun();
        }

        private void Update()
        {
            if (_game == null || _game.Player == null || _game.Exit != null && _game.Exit.IsSettled)
            {
                return;
            }

            var player = _game.Player.transform;
            var horizontalToPlayer = player.position - transform.position;
            horizontalToPlayer.y = 0f;
            var distance = horizontalToPlayer.magnitude;

            if (distance <= detectionRange && distance > attackRange)
            {
                var direction = horizontalToPlayer.normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
                PrototypeVisuals.Face(transform, transform.position + direction);
            }

            _damageCooldown -= Time.deltaTime;
            if (distance <= attackRange && _damageCooldown <= 0f)
            {
                _game.Player.ReceiveDamage(damagePerSecond);
                _damageCooldown = 1f;
            }
        }

        public void ResetForRun()
        {
            transform.position = _spawnPoint;
            transform.rotation = Quaternion.identity;
            _damageCooldown = 0f;
        }
    }
}
