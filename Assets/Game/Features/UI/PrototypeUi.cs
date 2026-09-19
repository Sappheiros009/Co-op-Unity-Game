using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;

namespace SlimeCoop.Prototype
{
    public static class PrototypeUi
    {
        private static TMP_FontAsset _font;
        private static GameObject _modal;
        public static bool IsModalOpen => _modal != null;
        public static readonly Color Ink = new Color(0.035f, 0.055f, 0.09f, 0.96f);
        public static readonly Color Accent = new Color(0.3f, 0.93f, 0.77f);
        public static TMP_FontAsset Font
        {
            get
            {
                if (_font != null) return _font;
                // Use the user's installed Windows font; do not redistribute proprietary font files.
                _font = TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular")
                    ?? TMP_FontAsset.CreateFontAsset("맑은 고딕", "Regular")
                    ?? TMP_FontAsset.CreateFontAsset("Arial", "Regular");
                if (_font == null) throw new InvalidOperationException("Windows 한글 시스템 폰트를 불러올 수 없습니다.");
                _font.name = "Prototype Korean runtime font";
                return _font;
            }
        }
        public static RectTransform Canvas(Transform owner, string name = "PrototypeCanvas", int sort = 0)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            obj.transform.SetParent(owner, false);
            var canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = sort;
            var scaler = obj.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            if (sort == 0) obj.AddComponent<PrototypeSaveNotice>();
            return (RectTransform)obj.transform;
        }
        public static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x, 1 - y - height); rect.anchorMax = new Vector2(x + width, 1 - y);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; return rect;
        }
        public static RectTransform Panel(Transform parent, string name, float x, float y, float width, float height, Color? color = null, bool blocking = false)
        {
            var rect = Rect(parent, name, x, y, width, height);
            var img = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); img.color = color ?? Ink; img.raycastTarget = blocking;
            return rect;
        }
        public static TextMeshProUGUI Text(Transform parent, string name, string text, float x, float y, float w, float h, float size = 22, Color? color = null)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = Font; label.fontSize = size;
            label.text = text; label.color = color ?? Color.white; label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            return label;
        }
        public static UnityEngine.UI.Button Button(Transform parent, string name, string title, float x, float y, float w, float h, Action action)
        {
            var rect = Panel(parent, name, x, y, w, h, new Color(0.12f, 0.23f, 0.3f), true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>();
            var colors = button.colors; colors.highlightedColor = new Color(0.6f, 1, 0.9f); colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            var label = Text(rect, "Label", title, 0.04f, 0, 0.92f, 1, 21); label.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => action()); return button;
        }
        public static TMP_InputField Input(Transform parent, string name, string value, float x, float y, float w, float h, int limit)
        {
            var rect=Panel(parent,name,x,y,w,h,new Color(.08f,.15f,.22f),true);
            var viewport=Rect(rect,"TextArea",.035f,.06f,.93f,.88f); viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var label=Text(viewport,"Text","",0,0,1,1,23); label.richText=false; label.textWrappingMode=TextWrappingModes.NoWrap;
            var field=rect.gameObject.AddComponent<TMP_InputField>(); field.targetGraphic=rect.GetComponent<UnityEngine.UI.Image>();
            field.textViewport=viewport; field.textComponent=label; field.fontAsset=Font;
            field.lineType=TMP_InputField.LineType.SingleLine; field.characterLimit=limit; field.richText=false; field.text=value;
            field.customCaretColor=true; field.caretColor=Accent; field.selectionColor=new Color(.3f,.7f,.65f,.45f);
            return field;
        }
        public static void WorldLabel(Transform parent, string text, Vector3 position, float scale)
        {
            if (!Application.isPlaying) return;
            var obj = new GameObject("Label_" + text); obj.transform.SetParent(parent); obj.transform.position = position;
            var label = obj.AddComponent<TextMeshPro>(); label.font = Font; label.text = text; label.fontSize = 8;
            label.alignment = TextAlignmentOptions.Center; label.color = Color.white;
            label.rectTransform.sizeDelta = new Vector2(45, 5); obj.transform.localScale = Vector3.one * scale;
        }
        public static RectTransform Modal(Transform owner, string title)
        {
            CloseModal();
            var canvas = Canvas(owner, "ModalCanvas", 100); _modal = canvas.gameObject;
            Panel(canvas, "Backdrop", 0, 0, 1, 1, new Color(0, 0, 0, 0.75f), true);
            var panel = Panel(canvas, "Content", 0.08f, 0.055f, 0.84f, 0.89f, Ink, true);
            Text(panel, "Title", title, 0.035f, 0.02f, 0.85f, 0.09f, 28, Accent);
            PrototypeCapsulePlayer.SetCursor(false); return panel;
        }
        public static void CloseModal() { if (_modal != null) { _modal.SetActive(false); UnityEngine.Object.Destroy(_modal); _modal = null; } }
    }
}
