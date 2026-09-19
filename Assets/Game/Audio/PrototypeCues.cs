using UnityEngine;
namespace SlimeCoop.Prototype
{
    // Synthesized functional cue, not the final SFX/VFX choice.
    public sealed class PrototypeCues : MonoBehaviour
    {
        private static AudioClip _tone;
        private float _expires;
        public static void Ping(Vector3 point)
        {
            var obj = PrototypeVisuals.CreateSphere("PrototypePing", null, point + Vector3.up * 2, Vector3.one * 0.4f,
                PrototypeVisuals.CreateMaterial("PingCyan", Color.cyan, true));
            var cue = obj.AddComponent<PrototypeCues>(); cue._expires = Time.time + 1.3f;
            if (_tone == null)
            {
                const int count = 8820; var samples = new float[count];
                for (var i = 0; i < count; i++) samples[i] = Mathf.Sin(i * 2 * Mathf.PI * 660 / 44100) * 0.12f * (1 - i / (float)count);
                _tone = AudioClip.Create("Prototype generated ping", count, 1, 44100, false); _tone.SetData(samples, 0);
            }
            var source = obj.AddComponent<AudioSource>(); source.spatialBlend = 0.6f; source.maxDistance = 60;
            source.volume = PrototypeSettings.Current.sfxVolume; source.PlayOneShot(_tone);
        }
        private void Update() { transform.localScale += Vector3.one * Time.deltaTime; if (Time.time >= _expires) Destroy(gameObject); }
    }
}
