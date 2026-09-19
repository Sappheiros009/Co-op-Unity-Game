using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCoop.Prototype
{
    public sealed class PrototypeSettingsPanel : MonoBehaviour
    {
        private PrototypeSettingsData _draft, _original;
        private RectTransform _panel, _body;
        private TextMeshProUGUI _message;
        private string _waitingFor;
        private string _previewSignature;
        private float _confirmUntil;
        private int _page;
        private static readonly string[] Actions = { "Forward", "Backward", "Left", "Right", "Jump", "Sprint", "Crouch", "Interact", "Use", "Drop", "Ping", "Necklace", "Assist", "Follow", "Journal" };
        private static readonly string[] Labels = { "앞", "뒤", "왼쪽", "오른쪽", "점프", "달리기", "앉기", "상호작용", "아이템 사용", "내려놓기", "핑", "목걸이", "협동 배치", "따라오기", "기록" };
        public static void Open(Transform owner)
        {
            var panel = PrototypeUi.Modal(owner, "설정 · 로컬 표현만 변경");
            panel.gameObject.AddComponent<PrototypeSettingsPanel>().Initialize(panel);
        }
        private void Initialize(RectTransform panel)
        {
            _panel = panel; _draft = PrototypeSettings.Copy(); _original = PrototypeSettings.Copy();
            PrototypeUi.Button(panel, "Graphics", "화면·품질", .03f, .12f, .28f, .075f, () => Draw(0));
            PrototypeUi.Button(panel, "Input", "조작·키 변경", .36f, .12f, .28f, .075f, () => Draw(1));
            PrototypeUi.Button(panel, "Audio", "소리·접근성", .69f, .12f, .28f, .075f, () => Draw(2));
            _message = PrototypeUi.Text(panel, "Message", PrototypeSettings.Error.Length > 0 ? PrototypeSettings.Error : "설정은 충돌·협동 조건·점수를 바꾸지 않습니다.", .03f, .79f, .94f, .095f, 17);
            PrototypeUi.Button(panel, "Apply", "적용 / 유지", .03f, .9f, .44f, .075f, Apply);
            PrototypeUi.Button(panel, "Revert", "취소 / 되돌리기", .53f, .9f, .44f, .075f, Revert);
            Draw(0);
        }
        private void Draw(int page)
        {
            _page = page;
            if (_body != null) { _body.gameObject.SetActive(false); Destroy(_body.gameObject); }
            _body = PrototypeUi.Rect(_panel, "SettingsPage", .03f, .22f, .94f, .56f);
            if (page == 0)
            {
                var options = new[] { "자동", "낮음", "중간", "높음", "사용자 지정" };
                for (var i = 0; i < options.Length; i++)
                {
                    var option = options[i];
                    PrototypeUi.Button(_body, "Quality_" + i, (_draft.quality == option ? "● " : "") + option, i * .2f, 0, .185f, .14f,
                        () => { _draft.quality = option; Changed(); Draw(0); });
                }
                Step("렌더 해상도", PrototypeSettings.ResolvedRenderScale(_draft).ToString("P0") + " (" + PrototypeSettings.ResolvedQuality(_draft) + ")", .22f,
                    () => ChangeRenderScale(-.1f), () => ChangeRenderScale(.1f));
                Step("시야각", _draft.fieldOfView.ToString("0"), .41f, () => _draft.fieldOfView -= 5, () => _draft.fieldOfView += 5);
                Step("최대 FPS", _draft.frameCap.ToString(), .6f, () => _draft.frameCap = 30, () => _draft.frameCap = _draft.frameCap == 30 ? 60 : 120);
                PrototypeUi.Button(_body, "VSync", "수직 동기화: " + (_draft.vsync ? "켜짐" : "꺼짐"), 0, .8f, .48f, .16f,
                    () => { _draft.vsync = !_draft.vsync; Changed(); Draw(0); });
                PrototypeUi.Text(_body, "QualityHint", "낮음도 위험 표시·장치·동료 표시는 유지합니다.", .52f, .79f, .48f, .18f, 17);
            }
            else if (page == 1)
            {
                for (var i = 0; i < Actions.Length; i++)
                {
                    var index = i; var binding = _draft.keys.Find(k => k.action == Actions[index]);
                    var key = binding == null ? PrototypeInput.Defaults[Actions[index]] : binding.key;
                    PrototypeUi.Button(_body, "Bind_" + Actions[index], Labels[index] + " : " + key, i / 8 * .51f, i % 8 * .12f, .48f, .1f,
                        () => { _waitingFor = Actions[index]; _message.text = Labels[index] + "에 사용할 키를 누르세요. Esc: 취소. 중복 키는 불가합니다."; });
                }
            }
            else
            {
                Step("전체 음량", _draft.masterVolume.ToString("P0"), 0, () => _draft.masterVolume -= .1f, () => _draft.masterVolume += .1f);
                Step("효과음", _draft.sfxVolume.ToString("P0"), .19f, () => _draft.sfxVolume -= .1f, () => _draft.sfxVolume += .1f);
                Step("마우스 감도", _draft.sensitivity.ToString("0.00"), .38f, () => _draft.sensitivity -= .02f, () => _draft.sensitivity += .02f);
                PrototypeUi.Button(_body, "CrouchMode", "앉기: " + (_draft.crouchToggle ? "전환" : "유지"), 0, .59f, .48f, .15f,
                    () => { _draft.crouchToggle = !_draft.crouchToggle; Changed(); Draw(2); });
                PrototypeUi.Button(_body, "SprintMode", "달리기: " + (_draft.sprintToggle ? "전환" : "유지"), .52f, .59f, .48f, .15f,
                    () => { _draft.sprintToggle = !_draft.sprintToggle; Changed(); Draw(2); });
                PrototypeUi.Text(_body, "Accessibility", "카메라 흔들림 0 / 모션 블러 없음\n위험은 문구와 색상을 함께 표시", 0, .8f, .5f, .2f, 17);
                var retry = PrototypeUi.Button(_body, "RetryProgress", "미저장 진행 재시도", .52f, .8f, .48f, .16f,
                    () => { _message.text = PrototypeSave.WriteProgress() ? "진행 저장 완료" : PrototypeSave.Error; });
                retry.interactable = PrototypeSave.HasPendingProgress && !PrototypeSave.ReadProtected;
            }
        }
        private void ChangeRenderScale(float delta)
        {
            _draft.renderScale = PrototypeSettings.ResolvedRenderScale(_draft) + delta;
            _draft.quality = "사용자 지정";
        }
        private void Step(string title, string value, float y, System.Action minus, System.Action plus)
        {
            PrototypeUi.Text(_body, title, title + "  " + value, 0, y, .68f, .14f, 22);
            PrototypeUi.Button(_body, title + "Minus", "−", .7f, y, .12f, .14f, () => { minus(); PrototypeSettings.Sanitize(_draft); Changed(); Draw(_page); });
            PrototypeUi.Button(_body, title + "Plus", "+", .86f, y, .12f, .14f, () => { plus(); PrototypeSettings.Sanitize(_draft); Changed(); Draw(_page); });
        }
        private void Update()
        {
            if (_waitingFor != null && Keyboard.current != null)
            {
                foreach (var key in Keyboard.current.allKeys)
                {
                    if (!key.wasPressedThisFrame) continue;
                    if (key.keyCode == Key.Escape) { _waitingFor = null; _message.text = "키 변경 취소"; break; }
                    if (PrototypeInput.Rebind(_draft, _waitingFor, key.keyCode))
                    { _waitingFor = null; Changed(); _message.text = "키 변경 준비 완료. 적용을 눌러 저장하세요."; Draw(1); }
                    else _message.text = "이미 사용 중이거나 예약된 키입니다. 다른 키를 선택하세요.";
                    break;
                }
            }
            if (_confirmUntil > 0)
            {
                _message.text = $"새 설정을 유지하려면 적용을 다시 누르세요. {Mathf.CeilToInt(_confirmUntil - Time.unscaledTime)}초 뒤 복원";
                if (Time.unscaledTime >= _confirmUntil) Revert();
            }
        }
        private void Apply()
        {
            _waitingFor = null;
            if (_confirmUntil > 0 && Time.unscaledTime >= _confirmUntil) { Revert(); return; }
            PrototypeSettings.Sanitize(_draft);
            var signature = JsonUtility.ToJson(_draft);
            if (_confirmUntil <= 0 || _previewSignature != signature)
            {
                PrototypeSettings.Apply(_draft, false); _previewSignature = signature;
                _confirmUntil = Time.unscaledTime + 15; return;
            }
            var saved = PrototypeSettings.Apply(_draft, true); _confirmUntil = 0; _previewSignature = null;
            if (!saved)
            {
                PrototypeSettings.Apply(_original, false);
                _message.text = "저장되지 않아 이전 설정으로 복원했습니다. " + PrototypeSettings.Error;
                return;
            }
            _original = PrototypeSettings.Copy(); PrototypeUi.CloseModal();
        }
        private void Changed()
        {
            if (_confirmUntil > 0) PrototypeSettings.Apply(_original, false);
            _confirmUntil = 0; _previewSignature = null;
            _message.text = "변경 사항을 미리 보려면 적용을 누르세요. 유지 확인 후에만 저장합니다.";
        }
        private void Revert() { _confirmUntil = 0; PrototypeSettings.Apply(_original, false); PrototypeUi.CloseModal(); }
        private void OnDisable() { if (_confirmUntil > 0) { _confirmUntil = 0; PrototypeSettings.Apply(_original, false); } }
    }
}
