using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeNetworkReplicaTests
    {
        private PrototypeNetworkReplica _replica;
        private PrototypeNetworkWorldState _state;
        [UnitySetUp] public IEnumerator Setup()
        {
            PrototypeSave.RootOverride=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Temp","NetworkReplicaTests",Guid.NewGuid().ToString("N")));
            PrototypeUi.CloseModal(); PrototypeCapsulePlayer.SetCursor(false);
            yield return SceneManager.LoadSceneAsync("PrototypeLobby");
            _replica=new GameObject("Replica QA").AddComponent<PrototypeNetworkReplica>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeLobby");
            PrototypeSave.RootOverride=null; PrototypeSave.Reload();
        }
        private IEnumerator CaptureFixture(int count=4,int chapter=1,int stage=1,long sequence=1,string run=null)
        {
            PrototypeSession.PrepareReplicaStage(chapter,stage,run??Guid.NewGuid().ToString("N"),count,1839);
            var server=new GameObject("Fixture seed geometry").AddComponent<PrototypeGame>(); server.ConfigureChapter(chapter);
            var names=new string[count]; for(var i=0;i<count;i++) names[i]="Peer "+i;
            server.ConfigureReplicaPlayers(names); var map=server.gameObject.AddComponent<PrototypeMapBuilder>(); map.Build(server);
            _state=new PrototypeNetworkWorldState { runId=PrototypeSession.RunId,chapter=chapter,stage=stage,seed=1839,
                sequence=sequence,simulationTick=10,phase="Playing",roles=count,score=70,yourActor=count-1,
                actors=new PrototypeNetworkActorState[count],props=Array.ConvertAll(PrototypeNetworkWorldState.DynamicProps(server),PrototypeNetworkPose.Read),
                pickups=new PrototypeNetworkPickupState[map.Pickups.Count] };
            for(var i=0;i<count;i++)
                _state.actors[i]=new PrototypeNetworkActorState { id=i,slot=i,name=names[i],health=100,stamina=100,alive=true,connected=true,
                    cameraHeight=1.55f,position=new Vector3(-4+i*2,.15f,-5),items=Array.Empty<PrototypeItemKind>() };
            for(var i=0;i<map.Pickups.Count;i++) _state.pickups[i]=new PrototypeNetworkPickupState {kind=map.Pickups[i].Item,pose=PrototypeNetworkPose.Read(map.Pickups[i].transform)};
            server.gameObject.SetActive(false); Object.Destroy(server.gameObject); yield return null;
            Assert.IsTrue(_state.IsValid());
        }
        private PrototypeNetworkWorldState Next()
        { var value=JsonUtility.FromJson<PrototypeNetworkWorldState>(JsonUtility.ToJson(_state)); value.sequence++; return value; }
        [UnityTest] public IEnumerator TwoThreeFourPlayerViewsBindOnlyOwnCameraAndRetainOtherHeads()
        {
            for(var count=2;count<=4;count++)
            {
                yield return CaptureFixture(count,sequence:count);
                Assert.IsTrue(_replica.Apply(_state));
                Assert.AreEqual(count,_replica.Game.ControlledPlayers.Count);
                Assert.AreEqual(0,_replica.Game.Teammates.Count);
                Assert.AreEqual(1,Array.FindAll(_replica.Game.GetComponentsInChildren<Camera>(),c=>c.enabled).Length);
                Assert.AreEqual(1,_replica.Game.GetComponentsInChildren<AudioListener>().Length);
                for(var i=0;i<count;i++)
                {
                    var p=_replica.Game.ControlledPlayers[i]; Assert.AreEqual(PrototypePlayerControl.Replica,p.Control);
                    Assert.AreEqual(i!=count-1,p.transform.Find("CharacterParts/Head").gameObject.activeSelf);
                    Assert.AreEqual(i==count-1,p.ViewCamera.enabled);
                }
            }
        }
        [UnityTest] public IEnumerator ReplicaCannotRunPhysicsPuzzlesExitOrLocalInput()
        {
            yield return CaptureFixture(); Assert.IsTrue(_replica.Apply(_state));
            foreach(var b in _replica.Game.GetComponentsInChildren<MonoBehaviour>(true))
                if(b.GetType().Namespace==typeof(PrototypeGame).Namespace && !(b is PrototypeSlimeBody)) Assert.IsFalse(b.enabled,b.GetType().Name);
            foreach(var c in _replica.Game.GetComponentsInChildren<Collider>()) Assert.IsFalse(c.enabled,c.name);
            var points=new Vector3[4]; var score=PrototypeSession.RunScore;
            for(var i=0;i<4;i++) points[i]=_replica.Game.ControlledPlayers[i].transform.position;
            yield return new WaitForSeconds(.3f);
            Assert.AreEqual(score,PrototypeSession.RunScore); Assert.AreEqual(0,_replica.Game.Exit.ArrivedCount);
            Assert.IsFalse(_replica.Game.Objective.Completed);
            for(var i=0;i<4;i++) Assert.AreEqual(points[i],_replica.Game.ControlledPlayers[i].transform.position);
        }
        [UnityTest] public IEnumerator ServerPoseStatusInventoryDataAndPickupVisibilityAreAppliedWithoutRebuilding()
        {
            yield return CaptureFixture(); Assert.IsTrue(_replica.Apply(_state)); var count=_replica.BuildCount;
            var next=Next(); next.actors[1].position+=Vector3.forward*10; next.actors[1].health=0; next.actors[1].alive=false;
            next.actors[2].connected=false; next.actors[3].items=new[]{PrototypeItemKind.Key}; next.pickups[0].pose.active=false;
            next.props[0].position+=Vector3.up; Assert.IsTrue(_replica.Apply(next));
            Assert.AreEqual(next.actors[1].position,_replica.Game.ControlledPlayers[1].transform.position);
            Assert.IsFalse(_replica.Game.ControlledPlayers[1].Participant.IsAlive);
            Assert.IsFalse(_replica.Game.ControlledPlayers[2].gameObject.activeSelf);
            Assert.AreEqual(PrototypeItemKind.Key,_replica.State.actors[3].items[0]);
            Assert.IsFalse(_replica.Game.Map.Pickups[0].gameObject.activeSelf);
            Assert.AreEqual(next.props[0].position,PrototypeNetworkWorldState.DynamicProps(_replica.Game)[0].position);
            Assert.AreEqual(count,_replica.BuildCount); yield return null;
        }
        [UnityTest] public IEnumerator OldSequenceInvalidValuesOrMidRunIdentityChangesAreRejected()
        {
            yield return CaptureFixture(); Assert.IsTrue(_replica.Apply(_state)); Assert.IsFalse(_replica.Apply(_state));
            var next=Next(); next.actors[0].health=float.NaN; Assert.IsFalse(_replica.Apply(next));
            next=Next(); next.yourActor=0; Assert.IsFalse(_replica.Apply(next));
            next=Next(); next.actors[0].name="Impostor"; Assert.IsFalse(_replica.Apply(next));
            next=Next(); next.stage=2; next.actors[0].slot=0; next.seed++; Assert.IsFalse(_replica.Apply(next));
            Assert.AreSame(_state,_replica.State); yield return null;
        }
        [UnityTest] public IEnumerator NextStageRebuildsOnceAndShowsServerBossWarning()
        {
            yield return CaptureFixture(chapter:2,stage:5); Assert.IsTrue(_replica.Apply(_state)); var run=_state.runId;
            yield return CaptureFixture(chapter:2,stage:6,sequence:2,run:run); _state.bossWarning=true;
            Assert.IsTrue(_replica.Apply(_state)); Assert.AreEqual(2,_replica.BuildCount); Assert.IsTrue(_replica.Game.Objective.AttackArea.enabled);
            var next=Next(); next.bossWarning=false; next.bossDanger=true; Assert.IsTrue(_replica.Apply(next));
            Assert.AreEqual(2,_replica.BuildCount); Assert.IsTrue(_replica.Game.Objective.AttackArea.enabled);
            var story=JsonUtility.FromJson<PrototypeNetworkWorldState>(JsonUtility.ToJson(next));
            story.sequence++; story.phase="Story"; Assert.IsTrue(_replica.Apply(story)); Assert.IsFalse(_replica.Game.gameObject.activeSelf);
        }
        [Test] public void ServerRunOwnerUsesActorBindingNotAlwaysActorZero()
        {
            PrototypeSession.BeginNetworkChapter(1,"bound-run",3,1839,1); Assert.AreEqual(1,PrototypeSession.RoomOwner);
            PrototypeSession.Disconnect(0); Assert.AreEqual(1,PrototypeSession.RoomOwner); Assert.AreEqual(3,PrototypeSession.StartingCount);
            Assert.Throws<ArgumentOutOfRangeException>(()=>PrototypeSession.BeginNetworkChapter(1,"bad",3,1839,3));
        }
        [UnityTest] public IEnumerator WipeClearsClientTemporaryStateOnceAndReturnsThroughTwoDimensionalLobby()
        {
            yield return CaptureFixture(stage:2);
            PrototypeSession.BeginNetworkChapter(1,_state.runId,4,1839);
            PrototypeSession.CompleteStage(70,6); Assert.IsTrue(PrototypeSession.AdvanceStage());
            PrototypeSession.Journal.Add("temporary discovery");
            PrototypeSave.Progress.unlockedChapter=4;
            PrototypeSave.Progress.achievements.Add("kept achievement");
            PrototypeSave.Remember(new[]{"permanent memory"});
            var permanent=JsonUtility.ToJson(PrototypeSave.Progress);
            var settings=PrototypeSettings.Copy(); settings.sensitivity=.13f;
            Assert.IsTrue(PrototypeSettings.Apply(settings,true));
            var settingsJson=JsonUtility.ToJson(PrototypeSettings.Current);
            var host=new GameObject("Wipe client QA");
            var network=new GameObject("Wipe fixture transport").AddComponent<PrototypeNetworkTransport>();
            var client=host.AddComponent<PrototypeNetworkClientWorld>(); client.Configure(network,false);
            client.SendMessage("OnWorld",_state);
            var wipe=Next(); wipe.phase="Lobby"; wipe.score=0; wipe.elapsed=0;
            foreach(var actor in wipe.actors) { actor.alive=false; actor.health=0; }
            client.SendMessage("OnWorld",wipe);
            Assert.AreEqual("",client.Failure); Assert.IsTrue(client.ShowingWorld);
            Assert.AreEqual(0,PrototypeSession.RunScore); Assert.AreEqual(0,PrototypeSession.RunSeconds);
            Assert.AreEqual(0,PrototypeSession.Chapter); Assert.AreEqual(1,PrototypeSession.Stage);
            Assert.IsEmpty(PrototypeSession.Journal);
            var camera=host.GetComponentInChildren<Camera>(); Assert.IsTrue(camera.orthographic);
            var button=Array.Find(host.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.name=="Continue");
            Assert.IsNotNull(button); button.onClick.Invoke();
            Assert.IsFalse(client.ShowingWorld);
            yield return null;
            var events=PrototypeSession.Events.Count;
            wipe.sequence++; client.SendMessage("OnWorld",wipe);
            Assert.AreEqual(events,PrototypeSession.Events.Count); Assert.IsFalse(client.ShowingWorld);
            Assert.IsNull(host.GetComponentInChildren<Camera>());
            PrototypeSave.Reload(); PrototypeSettings.Reload();
            Assert.AreEqual(permanent,JsonUtility.ToJson(PrototypeSave.Progress));
            Assert.AreEqual(settingsJson,JsonUtility.ToJson(PrototypeSettings.Current));
            yield return CaptureFixture(stage:1,sequence:4);
            _state.score=0; client.SendMessage("OnWorld",_state);
            Assert.AreEqual("",client.Failure); Assert.IsTrue(client.ShowingWorld);
            Assert.AreEqual(1,client.Replica.State.stage); Assert.AreEqual(0,client.Replica.State.score);
            Assert.IsFalse(client.Replica.ViewCamera.orthographic);
        }
        [UnityTest] public IEnumerator ClientHudUsesItsOwnBindingsAndReadableBackgrounds()
        {
            yield return CaptureFixture();
            var saved=PrototypeSettings.Copy();
            try
            {
                var settings=new PrototypeSettingsData();
                Assert.IsTrue(PrototypeInput.Rebind(settings,"Interact",Key.R));
                Assert.IsTrue(PrototypeInput.Rebind(settings,"Drop",Key.B));
                Assert.IsTrue(PrototypeInput.Rebind(settings,"Forward",Key.I));
                PrototypeSettings.Apply(settings,false);
                Assert.AreEqual("R: 상자 들기 / B: 내려놓기",PrototypeNetworkClientWorld.FormatPrompt("{key:Interact}: 상자 들기 / {key:Drop}: 내려놓기"));
                Assert.AreEqual("",PrototypeNetworkClientWorld.FormatPrompt(null));
                var host=new GameObject("Client presentation QA");
                var network=new GameObject("Fixture transport").AddComponent<PrototypeNetworkTransport>();
                var client=host.AddComponent<PrototypeNetworkClientWorld>(); client.Configure(network,false);
                client.SendMessage("OnWorld",_state);
                Assert.AreEqual("",client.Failure);
                var labels=host.GetComponentsInChildren<TextMeshProUGUI>();
                var controls=Array.Find(labels,label=>label.name=="Controls");
                StringAssert.Contains("I/A/S/D 이동",controls.text); StringAssert.Contains("R 상호작용",controls.text);
                StringAssert.Contains("B 내려놓기",controls.text);
                Assert.AreEqual("ServerStatusPanel",controls.transform.parent.name);
                Assert.Greater(controls.transform.parent.GetComponent<UnityEngine.UI.Image>().color.a,.9f);
                var prompt=Array.Find(labels,label=>label.name=="Prompt");
                Assert.AreEqual("PromptPanel",prompt.transform.parent.name);
                Assert.Greater(prompt.transform.parent.GetComponent<UnityEngine.UI.Image>().color.a,.9f);
                yield return null;
            }
            finally { PrototypeSettings.Apply(saved,false); }
        }
    }
}
