using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>
    /// Local placeholder companions make the 2–4 participant exit rule testable without networking.
    /// They follow the local player and converge on the exit after the player approaches it.
    /// </summary>
    public sealed class PrototypeTeammate : MonoBehaviour
    {
        [SerializeField] private float followSpeed = 2.8f;

        private PrototypeGame _game;
        private PrototypeParticipant _participant;
        private Vector3 _spawnPoint;
        private Vector3 _followOffset;
        private Vector3 _exitPoint;

        public PrototypeParticipant Participant => _participant;

        public void Configure(
            PrototypeGame game,
            int participantId,
            string displayName,
            Vector3 spawnPoint,
            Vector3 followOffset,
            Vector3 exitPoint,
            Color bodyColor)
        {
            _game = game;
            _spawnPoint = spawnPoint;
            _followOffset = followOffset;
            _exitPoint = exitPoint;
            _participant = GetComponent<PrototypeParticipant>();
            if (_participant == null)
            {
                _participant = gameObject.AddComponent<PrototypeParticipant>();
            }

            _participant.Configure(participantId, displayName);
            var collider = GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CapsuleCollider>();
            }

            collider.height = 2f;
            collider.radius = 0.5f;
            collider.center = new Vector3(0f, 1f, 0f);
            collider.isTrigger = true;

            var body = transform.Find(displayName + " Capsule Body");
            if (body == null)
            {
                var bodyObject = PrototypeVisuals.CreateCapsule(
                    displayName + " Capsule Body",
                    transform,
                    transform.position + Vector3.up,
                    bodyColor,
                    false);
                bodyObject.transform.localPosition = Vector3.up;
            }

            var rigidbody = GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            ResetForRun();
        }

        private void Update()
        {
            if (_game == null || _game.Player == null || _participant.HasEnteredExit || (_game.Exit != null && _game.Exit.IsSettled))
            {
                return;
            }

            var playerDistanceToExit = Vector3.Distance(_game.Player.transform.position, _exitPoint);
            var target = playerDistanceToExit < 12f
                ? _exitPoint + (_followOffset * 0.2f)
                : _game.Player.transform.position + _followOffset;
            target.y = 0f;

            var position = transform.position;
            var horizontalTarget = new Vector3(target.x, position.y, target.z);
            transform.position = Vector3.MoveTowards(position, horizontalTarget, followSpeed * Time.deltaTime);
            PrototypeVisuals.Face(transform, horizontalTarget);

            if (Vector3.Distance(transform.position, _exitPoint) < 1.35f)
            {
                _game.Exit.RegisterArrival(_participant);
            }
        }

        public void ResetForRun()
        {
            transform.position = _spawnPoint;
            transform.rotation = Quaternion.identity;
            _participant.ResetParticipant();
        }
    }
}
