using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SlimeCoop.Prototype
{
    public sealed class PrototypeWaitingRoomController : MonoBehaviour
    {
        private TextMeshProUGUI _state, _prompt, _notice;
        private readonly List<PrototypeWaitingRoomStation> _stations = new List<PrototypeWaitingRoomStation>();
        private string _message = "먼저 챕터 선택 지점으로 이동한 뒤 중앙 준비 장치에서 출발하세요.";
        public PrototypeWaitingRoomWalker Walker { get; private set; }
        public int SelectedChapter { get; private set; }
        public IReadOnlyList<PrototypeWaitingRoomStation> Stations => _stations;
        private void Start()
        {
            BuildRoom();
            foreach (var camera in GetComponentsInChildren<Camera>(true)) camera.gameObject.SetActive(false);
            var player = new GameObject("WaitingRoom_LocalSlime"); player.transform.SetParent(transform, false);
            Walker = player.AddComponent<PrototypeWaitingRoomWalker>(); Walker.Configure();
            _stations.AddRange(GetComponentsInChildren<PrototypeWaitingRoomStation>());
            foreach (var station in _stations) station.EnsureLabel();
            EnsureRuntimeCompanions();
            PrototypeCapsulePlayer.SetCursor(false); PrototypeSettings.Apply(PrototypeSettings.Current, false);
            RenderSettings.ambientLight = new Color(.42f,.48f,.53f); RenderSettings.fog = false;
            var canvas = PrototypeUi.Canvas(transform, "WaitingRoomHUD");
            var title = PrototypeUi.Panel(canvas, "WaitingRoomGuide", .02f,.025f,.43f,.14f);
            PrototypeUi.Text(title,"Title","슬라임 대기방 · 직접 걷는 3D 공간",.04f,.03f,.92f,.36f,25,PrototypeUi.Accent);
            PrototypeUi.Text(title,"Guide","1  챕터 선택 지점 → 2  중앙 준비 장치\n화면 클릭: 조작 시작 · Esc: 설정 / 마우스 해제",.04f,.43f,.92f,.53f,17);
            var statePanel = PrototypeUi.Panel(canvas,"WaitingRoomState",.66f,.025f,.32f,.16f);
            _state = PrototypeUi.Text(statePanel,"PartyAndSelection","",.04f,.04f,.92f,.9f,18);
            var noticePanel = PrototypeUi.Panel(canvas,"WaitingRoomNoticePanel",.02f,.19f,.56f,.10f);
            _notice = PrototypeUi.Text(noticePanel,"WaitingRoomNotice","",.035f,.04f,.93f,.92f,17,PrototypeUi.Accent);
            var bottom = PrototypeUi.Panel(canvas,"WaitingInteraction",.14f,.875f,.72f,.105f);
            _prompt = PrototypeUi.Text(bottom,"StationPrompt","",.03f,.02f,.94f,.94f,20);
            _prompt.alignment = TextAlignmentOptions.Center;
            var crosshair = PrototypeUi.Text(canvas,"WaitingCrosshair","+",.485f,.46f,.03f,.055f,26);
            crosshair.alignment = TextAlignmentOptions.Center;
            Refresh();
        }
        private void LateUpdate()
        {
            if (Walker == null) return;
            foreach (var station in _stations) station.FaceLabelToward(Walker.ViewCamera);
        }
        private void Update()
        {
            if (Walker == null || _prompt == null) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !PrototypeUi.IsModalOpen)
                PrototypeSettingsPanel.Open(transform);
            if (PrototypeInput.Pressed("Journal") && !PrototypeUi.IsModalOpen) PrototypeRecordsPanel.OpenJournal(transform);
            var nearest = FindUsableStation();
            _prompt.text = PrototypeUi.IsModalOpen ? "설정·기록을 닫으면 대기방을 계속 이용할 수 있습니다."
                : Cursor.lockState != CursorLockMode.Locked ? "화면을 클릭해 조작 시작 · WASD 이동 · 마우스 시점 · E 장치 사용"
                : nearest == null ? $"{PrototypeInput.Label("Forward")}{PrototypeInput.Label("Left")}{PrototypeInput.Label("Backward")}{PrototypeInput.Label("Right")} 이동 · {PrototypeInput.Label("Jump")} 점프 · 가까운 장치를 바라보고 {PrototypeInput.Label("Interact")}"
                : PrototypeInput.Label("Interact") + "  " + nearest.Label.Replace('\n',' ') + (nearest.Kind == PrototypeWaitingStationKind.Chapter ? " 선택" : " 사용");
            if (!PrototypeUi.IsModalOpen && Cursor.lockState == CursorLockMode.Locked && PrototypeInput.Pressed("Interact") && nearest != null)
                TryUseStation(nearest);
        }
        public PrototypeWaitingRoomStation FindUsableStation()
        {
            PrototypeWaitingRoomStation nearest = null; var distance = float.PositiveInfinity;
            foreach (var station in _stations)
            {
                if (!station.CanUse(Walker)) continue;
                var next = Vector3.SqrMagnitude(station.transform.position - Walker.transform.position);
                if (next < distance) { nearest = station; distance = next; }
            }
            return nearest;
        }
        public bool TryUseStation(PrototypeWaitingRoomStation station)
        {
            if (station == null || !_stations.Contains(station) || PrototypeUi.IsModalOpen || !station.CanUse(Walker)) return false;
            switch (station.Kind)
            {
                case PrototypeWaitingStationKind.Chapter:
                    if (!PrototypeSession.AllChaptersForTesting && station.Chapter > PrototypeSave.Progress.unlockedChapter)
                    { _message = "아직 해금되지 않은 챕터입니다. 시험 모드 장치에서 전체 챕터를 열 수 있습니다."; Refresh(); return false; }
                    SelectedChapter = station.Chapter;
                    _message = PrototypeChapterCatalog.Get(SelectedChapter).ChapterLabel + " 선택 완료. 중앙 준비 장치에서 출발하세요."; break;
                case PrototypeWaitingStationKind.Ready:
                    if (SelectedChapter == 0) { _message = "먼저 앞쪽의 챕터 선택 지점 하나를 선택하세요."; Refresh(); return false; }
                    LoadChapter(SelectedChapter); return true;
                case PrototypeWaitingStationKind.Party:
                    PrototypeSession.PartySize = PrototypeSession.PartySize >= 4 ? 2 : PrototypeSession.PartySize + 1;
                    _message = "로컬 시험 인원을 변경했습니다. 동료 AI 준비는 자동입니다."; break;
                case PrototypeWaitingStationKind.Specialty:
                    PrototypeSession.SelectedSpecialty = (PrototypeSpecialty)(((int)PrototypeSession.SelectedSpecialty + 1) % 3);
                    _message = "특기를 변경했습니다. 정찰·운반 특기의 세부 효과는 미확정입니다."; break;
                case PrototypeWaitingStationKind.TrialMode:
                    PrototypeSession.AllChaptersForTesting = !PrototypeSession.AllChaptersForTesting;
                    if (!PrototypeSession.AllChaptersForTesting && SelectedChapter > PrototypeSave.Progress.unlockedChapter) SelectedChapter = 0;
                    _message = "전체 챕터 시험 / 영구 해금 모드를 전환했습니다."; break;
                case PrototypeWaitingStationKind.Practice: StartPractice(); return true;
                case PrototypeWaitingStationKind.Records: PrototypeRecordsPanel.OpenRanking(transform); return true;
                case PrototypeWaitingStationKind.Memories: PrototypeRecordsPanel.OpenJournal(transform); return true;
                case PrototypeWaitingStationKind.Intro: ShowIntro(); return true;
                case PrototypeWaitingStationKind.Back: SceneManager.LoadScene("PrototypeLobby"); return true;
            }
            Refresh(); return true;
        }
        private void Refresh()
        {
            if (_state == null) return;
            var specialty = new[] { "힐러", "정찰", "운반" }[(int)PrototypeSession.SelectedSpecialty];
            _state.text = $"선택: {(SelectedChapter == 0 ? "챕터 미선택" : PrototypeChapterCatalog.Get(SelectedChapter).ChapterLabel)}\n" +
                $"나 + 시험 동료 {PrototypeSession.PartySize-1}명 · {specialty}\n전체 시험: {(PrototypeSession.AllChaptersForTesting ? "켜짐" : "꺼짐")} · 해금 {PrototypeSave.Progress.unlockedChapter}";
            _notice.text = "로컬 시험 · 동료 준비 자동 · 외형은 임시\n" + _message;
            var root = transform.Find("WaitingRoom_Walkable3D");
            for (var i = 1; i < 4; i++) root.Find("Waiting Slime " + (i+1))?.gameObject.SetActive(i < PrototypeSession.PartySize);
        }
        public void LoadChapter(int chapter)
        {
            if (chapter < 1 || chapter > 7 || (!PrototypeSession.AllChaptersForTesting && chapter > PrototypeSave.Progress.unlockedChapter)) return;
            PrototypeSession.Practice = false; PrototypeSession.BeginChapter(chapter);
            SceneManager.LoadScene(PrototypeChapterCatalog.Get(chapter).SceneName);
        }
        private void StartPractice()
        {
            PrototypeSession.Practice = true; PrototypeSession.BeginChapter(1);
            SceneManager.LoadScene(PrototypeChapterCatalog.Get(1).SceneName);
        }
        private void ShowIntro()
        {
            var panel = PrototypeUi.Modal(transform, "프롤로그 · 임시 글 콘티");
            PrototypeUi.Text(panel, "IntroText", "부모가 살던 도시를 떠난 슬라임들은 광산 사건의 흔적을 따라간다.\n\n처음에는 사고라고 믿었다. 목걸이가 가리키는 귀향로에는 아직 알지 못하는 기억이 남아 있다.\n\n함께 길을 열고, 귀향패 다섯 조각을 찾아 고향으로 돌아가자.\n\n이 문장은 기획의 방향을 확인하는 임시 콘티이며 최종 대사·영상은 아니다.", .05f,.18f,.9f,.58f,24);
            PrototypeUi.Button(panel, "Close", "대기실로", .3f,.83f,.4f,.1f, PrototypeUi.CloseModal);
        }
        public void BuildRoom()
        {
            // Compatibility with the previous generated scene until editor regeneration.
            transform.Find("WaitingRoom_Presentation")?.gameObject.SetActive(false);
            transform.Find("Waiting Room Camera")?.gameObject.SetActive(false);
            PrototypeWaitingRoomLayout.Build(transform);
        }
        private void EnsureRuntimeCompanions()
        {
            var root = transform.Find("WaitingRoom_Walkable3D");
            for (var i=1;i<4;i++)
            {
                var obj = root.Find("Waiting Slime "+(i+1)).gameObject;
                obj.transform.Find("Editor Slime Preview")?.gameObject.SetActive(false);
                if (obj.GetComponent<PrototypeParticipant>() != null) continue;
                var actor = obj.AddComponent<PrototypeParticipant>(); actor.Configure(i,"시험 동료 "+i);
                var parts = new GameObject("CharacterParts"); parts.transform.SetParent(obj.transform,false);
                parts.AddComponent<PrototypeSlimeBody>().Configure(actor,Color.HSVToRGB(.45f+i*.12f,.5f,1),false);
            }
        }
    }
}
