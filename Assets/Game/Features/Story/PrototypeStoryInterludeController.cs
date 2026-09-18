using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SlimeCoop.Prototype
{
    /// <summary>
    /// Placeholder for the chapter-to-chapter story video scene.
    /// Replace the marked presentation area with an approved VideoPlayer timeline later.
    /// </summary>
    public sealed class PrototypeStoryInterludeController : MonoBehaviour
    {
        [SerializeField] private int completedChapter = 1;

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;

        public int CompletedChapter => completedChapter;
        public PrototypeChapterDefinition Chapter => PrototypeChapterCatalog.Get(completedChapter);

        public void ConfigureChapter(int chapterNumber)
        {
            completedChapter = Mathf.Clamp(chapterNumber, 1, 7);
        }

        private void Start()
        {
            BuildRoom();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                ReturnToWaitingRoom();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            var chapter = Chapter;
            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 300f, 80f, 600f, 460f), GUI.skin.box);
            GUILayout.Label("STORY INTERLUDE // " + chapter.ChapterLabel.ToUpperInvariant(), _titleStyle);
            GUILayout.Space(18f);
            GUILayout.Label("CHAPTER CLEAR", _titleStyle);
            GUILayout.Label("This is the future video-scene insertion point.", _labelStyle);
            GUILayout.Label("Replace the placeholder panel with the approved story video, subtitles, and timeline.", _labelStyle);
            GUILayout.Space(30f);
            if (GUILayout.Button("Continue to Slime Waiting Room", GUILayout.Height(56f)))
            {
                ReturnToWaitingRoom();
            }
            GUILayout.Space(10f);
            GUILayout.Label("Press Enter to continue", _labelStyle);
            GUILayout.EndArea();
        }

        public void BuildRoom()
        {
            var root = transform.Find("StoryInterlude_2D_Presentation");
            if (root != null)
            {
                return;
            }

            root = new GameObject("StoryInterlude_2D_Presentation").transform;
            root.SetParent(transform, false);
            var chapter = Chapter;
            var backgroundColor = new Color(chapter.FogColor.r, chapter.FogColor.g, chapter.FogColor.b, 1f);
            var panelColor = new Color(chapter.WallColor.r, chapter.WallColor.g, chapter.WallColor.b, 0.94f);
            var accentColor = new Color(chapter.AccentColor.r, chapter.AccentColor.g, chapter.AccentColor.b, 0.90f);
            PrototypeVisuals.CreateSprite(
                "Story Video Placeholder Background",
                root,
                new Vector3(0f, 0f, 0f),
                new Vector2(20f, 12f),
                backgroundColor,
                -20);
            PrototypeVisuals.CreateSprite(
                "Story Video Placeholder Panel",
                root,
                new Vector3(0f, 0f, 0f),
                new Vector2(13.5f, 7.4f),
                panelColor,
                -19);
            PrototypeVisuals.CreateSprite(
                "Story Video Placeholder Accent",
                root,
                new Vector3(0f, -3.45f, 0f),
                new Vector2(12.3f, 0.08f),
                accentColor,
                -18);

            var camera = PrototypeVisuals.CreateOrthographicCamera(
                "Story Interlude 2D Camera",
                new Vector3(0f, 0f, -10f),
                5.6f,
                backgroundColor);
            camera.transform.SetParent(transform, true);
        }

        private static void ReturnToWaitingRoom()
        {
            SceneManager.LoadScene("PrototypeWaitingRoom");
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.42f, 1f, 0.82f) }
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
    }
}
