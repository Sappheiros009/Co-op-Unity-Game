using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SlimeCoop.Prototype
{
    public sealed class PrototypeWaitingRoomController : MonoBehaviour
    {
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;

        private void Start()
        {
            BuildRoom();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                LoadChapter(PrototypeChapterCatalog.Get(1));
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            var areaWidth = 760f;
            var areaHeight = Mathf.Min(Screen.height - 40f, 700f);
            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - areaWidth * 0.5f, 20f, areaWidth, areaHeight), GUI.skin.box);
            GUILayout.Label("SLIME WAITING ROOM", _titleStyle);
            GUILayout.Label("Choose a prototype chapter. All chapters currently share the test route and use a distinct color palette.", _labelStyle);
            GUILayout.Space(16f);

            var chapters = PrototypeChapterCatalog.All;
            for (var index = 0; index < chapters.Count; index += 2)
            {
                GUILayout.BeginHorizontal();
                DrawChapterButton(chapters[index]);
                if (index + 1 < chapters.Count)
                {
                    DrawChapterButton(chapters[index + 1]);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(8f);
            }

            GUILayout.Space(8f);
            GUILayout.Label("Enter: Chapter 01 shortcut", _labelStyle);
            GUILayout.Label("After a chapter clears, its story-video placeholder opens before returning here.", _labelStyle);
            GUILayout.EndArea();
        }

        private void DrawChapterButton(PrototypeChapterDefinition chapter)
        {
            var label = chapter.Number.ToString("00") + "  " + chapter.RegionName;
            if (GUILayout.Button(label, GUILayout.Height(52f), GUILayout.ExpandWidth(true)))
            {
                LoadChapter(chapter);
            }
        }

        private static void LoadChapter(PrototypeChapterDefinition chapter)
        {
            SceneManager.LoadScene(chapter.SceneName);
        }

        public void BuildRoom()
        {
            var root = transform.Find("WaitingRoom_Presentation");
            if (root != null)
            {
                return;
            }

            root = new GameObject("WaitingRoom_Presentation").transform;
            root.SetParent(transform, false);
            var floor = PrototypeVisuals.CreateMaterial("Waiting Floor", new Color(0.19f, 0.24f, 0.28f));
            var wall = PrototypeVisuals.CreateMaterial("Waiting Wall", new Color(0.30f, 0.38f, 0.44f));
            PrototypeVisuals.CreateCube("Waiting Floor", root, new Vector3(0f, -0.5f, 0f), new Vector3(26f, 1f, 20f), floor);
            PrototypeVisuals.CreateCube("Waiting Back Wall", root, new Vector3(0f, 3f, 9f), new Vector3(26f, 6f, 1f), wall);
            PrototypeVisuals.CreateCube("Waiting Left Wall", root, new Vector3(-13f, 3f, 0f), new Vector3(1f, 6f, 20f), wall);
            PrototypeVisuals.CreateCube("Waiting Right Wall", root, new Vector3(13f, 3f, 0f), new Vector3(1f, 6f, 20f), wall);

            var colors = new[]
            {
                new Color(0.36f, 0.86f, 1f),
                new Color(1f, 0.56f, 0.72f),
                new Color(1f, 0.78f, 0.36f),
                new Color(0.68f, 1f, 0.46f)
            };
            var positions = new[]
            {
                new Vector3(-4f, 1f, 2f),
                new Vector3(-1.3f, 1f, 3.2f),
                new Vector3(1.6f, 1f, 3.2f),
                new Vector3(4.3f, 1f, 2f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                PrototypeVisuals.CreateCapsule("Waiting Slime " + (index + 1), root, positions[index], colors[index], false);
            }

            PrototypeVisuals.CreateDirectionalLight(root);
            var camera = PrototypeVisuals.CreateCamera("Waiting Room Camera", new Vector3(0f, 6.5f, -12f), new Vector3(0f, 1.2f, 2.5f));
            camera.transform.SetParent(transform, true);
            RenderSettings.ambientLight = new Color(0.28f, 0.34f, 0.38f);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
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
