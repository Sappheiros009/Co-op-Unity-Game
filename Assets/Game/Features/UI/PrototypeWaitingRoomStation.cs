using TMPro;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    public enum PrototypeWaitingStationKind { Chapter, Ready, Party, Specialty, TrialMode, Practice, Records, Memories, Intro, Back }
    public sealed class PrototypeWaitingRoomStation : MonoBehaviour
    {
        private readonly RaycastHit[] _occlusionHits = new RaycastHit[16];
        private Transform _labelTransform;
        public PrototypeWaitingStationKind Kind;
        public int Chapter;
        public string Label;
        public Vector3 FocusPoint => transform.position + Vector3.up * .65f;
        public void EnsureLabel()
        {
            if (!Application.isPlaying) return;
            _labelTransform = transform.Find("StationLabel");
            if (_labelTransform != null) return;
            var obj = new GameObject("StationLabel"); obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.up * (Kind == PrototypeWaitingStationKind.Chapter ? 3.35f : Kind == PrototypeWaitingStationKind.Ready ? 1.35f : 1.9f);
            obj.transform.localScale = Vector3.one * .14f;
            var text = obj.AddComponent<TextMeshPro>(); text.font = PrototypeUi.Font;
            // Adding TMP replaces Transform with RectTransform; cache only the surviving component.
            _labelTransform = text.transform;
            text.fontSize = Kind == PrototypeWaitingStationKind.Ready ? 18 : Kind == PrototypeWaitingStationKind.Chapter ? 26 : 22;
            text.rectTransform.sizeDelta = new Vector2(20,6); text.alignment = TextAlignmentOptions.Center;
            text.text = Kind == PrototypeWaitingStationKind.Ready ? "준비 장치\n출발" : Label;
            text.color = Color.white; text.textWrappingMode = TextWrappingModes.NoWrap;
            if (Kind == PrototypeWaitingStationKind.Chapter)
            {
                // Long approved names must fit a station's width even from an off-centre spawn.
                var width = text.GetPreferredValues(text.text).x;
                if (width * .14f > 2.25f) text.fontSize *= 2.25f / (width * .14f);
            }
        }
        public void FaceLabelToward(Camera view)
        {
            if (_labelTransform == null || view == null) return;
            // TMP's visible front faces local -Z. Turn only the caption, never the interaction device.
            var away = _labelTransform.position - view.transform.position; away.y = 0;
            if (away.sqrMagnitude > .0001f) _labelTransform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
        public bool CanUse(PrototypeWaitingRoomWalker player, bool ignoreOtherParticipants = false)
        {
            if (player == null || player.ViewCamera == null || !gameObject.activeInHierarchy
                || Vector3.Distance(player.transform.position, transform.position) > 3.2f) return false;
            var camera = player.ViewCamera.transform; var toward = FocusPoint - camera.position;
            if (Vector3.Dot(camera.forward, toward.normalized) < .7f) return false;
            var count = Physics.RaycastNonAlloc(camera.position, toward.normalized, _occlusionHits, toward.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (count == _occlusionHits.Length) return false; // A saturated query must not accidentally allow use through a wall.
            for (var i = 0; i < count; i++)
            {
                var collider = _occlusionHits[i].collider;
                if (collider == null || collider.transform.IsChildOf(player.transform)) continue;
                if (ignoreOtherParticipants && collider.GetComponentInParent<PrototypeParticipant>() != null) continue;
                if (collider.GetComponentInParent<PrototypeWaitingRoomStation>() != this) return false;
            }
            return true;
        }
    }
}
