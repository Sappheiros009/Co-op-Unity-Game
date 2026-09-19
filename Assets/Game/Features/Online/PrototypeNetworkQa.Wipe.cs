using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SlimeCoop.Prototype
{
    // Opt-in QA: stage 1 uses the existing server placement fixture. In stage 2 the
    // server places actors beside the death route; actual client intents walk into it.
    // This is a transition test, not proof of an input-driven complete course.
    public sealed partial class PrototypeNetworkQa
    {
        private bool _wipePrepared, _wipeDismissed, _wipeSavedFixture, _wipeRetryChecked;
        private bool _wipeReturnCaptureStarted, _wipeReturnCaptureDone;
        private bool _wipeRetryCaptureStarted, _wipeRetryCaptureDone;
        private float _wipeAt=-1, _wipeAloneAt=-1, _wipeReturnedAt=-1, _wipeRetryAt=-1;
        private Vector3 _wipeRetryOrigin;
        private string _wipePermanent, _wipeSettings;

        private void WipeAndRetry(PrototypeNetworkSnapshot room)
        {
            if(room==null) return;
            var connected=room.peers.Count(p=>p.connected);
            var world=_network.WorldState;
            if(connected==_report.capacity) _sawFull=true;
            if(world!=null && string.IsNullOrEmpty(_report.wipedRunId)) _report.wipedRunId=world.runId;
            if(_role=="wipe-server") { WipeServer(room,world,connected); return; }
            if(!_network.Connected) { Fail("wipe_unexpected_disconnect"); return; }
            if(!_wipeSavedFixture)
            {
                // Seed an unrelated permanent completion in this isolated QA profile only.
                if(!PrototypeSave.ApplyCompletion(new PrototypeChapterCompletion {runId=Guid.NewGuid().ToString("N"),
                    chapter=3,participants=4,score=321,seconds=45,scoreRuleVersion="wipe-fixture",gameBuildVersion="prototype-v2"}))
                { Fail("wipe_permanent_fixture_rejected"); return; }
                var settings=PrototypeSettings.Copy(); settings.sensitivity=.13f; settings.frameCap=30;
                if(!PrototypeSettings.Apply(settings,true)) { Fail("wipe_settings_fixture_not_saved"); return; }
                _wipeSettings=JsonUtility.ToJson(PrototypeSettings.Current); _wipeSavedFixture=true;
            }
            if(world==null && room.phase=="WaitingRoom" && connected==_report.capacity)
            { WipeStartAtStation(room,true); return; }
            if(world==null) return;
            if(world.runId!=_report.wipedRunId)
            { WipeRetryClient(room,world); return; }
            var presentation=GetComponent<PrototypeNetworkClientWorld>();
            if(presentation==null || presentation.Failure.Length>0) { Fail("wipe_client_view_failed"); return; }
            if(world.phase=="Playing" && world.stage==2)
            {
                if(_report.scoreBeforeWipe==0)
                {
                    if(world.score<=0 || world.journal.Length!=1 || PrototypeSave.Progress.memories.Count!=1)
                    { Fail("wipe_missing_stage_one_score_or_memory"); return; }
                    _report.scoreBeforeWipe=world.score;
                    _wipePermanent=JsonUtility.ToJson(PrototypeSave.Progress);
                    Check("stage_one_score_and_permanent_memory_observed");
                }
                var alive=world.actors.Count(a=>a.alive && a.connected);
                if(alive==1)
                {
                    if(_wipeAloneAt<0) _wipeAloneAt=Time.realtimeSinceStartup;
                    if(Time.realtimeSinceStartup-_wipeAloneAt>2) Check("role_shortage_did_not_auto_wipe");
                }
                var self=world.actors[world.yourActor];
                // Last player remains alive for a full three seconds, then takes the same death route.
                var mayWalk=self.id!=_report.capacity-1 || (_wipeAloneAt>=0 && Time.realtimeSinceStartup-_wipeAloneAt>3);
                if(self.alive && self.position.z>43 && mayWalk && Time.realtimeSinceStartup>=_inputAt)
                {
                    _inputAt=Time.realtimeSinceStartup+.05f;
                    var input=PrototypePlayerInput.Neutral(); input.hasControl=true;
                    input.motor.move=self.position.z<49.5f?Vector2.up:Vector2.zero;
                    _network.SendPlayerInput(new PrototypeNetworkPlayerInput{runId=world.runId,stage=2,sequence=++_inputSequence,input=input});
                }
                return;
            }
            if(world.phase!="Lobby" || room.phase!="WaitingRoom" || room.waiting==null) return;
            if(_wipeAt<0)
            {
                if(!_report.checks.Contains("role_shortage_did_not_auto_wipe") || world.actors.Any(a=>a.alive)
                    || world.score!=0 || world.elapsed!=0 || world.journal.Length!=0 || world.chapterCompleted
                    || PrototypeSession.RunScore!=0 || PrototypeSession.Stage!=1 || PrototypeSession.Chapter!=0
                    || PrototypeSession.Journal.Count!=0 || room.waitingEpoch!=2 || room.startingCount!=0
                    || !string.IsNullOrEmpty(room.runId) || room.peers.Any(p=>p.ready || p.storyReady))
                { Fail("wipe_state_or_ready_flags_not_reset"); return; }
                if(!presentation.ShowingWorld || presentation.Progress.AppliedCompletions!=0)
                { Fail("wipe_skipped_lobby_or_awarded_completion"); return; }
                var cameras=GetComponentsInChildren<Camera>().Where(c=>c.isActiveAndEnabled).ToArray();
                if(cameras.Length!=1 || !cameras[0].orthographic) { Fail("wipe_lobby_not_two_dimensional"); return; }
                if(!WipeSaveUnchanged()) { Fail("wipe_changed_permanent_progress_or_settings"); return; }
                Check("actual_hazard_wipe_reset_server_and_client"); Check("two_dimensional_wipe_lobby");
                _wipeAt=Time.realtimeSinceStartup;
                if(_report.capture.Length>0)
                {
                    _captureStarted=true;
                    StartCoroutine(PrototypeFrameCapture.Save(_report.capture,error=>
                    { if(error.Length>0) _report.errors.Add(error); else Check("rendered_wipe_lobby"); _captureDone=true; }));
                }
            }
            if(!_wipeDismissed)
            {
                if(Time.realtimeSinceStartup-_wipeAt<1 || (_captureStarted && !_captureDone)) return;
                var button=GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="Continue");
                if(button==null || !button.interactable) { Fail("wipe_return_button_missing"); return; }
                button.onClick.Invoke(); _wipeDismissed=true;
                _step=0; _waitingTarget=-1; _waitingStopSequence=0; _waitingSequence=0;
                if(room.yourSlot==0)
                    _network.SendWaitingInput(new PrototypeNetworkWaitingInput{epoch=1,sequence=999,input=PrototypePlayerInput.Neutral()});
                return;
            }
            var waiting=GetComponent<PrototypeNetworkWaitingClient>();
            if(waiting==null || !waiting.Visible || waiting.Walker==null) return;
            if(_wipeReturnedAt<0) _wipeReturnedAt=Time.realtimeSinceStartup;
            if(Time.realtimeSinceStartup-_wipeReturnedAt<.5f) return;
            if(presentation.ShowingWorld || room.chapter!=1 || waiting.Walker.ViewCamera.orthographic)
            { Fail("wipe_returned_to_wrong_room_or_chapter"); return; }
            Check("three_dimensional_waiting_room_returned");
            if(_report.capture.Length>0 && !_wipeReturnCaptureStarted)
            {
                _wipeReturnCaptureStarted=true;
                StartCoroutine(PrototypeFrameCapture.Save(Path.Combine(Path.GetDirectoryName(_report.capture),"wipe-returned-waiting.png"),error=>
                { if(error.Length>0) _report.errors.Add(error); else Check("rendered_wipe_returned_waiting"); _wipeReturnCaptureDone=true; }));
            }
            if(Time.realtimeSinceStartup-_wipeReturnedAt<1.5f || (_wipeReturnCaptureStarted && !_wipeReturnCaptureDone)) return;
            WipeStartAtStation(room,false);
        }
        private void WipeStartAtStation(PrototypeNetworkSnapshot room,bool selectChapter)
        {
            if(selectChapter && room.yourSlot==room.owner && !_chapterConfirmed)
            {
                if(!_chapterRequested && WalkToStation(room,1)) { _network.Send("chapter",1); _chapterRequested=true; }
                if(_chapterRequested && room.lastSequence>=1 && room.lastError=="") _chapterConfirmed=true;
                return;
            }
            if(selectChapter && room.yourSlot!=room.owner && Array.Find(room.peers,p=>p.slot==room.owner)?.lastSequence<1) return;
            if(_step==0 && WalkToStation(room,0)) { _network.Send("ready",ready:true); _step=1; }
            else if(room.yourSlot==room.owner && _step==1 && room.peers.All(p=>p.ready))
            { _network.Send("reserve"); _step=2; }
        }
        private void WipeServer(PrototypeNetworkSnapshot room,PrototypeNetworkWorldState world,int connected)
        {
            var authority=GetComponent<PrototypeNetworkWorld>();
            if(authority==null || world==null) return;
            if(world.runId!=_report.wipedRunId)
            {
                // Room snapshots and world packets have independent publication intervals.
                if(!_wipeRetryChecked && room.runId!=world.runId) return;
                if(!_wipeRetryChecked)
                {
                    if(_wipeAt<0 || world.phase!="Playing" || world.stage!=1 || world.chapter!=1 || world.score!=0
                        || world.arrivals!=0 || world.journal.Length!=0 || world.actors.Length!=_report.capacity
                        || world.actors.Any(a=>!a.alive || a.health!=100 || a.items.Length!=0) || room.startingCount!=_report.capacity)
                    { Fail("server_retry_did_not_start_clean"); return; }
                    _report.retryRunId=world.runId; _wipeRetryChecked=true; Check("new_run_same_chapter_from_stage_one");
                }
                if(_sawFull && connected==0)
                {
                    if(_network.RejectedInputs<1 || _network.RejectedWaitingInputs<1 || _network.RejectedCommands!=0)
                    { Fail("stale_inputs_not_rejected_or_valid_retry_commands_failed"); return; }
                    if(File.Exists(Path.Combine(PrototypeSave.Root,"progress.json"))) { Fail("wipe_server_wrote_personal_save"); return; }
                    Check("old_run_and_waiting_epoch_inputs_rejected"); Check("dedicated_server_has_no_personal_save"); Finish();
                }
                return;
            }
            if(world.phase=="Playing")
            {
                var game=authority.ServerGame;
                if(game==null || game.Exit==null) return;
                if(game.StageNumber==1) { DriveLifecycleFixture(game); return; }
                if(game.StageNumber==2 && !_wipePrepared)
                {
                    if(PrototypeSession.RunScore<=0) { Fail("server_wipe_missing_earned_score"); return; }
                    _report.scoreBeforeWipe=PrototypeSession.RunScore;
                    foreach(var player in game.ControlledPlayers) player.Teleport(new Vector3(8.6f,.15f,44));
                    Physics.SyncTransforms(); _wipePrepared=true; Check("placed_beside_fatal_route_client_input_required");
                }
            }
            if(world.phase=="Lobby" && _wipeAt<0)
            {
                if(room.phase!="WaitingRoom") return;
                if(!_wipePrepared || world.score!=0 || world.elapsed!=0 || world.journal.Length!=0 || world.chapterCompleted
                    || world.actors.Any(a=>a.alive) || PrototypeSession.Stage!=1 || PrototypeSession.Chapter!=0
                    || room.waitingEpoch!=2 || room.startingCount!=0 || room.peers.Any(p=>p.ready || p.storyReady))
                { Fail("server_wipe_not_reset"); return; }
                _wipeAt=Time.realtimeSinceStartup; Check("actual_hazard_wipe_reset_server_and_client");
            }
        }
        private bool WipeSaveUnchanged()
        {
            if(PrototypeSave.HasPendingProgress || PrototypeSave.Error.Length>0 || PrototypeSettings.Error.Length>0) return false;
            PrototypeSave.Reload(); PrototypeSettings.Reload();
            return _wipePermanent==JsonUtility.ToJson(PrototypeSave.Progress) && _wipeSettings==JsonUtility.ToJson(PrototypeSettings.Current);
        }
        private void WipeRetryClient(PrototypeNetworkSnapshot room,PrototypeNetworkWorldState world)
        {
            if(world.phase!="Playing") return;
            if(_wipeRetryAt<0)
            {
                if(!_wipeDismissed || world.stage!=1 || world.chapter!=1 || world.score!=0 || world.arrivals!=0 || world.journal.Length!=0
                    || world.actors.Length!=_report.capacity || world.actors.Any(a=>!a.alive || a.health!=100 || a.items.Length!=0))
                { Fail("client_retry_did_not_start_clean"); return; }
                _report.retryRunId=world.runId; _wipeRetryAt=Time.realtimeSinceStartup; _inputSequence=0;
                _wipeRetryOrigin=world.actors[0].position; Check("new_run_same_chapter_from_stage_one");
                if(room.yourSlot==0)
                {
                    var stale=PrototypePlayerInput.Neutral(); stale.hasControl=true; stale.motor.move=Vector2.right;
                    _network.SendPlayerInput(new PrototypeNetworkPlayerInput{runId=_report.wipedRunId,stage=2,sequence=999,input=stale});
                }
            }
            if(world.yourActor==0 && Time.realtimeSinceStartup-_wipeRetryAt<1.5f && Time.realtimeSinceStartup>=_inputAt)
            {
                _inputAt=Time.realtimeSinceStartup+.05f;
                var input=PrototypePlayerInput.Neutral(); input.hasControl=true; input.motor.move=Vector2.up;
                _network.SendPlayerInput(new PrototypeNetworkPlayerInput{runId=world.runId,stage=1,sequence=++_inputSequence,input=input});
            }
            if(Time.realtimeSinceStartup-_wipeRetryAt<2) return;
            var view=GetComponent<PrototypeNetworkClientWorld>();
            if(world.actors[0].position.z<_wipeRetryOrigin.z+2 || Mathf.Abs(world.actors[0].position.x-_wipeRetryOrigin.x)>.1f
                || view==null || view.Failure.Length>0 || !view.ShowingWorld || view.Replica.ViewCamera.orthographic
                || view.Progress.AppliedCompletions!=0 || world.score!=0 || !WipeSaveUnchanged())
            { Fail("retry_input_presentation_or_preserved_save_failed"); return; }
            Check("retry_accepts_fresh_input_without_stale_sideways_move"); Check("permanent_progress_records_memories_settings_preserved");
            if(_report.capture.Length>0 && !_wipeRetryCaptureStarted)
            {
                _wipeRetryCaptureStarted=true;
                StartCoroutine(PrototypeFrameCapture.Save(Path.Combine(Path.GetDirectoryName(_report.capture),"wipe-retry-chapter.png"),error=>
                { if(error.Length>0) _report.errors.Add(error); else Check("rendered_wipe_retry_chapter"); _wipeRetryCaptureDone=true; }));
            }
            if(_wipeRetryCaptureStarted && !_wipeRetryCaptureDone) return;
            Finish();
        }
    }
}
