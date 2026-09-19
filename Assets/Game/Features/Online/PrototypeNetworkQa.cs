using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Opt-in separate-process protocol checks. Never runs in ordinary play; no gameplay authority bypass.</summary>
    public sealed partial class PrototypeNetworkQa : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string runTag,role,status="RUNNING",startedUtc,finishedUtc,transportStatus,capture,graphicsDevice;
            public int processId,capacity,acceptedCommands,rejectedCommands,rejectedJoins,acceptedInputs,rejectedInputs;
            public int acceptedWaitingInputs,rejectedWaitingInputs;
            public int reportWriteRetries;
            public List<string> checks=new List<string>(),errors=new List<string>();
            public PrototypeNetworkSnapshot snapshot;
            public PrototypeNetworkWorldState world;
            public PrototypeProgress progress;
            public string saveRoot;
            public bool serverPlacementFixture;
            public string wipedRunId, retryRunId;
            public int scoreBeforeWipe;
        }
        private readonly Report _report=new Report();
        private PrototypeNetworkTransport _network;
        private string _file,_role,_serverId,_runId;
        private float _deadline,_nextWrite,_fullAt=-1,_reservedAt=-1,_handoffAt=-1,_quitAt=-1;
        private double _reservedSeconds;
        private int _step;
        private bool _sawHandoff,_captureStarted,_captureDone,_sawFull;
        private PrototypeNetworkCommand _replay;
        private Vector3[] _worldStart;
        private float _worldStarted=-1,_inputAt;
        private long _inputSequence;
        private bool _worldMovementVerified;
        private float _waitingSendAt;
        private long _waitingSequence,_waitingStopSequence;
        private int _waitingTarget=-1;
        private bool _waitingGuardChecked,_chapterRequested,_chapterConfirmed;
        private bool _finishing;
        private bool _readyCaptureStarted,_readyCaptureDone;
        private float _writeFailedAt=-1;

        public void Configure(PrototypeNetworkTransport network,string role,string output,string runTag,int capacity,string capture="")
        {
            _network=network;_role=role;_file=Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(_file));
            _report.role=role;_report.runTag=runTag;_report.capacity=capacity;
            _report.capture=capture;_report.graphicsDevice=SystemInfo.graphicsDeviceName;
            _report.processId=System.Diagnostics.Process.GetCurrentProcess().Id;
            _report.startedUtc=DateTime.UtcNow.ToString("o");_deadline=Time.realtimeSinceStartup+50;
            if (role=="lifecycle-server" || role=="lifecycle-party" || role=="wipe-server" || role=="wipe-party")
            {
                if (string.IsNullOrEmpty(PrototypeSave.RootOverride) || !Guid.TryParseExact(runTag,"N",out _))
                    throw new ArgumentException("Lifecycle/wipe fixture requires an isolated save root and explicit QA run tag.");
                _report.serverPlacementFixture=true; _deadline=Time.realtimeSinceStartup+180;
            }
            Application.logMessageReceived+=OnLog;
        }
        private void OnLog(string message,string stack,LogType kind)
        {if((kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)&&_report.errors.Count<12)_report.errors.Add(message+"\n"+stack);}
        private void Check(string text){if(!_report.checks.Contains(text))_report.checks.Add(text);}
        private void Update()
        {
            if(_network==null)return;
            if(_quitAt>=0){if(Time.realtimeSinceStartup>=_quitAt)Application.Quit(_report.status=="PASSED"?0:2);return;}
            if(_finishing) { if(Write()) CloseAfterReport(); return; }
            var state=_network.Snapshot;
            if(state!=null && _serverId==null)_serverId=state.serverId;
            if(state!=null && state.serverId!=_serverId){Fail("server_identity_changed");return;}
            if(_role=="server")Server(state);
            else if(_role=="party")Party(state);
            else if(_role=="guard-server"||_role=="survivor"||_role=="flood")Guard(state);
            else if(_role=="world-server"||_role=="world-party")World(state);
            else if(_role=="lifecycle-server"||_role=="lifecycle-party")Lifecycle(state);
            else if(_role=="wipe-server"||_role=="wipe-party")WipeAndRetry(state);
            else if(_role=="version_mismatch"||_role=="room_full"||_role=="run_locked")
            {
                if(!_network.Connected&&_network.Status.Contains(_role)){Check("join_rejected:"+_role);Finish();}
            }
            if(Time.realtimeSinceStartup>=_deadline){Fail("process_watchdog_timeout; step="+_step);return;}
            if(Time.realtimeSinceStartup>=_nextWrite){_nextWrite=Time.realtimeSinceStartup+.2f;Write();}
        }
        private void World(PrototypeNetworkSnapshot room)
        {
            if(room==null)return;
            var connected=0; foreach(var peer in room.peers) if(peer.connected)connected++;
            if(connected==_report.capacity)_sawFull=true;
            // The last disconnect can legitimately end the simulation before this component's Update.
            if(_role=="world-server" && _sawFull && _worldMovementVerified && connected==0)
            {
                if(_network.AcceptedInputs<10){Fail("server_did_not_accept_player_input");return;}
                Check("real_client_inputs_consumed_by_server"); Finish(); return;
            }
            if(_role=="world-party" && room.phase=="WaitingRoom" && connected==_report.capacity)
            {
                if(room.yourSlot==room.owner && !_chapterConfirmed)
                {
                    if(!_chapterRequested && WalkToStation(room,1)) { _network.Send("chapter",1); _chapterRequested=true; }
                    if(_chapterRequested && room.lastSequence>=1 && room.lastError=="") _chapterConfirmed=true;
                    return;
                }
                // Owner selecting a chapter clears ready flags, so other peers wait for its acknowledged choice.
                if(room.yourSlot!=room.owner && Array.Find(room.peers,p=>p.slot==room.owner)?.lastSequence<1) return;
                if(_step==0 && WalkToStation(room,0)) { _network.Send("ready",ready:true); _step=1; }
                else if(room.yourSlot==room.owner && _step==1)
                { foreach(var peer in room.peers) if(!peer.ready)return; _network.Send("reserve"); _step=2; }
                return;
            }
            var world=_network.WorldState;
            if(world==null || world.phase!="Playing")return;
            if(world.actors.Length!=_report.capacity || room.startingCount!=_report.capacity || world.runId!=room.runId)
            {Fail("world_roster_or_run_differs");return;}
            if(_worldStart==null)
            {
                _worldStart=new Vector3[world.actors.Length]; for(var i=0;i<_worldStart.Length;i++)_worldStart[i]=world.actors[i].position;
                _worldStarted=Time.realtimeSinceStartup; Check("server_chapter_received");
            }
            if(_role=="world-party")
            {
                if(world.yourActor<0 || world.actors[world.yourActor].slot!=room.yourSlot){Fail("client_actor_binding_wrong");return;}
                var presentation=GetComponent<PrototypeNetworkClientWorld>();
                if(presentation==null || presentation.Replica.Game==null || presentation.Replica.Game.Teammates.Count!=0)
                {Fail("client_replica_missing_or_has_bots");return;}
                if(world.yourActor==1 && Time.realtimeSinceStartup<_worldStarted+1.6f && Time.realtimeSinceStartup>=_inputAt)
                {
                    _inputAt=Time.realtimeSinceStartup+.05f;
                    var input=PrototypePlayerInput.Neutral(); input.hasControl=true; input.motor.move=Vector2.up;
                    _network.SendPlayerInput(new PrototypeNetworkPlayerInput {runId=world.runId,stage=world.stage,sequence=++_inputSequence,input=input});
                }
            }
            if(Time.realtimeSinceStartup-_worldStarted<2.5f)return;
            if(!_worldMovementVerified)
            {
                if(world.actors[1].position.z<_worldStart[1].z+2) {Fail("bound_actor_did_not_move");return;}
                for(var i=0;i<_worldStart.Length;i++) if(i!=1 && Vector2.Distance(new Vector2(_worldStart[i].x,_worldStart[i].z),new Vector2(world.actors[i].position.x,world.actors[i].position.z))>.1f)
                {Fail("another_actor_moved_without_input");return;}
                if(world.simulationTick<20 || world.score!=0 || world.arrivals!=0){Fail("unexpected_world_clock_or_settlement");return;}
                _worldMovementVerified=true; Check("only_bound_actor_moved"); Check("server_ticks_without_client_score_authority");
            }
            if(_role=="world-party")
            {
                if(_report.capture.Length>0 && !_captureStarted)
                {
                    _captureStarted=true; StartCoroutine(PrototypeFrameCapture.Save(_report.capture,error=>
                    {if(error.Length>0)_report.errors.Add(error);else Check("rendered_network_chapter");_captureDone=true;}));
                }
                Finish();
            }
        }
        private void Guard(PrototypeNetworkSnapshot state)
        {
            if(_role=="flood"&&!_network.Connected&&_network.Status.Contains("rate_limit"))
            {Check("burst_client_disconnected");Finish();return;}
            if(state==null)return;
            var connected=0;foreach(var peer in state.peers)if(peer.connected)connected++;
            if(connected==2){_sawFull=true;if(_fullAt<0)_fullAt=Time.realtimeSinceStartup;}
            if(_role=="guard-server")
            {
                if(_sawFull&&_network.RejectedCommands==1&&connected==0){Check("only_burst_peer_rejected");Finish();}
            }
            else if(_role=="flood"&&_step==0&&_fullAt>=0&&Time.realtimeSinceStartup-_fullAt>1 && WalkToStation(state,0))
            {
                for(var i=1;i<=60;i++)_network.SendCommand(new PrototypeNetworkCommand{sequence=i,kind="ready",ready=true});
                _step=1;
            }
            else if(_role=="survivor"&&_sawFull&&connected==1)
            {
                if(!_network.Connected||state.owner!=state.yourSlot){Fail("legitimate_peer_or_owner_affected");return;}
                if(_handoffAt<0){_handoffAt=Time.realtimeSinceStartup;_reservedSeconds=state.serverSeconds;}
                if(Time.realtimeSinceStartup-_handoffAt>1&&state.serverSeconds>_reservedSeconds)
                {Check("legitimate_peer_and_server_continued");Finish();}
            }
        }
        private void Server(PrototypeNetworkSnapshot state)
        {
            if(state==null)return;
            var connected=0;foreach(var p in state.peers)if(p.connected)connected++;
            if(connected==_report.capacity)Check("full_party_connected");
            if(_sawHandoff&&connected==0)
            {
                var world=_network.WorldState;
                if(world==null||world.runId!=_runId||world.actors.Length!=_report.capacity){Fail("final_world_roster_changed");return;}
                Check("fixed_roster_retained_after_all_disconnect");Finish();return;
            }
            if(state.phase=="Reserved")
            {
                if(_runId==null)_runId=state.runId;
                if(state.runId!=_runId||state.startingCount!=_report.capacity){Fail("server_roster_or_run_changed");return;}
                if(state.owner>0){_sawHandoff=true;Check("owner_handoff_without_server_restart");}
            }
        }
        private void Party(PrototypeNetworkSnapshot state)
        {
            if(state==null)return;
            if(!_network.Connected){Fail("unexpected_disconnect");return;}
            Check("received_authoritative_slot");
            if(state.phase=="Reserved")
            {
                if(_reservedAt<0)
                {
                    _reservedAt=Time.realtimeSinceStartup;_reservedSeconds=state.serverSeconds;_runId=state.runId;
                    Check("all_ready_and_roster_reserved");
                }
                if(state.startingCount!=_report.capacity||state.peers.Length!=_report.capacity||state.runId!=_runId||state.chapter!=3)
                {Fail("reserved_state_changed");return;}
                if(state.yourSlot==0)
                {
                    if(Time.realtimeSinceStartup-_reservedAt>2){Check("original_owner_left_voluntarily");Finish();}
                }
                else if(state.owner!=0 && state.serverSeconds>_reservedSeconds)
                {
                    if(_handoffAt<0)_handoffAt=Time.realtimeSinceStartup;
                    if(Time.realtimeSinceStartup-_handoffAt>.6f)
                    {
                        if(state.peers[0].connected){Fail("departed_owner_still_connected");return;}
                        Check("handoff_kept_server_run_roster_and_clock");Finish();
                    }
                }
                return;
            }
            var connected=0;foreach(var p in state.peers)if(p.connected)connected++;
            if(connected!=_report.capacity)return;
            Check("full_party_visible");
            if(_report.capture.Length>0&&!_captureStarted)
            {
                _captureStarted=true;
                StartCoroutine(PrototypeFrameCapture.Save(_report.capture,error=>
                {if(error.Length>0)_report.errors.Add(error);else Check("rendered_waiting_room");_captureDone=true;}));
            }
            if(state.yourSlot==0)
            {
                if(_fullAt<0)_fullAt=Time.realtimeSinceStartup;
                if(!_waitingGuardChecked)
                {
                    if(_step==0) { _network.Send("ready",ready:true); _step=-1; }
                    else if(state.lastError=="station_out_of_reach") { _waitingGuardChecked=true; _step=0; Check("remote_ready_rejected_by_server_distance"); }
                    return;
                }
                // Gives the external harness a bounded window to exercise capacity denial.
                if(_step==0 && WalkToStation(state,3) && Time.realtimeSinceStartup-_fullAt>=8){_network.Send("chapter",3);_step=1;}
                else if(_step==1&&state.chapter==3 && WalkToStation(state,0)){_network.Send("ready",ready:true);_step=2;}
                else if(_step==2)
                {
                    foreach(var peer in state.peers)if(!peer.ready)return;
                    if(_report.capture.Length>0)
                    {
                        if(!_readyCaptureStarted)
                        {
                            if(!GetComponent<PrototypeNetworkWaitingClient>().OpenReadyAtStation()) return;
                            _readyCaptureStarted=true;
                            StartCoroutine(PrototypeFrameCapture.Save(Path.Combine(Path.GetDirectoryName(_report.capture),"waiting-ready.png"),error=>
                            { if(error.Length>0)_report.errors.Add(error); else Check("rendered_nearby_ready_device"); _readyCaptureDone=true; }));
                        }
                        if(!_readyCaptureDone) return;
                        PrototypeUi.CloseModal();
                    }
                    _network.Send("reserve");_step=3;
                }
            }
            else if(state.chapter==3)
            {
                if(_step==0)
                {
                    _replay=new PrototypeNetworkCommand{sequence=state.lastSequence+1,kind="chapter",chapter=7};
                    _network.SendCommand(_replay);_step=1;
                }
                else if(_step==1&&state.lastError=="owner_only")
                {Check("non_owner_request_rejected");_network.SendCommand(_replay);_step=2;}
                else if(_step==2&&state.lastError=="invalid_sequence")
                {Check("duplicate_request_rejected"); if(WalkToStation(state,0)){_network.Send("ready",ready:true);_step=3;}}
            }
        }
        private bool WalkToStation(PrototypeNetworkSnapshot state,int chapter)
        {
            if(state.waiting==null) return false;
            var pose=Array.Find(state.waiting.actors,p=>p.slot==state.yourSlot); if(pose==null) return false;
            if(_waitingTarget!=chapter) { _waitingTarget=chapter; _waitingStopSequence=0; }
            var station=chapter>0?PrototypeWaitingRoomLayout.ChapterPosition(chapter):PrototypeWaitingRoomLayout.ReadyPosition;
            var offsets=new[]{Vector3.back*2.2f,new Vector3(-2.2f,0,-.5f),new Vector3(2.2f,0,-.5f),Vector3.forward*2.2f};
            var goal=station+(chapter>0?Vector3.back*2.2f:offsets[state.yourSlot]);
            var moveGoal=goal;
            // Slot 4 approaches the back of the ready device around its solid cube, never through it.
            if(chapter==0 && state.yourSlot==3 && pose.position.z<3.7f) moveGoal=new Vector3(3,0,4);
            var delta=moveGoal-pose.position; delta.y=0;
            var distance=new Vector2(goal.x-pose.position.x,goal.z-pose.position.z).magnitude;
            var rotation=Quaternion.LookRotation(station+Vector3.up*.65f-(pose.position+Vector3.up*pose.cameraHeight));
            var yaw=rotation.eulerAngles.y; var pitch=Mathf.Clamp(Mathf.DeltaAngle(0,rotation.eulerAngles.x),-82,82);
            if(Time.realtimeSinceStartup>=_waitingSendAt)
            {
                _waitingSendAt=Time.realtimeSinceStartup+.05f;
                var input=PrototypePlayerInput.Neutral(yaw,pitch); input.hasControl=true;
                if(distance>.22f)
                {
                    var local=Quaternion.Inverse(Quaternion.Euler(0,yaw,0))*delta.normalized;
                    input.motor.move=new Vector2(local.x,local.z)*Mathf.Clamp01(delta.magnitude/.6f);
                }
                _network.SendWaitingInput(new PrototypeNetworkWaitingInput{epoch=state.waitingEpoch,sequence=++_waitingSequence,input=input});
                if(distance<=.22f && _waitingStopSequence==0) _waitingStopSequence=_waitingSequence;
            }
            if(distance>.35f || _waitingStopSequence==0 || pose.acknowledged<_waitingStopSequence) return false;
            var presentation=GetComponent<PrototypeNetworkWaitingClient>();
            if(presentation==null || presentation.ActorCount!=state.waiting.actors.Length || presentation.Walker==null
                || presentation.Walker.Participant.ParticipantId!=state.yourSlot || presentation.Walker.GetComponent<CharacterController>().enabled)
            { Fail("waiting_replica_binding_or_physics_wrong"); return false; }
            Check(chapter>0?"walked_to_chapter_station":"walked_to_ready_station"); Check("waiting_replica_bound_without_client_physics"); return true;
        }
        private bool Write()
        {
            _report.snapshot=_network.Snapshot;_report.transportStatus=_network.Status;
            _report.acceptedCommands=_network.AcceptedCommands;_report.rejectedCommands=_network.RejectedCommands;_report.rejectedJoins=_network.RejectedJoins;
            _report.world=_network.WorldState;_report.acceptedInputs=_network.AcceptedInputs;_report.rejectedInputs=_network.RejectedInputs;
            _report.acceptedWaitingInputs=_network.AcceptedWaitingInputs;_report.rejectedWaitingInputs=_network.RejectedWaitingInputs;
            if (_report.serverPlacementFixture)
            {
                _report.saveRoot=PrototypeSave.Root;
                if (_role=="lifecycle-party" || _role=="wipe-party") _report.progress=PrototypeSave.Progress;
            }
            try
            {
                var temporary=_file+".tmp"; File.WriteAllText(temporary,JsonUtility.ToJson(_report,true));
                if(File.Exists(_file)) File.Replace(temporary,_file,null); else File.Move(temporary,_file);
                _writeFailedAt=-1; return true;
            }
            catch(IOException error)
            {
                // A reader or scanner may briefly hold the destination. Never turn an unwritten final result into success.
                _report.reportWriteRetries++;
                if(_writeFailedAt<0) _writeFailedAt=Time.realtimeSinceStartup;
                if(Time.realtimeSinceStartup-_writeFailedAt>=2)
                { Debug.LogError("[PrototypeNetworkQa] Report remained unwritable: "+error.Message); Application.Quit(2); }
                return false;
            }
        }
        private void Fail(string error){_report.errors.Add(error);Finish();}
        private void Finish()
        {
            if(_quitAt>=0)return;
            if(_captureStarted&&!_captureDone&&_report.errors.Count==0&&Time.realtimeSinceStartup<_deadline)return;
            _report.status=_report.errors.Count==0?"PASSED":"FAILED";_report.finishedUtc=DateTime.UtcNow.ToString("o");
            _finishing=true; if(Write()) CloseAfterReport();
        }
        private void CloseAfterReport() { _network.Close(); _quitAt=Time.realtimeSinceStartup+.5f; }
        private void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
