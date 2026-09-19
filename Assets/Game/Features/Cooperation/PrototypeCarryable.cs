using UnityEngine;

namespace SlimeCoop.Prototype
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PrototypeCarryable : MonoBehaviour
    {
        public bool IsLarge;
        public bool Essential = true;
        public bool IsCarried => _carrier != null;
        private Transform _carrier;
        private Rigidbody _body;
        private Vector3 _origin;
        private Collider _collider;
        private readonly System.Collections.Generic.Dictionary<int, Vector3> _pushDirections = new System.Collections.Generic.Dictionary<int, Vector3>();
        private readonly System.Collections.Generic.Dictionary<int, float> _pushTimes = new System.Collections.Generic.Dictionary<int, float>();
        public void Configure(bool large)
        {
            IsLarge = large; _origin = transform.position;
            _body = GetComponent<Rigidbody>(); _collider = GetComponent<Collider>();
            _body.mass = large ? 35 : 2; _body.constraints = RigidbodyConstraints.FreezeRotation;
        }
        private void Awake() { _body = GetComponent<Rigidbody>(); _collider = GetComponent<Collider>(); _origin = transform.position; }
        public bool PickUp(Transform cameraTransform)
        {
            if (IsLarge || _carrier != null || Vector3.Distance(transform.position, cameraTransform.position) > 3.5f) return false;
            _carrier = cameraTransform; _body.isKinematic = true; _collider.enabled = false; return true;
        }
        public void Drop()
        {
            if (_carrier == null) return;
            _carrier = null; _collider.enabled = true; _body.isKinematic = false;
        }
        public void Push(Vector3 force, int participantId = 0)
        {
            if (!IsLarge || IsCarried || participantId < 0 || participantId >= PrototypeSession.StartingCount ||
                !float.IsFinite(force.x) || !float.IsFinite(force.y) || !float.IsFinite(force.z)) return;
            force.y = 0;
            // One held direction per participant; rendering extra frames never adds extra force.
            _pushDirections[participantId] = Vector3.ClampMagnitude(force, 1);
            _pushTimes[participantId] = Time.time;
        }
        private void FixedUpdate()
        {
            if (IsLarge && !IsCarried)
            {
                var force = Vector3.zero;
                foreach (var pair in _pushDirections)
                    if (Time.time - _pushTimes[pair.Key] <= .1f && PrototypeSession.IsConnected(pair.Key)) force += pair.Value;
                _body.AddForce(force * 100f, ForceMode.Force);
            }
            if (_carrier != null)
            {
                var desired = _carrier.position + _carrier.forward * 1.6f;
                if (Physics.Raycast(_carrier.position, _carrier.forward, out var hit, 2f, ~0, QueryTriggerInteraction.Ignore))
                    desired = hit.point - _carrier.forward * 0.65f;
                _body.MovePosition(desired);
            }
            if (Essential && transform.position.y < -8)
            {
                Drop(); _body.linearVelocity = Vector3.zero; transform.position = _origin;
            }
        }
    }
}
