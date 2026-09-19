using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SlimeCoop.Prototype
{
    public sealed class PrototypeLobbyController : MonoBehaviour
    {
        private TMPro.TextMeshProUGUI _lastResult;
        private void Start()
        {
            BuildRoom(); PrototypeCapsulePlayer.SetCursor(false);
            PrototypeSettings.Apply(PrototypeSettings.Current, false);
            var canvas = PrototypeUi.Canvas(transform);
            var panel = PrototypeUi.Panel(canvas, "LobbyPanel", .24f, .12f, .52f, .76f);
            PrototypeUi.Text(panel, "Title", "귀향하는 슬라임", .06f, .07f, .88f, .13f, 40, PrototypeUi.Accent);
            PrototypeUi.Text(panel, "Subtitle", "협동 모험 · 기능 검증 프로토타입", .06f, .21f, .88f, .08f, 23);
            PrototypeUi.Text(panel, "Scope", "로컬 시험 동료 또는 같은 PC의 실제 참가자\nPlayFab / Steam 온라인은 미연결\n외형·UI·효과음은 승인 전 임시 표현", .06f, .32f, .88f, .18f, 20);
            PrototypeUi.Button(panel, "StartGame", "게임 시작 · 시험 동료와 3D 대기방", .06f, .53f, .88f, .105f, StartGame);
            PrototypeUi.Button(panel, "LocalMultiplayer", "같은 PC 멀티플레이 · 만들기 / 참가", .06f, .66f, .88f, .105f, () => PrototypeLocalNetworkPanel.Open(transform));
            PrototypeUi.Button(panel, "Settings", "설정", .06f, .80f, .42f, .1f, () => PrototypeSettingsPanel.Open(transform));
            PrototypeUi.Button(panel, "Quit", "종료", .52f, .80f, .42f, .1f, () => Application.Quit());
            _lastResult=PrototypeUi.Text(canvas, "LastResult", "", .05f, .9f, .9f, .07f, 18); RefreshProfile();
        }
        private void Update()
        {
            RefreshProfile();
            if (!PrototypeUi.IsModalOpen && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) StartGame();
        }
        private void RefreshProfile()
        {
            if(_lastResult==null) return;
            var profile=PrototypeLocalProfile.ActiveName;
            var text=(profile.Length>0?"현재 시험 저장: "+profile+" · ":"")+PrototypeSession.LastResult;
            if(_lastResult.text!=text) _lastResult.text=text;
        }
        public void StartGame() { SceneManager.LoadScene("PrototypeWaitingRoom"); }
        public void BuildRoom()
        {
            if (transform.Find("Lobby_2D_Presentation") != null) return;
            var root = new GameObject("Lobby_2D_Presentation").transform; root.SetParent(transform, false);
            var camera = PrototypeVisuals.CreateOrthographicCamera("Lobby 2D Camera", new Vector3(0, 0, -10), 5.6f, new Color(.035f,.05f,.12f));
            camera.transform.SetParent(transform, true);
            PrototypeVisuals.CreateSprite("Lobby Background", root, Vector3.zero, new Vector2(24, 14), new Color(.035f,.05f,.12f), -20);
        }
    }
}
