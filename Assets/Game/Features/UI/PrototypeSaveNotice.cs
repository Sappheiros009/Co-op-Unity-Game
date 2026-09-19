using TMPro;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Persistent across each scene's HUD lifetime, not across save roots. No automatic data reset.</summary>
    public sealed class PrototypeSaveNotice : MonoBehaviour
    {
        private RectTransform _panel;
        private TextMeshProUGUI _label;
        private UnityEngine.UI.Button _retry;
        private float _nextRefresh;
        private void Start()
        {
            _ = PrototypeSave.Progress; // Surface a read failure even before the first completion/save.
            _panel = PrototypeUi.Panel(transform, "SaveNotice", .24f, .006f, .52f, .12f, new Color(.18f, .09f, .025f, .97f));
            _label = PrototypeUi.Text(_panel, "SaveStatus", "", .025f, .05f, .73f, .9f, 16, Color.yellow);
            _retry = PrototypeUi.Button(_panel, "RetrySave", "저장 재시도", .77f, .2f, .21f, .6f, () => { PrototypeSave.WriteProgress(); Refresh(); });
            Refresh();
        }
        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .2f; Refresh();
        }
        private void Refresh()
        {
            if (_panel == null) return;
            var progressError = PrototypeSave.Error.Length > 0;
            _panel.gameObject.SetActive(progressError || PrototypeSettings.Error.Length > 0);
            _retry.gameObject.SetActive(progressError && !PrototypeSave.ReadProtected && PrototypeSave.HasPendingProgress);
            _label.text = progressError
                ? PrototypeSave.ReadProtected ? "진행 파일 보호 중 · 이번 진행은 저장되지 않습니다.\n손상/버전을 확인한 뒤 다시 실행하세요. 원본은 유지됩니다."
                    : "진행 미저장 · 디스크 공간/쓰기 권한을 확인하세요.\nEsc → 소리·접근성에서 재시도 · 종료하면 미저장 진행 소실"
                : "설정 미저장 · 설정창에서 원인을 확인하세요.\n진행 파일과 설정 파일은 별도로 보호됩니다.";
        }
    }
}
