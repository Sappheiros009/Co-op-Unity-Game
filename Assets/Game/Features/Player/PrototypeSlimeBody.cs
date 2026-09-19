using UnityEngine;

namespace SlimeCoop.Prototype
{
    public sealed class PrototypeSlimeBody : MonoBehaviour
    {
        private PrototypeParticipant _participant;
        private Transform _torso, _leftFoot, _rightFoot;
        private Vector3 _lastPosition;
        public void Configure(PrototypeParticipant participant, Color color, bool firstPerson)
        {
            _participant = participant;
            var skin = PrototypeVisuals.CreateMaterial("SlimeSkin_" + color, color);
            var dark = PrototypeVisuals.CreateMaterial("SlimeFaceInk", new Color(0.06f, 0.09f, 0.13f));
            _torso = Part("Body_Torso", new Vector3(0, 0.68f, 0), new Vector3(1.15f, 1.15f, 0.95f), skin);
            var head = Part("Head", new Vector3(0, 1.34f, 0.02f), new Vector3(0.85f, 0.7f, 0.8f), skin);
            var face = new GameObject("Face").transform; face.SetParent(head, false);
            Part("Eye_Left", new Vector3(-0.17f, 1.4f, 0.38f), Vector3.one * 0.12f, dark).SetParent(face, true);
            Part("Eye_Right", new Vector3(0.17f, 1.4f, 0.38f), Vector3.one * 0.12f, dark).SetParent(face, true);
            Part("Mouth", new Vector3(0, 1.19f, 0.41f), new Vector3(0.18f, 0.055f, 0.06f), dark).SetParent(face, true);
            Part("Hand_Left", new Vector3(-0.65f, 0.67f, 0.23f), new Vector3(0.3f, 0.32f, 0.3f), skin);
            Part("Hand_Right", new Vector3(0.65f, 0.67f, 0.23f), new Vector3(0.3f, 0.32f, 0.3f), skin);
            _leftFoot = Part("Foot_Left", new Vector3(-0.3f, 0.13f, 0.17f), new Vector3(0.42f, 0.25f, 0.55f), skin);
            _rightFoot = Part("Foot_Right", new Vector3(0.3f, 0.13f, 0.17f), new Vector3(0.42f, 0.25f, 0.55f), skin);
            Part("Necklace", new Vector3(0, 0.88f, 0.48f), Vector3.one * 0.16f,
                PrototypeVisuals.CreateMaterial("NecklaceGold", new Color(1, 0.82f, 0.35f), true));
            if (firstPerson) head.gameObject.SetActive(false);
            _lastPosition = transform.position;
        }
        private Transform Part(string name, Vector3 local, Vector3 scale, Material material)
        {
            var obj = PrototypeVisuals.CreateSphere(name, transform, transform.position + local, scale, material);
            obj.transform.localPosition = local; return obj.transform;
        }
        private void Update()
        {
            if (_participant == null || _torso == null) return;
            var speed = Mathf.Clamp01(Vector3.Distance(transform.position, _lastPosition) / Mathf.Max(0.001f, Time.deltaTime) / 4);
            _lastPosition = transform.position;
            var squash = !_participant.IsAlive ? 0.38f : _participant.IsCrouching ? 0.65f : 1f;
            _torso.localScale = new Vector3(1.15f, 1.15f * squash, 0.95f);
            _leftFoot.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 12) * 20 * speed, 0, 0);
            _rightFoot.localRotation = Quaternion.Euler(-Mathf.Sin(Time.time * 12) * 20 * speed, 0, 0);
        }
    }
}
