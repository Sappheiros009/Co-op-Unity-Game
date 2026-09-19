using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCoop.Prototype
{
    /// <summary>Client input and presentation only. Never advances puzzles, timers, health, inventory or score.</summary>
    public sealed class PrototypeNetworkClientWorld : MonoBehaviour
    {
        private PrototypeNetworkTransport _network;
        private GameObject _ui;
        private Camera _overlayCamera;
        private TextMeshProUGUI _objective,_team,_vitals,_inventory,_prompt,_status,_danger,_result,_consent,_controls;
        private GameObject _promptPanel;
        private UnityEngine.UI.Button _continue;
        private string _viewKey="",_runId="",_dismissedWipe="";
        private long _sequence;
        private bool _readInput,_crouchToggle,_sprintToggle;
        private float _sendAt;
        private PrototypePlayerInput _pending=PrototypePlayerInput.Neutral();
        public PrototypeNetworkReplica Replica { get; private set; }
        public PrototypeNetworkProgress Progress { get; } = new PrototypeNetworkProgress();
        public bool ShowingWorld => Replica!=null && Replica.State!=null && Replica.State.phase!="WaitingRoom"
            && !(Replica.State.phase=="Lobby" && _dismissedWipe==Replica.State.runId);
        public string Failure { get; private set; }="";

        public void Configure(PrototypeNetworkTransport network,bool readInput)
        {
            _network=network; _readInput=readInput;
            Replica=gameObject.AddComponent<PrototypeNetworkReplica>(); Replica.LocalLook=readInput;
            _network.WorldReceived+=OnWorld;
        }
        private void OnWorld(PrototypeNetworkWorldState state)
        {
            try
            {
                if(!Replica.Apply(state)) return;
                if(!Progress.Apply(state)) throw new InvalidOperationException("Conflicting server completion.");
                if(_runId!=state.runId) { _runId=state.runId; _sequence=0; _pending=PrototypePlayerInput.Neutral(); }
                var key=state.runId+":"+state.stage+":"+state.phase;
                if(_viewKey!=key)
                {
                    _viewKey=key;
                    // The server already reset its run. Also discard this client's map-building
                    // context before it can leave the network session and start a local run.
                    // Repeated Lobby packets must not clear state or release the cursor again.
                    if(state.phase=="Lobby") PrototypeSession.Wipe();
                    BuildView(state);
                }
                Refresh(state);
            }
            catch(Exception error)
            {
                Failure="서버와 화면 데이터가 일치하지 않습니다. 접속을 종료하고 같은 빌드로 다시 실행하세요.";
                Debug.LogError("[PrototypeNetwork] Client world rejected: "+error.Message);
                _network.Close();
            }
        }
        private void BuildView(PrototypeNetworkWorldState state)
        {
            PrototypeUi.CloseModal(); PrototypeCapsulePlayer.SetCursor(false);
            if(_ui!=null) { _ui.SetActive(false); Destroy(_ui); }
            if(_overlayCamera!=null) { _overlayCamera.gameObject.SetActive(false); Destroy(_overlayCamera.gameObject); }
            _objective=_team=_vitals=_inventory=_prompt=_status=_danger=_result=_consent=_controls=null; _continue=null; _promptPanel=null;
            if(!ShowingWorld) return;
            _ui=new GameObject("Network "+state.phase+" UI"); _ui.transform.SetParent(transform,false);
            var canvas=PrototypeUi.Canvas(_ui.transform,"NetworkWorldHUD");
            if(state.phase=="Playing")
            {
                var top=PrototypeUi.Panel(canvas,"ObjectivePanel",.018f,.025f,.4f,.23f);
                PrototypeUi.Text(top,"Stage",PrototypeChapterCatalog.Get(state.chapter).ChapterLabel+" / "+(state.stage==6?"BOSS":state.stage+" / 5"),.04f,.025f,.92f,.23f,22,PrototypeUi.Accent);
                _objective=PrototypeUi.Text(top,"Objective","",.04f,.28f,.92f,.69f,18);
                var team=PrototypeUi.Panel(canvas,"TeamPanel",.72f,.025f,.262f,.28f);
                _team=PrototypeUi.Text(team,"Team","",.04f,.035f,.92f,.93f,18);
                var bottom=PrototypeUi.Panel(canvas,"VitalsPanel",.018f,.77f,.38f,.21f);
                _vitals=PrototypeUi.Text(bottom,"Vitals","",.04f,.02f,.92f,.23f,21,PrototypeUi.Accent);
                _inventory=PrototypeUi.Text(bottom,"Inventory","",.04f,.28f,.92f,.69f,17);
                var prompt=PrototypeUi.Panel(canvas,"PromptPanel",.3f,.63f,.58f,.09f); _promptPanel=prompt.gameObject;
                _prompt=PrototypeUi.Text(prompt,"Prompt","",.03f,.05f,.94f,.9f,21); _prompt.alignment=TextAlignmentOptions.Center;
                var status=PrototypeUi.Panel(canvas,"ServerStatusPanel",.418f,.77f,.564f,.21f);
                _status=PrototypeUi.Text(status,"ServerStatus","",.025f,.03f,.95f,.56f,18);
                _danger=PrototypeUi.Text(canvas,"Danger","",.31f,.3f,.47f,.1f,24,Color.yellow); _danger.alignment=TextAlignmentOptions.Center;
                var cross=PrototypeUi.Text(canvas,"Crosshair","+",.485f,.465f,.03f,.055f,26); cross.alignment=TextAlignmentOptions.Center;
                _controls=PrototypeUi.Text(status,"Controls","",.025f,.63f,.95f,.35f,15);
            }
            else
            {
                _overlayCamera=PrototypeVisuals.CreateOrthographicCamera("Network result camera",new Vector3(0,0,-10),5.6f,new Color(.035f,.05f,.09f));
                _overlayCamera.transform.SetParent(_ui.transform,true);
                var panel=PrototypeUi.Panel(canvas,"ResultPanel",.1f,.07f,.8f,.86f);
                PrototypeUi.Text(panel,"Title",state.phase=="Story"?$"챕터 {state.chapter} 완료 · 이야기 인터루드":"전멸 · 2D 로비",.04f,.025f,.92f,.1f,29,PrototypeUi.Accent);
                _result=PrototypeUi.Text(panel,"Result","",.05f,.2f,.9f,.47f,25);
                _consent=PrototypeUi.Text(panel,"Consent","",.05f,.68f,.9f,.12f,21);
                _continue=PrototypeUi.Button(panel,"Continue",state.phase=="Story"?"계속하기 동의":"슬라임 대기방으로",.2f,.84f,.6f,.1f,()=>
                {
                    if(state.phase=="Story") ToggleStoryConsent();
                    else { _dismissedWipe=state.runId; BuildView(state); }
                });
            }
        }
        public void ToggleStoryConsent()
        {
            var room=_network.Snapshot;
            if(room==null || room.phase!="Story") return;
            foreach(var peer in room.peers) if(peer.slot==room.yourSlot) _network.Send("story_ready",ready:!peer.storyReady);
        }
        private void Refresh(PrototypeNetworkWorldState state)
        {
            if(!ShowingWorld) return;
            var self=state.actors[state.yourActor];
            if(_result!=null)
            {
                _result.text=state.phase=="Story"?$"영상 삽입 위치 · 임시 콘티\n\n챕터 {state.chapter}의 모험을 함께 마쳤습니다.\n팀 {state.score}점 · {state.elapsed:0.0}초 (시험 배점)\n\n최종 대사·영상은 승인 후 추가합니다.":state.status;
                RefreshConsent(); return;
            }
            _objective.text=state.objective; _vitals.text=$"체력 {self.health:0} / 스태미나 {self.stamina:0}";
            var text=new StringBuilder("실제 참가자 · 전용 서버\n");
            foreach(var actor in state.actors) text.AppendLine($"{(actor.id==state.yourActor?"나 · ":"")}{actor.name}: {(!actor.connected?"이탈":actor.exited?"탈출·안전":!actor.alive?"구조 대기":"HP "+actor.health.ToString("0"))}");
            text.Append($"출구 {state.arrivals}/{state.actors.Length} · {(state.settled?"집계 완료":state.arrivals>0?state.exitRemaining.ToString("0.0")+"초":"대기")}"); _team.text=text.ToString();
            text.Clear(); for(var i=0;i<3;i++) text.AppendLine($"{(self.selected==i?"▶":" ")} {i+1}. {(i<self.items.Length?PrototypeInventory.Label(self.items[i]):"빈 슬롯")}"); _inventory.text=text.ToString();
            _prompt.text=Cursor.lockState==CursorLockMode.Locked?FormatPrompt(self.prompt):"화면을 클릭하면 조작을 시작합니다.";
            if(self.rescue>0) _prompt.text+=$" {self.rescue:P0}";
            _promptPanel.SetActive(_prompt.text.Length>0);
            _status.text=$"팀 {state.score}점 · {state.elapsed:0.0}초\n"+(_network.Connected?state.status:_network.Status);
            _controls.text=$"{PrototypeInput.Label("Forward")}/{PrototypeInput.Label("Left")}/{PrototypeInput.Label("Backward")}/{PrototypeInput.Label("Right")} 이동 · {PrototypeInput.Label("Interact")} 상호작용 · {PrototypeInput.Label("Use")} 사용 · {PrototypeInput.Label("Drop")} 내려놓기\n{PrototypeInput.Label("Journal")} 기억 · {PrototypeInput.Label("Necklace")} 목걸이 · Esc 설정";
            _danger.text=!self.alive?"쓰러짐 · "+PrototypeInput.Label("Ping")+"로 구조 요청":state.bossDanger?"위험! 붉은 바닥을 벗어나세요":state.bossWarning?"공격 예고 · 가장자리로 대피":self.necklace?"목걸이 · "+PrototypeStoryMemory.Compass(Quaternion.Euler(0,Replica.Look.x,0)*Vector3.forward,self.guidance-self.position):"";
        }
        // The server sends action identities, never its own keyboard preferences.
        public static string FormatPrompt(string prompt)
        {
            var text=prompt??"";
            foreach(var action in PrototypeInput.Defaults.Keys) text=text.Replace("{key:"+action+"}",PrototypeInput.Label(action));
            return text;
        }
        private void RefreshConsent()
        {
            if(_consent==null || Replica.State.phase!="Story") return;
            var room=_network.Snapshot; var count=0; var ready=0; var mine=false;
            if(room!=null) foreach(var peer in room.peers)
                if(peer.connected) { count++; if(peer.storyReady) ready++; if(peer.slot==room.yourSlot) mine=peer.storyReady; }
            _consent.text=$"현재 접속자 동의 {ready}/{count} · 전원이 동의하면 함께 복귀합니다.";
            _continue.GetComponentInChildren<TextMeshProUGUI>().text=mine?"동의 철회":"계속하기 동의";
            _continue.interactable=_network.Connected && room!=null && room.phase=="Story";
        }
        private void Update()
        {
            if(Replica?.State==null) return;
            RefreshConsent();
            if(!_readInput || !ShowingWorld || Replica.State.phase!="Playing") return;
            var keys=Keyboard.current; var mouse=Mouse.current;
            if(keys!=null && keys.escapeKey.wasPressedThisFrame && !PrototypeUi.IsModalOpen) PrototypeSettingsPanel.Open(transform);
            if(PrototypeInput.Pressed("Journal") && !PrototypeUi.IsModalOpen) OpenJournal();
            if(PrototypeUi.IsModalOpen) PrototypeCapsulePlayer.SetCursor(false);
            else if(mouse!=null && mouse.leftButton.wasPressedThisFrame) PrototypeCapsulePlayer.SetCursor(true);
            var look=Replica.Look;
            var frame=PrototypePlayerInput.Neutral(look.x,look.y);
            frame.hasControl=!PrototypeUi.IsModalOpen && Cursor.lockState==CursorLockMode.Locked;
            if(frame.hasControl)
            {
                if(mouse!=null) { var delta=mouse.delta.ReadValue()*PrototypeSettings.Current.sensitivity; Replica.SetLook(look.x+delta.x,look.y-delta.y); }
                look=Replica.Look; frame.yaw=look.x; frame.pitch=look.y;
                if(PrototypeInput.Pressed("Crouch")) _crouchToggle=!_crouchToggle;
                if(PrototypeInput.Pressed("Sprint")) _sprintToggle=!_sprintToggle;
                frame.motor.move=new Vector2((PrototypeInput.Held("Right")?1:0)-(PrototypeInput.Held("Left")?1:0),(PrototypeInput.Held("Forward")?1:0)-(PrototypeInput.Held("Backward")?1:0));
                frame.motor.jump=PrototypeInput.Pressed("Jump"); frame.motor.climb=mouse!=null && mouse.leftButton.isPressed;
                frame.motor.crouch=PrototypeSettings.Current.crouchToggle?_crouchToggle:PrototypeInput.Held("Crouch");
                frame.motor.sprint=PrototypeSettings.Current.sprintToggle?_sprintToggle:PrototypeInput.Held("Sprint");
                frame.interaction=PrototypeInteraction.ReadInput(); frame.necklacePressed=PrototypeInput.Pressed("Necklace"); frame.pingPressed=PrototypeInput.Pressed("Ping");
            }
            Accumulate(frame);
            if(Time.unscaledTime>=_sendAt)
            {
                _sendAt=Time.unscaledTime+.05f;
                _network.SendPlayerInput(new PrototypeNetworkPlayerInput{runId=Replica.State.runId,stage=Replica.State.stage,sequence=++_sequence,input=_pending});
                _pending.ClearEdges();
            }
            Refresh(Replica.State);
        }
        private void Accumulate(PrototypePlayerInput frame)
        {
            if(frame.hasControl && _pending.hasControl)
            {
                frame.motor.jump|=_pending.motor.jump; frame.interaction.interactPressed|=_pending.interaction.interactPressed;
                frame.interaction.dropPressed|=_pending.interaction.dropPressed; frame.interaction.usePressed|=_pending.interaction.usePressed;
                if(frame.interaction.selectedSlot<0) frame.interaction.selectedSlot=_pending.interaction.selectedSlot;
                frame.necklacePressed|=_pending.necklacePressed; frame.pingPressed|=_pending.pingPressed;
            }
            _pending=frame;
        }
        private void OpenJournal()
        {
            var panel=PrototypeUi.Modal(transform,"이번 런에서 발견한 기억 · 서버 기록");
            PrototypeUi.Text(panel,"Journal",Replica.State.journal.Length==0?"아직 발견한 기억이 없습니다.":string.Join("\n\n",Replica.State.journal),.04f,.15f,.92f,.67f,20);
            PrototypeUi.Button(panel,"Close","닫기",.3f,.86f,.4f,.1f,PrototypeUi.CloseModal);
        }
        private void OnDestroy() { if(_network!=null) _network.WorldReceived-=OnWorld; }
    }
}
