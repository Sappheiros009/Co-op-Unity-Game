using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCoop.Prototype
{
    /// <summary>Walkable room presentation: local camera, server positions, no client physics authority.</summary>
    public sealed class PrototypeNetworkWaitingClient : MonoBehaviour
    {
        private readonly Dictionary<int,PrototypeWaitingRoomWalker> _walkers=new Dictionary<int,PrototypeWaitingRoomWalker>();
        private PrototypeNetworkTransport _network;
        private PrototypeNetworkClientWorld _world;
        private PrototypeNetworkSnapshot _snapshot;
        private Transform _room;
        private GameObject _hud;
        private PrototypeWaitingRoomStation[] _stations;
        private TextMeshProUGUI _state,_prompt,_notice,_readyState;
        private UnityEngine.UI.Button _ready,_start;
        private bool _readInput,_crouchToggle,_sprintToggle;
        private long _epoch,_sequence;
        private float _sendAt;
        private Vector2 _look;
        private PrototypePlayerInput _pending=PrototypePlayerInput.Neutral();
        private Action _leave;
        public PrototypeWaitingRoomWalker Walker => _snapshot!=null && _walkers.TryGetValue(_snapshot.yourSlot,out var walker) ? walker : null;
        public int ActorCount => _walkers.Count;
        public bool Visible => _room!=null && _room.gameObject.activeSelf;

        public void Configure(PrototypeNetworkTransport network,PrototypeNetworkClientWorld world,bool readInput,Action leave)
        {
            _network=network; _world=world; _readInput=readInput; _leave=leave;
            Initialize(); _network.SnapshotReceived+=ApplySnapshot; _network.WorldReceived+=OnWorld;
            PrototypeCapsulePlayer.SetCursor(false); PrototypeSettings.Apply(PrototypeSettings.Current,false);
        }
        private void Initialize()
        {
            if(_room!=null) return;
            _room=PrototypeWaitingRoomLayout.Build(transform,true); _stations=_room.GetComponentsInChildren<PrototypeWaitingRoomStation>();
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            _hud=new GameObject("Network waiting HUD"); _hud.transform.SetParent(transform,false);
            var canvas=PrototypeUi.Canvas(_hud.transform,"NetworkWaitingHUD");
            var guide=PrototypeUi.Panel(canvas,"Guide",.02f,.025f,.43f,.14f);
            PrototypeUi.Text(guide,"Title","슬라임 대기방 · 실제 참가자",.04f,.03f,.92f,.36f,25,PrototypeUi.Accent);
            PrototypeUi.Text(guide,"Flow","방장: 챕터 지점 선택 → 모두: 중앙 준비 장치\n화면 클릭: 조작 시작 · Esc: 설정 / 마우스 해제",.04f,.43f,.92f,.53f,17);
            var state=PrototypeUi.Panel(canvas,"RoomState",.65f,.025f,.33f,.28f);
            _state=PrototypeUi.Text(state,"Participants","연결 중…",.04f,.02f,.92f,.96f,17);
            var notice=PrototypeUi.Panel(canvas,"Notice",.02f,.19f,.55f,.1f);
            _notice=PrototypeUi.Text(notice,"Status","로컬 전용 서버 연결 중…",.035f,.02f,.93f,.96f,17,PrototypeUi.Accent);
            var prompt=PrototypeUi.Panel(canvas,"StationPrompt",.14f,.875f,.72f,.105f);
            _prompt=PrototypeUi.Text(prompt,"Prompt","",.03f,.02f,.94f,.94f,20); _prompt.alignment=TextAlignmentOptions.Center;
            var crosshair=PrototypeUi.Text(canvas,"Crosshair","+",.485f,.46f,.03f,.055f,26); crosshair.alignment=TextAlignmentOptions.Center;
            PrototypeUi.Button(canvas,"Leave","로비로",.88f,.93f,.10f,.05f,OpenLeave);
        }
        public void ApplySnapshot(PrototypeNetworkSnapshot snapshot)
        {
            if(snapshot==null || snapshot.phase!="WaitingRoom" || snapshot.waiting==null || !snapshot.waiting.IsValid()) return;
            Initialize(); var reset=_epoch!=snapshot.waitingEpoch;
            if(reset)
            {
                foreach(var walker in _walkers.Values) { walker.gameObject.SetActive(false); Destroy(walker.gameObject); }
                _walkers.Clear(); _epoch=snapshot.waitingEpoch; _sequence=0; _pending=PrototypePlayerInput.Neutral(); _look=Vector2.zero;
                _crouchToggle=_sprintToggle=false;
            }
            _snapshot=snapshot;
            var removed=new List<int>();
            foreach(var slot in _walkers.Keys) if(Array.Find(snapshot.waiting.actors,pose=>pose.slot==slot)==null) removed.Add(slot);
            foreach(var slot in removed) { _walkers[slot].gameObject.SetActive(false); Destroy(_walkers[slot].gameObject); _walkers.Remove(slot); }
            foreach(var pose in snapshot.waiting.actors)
            {
                if(_walkers.ContainsKey(pose.slot)) continue;
                var peer=Array.Find(snapshot.peers,p=>p.slot==pose.slot && p.connected); if(peer==null) continue;
                var obj=new GameObject("Waiting replica "+pose.slot); obj.transform.SetParent(_room,false);
                var walker=obj.AddComponent<PrototypeWaitingRoomWalker>(); walker.Configure(pose.slot,peer.name,PrototypePlayerControl.Replica,pose.position);
                walker.ApplyReplicaPose(pose.position,pose.yaw,pose.pitch,pose.crouching,pose.cameraHeight);
                if(pose.slot==snapshot.yourSlot) { walker.EnableReplicaView(); _look=new Vector2(pose.yaw,pose.pitch); }
                _walkers.Add(pose.slot,walker);
            }
            Refresh();
        }
        private void OnWorld(PrototypeNetworkWorldState state)
        {
            // After dismissing a wipe, the server can still broadcast its last Lobby result.
            // Do not repeatedly hide/reopen the waiting room and release its mouse on every packet.
            if(_world!=null && _world.ShowingWorld) SetVisible(false);
        }
        private void SetVisible(bool visible)
        {
            if(_room==null || _room.gameObject.activeSelf==visible) return;
            _room.gameObject.SetActive(visible); if(_hud!=null) _hud.SetActive(visible);
            if(visible) { PrototypeWaitingRoomLayout.ApplyLighting(); PrototypeCapsulePlayer.SetCursor(false); }
            else if(_readyState!=null) { PrototypeUi.CloseModal(); _readyState=null; }
        }
        private void Update()
        {
            if(_network!=null) SetVisible(_network.Snapshot?.phase=="WaitingRoom" && (_world==null || !_world.ShowingWorld));
            if(!Visible || Walker==null) return;
            if(_readInput) ReadInput();
            Refresh();
        }
        private void LateUpdate()
        {
            if(!Visible || _snapshot?.waiting==null) return;
            foreach(var pose in _snapshot.waiting.actors) if(_walkers.TryGetValue(pose.slot,out var walker))
            {
                var mine=pose.slot==_snapshot.yourSlot && _readInput;
                var position=Vector3.Lerp(walker.transform.position,pose.position,Mathf.Clamp01(Time.unscaledDeltaTime*18));
                walker.ApplyReplicaPose(position,mine?_look.x:pose.yaw,mine?_look.y:pose.pitch,pose.crouching,pose.cameraHeight);
                if(mine) walker.ViewCamera.fieldOfView=PrototypeSettings.Current.fieldOfView;
            }
            foreach(var station in _stations) station.FaceLabelToward(Walker?.ViewCamera);
        }
        private void ReadInput()
        {
            var mouse=Mouse.current;
            if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame && !PrototypeUi.IsModalOpen) PrototypeSettingsPanel.Open(transform);
            if(PrototypeInput.Pressed("Journal") && !PrototypeUi.IsModalOpen) PrototypeRecordsPanel.OpenJournal(transform);
            if(PrototypeUi.IsModalOpen) PrototypeCapsulePlayer.SetCursor(false);
            else if(mouse!=null && mouse.leftButton.wasPressedThisFrame) PrototypeCapsulePlayer.SetCursor(true);
            var frame=PrototypePlayerInput.Neutral(_look.x,_look.y);
            frame.hasControl=!PrototypeUi.IsModalOpen && Cursor.lockState==CursorLockMode.Locked;
            if(frame.hasControl)
            {
                if(mouse!=null) { var delta=mouse.delta.ReadValue()*PrototypeSettings.Current.sensitivity; _look.x=Mathf.Repeat(_look.x+delta.x,360); _look.y=Mathf.Clamp(_look.y-delta.y,-82,82); }
                frame.yaw=_look.x; frame.pitch=_look.y; Walker.SetLook(_look.x,_look.y);
                if(PrototypeInput.Pressed("Crouch")) _crouchToggle=!_crouchToggle;
                if(PrototypeInput.Pressed("Sprint")) _sprintToggle=!_sprintToggle;
                frame.motor.move=new Vector2((PrototypeInput.Held("Right")?1:0)-(PrototypeInput.Held("Left")?1:0),(PrototypeInput.Held("Forward")?1:0)-(PrototypeInput.Held("Backward")?1:0));
                frame.motor.jump=PrototypeInput.Pressed("Jump") || _pending.motor.jump;
                frame.motor.crouch=PrototypeSettings.Current.crouchToggle?_crouchToggle:PrototypeInput.Held("Crouch");
                frame.motor.sprint=PrototypeSettings.Current.sprintToggle?_sprintToggle:PrototypeInput.Held("Sprint");
            }
            _pending=frame;
            var interact=frame.hasControl && PrototypeInput.Pressed("Interact");
            if(Time.unscaledTime>=_sendAt || interact)
            {
                _sendAt=Time.unscaledTime+.05f;
                _network.SendWaitingInput(new PrototypeNetworkWaitingInput{epoch=_epoch,sequence=++_sequence,input=_pending}); _pending.ClearEdges();
            }
            if(interact) UseStation(FindStation());
        }
        public PrototypeWaitingRoomStation FindStation()
        {
            PrototypeWaitingRoomStation nearest=null; var best=float.PositiveInfinity;
            foreach(var station in _stations)
            {
                if(!station.CanUse(Walker,true)) continue;
                var distance=(station.transform.position-Walker.transform.position).sqrMagnitude;
                if(distance<best) { nearest=station; best=distance; }
            }
            return nearest;
        }
        private void UseStation(PrototypeWaitingRoomStation station)
        {
            if(station==null || _network==null || !_network.Connected || !station.CanUse(Walker,true)) return;
            switch(station.Kind)
            {
                case PrototypeWaitingStationKind.Chapter: _network.Send("chapter",station.Chapter); break;
                case PrototypeWaitingStationKind.Ready: OpenReadyAtStation(); break;
                case PrototypeWaitingStationKind.Records: PrototypeRecordsPanel.OpenRanking(transform); break;
                case PrototypeWaitingStationKind.Memories: PrototypeRecordsPanel.OpenJournal(transform); break;
                case PrototypeWaitingStationKind.Intro:
                    var intro=PrototypeUi.Modal(transform,"이야기 도입 · 임시 콘티");
                    PrototypeUi.Text(intro,"Intro","광산에서 시작해 고향으로 향하는 일곱 챕터의 모험.\n\n챕터가 끝나면 영상 삽입용 이야기 장면을 거쳐\n현재 접속자 전원의 동의 후 대기방으로 돌아옵니다.\n\n최종 영상·대사·외형은 사용자 승인 후 제작합니다.",.05f,.2f,.9f,.5f,24);
                    PrototypeUi.Button(intro,"Close","닫기",.3f,.83f,.4f,.1f,PrototypeUi.CloseModal); break;
                case PrototypeWaitingStationKind.Back: OpenLeave(); break;
            }
        }
        private void OpenLeave()
        {
            var leave=PrototypeUi.Modal(transform,"접속 종료 · 2D 로비로");
            PrototypeUi.Text(leave,"Notice","이 클라이언트의 연결만 종료합니다.\n다른 참가자의 서버는 계속 실행됩니다.\n2D 로비에서 같은 서버에 다시 참가할 수 있습니다.",.05f,.2f,.9f,.4f,24);
            PrototypeUi.Button(leave,"ConfirmLeave","연결 종료 후 로비로",.1f,.7f,.8f,.1f,()=>{PrototypeUi.CloseModal();_leave?.Invoke();});
            PrototypeUi.Button(leave,"Cancel","대기방에 머물기",.1f,.84f,.8f,.1f,PrototypeUi.CloseModal);
        }
        public bool OpenReadyAtStation()
        {
            if(!Visible || !ReadyInReach()) return false;
            OpenReady(); return true;
        }
        private void OpenReady()
        {
            var panel=PrototypeUi.Modal(transform,"중앙 준비 장치");
            _readyState=PrototypeUi.Text(panel,"State","",.05f,.15f,.9f,.43f,24);
            _ready=PrototypeUi.Button(panel,"Ready","준비 / 해제",.1f,.62f,.38f,.11f,()=>
            {
                var self=Array.Find(_network.Snapshot.peers,p=>p.slot==_network.Snapshot.yourSlot);
                if(self!=null && ReadyInReach()) _network.Send("ready",ready:!self.ready);
            });
            _start=PrototypeUi.Button(panel,"Start","선택한 챕터로 출발",.52f,.62f,.38f,.11f,()=>{if(ReadyInReach()) _network.Send("reserve");});
            PrototypeUi.Button(panel,"Close","대기방으로",.3f,.84f,.4f,.1f,PrototypeUi.CloseModal); Refresh();
        }
        private bool ReadyInReach()
        { return _network!=null && _network.Connected && _network.Snapshot?.phase=="WaitingRoom" && Array.Exists(_stations,s=>s.Kind==PrototypeWaitingStationKind.Ready && s.CanUse(Walker,true)); }
        private void Refresh()
        {
            if(_snapshot==null || _state==null) return;
            var text=new StringBuilder($"선택: 챕터 {_snapshot.chapter} · 방장 {_snapshot.owner+1}번\n"); var connected=0; var ready=0;
            foreach(var peer in _snapshot.peers) if(peer.connected)
            { connected++; if(peer.ready) ready++; text.AppendLine($"{(peer.slot==_snapshot.yourSlot?"나 · ":"")}{peer.name} · {(peer.ready?"준비 완료":"준비 중")}"); }
            _state.text=text.ToString();
            _notice.text=$"실제 접속 {connected}/{_snapshot.capacity} · 준비 {ready}/{connected}\n"+TranslateError(_network?.Snapshot?.lastError);
            if(_network!=null && !_network.Connected) _notice.text=_network.Status+"\n오른쪽 아래 로비로 버튼으로 나갈 수 있습니다.";
            var station=FindStation();
            _prompt.text=PrototypeUi.IsModalOpen?"창을 닫으면 대기방을 계속 이용할 수 있습니다.":Cursor.lockState!=CursorLockMode.Locked?$"화면 클릭: 조작 시작 · {PrototypeInput.Label("Forward")}{PrototypeInput.Label("Left")}{PrototypeInput.Label("Backward")}{PrototypeInput.Label("Right")} 이동 · 마우스 시점":
                station!=null?$"{PrototypeInput.Label("Interact")}  {station.Label.Replace('\n',' ')}":$"가까운 장치를 바라보고 {PrototypeInput.Label("Interact")} · Esc 설정";
            if(_readyState!=null && PrototypeUi.IsModalOpen)
            {
                _readyState.text=text+$"\n준비 {ready}/{connected} · 챕터 변경 시 준비 해제\n"+TranslateError(_network?.Snapshot?.lastError);
                _ready.interactable=ReadyInReach(); _start.interactable=_ready.interactable && _snapshot.owner==_snapshot.yourSlot && connected>=2 && ready==connected;
            }
        }
        private static string TranslateError(string error)
        {
            switch(error)
            {
                case "owner_only": return "챕터 선택·출발은 방장만 가능합니다.";
                case "party_not_ready": return "2명 이상 접속하고 전원이 준비해야 합니다.";
                case "station_out_of_reach": return "해당 장치 가까이에서 바라본 뒤 다시 이용하세요.";
                case "run_locked": return "출발 명단이 확정되었습니다.";
                case "invalid_sequence": return "중복 요청을 무시했습니다.";
                case null: case "": return "Steam / PlayFab 미연결 · 외형은 임시";
                default: return error;
            }
        }
        private void OnDestroy()
        { if(_network!=null) { _network.SnapshotReceived-=ApplySnapshot; _network.WorldReceived-=OnWorld; } }
    }
}
