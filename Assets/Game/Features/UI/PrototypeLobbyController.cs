using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SlimeCoop.Prototype
{
    public sealed class PrototypeLobbyController : MonoBehaviour
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
                SceneManager.LoadScene("PrototypeWaitingRoom");
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 260f, 80f, 520f, 430f), GUI.skin.box);
            GUILayout.Label("SLIME CO-OP", _titleStyle);
            GUILayout.Label("Prototype lobby", _labelStyle);
            GUILayout.Space(20f);
            GUILayout.Label("Online target: 2–4 players", _labelStyle);
            GUILayout.Label("Current prototype: local flow validation", _labelStyle);
            GUILayout.Label("Server authority and PlayFab integration are not claimed by this build.", _labelStyle);
            GUILayout.Space(20f);
            if (GUILayout.Button("Enter Slime Waiting Room", GUILayout.Height(54f)))
            {
                SceneManager.LoadScene("PrototypeWaitingRoom");
            }
            GUILayout.Space(10f);
            GUILayout.Label("Press Enter to continue", _labelStyle);
            GUILayout.EndArea();
        }

        public void BuildRoom()
        {
            var root = transform.Find("Lobby_2D_Presentation");
            if (root != null)
            {
                return;
            }

            root = new GameObject("Lobby_2D_Presentation").transform;
            root.SetParent(transform, false);
            var camera = PrototypeVisuals.CreateOrthographicCamera(
                "Lobby 2D Camera",
                new Vector3(0f, 0f, -10f),
                5.6f,
                new Color(0.035f, 0.05f, 0.12f));
            camera.transform.SetParent(transform, true);

            PrototypeVisuals.CreateSprite(
                "Lobby 2D Background",
                root,
                new Vector3(0f, 0f, 0f),
                new Vector2(20f, 12f),
                new Color(0.035f, 0.05f, 0.12f),
                -20);
            PrototypeVisuals.CreateSprite(
                "Lobby 2D Back Glow",
                root,
                new Vector3(0f, 1.25f, 0f),
                new Vector2(15.5f, 6.7f),
                new Color(0.09f, 0.13f, 0.28f, 0.92f),
                -19);
            PrototypeVisuals.CreateSprite(
                "Lobby 2D Main Panel",
                root,
                new Vector3(0f, -0.15f, 0f),
                new Vector2(10.4f, 8.3f),
                new Color(0.07f, 0.10f, 0.20f, 0.97f),
                -18);
            PrototypeVisuals.CreateSprite(
                "Lobby 2D Panel Accent",
                root,
                new Vector3(0f, 3.65f, 0f),
                new Vector2(9.8f, 0.08f),
                new Color(0.30f, 1f, 0.78f, 0.9f),
                -17);

            var decorativePositions = new[]
            {
                new Vector3(-7.3f, 3.4f, 0f),
                new Vector3(-7.3f, 2.95f, 0f),
                new Vector3(7.3f, 3.4f, 0f),
                new Vector3(7.3f, 2.95f, 0f),
                new Vector3(-7.3f, -3.25f, 0f),
                new Vector3(7.3f, -3.25f, 0f)
            };
            for (var index = 0; index < decorativePositions.Length; index++)
            {
                PrototypeVisuals.CreateSprite(
                    "Lobby 2D Decorative Mark " + (index + 1),
                    root,
                    decorativePositions[index],
                    new Vector2(index < 4 ? 1.2f : 1.8f, 0.08f),
                    new Color(0.27f, 0.52f, 0.78f, 0.55f),
                    -16);
            }

            PrototypeVisuals.CreateSprite(
                "Lobby 2D Footer Glow",
                root,
                new Vector3(0f, -4.25f, 0f),
                new Vector2(14.5f, 0.12f),
                new Color(0.24f, 0.36f, 0.62f, 0.65f),
                -16);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
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
