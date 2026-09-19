using UnityEngine;
namespace SlimeCoop.Prototype
{
    public sealed class PrototypeCyclePlatform : MonoBehaviour
    {
        public float Offset;
        private Collider _collider;
        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private void Awake() { _block = new MaterialPropertyBlock(); _collider = GetComponent<Collider>(); _renderer = GetComponent<Renderer>(); }
        private void Update()
        {
            var phase = (Time.time + Offset) % 8;
            _collider.enabled = phase < 6;
            _block.SetColor("_BaseColor", phase < 4 ? new Color(0.25f, 0.7f, 0.85f) : phase < 6 ? Color.yellow : Color.red);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
