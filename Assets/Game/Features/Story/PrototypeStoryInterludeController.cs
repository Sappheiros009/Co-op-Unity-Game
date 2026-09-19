using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if PROTOTYPE_VIDEO
using UnityEngine.Video;
#endif

namespace SlimeCoop.Prototype
{
    public sealed class PrototypeStoryInterludeController : MonoBehaviour
    {
        [SerializeField] private int completedChapter = 1;
#if PROTOTYPE_VIDEO
        [SerializeField] private VideoClip approvedVideo;
#endif
        [SerializeField, TextArea] private string approvedSubtitles;
#if PROTOTYPE_VIDEO
        private VideoPlayer _video;
#endif
        private RenderTexture _texture;
        private float _prepareDeadline;
        private TextMeshProUGUI _subtitle;
        public int CompletedChapter => completedChapter;
        public PrototypeChapterDefinition Chapter => PrototypeChapterCatalog.Get(completedChapter);
        public void ConfigureChapter(int value) { completedChapter = Mathf.Clamp(value, 1, 7); }
        private void Start()
        {
            BuildRoom(); PrototypeCapsulePlayer.SetCursor(false);
            var canvas = PrototypeUi.Canvas(transform);
            var panel = PrototypeUi.Panel(canvas, "StoryPanel", .1f,.07f,.8f,.86f);
            PrototypeUi.Text(panel,"Title",$"챕터 {completedChapter} 완료 · 이야기 인터루드",.04f,.025f,.92f,.09f,29,PrototypeUi.Accent);
            var screen = PrototypeUi.Panel(panel,"VideoArea",.04f,.15f,.92f,.52f,new Color(.02f,.025f,.04f));
            var imageRect = PrototypeUi.Rect(screen,"VideoImage",0,0,1,1);
            var image = imageRect.gameObject.AddComponent<UnityEngine.UI.RawImage>(); image.raycastTarget = false; image.enabled = false;
            var themes = new[] {
                "광산을 빠져나왔다. 귀향로 목걸이는 다음 길을 희미하게 가리킨다.",
                "뜨거운 길을 함께 건넜다. 귀향패에 남은 기억을 되짚는다.",
                "오염 너머의 단서. 고향의 시간이 멈췄다는 의문이 남는다.",
                "흔들리는 바다에서도 함께 길을 이어 간다.",
                "광장의 기록에서 관리자와 과거 사건의 흔적을 발견한다.",
                "얼어붙은 산을 넘어, 부모의 사건에 관한 후반 진실로 향한다.",
                "마침내 고향. 마지막 장치를 함께 작동하고 귀향의 의미를 선택한다."
            };
            var placeholder = PrototypeUi.Text(screen,"Storyboard","영상 삽입 위치\n\n" + themes[completedChapter - 1] + "\n\n임시 글 콘티 · 최종 대사/영상 미승인",.07f,.1f,.86f,.8f,26);
            placeholder.alignment = TextAlignmentOptions.Center;
            _subtitle = PrototypeUi.Text(panel,"Subtitle",PrototypeSession.LastResult,.04f,.69f,.92f,.1f,21);
#if PROTOTYPE_VIDEO
            if (approvedVideo != null)
            {
                _texture = new RenderTexture(1280,720,0); _texture.Create(); image.texture = _texture;
                _video = gameObject.AddComponent<VideoPlayer>(); _video.clip = approvedVideo;
                _video.renderMode = VideoRenderMode.RenderTexture; _video.targetTexture = _texture; _video.playOnAwake = false;
                _video.prepareCompleted += _ => { image.enabled = true; placeholder.gameObject.SetActive(false); _subtitle.text = approvedSubtitles; _video.Play(); };
                _video.errorReceived += (_, error) => { placeholder.gameObject.SetActive(true); _subtitle.text = "영상 재생 오류. 콘티로 계속할 수 있습니다."; };
                _prepareDeadline = Time.unscaledTime + 15; _video.Prepare();
            }
#endif
            PrototypeUi.Button(panel,"Continue","동료 전원 동의 시험 → 대기실",.2f,.85f,.6f,.095f,ReturnToWaitingRoom);
            if (completedChapter == 7)
            {
                PrototypeUi.Button(panel,"EndingA","분기 A 시험",.04f,.79f,.22f,.05f,()=>SelectEnding("prototype-choice-a"));
                PrototypeUi.Button(panel,"EndingB","분기 B 시험",.74f,.79f,.22f,.05f,()=>SelectEnding("prototype-choice-b"));
            }
        }
        private void Update()
        {
            if (!PrototypeUi.IsModalOpen && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
                ReturnToWaitingRoom();
#if PROTOTYPE_VIDEO
            if (_video != null && !_video.isPrepared && Time.unscaledTime > _prepareDeadline)
                _subtitle.text = "영상을 준비하지 못했습니다. 계속하기로 대기실에 돌아갈 수 있습니다.";
#endif
        }
        private void SelectEnding(string value)
        {
            PrototypeSave.Progress.endingChoice = value; PrototypeSave.WriteProgress();
            _subtitle.text = "임시 분기 선택 저장. 최종 엔딩 조건·서사는 별도 승인 대상입니다.";
        }
        public void ReturnToWaitingRoom()
        {
            // All simulated seats consent here. This is not a final 3/4-player network skip-vote policy.
            PrototypeSession.Record("스토리 동의 시험", "로컬 시험 참가자의 전원 동의 입력");
            SceneManager.LoadScene("PrototypeWaitingRoom");
        }
        private void OnDestroy() { if (_texture != null) { _texture.Release(); Destroy(_texture); } }
        public void BuildRoom()
        {
            if (transform.Find("StoryInterlude_2D_Presentation") != null) return;
            var root = new GameObject("StoryInterlude_2D_Presentation").transform; root.SetParent(transform,false);
            var camera = PrototypeVisuals.CreateOrthographicCamera("Story Interlude 2D Camera", new Vector3(0,0,-10),5.6f,Chapter.FogColor);
            camera.transform.SetParent(transform,true);
        }
    }
}
