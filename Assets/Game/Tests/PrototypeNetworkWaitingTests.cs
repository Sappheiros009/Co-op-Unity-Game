using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeNetworkWaitingTests
    {
        private PrototypeNetworkWaitingServer _server;
        private PrototypeNetworkStartMember[] _roster;
        [UnitySetUp] public IEnumerator Setup()
        {
            PrototypeSave.RootOverride=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Temp","NetworkWaitingTests",Guid.NewGuid().ToString("N")));
            PrototypeUi.CloseModal(); PrototypeCapsulePlayer.SetCursor(false);
            yield return SceneManager.LoadSceneAsync("PrototypeLobby");
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects()) { root.SetActive(false); Object.Destroy(root); }
            yield return null;
            _server=new GameObject("Waiting server QA").AddComponent<PrototypeNetworkWaitingServer>();
            _roster=new[]{new PrototypeNetworkStartMember(12,0,"A"),new PrototypeNetworkStartMember(47,1,"B"),new PrototypeNetworkStartMember(83,2,"C"),new PrototypeNetworkStartMember(99,3,"D")};
            _server.Synchronize(_roster,1,true); Physics.SyncTransforms();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            PrototypeUi.CloseModal(); PrototypeCapsulePlayer.SetCursor(false);
            yield return SceneManager.LoadSceneAsync("PrototypeLobby"); PrototypeSave.RootOverride=null; PrototypeSave.Reload();
        }
        private static PrototypeNetworkWaitingInput Packet(long sequence=1,long epoch=1)
        {
            var input=PrototypePlayerInput.Neutral(); input.hasControl=true; input.motor.move=Vector2.up;
            return new PrototypeNetworkWaitingInput{epoch=epoch,sequence=sequence,input=input};
        }
        [Test] public void WaitingProtocolContainsIntentOnlyAndStateRejectsInvalidPoses()
        {
            foreach(var field in new[]{"slot","actorId","position","deltaTime","score"}) Assert.IsNull(typeof(PrototypeNetworkWaitingInput).GetField(field));
            var packet=JsonUtility.FromJson<PrototypeNetworkWaitingInput>(JsonUtility.ToJson(Packet()));
            Assert.AreEqual(Vector2.up,packet.input.motor.move); Assert.IsTrue(packet.input.IsValid);
            var state=_server.Capture(); Assert.IsTrue(state.IsValid()); state.actors[1].slot=0; Assert.IsFalse(state.IsValid());
            state=_server.Capture(); state.actors[0].position.x=float.NaN; Assert.IsFalse(state.IsValid());
            state=_server.Capture(); state.actors[0].cameraHeight=30; Assert.IsFalse(state.IsValid());
        }
        [Test] public void ReservedSnapshotRoundTripDropsEmptyWaitingDtoWithoutDroppingRoomTransition()
        {
            var room=new PrototypeNetworkRoom(2); room.Join(12,"A",out _); room.Join(47,"B",out _);
            room.Apply(12,new PrototypeNetworkCommand{kind="ready",ready=true,sequence=1},out _);
            room.Apply(47,new PrototypeNetworkCommand{kind="ready",ready=true,sequence=1},out _);
            room.Apply(12,new PrototypeNetworkCommand{kind="reserve",sequence=2},out _);
            var decoded=JsonUtility.FromJson<PrototypeNetworkSnapshot>(JsonUtility.ToJson(room.Snapshot(12)));
            Assert.AreEqual("Reserved",decoded.phase); Assert.IsTrue(decoded.NormalizeWaitingState()); Assert.IsNull(decoded.waiting);
            decoded.phase="WaitingRoom"; decoded.waiting=new PrototypeNetworkWaitingState(); Assert.IsFalse(decoded.NormalizeWaitingState());
            decoded.waiting=_server.Capture(); Assert.IsTrue(decoded.NormalizeWaitingState()); decoded.waiting.epoch++; Assert.IsFalse(decoded.NormalizeWaitingState());
        }
        [Test] public void OnlyBoundActorMovesAndSilenceStopsMovement()
        {
            var before=_server.Capture();
            for(var tick=0;tick<50;tick++)
            { Assert.IsTrue(_server.Submit(47,Packet(tick+1),1+tick*.02,out _)); _server.Step(.02f,1+tick*.02); }
            var after=_server.Capture(); Assert.Greater(after.actors[1].position.z,before.actors[1].position.z+3.5f);
            foreach(var slot in new[]{0,2,3}) Assert.AreEqual(before.actors[slot].position.z,after.actors[slot].position.z,.001f);
            _server.Step(.02f,3); Assert.AreEqual(after.actors[1].position.z,_server.Capture().actors[1].position.z,.001f);
            Assert.IsFalse(_server.Submit(48,Packet(),3,out var error)); Assert.AreEqual("unknown_connection",error);
        }
        [Test] public void JoinAndLeaveKeepSurvivorSequenceButNewEpochResetsRoom()
        {
            Assert.IsTrue(_server.Submit(47,Packet(3),1,out _)); _server.Step(.02f,1);
            _server.Synchronize(new[]{_roster[1],_roster[2]},1,true);
            Assert.IsNull(_server.FindWalker(12)); Assert.AreEqual(2,_server.Capture().actors.Length);
            Assert.IsFalse(_server.Submit(47,Packet(3),1.1,out var error)); Assert.AreEqual("invalid_sequence",error);
            _server.Synchronize(_roster,1,true); Assert.IsTrue(_server.Submit(47,Packet(4),1.2,out _));
            _server.Synchronize(_roster,2,true); Assert.IsFalse(_server.Submit(47,Packet(5),2,out error)); Assert.AreEqual("stale_waiting_room",error);
            Assert.IsTrue(_server.Submit(47,Packet(1,2),2.1,out _)); Assert.AreEqual(PrototypeWaitingRoomLayout.NetworkSpawn(1),_server.FindWalker(47).transform.position);
        }
        [Test] public void ChapterTransitionDeactivatesRoomAndRejectsWaitingInput()
        {
            Assert.IsTrue(_server.Submit(47,Packet(),1,out _)); var before=_server.Capture().actors[1].position;
            _server.Synchronize(_roster,1,false); _server.Step(.02f,1.1);
            Assert.AreEqual(before,_server.Capture().actors[1].position);
            Assert.IsFalse(_server.Submit(47,Packet(2),1.1,out var error)); Assert.AreEqual("stale_waiting_room",error);
            Assert.AreEqual(0,Array.FindAll(_server.GetComponentsInChildren<Collider>(true),c=>c.enabled&&c.gameObject.activeInHierarchy).Length);
        }
        [UnityTest] public IEnumerator StationsRequireProximityFacingAndUnobstructedViewOnServer()
        {
            var walker=_server.FindWalker(12); var command=new PrototypeNetworkCommand{kind="chapter",chapter=7,sequence=1};
            Assert.AreEqual("station_out_of_reach",_server.ValidateStation(12,command));
            var station=_server.transform.Find("WaitingRoom_Walkable3D/Chapter7").GetComponent<PrototypeWaitingRoomStation>();
            walker.Teleport(station.transform.position+Vector3.back*2); walker.AimAt(station.FocusPoint); Physics.SyncTransforms();
            Assert.AreEqual("",_server.ValidateStation(12,command)); walker.SetLook(180,0);
            Assert.AreEqual("station_out_of_reach",_server.ValidateStation(12,command)); walker.AimAt(station.FocusPoint);
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.transform.position=station.transform.position+Vector3.back+Vector3.up;
            blocker.transform.localScale=new Vector3(2,3,.3f); Physics.SyncTransforms();
            Assert.AreEqual("station_out_of_reach",_server.ValidateStation(12,command)); Object.Destroy(blocker); yield return null;
            Assert.AreEqual("",_server.ValidateStation(12,command));
        }
        [Test] public void FailedRemoteStationCommandsCannotMutateReadinessOrChapter()
        {
            var room=new PrototypeNetworkRoom(4); room.Join(12,"A",out _); room.Join(47,"B",out _);
            Assert.IsFalse(room.Apply(12,new PrototypeNetworkCommand{kind="chapter",chapter=7,sequence=1},out var error,_server.ValidateStation));
            Assert.AreEqual("station_out_of_reach",error); Assert.AreEqual(1,room.Chapter);
            Assert.IsFalse(room.Apply(47,new PrototypeNetworkCommand{kind="chapter",chapter=7,sequence=1},out error,_server.ValidateStation)); Assert.AreEqual("owner_only",error);
            Assert.IsFalse(room.Apply(12,new PrototypeNetworkCommand{kind="ready",ready=true,sequence=2},out error,_server.ValidateStation));
            Assert.IsFalse(room.Snapshot(12).peers[0].ready); room.ReturnToWaitingRoom(); Assert.AreEqual(2,room.WaitingEpoch);
        }
        [UnityTest] public IEnumerator ClientHasActualRosterOneOwnCameraAndNoCharacterPhysics()
        {
            var room=new PrototypeNetworkRoom(4); foreach(var entry in _roster) room.Join(entry.connection,entry.name,out _);
            var state=room.Snapshot(47); state.waiting=_server.Capture(); _server.gameObject.SetActive(false);
            var client=new GameObject("Waiting client QA").AddComponent<PrototypeNetworkWaitingClient>(); client.ApplySnapshot(state); yield return null;
            Assert.AreEqual(4,client.ActorCount); Assert.AreEqual(1,client.Walker.Participant.ParticipantId);
            Assert.IsFalse(client.Walker.ViewCamera.orthographic); Assert.IsFalse(client.Walker.transform.Find("CharacterParts/Head").gameObject.activeSelf);
            Assert.AreEqual(1,Array.FindAll(client.GetComponentsInChildren<Camera>(),c=>c.enabled).Length);
            Assert.AreEqual(1,client.GetComponentsInChildren<AudioListener>().Length);
            Assert.IsEmpty(Array.FindAll(client.GetComponentsInChildren<CharacterController>(),c=>c.enabled));
            Assert.IsEmpty(client.GetComponentsInChildren<PrototypeTeammate>());
            Assert.AreEqual(12,client.GetComponentsInChildren<PrototypeWaitingRoomStation>().Length);
            Assert.IsNull(client.transform.Find("WaitingRoom_Walkable3D/PartyStation"));
            var before=client.Walker.transform.position; client.Walker.StepMotor(new PrototypeMotorInput{move=Vector2.up},.02f); Assert.AreEqual(before,client.Walker.transform.position);
            state.waiting.actors=new[]{state.waiting.actors[1]}; client.ApplySnapshot(state); yield return null; Assert.AreEqual(1,client.ActorCount);
        }
    }
}
