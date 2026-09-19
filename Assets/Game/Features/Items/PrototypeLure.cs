using UnityEngine;
namespace SlimeCoop.Prototype
{
    public sealed class PrototypeLure : MonoBehaviour
    {
        public static PrototypeLure Active { get; private set; }
        private float _expires;
        public static void Create(Vector3 position)
        {
            if (Active != null) Destroy(Active.gameObject);
            var obj = PrototypeVisuals.CreateSphere("Lure", null, position, Vector3.one * 0.4f,
                PrototypeVisuals.CreateMaterial("LurePink", Color.magenta, true), true);
            obj.AddComponent<Rigidbody>(); Active = obj.AddComponent<PrototypeLure>(); Active._expires = Time.time + 10;
            PrototypeCues.Ping(position);
        }
        private void Update() { if (Time.time >= _expires) Destroy(gameObject); }
        private void OnDestroy() { if (Active == this) Active = null; }
    }
}
