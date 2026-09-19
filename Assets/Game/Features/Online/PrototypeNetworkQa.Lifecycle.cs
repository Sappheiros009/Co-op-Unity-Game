using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    // Explicit process-local QA fixture only: no RPC exposes actor placement or completion overrides.
    // Exercises real objectives/settlement/transport/storage, NOT traversal or human/full-course play.
    public sealed partial class PrototypeNetworkQa
    {
        private int _lifePreparedStage, _lifeSeenStages, _lifeVoteStep;
        private float _lifeVoteAt=-1, _lifeReturnedAt=-1, _lifeMemoryAt=-1;
        private bool _lifeSawFullVote, _lifeSawWithdraw, _lifeReturnCaptureStarted, _lifeReturnCaptureDone;
        private string _lifeRunId;

        private void Lifecycle(PrototypeNetworkSnapshot room)
        {
            if (room==null) return;
            var connected=0; foreach (var peer in room.peers) if (peer.connected) connected++;
            if (connected==_report.capacity) _sawFull=true;
            var world=_network.WorldState;
            if (world!=null && _lifeRunId==null) _lifeRunId=world.runId;
            if (world!=null && world.runId!=_lifeRunId) { Fail("lifecycle_run_changed"); return; }
            if (_role=="lifecycle-server")
            {
                var authority=GetComponent<PrototypeNetworkWorld>();
                if (authority!=null && authority.Phase=="Playing") DriveLifecycleFixture(authority.ServerGame);
                if (world!=null && world.chapterCompleted) Check("server_completion_frozen");
                if (world!=null && world.phase=="WaitingRoom" && world.chapterCompleted)
                {
                    if (room.waitingEpoch!=2 || room.startingCount!=0 || world.completion.participants!=_report.capacity)
                    { Fail("returned_room_or_completion_roster_wrong"); return; }
                    Check("same_server_returned_to_new_waiting_epoch");
                    if (_sawFull && connected==0)
                    {
                        if (File.Exists(Path.Combine(PrototypeSave.Root,"progress.json"))) { Fail("dedicated_server_wrote_personal_progress"); return; }
                        Check("dedicated_server_has_no_personal_save"); Finish();
                    }
                }
                return;
            }
            if (!_network.Connected) { Fail("lifecycle_unexpected_disconnect"); return; }
            if (_lifeRunId==null && room.phase=="WaitingRoom" && connected==_report.capacity)
            {
                if (room.yourSlot==room.owner && !_chapterConfirmed)
                {
                    if (!_chapterRequested && WalkToStation(room,1)) { _network.Send("chapter",1); _chapterRequested=true; }
                    if (_chapterRequested && room.lastSequence>=1 && room.lastError=="") _chapterConfirmed=true;
                    return;
                }
                if (room.yourSlot!=room.owner && Array.Find(room.peers,p=>p.slot==room.owner)?.lastSequence<1) return;
                if (_step==0 && WalkToStation(room,0)) { _network.Send("ready",ready:true); _step=1; }
                else if (room.yourSlot==room.owner && _step==1)
                { foreach (var peer in room.peers) if (!peer.ready) return; _network.Send("reserve"); _step=2; }
                return;
            }
            if (world==null) return;
            if (world.actors.Length!=_report.capacity || world.yourActor<0 || world.actors[world.yourActor].slot!=room.yourSlot)
            { Fail("lifecycle_actor_binding_changed"); return; }
            _lifeSeenStages |= 1 << (world.stage-1);
            if (world.stage==2 && world.arrivals==1 && !world.settled && world.exitRemaining>0)
                Check("one_arrival_waited_for_five_second_window");
            var presentation=GetComponent<PrototypeNetworkClientWorld>();
            if (presentation==null || presentation.Failure.Length>0) { Fail("lifecycle_client_view_failed"); return; }
            if (world.phase=="Story" && world.chapterCompleted && room.phase=="Story")
            {
                if (_lifeSeenStages!=63 || presentation.Progress.AppliedCompletions!=1 || !ValidateLifecycleSave(world,false))
                { Fail("lifecycle_missing_stage_or_completion_save"); return; }
                Check("all_six_fixture_stages_received"); Check("server_result_saved_once");
                LifecycleVote(room); return;
            }
            if (world.phase!="WaitingRoom" || !world.chapterCompleted || room.phase!="WaitingRoom") return;
            if (_lifeReturnedAt<0)
            {
                if (!_lifeSawWithdraw || room.waitingEpoch!=2 || room.startingCount!=0 || !string.IsNullOrEmpty(room.runId))
                { Fail("lifecycle_return_skipped_vote_or_room_reset"); return; }
                if (connected!=(_report.capacity==4?3:_report.capacity)) { Fail("story_disconnect_vote_count_wrong"); return; }
                Check("withdrawal_delayed_unanimous_return"); Check("same_server_returned_to_new_waiting_epoch");
                if (_report.capacity==4) Check("disconnected_voter_excluded_without_shrinking_completion");
                _lifeReturnedAt=Time.realtimeSinceStartup;
            }
            var waiting=GetComponent<PrototypeNetworkWaitingClient>();
            if (waiting==null || !waiting.Visible || waiting.Walker==null) return;
            if (_report.capture.Length>0 && !_lifeReturnCaptureStarted && Time.realtimeSinceStartup-_lifeReturnedAt>.5f)
            {
                _lifeReturnCaptureStarted=true;
                StartCoroutine(PrototypeFrameCapture.Save(Path.Combine(Path.GetDirectoryName(_report.capture),"returned-waiting-room.png"),error=>
                { if (error.Length>0) _report.errors.Add(error); else Check("rendered_returned_waiting_room"); _lifeReturnCaptureDone=true; }));
            }
            if (Time.realtimeSinceStartup-_lifeReturnedAt<2 || (_lifeReturnCaptureStarted && !_lifeReturnCaptureDone)) return;
            if (presentation.Progress.AppliedCompletions!=1 || !ValidateLifecycleSave(world,true))
            { Fail("completion_not_durable_or_duplicated_on_return"); return; }
            Check("local_progress_reloaded_after_return"); Finish();
        }
        private bool ValidateLifecycleSave(PrototypeNetworkWorldState world,bool reload)
        {
            if (PrototypeSave.HasPendingProgress || PrototypeSave.Error.Length>0) return false;
            if (reload) PrototypeSave.Reload();
            var data=PrototypeSave.Progress;
            if (data.completedMask!=1 || data.unlockedChapter!=2 || data.fragmentMask!=1 || data.memories.Count!=1
                || data.records.Count!=(_report.capacity==4?1:0) || !data.achievements.Contains("first-chapter")) return false;
            if (data.records.Count==0) return true;
            var record=data.records[0]; var receipt=world.completion;
            return record.runId==receipt.runId && record.participants==4 && record.score==receipt.score
                && record.seconds==receipt.seconds && record.scoreRuleVersion==receipt.scoreRuleVersion;
        }
        private void LifecycleVote(PrototypeNetworkSnapshot room)
        {
            var ready=0; PrototypeNetworkPeer self=null;
            foreach (var peer in room.peers) { if (peer.connected && peer.storyReady) ready++; if (peer.slot==room.yourSlot) self=peer; }
            if (self==null) { Fail("story_self_missing"); return; }
            if (ready==_report.capacity-1) _lifeSawFullVote=true;
            if (_lifeSawFullVote && ready==_report.capacity-2) _lifeSawWithdraw=true;
            if (_report.capture.Length>0 && ready==_report.capacity-1 && !_captureStarted)
            {
                _captureStarted=true; StartCoroutine(PrototypeFrameCapture.Save(_report.capture,error=>
                { if (error.Length>0) _report.errors.Add(error); else Check("rendered_network_story"); _captureDone=true; }));
            }
            // Last peer holds its vote; in the 4-player case it leaves only AFTER its result reached disk.
            if (self.slot==_report.capacity-1)
            {
                if (!_lifeSawWithdraw || ready!=_report.capacity-1) { _lifeVoteAt=-1; return; }
                if (_lifeVoteAt<0) _lifeVoteAt=Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup-_lifeVoteAt<1.2f) return;
                if (_report.capacity==4)
                {
                    if (!ValidateLifecycleSave(_network.WorldState,true)) { Fail("departing_voter_result_not_saved"); return; }
                    Check("voter_left_after_persisted_result"); Finish(); return;
                }
                if (_lifeVoteStep==0) { _network.Send("story_ready",ready:true); _lifeVoteStep=1; }
                return;
            }
            if (_lifeVoteStep==0) { _network.Send("story_ready",ready:true); _lifeVoteStep=1; return; }
            if (self.slot!=0) return;
            if (_lifeVoteStep==1 && self.storyReady && ready==_report.capacity-1)
            {
                if (_lifeVoteAt<0) _lifeVoteAt=Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup-_lifeVoteAt>1.2f)
                { Check("not_returned_with_missing_vote"); _network.Send("story_ready",ready:false); _lifeVoteStep=2; _lifeVoteAt=-1; }
            }
            else if (_lifeVoteStep==2 && !self.storyReady)
            {
                if (_lifeVoteAt<0) _lifeVoteAt=Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup-_lifeVoteAt>1.2f) { _network.Send("story_ready",ready:true); _lifeVoteStep=3; }
            }
        }
        private void DriveLifecycleFixture(PrototypeGame game)
        {
            if (game==null || game.Exit==null || game.Map==null) return;
            if (game.StageNumber==1 && _lifeMemoryAt<0)
            {
                var memory=game.GetComponentInChildren<PrototypeStoryMemory>();
                if (memory==null) { Fail("fixture_memory_missing"); return; }
                game.Player.Teleport(memory.transform.position+Vector3.back);
                var input=PrototypePlayerInput.Neutral(); input.hasControl=true; input.necklacePressed=true;
                game.Player.StepServerInput(input,.02f); _lifeMemoryAt=Time.realtimeSinceStartup; return;
            }
            if (game.StageNumber==1 && PrototypeSession.Journal.Count==0) return;
            if (_lifePreparedStage!=game.StageNumber)
            {
                var key=game.Map.Pickups.FirstOrDefault(p=>p.Item==PrototypeItemKind.Key);
                if (key==null) { Fail("fixture_key_missing"); return; }
                game.Player.Teleport(key.transform.position+Vector3.back);
                if (!key.TryUse(game.Player.Participant)) { Fail("fixture_key_rejected"); return; }
                foreach (var device in game.GetComponentsInChildren<PrototypeInteractable>())
                {
                    if (device.Kind!=PrototypeInteractionKind.Region && device.Kind!=PrototypeInteractionKind.Record && device.Kind!=PrototypeInteractionKind.KeySocket) continue;
                    game.Player.Teleport(device.transform.position+Vector3.back*2);
                    if (!device.TryUse(game.Player.Participant)) { Fail("fixture_device_rejected:"+device.Kind); return; }
                }
                if (game.Objective.RequiredBox!=null)
                {
                    game.Objective.RequiredBox.transform.position=game.Objective.BoxSocket;
                    game.Objective.RequiredBox.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
                }
                _lifePreparedStage=game.StageNumber; Physics.SyncTransforms();
            }
            if (game.Exit.IsSettled)
            {
                if (game.StageNumber==2 && (game.Exit.ArrivedCount!=1 || game.Exit.SettlementReason!="5초 집계 마감"))
                { Fail("partial_exit_did_not_keep_fixed_window"); return; }
                Check("fixture_stage_"+game.StageNumber+"_settled"); return;
            }
            if (game.Objective.Completed)
            {
                var arrivals=game.StageNumber==2?1:_report.capacity;
                for (var i=0;i<arrivals;i++)
                {
                    var player=game.ControlledPlayers[i];
                    if (player.Participant.HasEnteredExit) continue;
                    player.Teleport(game.Exit.EntryPoint.position); game.Exit.RegisterArrival(player.Participant);
                }
                return;
            }
            foreach (var player in game.ControlledPlayers)
            {
                if (!player.Participant.IsAlive) { Fail("fixture_participant_died"); return; }
                var target=game.Objective.Pads[player.Participant.ParticipantId].position+Vector3.up*.12f;
                if (game.Objective.BossWarning || game.Objective.BossDanger) target.x=target.x<0?-8:8;
                player.Teleport(target);
            }
        }
    }
}
