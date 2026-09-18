using UnityEngine;

namespace SlimeCoop.Prototype
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private PrototypeGame _game;
        private GUIStyle _boxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;

        public void Configure(PrototypeGame game)
        {
            _game = game;
        }

        private void OnGUI()
        {
            if (_game == null || _game.Player == null || _game.Exit == null)
            {
                return;
            }

            EnsureStyles();
            GUILayout.BeginArea(new Rect(18f, 18f, 430f, 250f), _boxStyle);
            GUILayout.Label("SLIME CO-OP // " + _game.ChapterDisplayName.ToUpperInvariant(), _titleStyle);
            GUILayout.Label("WASD Move   Shift Sprint   Space Jump", _labelStyle);
            GUILayout.Label("Click to look   Esc release cursor   R reset", _labelStyle);
            GUILayout.Space(8f);
            GUILayout.Label("Roster at stage start: 4", _labelStyle);
            GUILayout.Label("Arrived at exit: " + _game.Exit.ArrivedCount + "/4", _labelStyle);
            GUILayout.Label("Health: " + Mathf.CeilToInt(_game.Player.Health), _labelStyle);

            if (_game.Exit.HasStartedSettlement && !_game.Exit.IsSettled)
            {
                GUILayout.Label("Settlement window: " + _game.Exit.SecondsRemaining.ToString("0.0") + "s", _labelStyle);
            }
            else if (_game.Exit.IsSettled)
            {
                GUILayout.Label("CLEAR  Team score: " + _game.Exit.TeamScore, _titleStyle);
                GUILayout.Label("Reason: " + _game.Exit.SettlementReason, _labelStyle);
                GUILayout.Label("Story scene opens automatically, then returns to waiting room.", _labelStyle);
            }
            else
            {
                GUILayout.Label("One valid arrival starts the 5-second tally.", _labelStyle);
            }

            GUILayout.Label(_game.StatusMessage, _labelStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null)
            {
                return;
            }

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12)
            };
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.42f, 1f, 0.82f) }
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }
    }
}
