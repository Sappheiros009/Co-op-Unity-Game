using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCoop.Prototype
{
    /// <summary>Temporary functional menu; final UI styling remains subject to user approval.</summary>
    public sealed class PrototypeLocalNetworkPanel : MonoBehaviour
    {
        private TMP_InputField _name,_port;
        private TextMeshProUGUI _status,_capacityLabel;
        private UnityEngine.UI.Button _create,_join,_capacity;
        private PrototypeLocalServerProcess _server;
        private PrototypeNetworkBootstrap _attempt;
        private Coroutine _launch;
        private bool _busy,_entered;
        private int _count=4;
        public bool Busy => _busy;
        public string Status => _status!=null?_status.text:"";
        public string ServerLogPath { get; private set; }="";
        public int CreatedServerProcessId { get; private set; }
        public static PrototypeLocalNetworkPanel Open(Transform owner)
        {
            var panel=PrototypeUi.Modal(owner,"같은 PC 멀티플레이 · 로컬 전용 서버");
            var controller=panel.gameObject.AddComponent<PrototypeLocalNetworkPanel>(); controller.Build(panel); return controller;
        }
        private void Build(RectTransform panel)
        {
            PrototypeUi.Text(panel,"Scope","첫 창: 방 만들기 → 다른 창: 같은 포트로 참가 (각 창은 서로 다른 이름)\n이름별 시험 기록을 저장합니다. 인터넷 / 다른 PC 연결은 지원하지 않습니다.",.04f,.13f,.92f,.14f,20);
            PrototypeUi.Text(panel,"NameLabel","이름",.05f,.30f,.16f,.08f,22);
            _name=PrototypeUi.Input(panel,"PlayerName",PrototypeLocalProfile.ActiveName.Length>0?PrototypeLocalProfile.ActiveName:"Slime",.22f,.30f,.71f,.08f,24);
            PrototypeUi.Text(panel,"PortLabel","포트",.05f,.41f,.16f,.08f,22);
            _port=PrototypeUi.Input(panel,"Port","7797",.22f,.41f,.26f,.08f,5);
            _port.contentType=TMP_InputField.ContentType.IntegerNumber;
            _capacity=PrototypeUi.Button(panel,"Capacity","만들 방 정원: 4명",.53f,.41f,.40f,.08f,()=>{ _count=_count==4?2:_count+1; _capacityLabel.text=$"만들 방 정원: {_count}명"; });
            _capacityLabel=_capacity.GetComponentInChildren<TextMeshProUGUI>();
            _status=PrototypeUi.Text(panel,"ConnectionStatus","127.0.0.1 전용 · 계정 인증 없는 개발 시험\n방장이 나가도 서버 유지 · 마지막 참가자가 나가면 약 5초 뒤 서버 종료",.05f,.53f,.90f,.17f,20,PrototypeUi.Accent);
            _create=PrototypeUi.Button(panel,"CreateLocalRoom","방 만들기",.05f,.73f,.42f,.095f,()=>Begin(true));
            _join=PrototypeUi.Button(panel,"JoinLocalRoom","참가",.53f,.73f,.40f,.095f,()=>Begin(false));
            PrototypeUi.Button(panel,"CancelLocalRoom","취소 / 로비로",.28f,.86f,.44f,.085f,Cancel);
        }
        private void Begin(bool create)
        {
            if(_busy) return;
            if(!PrototypeLocalConnection.TryCreate(_name.text,_port.text,_count,out var request,out var error)) { _status.text=error; return; }
            if(!PrototypeLocalProfile.Activate(request.Name,out error)) { _status.text=error; return; }
            SetBusy(true); _launch=StartCoroutine(Connect(request,create));
        }
        private IEnumerator Connect(PrototypeLocalConnection request, bool create)
        {
            // A cancelled NGO manager is destroyed at the end of the frame; don't race it on a retry.
            yield return null;
            if(create)
            {
                if(!PrototypeLocalServerProcess.TryStart(request,out _server,out var error)) { Fail(error); yield break; }
                ServerLogPath=_server.LogPath; CreatedServerProcessId=_server.ProcessId;
                _status.text="별도 전용 서버 시작 중…\n취소한 빈 서버는 시작 후 30초 안에 자동 종료됩니다.";
                var deadline=Time.realtimeSinceStartup+25;
                while(!_server.IsReady)
                {
                    if(_server.HasExited || Time.realtimeSinceStartup>=deadline)
                    { Fail("서버를 시작하지 못했습니다. 다른 포트를 사용하세요.\n서버 로그: "+ServerLogPath); yield break; }
                    yield return null;
                }
            }
            _status.text=$"127.0.0.1:{request.Port} 접속 중…\n서버의 참가 승인과 3D 대기방 상태를 기다립니다.";
            if(!PrototypeNetworkBootstrap.TryJoinFromLobby(request,_server?.InstanceId??"",Joined,Fail,out _attempt))
                Fail("이전 연결을 정리 중입니다. 잠시 뒤 다시 시도하세요.");
            _launch=null;
        }
        private void Joined() { _entered=true; _attempt=null; ReleaseServerHandle(); }
        private void Fail(string error)
        {
            _attempt=null; _launch=null; ReleaseServerHandle(); SetBusy(false); _status.text=error;
        }
        private void SetBusy(bool busy)
        {
            _busy=busy; _name.interactable=_port.interactable=_create.interactable=_join.interactable=_capacity.interactable=!busy;
        }
        public void Cancel()
        {
            if(_launch!=null) { StopCoroutine(_launch); _launch=null; }
            _attempt?.CancelJoin(); _attempt=null; ReleaseServerHandle(); PrototypeUi.CloseModal();
        }
        private void ReleaseServerHandle() { _server?.Dispose(); _server=null; }
        private void Update() { if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame) Cancel(); }
        private void OnDestroy()
        {
            if(!_entered) _attempt?.CancelJoin();
            ReleaseServerHandle(); // Never kill a separate server which another player may already have joined.
        }
    }
}
