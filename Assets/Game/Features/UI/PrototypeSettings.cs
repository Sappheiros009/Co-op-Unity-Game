using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SlimeCoop.Prototype
{
    [Serializable] public sealed class PrototypeKeyBinding { public string action; public Key key; }
    [Serializable] public sealed class PrototypeSettingsData
    {
        public int schemaVersion = 1;
        public string quality = "자동";
        public float renderScale = 1, sensitivity = 0.08f, fieldOfView = 72, cameraShake = 0;
        public float masterVolume = 0.7f, sfxVolume = 0.8f;
        public int frameCap = 60;
        public bool vsync, crouchToggle, sprintToggle;
        public List<PrototypeKeyBinding> keys = new List<PrototypeKeyBinding>();
    }
    public static class PrototypeSettings
    {
        private static PrototypeSettingsData _current;
        private static UniversalRenderPipelineAsset _runtimePipeline;
        public static string Error { get; private set; } = "";
        public static bool ReadProtected { get; private set; }
        public static void Reload() { _current = null; Error = ""; ReadProtected = false; }
        public static PrototypeSettingsData Current => _current ??= Load();
        public static PrototypeSettingsData Copy() => JsonUtility.FromJson<PrototypeSettingsData>(JsonUtility.ToJson(Current));
        public static string ResolvedQuality(PrototypeSettingsData data) => data.quality == "자동"
            ? (SystemInfo.graphicsMemorySize < 2048 ? "낮음" : "중간") : data.quality;
        public static float ResolvedRenderScale(PrototypeSettingsData data) => ResolvedQuality(data) switch
        { "낮음" => .7f, "중간" => .9f, "높음" => 1f, _ => data.renderScale };
        private static PrototypeSettingsData Load()
        {
            var data = new PrototypeSettingsData();
            var path = Path.Combine(PrototypeSave.Root, "settings.json");
            try
            {
                var raw = File.ReadAllText(path);
                data = JsonUtility.FromJson<PrototypeSettingsData>(raw);
                if (data == null || data.schemaVersion != 1 || !raw.Contains("schemaVersion")) throw new InvalidDataException("설정 버전 오류");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException || e is UnauthorizedAccessException)
            { ReadProtected = true; Error = "설정 원본 보존: " + e.Message; data = new PrototypeSettingsData(); }
            Sanitize(data); return data;
        }
        public static void Sanitize(PrototypeSettingsData data)
        {
            data.keys ??= new List<PrototypeKeyBinding>();
            data.sensitivity = Mathf.Clamp(float.IsFinite(data.sensitivity) ? data.sensitivity : 0.08f, 0.02f, 0.3f);
            data.fieldOfView = Mathf.Clamp(float.IsFinite(data.fieldOfView) ? data.fieldOfView : 72, 60, 100);
            data.renderScale = Mathf.Clamp(float.IsFinite(data.renderScale) ? data.renderScale : 1, 0.6f, 1);
            data.masterVolume = Mathf.Clamp01(float.IsFinite(data.masterVolume) ? data.masterVolume : .7f);
            data.sfxVolume = Mathf.Clamp01(float.IsFinite(data.sfxVolume) ? data.sfxVolume : .8f);
            if (data.quality != "자동" && data.quality != "낮음" && data.quality != "중간" && data.quality != "높음" && data.quality != "사용자 지정") data.quality = "자동";
            data.frameCap = data.frameCap == 30 || data.frameCap == 120 ? data.frameCap : 60;
            data.cameraShake = Mathf.Clamp01(float.IsFinite(data.cameraShake) ? data.cameraShake : 0);
            PrototypeInput.SanitizeBindings(data);
        }
        public static bool Apply(PrototypeSettingsData data, bool save)
        {
            _ = Current; // Inspect the existing settings file before any save, even on a first Apply call.
            Sanitize(data); _current = JsonUtility.FromJson<PrototypeSettingsData>(JsonUtility.ToJson(data));
            var quality = ResolvedQuality(data);
            var index = quality == "낮음" ? 0 : quality == "높음" ? 2 : 1;
            if (_runtimePipeline == null && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset source)
            { _runtimePipeline = UnityEngine.Object.Instantiate(source); _runtimePipeline.name = "Prototype local quality clone"; }
            if (_runtimePipeline != null)
            {
                QualitySettings.renderPipeline = _runtimePipeline;
                _runtimePipeline.renderScale = ResolvedRenderScale(data);
                _runtimePipeline.shadowDistance = index == 0 ? 12 : index == 1 ? 30 : 60;
                _runtimePipeline.msaaSampleCount = index == 0 ? 1 : index == 1 ? 2 : 4;
            }
            QualitySettings.globalTextureMipmapLimit = index == 0 ? 1 : 0;
            QualitySettings.vSyncCount = data.vsync ? 1 : 0;
            Application.targetFrameRate = data.frameCap; AudioListener.volume = data.masterVolume;
            if (!save) return true;
            if (ReadProtected) return false;
            var success = PrototypeSave.TryWriteJson("settings.json", JsonUtility.ToJson(data, true), out var error);
            Error = error; return success;
        }
    }
    public static class PrototypeInput
    {
        public static readonly Dictionary<string, Key> Defaults = new Dictionary<string, Key>
        {
            {"Forward",Key.W},{"Backward",Key.S},{"Left",Key.A},{"Right",Key.D},{"Jump",Key.Space},
            {"Sprint",Key.LeftShift},{"Crouch",Key.LeftCtrl},{"Interact",Key.E},{"Use",Key.F},{"Drop",Key.G},
            {"Ping",Key.Q},{"Necklace",Key.N},{"Assist",Key.T},{"Follow",Key.Y},{"Journal",Key.J}
        };
        public static Key Binding(string action)
        {
            var custom = PrototypeSettings.Current.keys.Find(b => b.action == action);
            return custom == null ? Defaults[action] : custom.key;
        }
        public static bool Held(string action) => Keyboard.current != null && Keyboard.current[Binding(action)].isPressed;
        public static bool Pressed(string action) => Keyboard.current != null && Keyboard.current[Binding(action)].wasPressedThisFrame;
        public static string Label(string action) => Binding(action) switch
        { Key.LeftShift => "Shift", Key.LeftCtrl => "Ctrl", Key.Space => "Space", _ => Binding(action).ToString() };
        public static bool IsBindable(Key key) => Enum.IsDefined(typeof(Key), key) && key != Key.None && key != Key.Escape &&
            key != Key.F2 && key != Key.Digit1 && key != Key.Digit2 && key != Key.Digit3;
        public static void SanitizeBindings(PrototypeSettingsData data)
        {
            data.keys ??= new List<PrototypeKeyBinding>();
            var actions = new HashSet<string>();
            data.keys.RemoveAll(b => b == null || b.action == null || !Defaults.ContainsKey(b.action) ||
                !IsBindable(b.key) || !actions.Add(b.action));
            // Include implicit defaults. A loaded Jump=W must not steal Forward's unchanged W.
            // Valid complete swaps survive; conflicting overrides fall back until all actions are unique.
            bool changed;
            do
            {
                changed = false;
                var owners = new Dictionary<Key, string>();
                foreach (var pair in Defaults)
                {
                    var custom = data.keys.Find(b => b.action == pair.Key);
                    var key = custom == null ? pair.Value : custom.key;
                    if (!owners.TryGetValue(key, out var owner)) { owners.Add(key, pair.Key); continue; }
                    var loser = pair.Value == key ? owner : pair.Key;
                    if (data.keys.RemoveAll(b => b.action == loser) > 0) { changed = true; break; }
                }
            } while (changed);
        }
        public static bool Rebind(PrototypeSettingsData data, string action, Key key)
        {
            if (data == null || action == null || !Defaults.ContainsKey(action) || !IsBindable(key)) return false;
            SanitizeBindings(data);
            foreach (var pair in Defaults)
            {
                var custom = data.keys.Find(b => b.action == pair.Key);
                if (pair.Key != action && (custom == null ? pair.Value : custom.key) == key) return false;
            }
            data.keys.RemoveAll(b => b.action == action); data.keys.Add(new PrototypeKeyBinding { action = action, key = key }); return true;
        }
    }
}
