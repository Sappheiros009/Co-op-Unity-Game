using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Replaceable text-memory prototype, not an approved cinematic or final dialogue.</summary>
    public sealed class PrototypeStoryMemory : MonoBehaviour
    {
        private PrototypeGame _game;
        private bool _found;
        public void Configure(PrototypeGame game) { _game = game; }
        private void Update()
        {
            if (_found || _game == null) return;
            var discovererPresent = false;
            foreach (var player in _game.ControlledPlayers)
                if (player.NecklaceActive && player.Participant.CanAct && Vector3.Distance(transform.position, player.transform.position) <= 3.2f)
                { discovererPresent = true; break; }
            if (!discovererPresent) return;
            _found = true;
            var themes = new[] { "광산 사건과 부모가 살던 도시의 흔적", "뜨거운 귀향로와 귀향패의 기억", "멈춘 고향의 시간을 짐작하게 하는 단서",
                "바다를 함께 건너며 이어지는 귀향로", "관리자와 과거 사건에 관한 광장 기록", "부모의 사건에 관한 후반 진실의 단서", "고향의 마지막 공동 장치와 귀향의 의미" };
            var text = $"[챕터 {_game.ChapterNumber} · 임시 기억 콘티] {themes[_game.ChapterNumber - 1]}.";
            if (!PrototypeSession.Journal.Contains(text)) PrototypeSession.Journal.Add(text);
            // A dedicated process has no personal account save. Each client persists the replicated journal.
            if (!_game.IsServerControlled) PrototypeSave.Remember(new[] { text });
            _game.SetStatus("목걸이에 남은 기억을 발견했습니다. " + PrototypeInput.Label("Journal") + ": 다시 읽기 (게임은 계속 진행)");
            PrototypeCues.Ping(transform.position);
        }
        public static string Compass(Vector3 forward, Vector3 toward)
        {
            forward.y = 0; toward.y = 0;
            if (toward.sqrMagnitude < .25f) return "주변";
            var angle = Vector3.SignedAngle(forward, toward, Vector3.up);
            if (Mathf.Abs(angle) < 25) return "↑ 앞";
            if (Mathf.Abs(angle) > 155) return "↓ 뒤";
            if (angle > 0) return angle < 65 ? "↗ 오른쪽 앞" : angle < 115 ? "→ 오른쪽" : "↘ 오른쪽 뒤";
            return angle > -65 ? "↖ 왼쪽 앞" : angle > -115 ? "← 왼쪽" : "↙ 왼쪽 뒤";
        }
    }
}
