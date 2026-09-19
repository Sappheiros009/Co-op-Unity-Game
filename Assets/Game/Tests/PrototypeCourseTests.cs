using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeCourseTests
    {
        private float _timeScale;
        [UnitySetUp] public IEnumerator Setup()
        {
            _timeScale = Time.timeScale;
            PrototypeSave.RootOverride = Path.GetFullPath(Path.Combine(Application.dataPath,"..","Temp","PrototypeCourseTests",Guid.NewGuid().ToString("N")));
            PrototypeSave.Reload(); PrototypeSession.PartySize = 4; PrototypeSession.Practice = false;
            PrototypeSession.BeginChapter(1);
            yield return SceneManager.LoadSceneAsync("PrototypeLobby"); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = _timeScale; PrototypeUi.CloseModal();
            yield return SceneManager.LoadSceneAsync("PrototypeLobby");
            PrototypeSave.RootOverride = null; PrototypeSave.Reload();
        }
        private static void Place(PrototypeGame game, PrototypeParticipant actor, Vector3 position)
        {
            if (actor == game.Player.Participant) game.Player.Teleport(position);
            else actor.GetComponent<PrototypeTeammate>().Teleport(position);
        }
        private static void Pads(PrototypeGame game, bool safeEdge)
        {
            foreach (var actor in game.Participants)
            {
                var position = game.Objective.Pads[actor.ParticipantId].position + Vector3.up * .12f;
                if (safeEdge) position.x = position.x < 0 ? -8 : 8;
                Place(game, actor, position);
            }
        }
        private static string Snapshot(PrototypeGame game)
        {
            var text = $"stage={game.StageNumber}, raft={game.Map.Raft?.transform.position}, riders={game.Map.Raft?.RiderCount}; {game.StatusMessage}";
            foreach (var actor in game.Participants) text += $"\n{actor.DisplayName}: {actor.transform.position} HP={actor.Health} alive={actor.IsAlive} connected={actor.IsConnected}";
            foreach (var monster in game.Monsters) text += $"\nMonster: {monster.transform.position}, active={monster.gameObject.activeSelf}, {monster.State}";
            return text;
        }
        [UnityTest] public IEnumerator RaftStageTwoCarriesFourRidersWithMonsterPresent()
        {
            Time.timeScale=6; PrototypeSession.BeginChapter(4); PrototypeSession.CompleteStage(100,10); PrototypeSession.AdvanceStage();
            yield return SceneManager.LoadSceneAsync("PrototypeChapter04"); yield return null;
            var game=Object.FindFirstObjectByType<PrototypeGame>(); var raft=game.Map.Raft;
            game.Player.Teleport(new Vector3(3,.05f,24)); Assert.IsTrue(raft.ReleaseWaterLock(game.Player.Participant));
            game.Player.Teleport(raft.transform.position+new Vector3(0,.3f,-.7f));
            foreach(var bot in game.Teammates) { bot.SetOrder(PrototypeBotOrder.RegionAssist); bot.Teleport(raft.BoardingPoint(bot.Participant.ParticipantId)); }
            Assert.IsTrue(raft.RequestTravel(game.Player.Participant));
            var deadline=Time.time+9; while(!raft.Arrived && Time.time<deadline) yield return null;
            Assert.IsTrue(raft.Arrived,Snapshot(game));
        }
        [UnityTest] public IEnumerator MissingKeyReturnsWithoutDuplicatingDisconnectedInventory()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeChapter01"); yield return null;
            var game = Object.FindFirstObjectByType<PrototypeGame>();
            var source = System.Linq.Enumerable.First(game.Map.Pickups, p => p.Item == PrototypeItemKind.Key);
            var bot = game.Teammates[0]; bot.SetOrder(PrototypeBotOrder.Hold); bot.Teleport(source.transform.position + Vector3.back);
            Assert.IsTrue(source.TryUse(bot.Participant)); Assert.IsFalse(source.gameObject.activeSelf);
            PrototypeSession.Disconnect(bot.Participant.ParticipantId);
            yield return new WaitForSeconds(.8f);
            Assert.IsTrue(source.gameObject.activeSelf); Assert.IsFalse(bot.Participant.Inventory.Has(PrototypeItemKind.Key));
            Assert.AreEqual(4, game.Objective.RequiredRoles);
            Assert.IsTrue(PrototypeSession.Reconnect(bot.Participant.ParticipantId));
            yield return null;
            Assert.IsFalse(bot.Participant.Inventory.Has(PrototypeItemKind.Key));
        }
        [UnityTest] public IEnumerator DroppedKeyBelowWorldReturnsToDesignatedSource()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeChapter01"); yield return null;
            var game=Object.FindFirstObjectByType<PrototypeGame>();
            var source=System.Linq.Enumerable.First(game.Map.Pickups,p=>p.Item==PrototypeItemKind.Key);
            var origin=source.RecoveryOrigin;
            game.Map.DropItem(PrototypeItemKind.Key,new Vector3(99,-12,99)); yield return null; yield return null;
            var dropped=game.Map.Pickups[game.Map.Pickups.Count-1];
            Assert.Less(Vector3.Distance(origin,dropped.transform.position),.2f);
        }
        [UnityTest] public IEnumerator RepeatedPickupDropReusesObjectAndPreservesEssentialItemOrigin()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeChapter01"); yield return null;
            var game=Object.FindFirstObjectByType<PrototypeGame>();
            var source=System.Linq.Enumerable.First(game.Map.Pickups,p=>p.Item==PrototypeItemKind.Key);
            var origin=source.RecoveryOrigin; var count=game.Map.Pickups.Count;
            for (var i=0;i<30;i++)
            {
                game.Player.Teleport(source.transform.position+Vector3.back); Physics.SyncTransforms();
                Assert.IsTrue(source.TryUse(game.Player.Participant)); Assert.IsTrue(game.Player.Participant.Inventory.Consume(PrototypeItemKind.Key));
                game.Map.DropItem(PrototypeItemKind.Key,origin+Vector3.right);
                Assert.IsTrue(source.gameObject.activeSelf); Assert.IsNotNull(source.GetComponent<Rigidbody>());
                Assert.AreEqual(count,game.Map.Pickups.Count); Assert.AreEqual(origin,source.RecoveryOrigin);
            }
        }
        [UnityTest] public IEnumerator RaftRequiresWaterActionAndAllOriginalParticipants()
        {
            PrototypeSession.PartySize = 2; PrototypeSession.BeginChapter(4);
            yield return SceneManager.LoadSceneAsync("PrototypeChapter04"); yield return null;
            var game = Object.FindFirstObjectByType<PrototypeGame>(); var raft = game.Map.Raft;
            Assert.IsFalse(raft.ReleaseWaterLock(game.Player.Participant));
            game.Player.Teleport(new Vector3(3,.05f,24));
            Assert.IsTrue(raft.ReleaseWaterLock(game.Player.Participant));
            game.Player.Teleport(raft.transform.position + new Vector3(0,.3f,-.7f));
            var bot = game.Teammates[0]; bot.SetOrder(PrototypeBotOrder.Hold); bot.Teleport(new Vector3(5,.1f,21));
            Assert.IsTrue(raft.RequestTravel(game.Player.Participant)); var start = raft.transform.position;
            PrototypeSession.Disconnect(1); yield return new WaitForSeconds(.8f);
            Assert.That(Vector3.Distance(start,raft.transform.position), Is.LessThan(.01f)); Assert.AreEqual(2,game.Objective.RequiredRoles);
            PrototypeSession.Reconnect(1); bot.Participant.SetConnected(true); bot.Teleport(raft.BoardingPoint(1));
            yield return new WaitForSeconds(4.7f);
            Assert.IsTrue(raft.Arrived); Assert.IsTrue(game.Objective.RegionalReady);
            Assert.Greater(game.Player.transform.position.z,26);
        }
        [UnityTest] public IEnumerator AllFortyTwoSegmentsSettleSaveAndReturnThroughStory()
        {
            // Accelerated integration: real scene/objective/timer code, controlled actor placement.
            // Not a claim of 42 manually completed keyboard courses.
            Time.timeScale = 6;
            for (var chapter=1; chapter<=7; chapter++)
            {
                PrototypeSession.BeginChapter(chapter);
                yield return SceneManager.LoadSceneAsync(PrototypeChapterCatalog.Get(chapter).SceneName); yield return null;
                for (var stage=1; stage<=6; stage++)
                {
                    var game=Object.FindFirstObjectByType<PrototypeGame>();
                    Assert.IsNotNull(game,$"Chapter {chapter} stage {stage}"); Assert.AreEqual(stage,game.StageNumber);
                    foreach(var bot in game.Teammates) bot.SetOrder(PrototypeBotOrder.Hold);
                    var key=System.Linq.Enumerable.First(game.Map.Pickups,p=>p.Item==PrototypeItemKind.Key);
                    game.Player.Teleport(key.transform.position + Vector3.back);
                    Assert.IsTrue(key.TryUse(game.Player.Participant));
                    if (game.Map.Raft != null)
                    {
                        var raft=game.Map.Raft; game.Player.Teleport(new Vector3(3,.05f,24));
                        Assert.IsTrue(raft.ReleaseWaterLock(game.Player.Participant));
                        game.Player.Teleport(raft.transform.position+new Vector3(0,.3f,-.7f));
                        foreach(var bot in game.Teammates) { bot.SetOrder(PrototypeBotOrder.RegionAssist); bot.Teleport(raft.BoardingPoint(bot.Participant.ParticipantId)); }
                        Assert.IsTrue(raft.RequestTravel(game.Player.Participant));
                        var deadline=Time.time+9;
                        while(!raft.Arrived && Time.time<deadline) yield return null;
                        Assert.IsTrue(raft.Arrived,$"Boat chapter {chapter} stage {stage}\n{Snapshot(game)}");
                        foreach(var bot in game.Teammates) bot.SetOrder(PrototypeBotOrder.Hold);
                    }
                    else
                    {
                        foreach(var device in Object.FindObjectsByType<PrototypeInteractable>(FindObjectsSortMode.None))
                        {
                            if(device.Kind!=PrototypeInteractionKind.Region && device.Kind!=PrototypeInteractionKind.Record) continue;
                            game.Player.Teleport(device.transform.position+Vector3.back*2);
                            Assert.IsTrue(device.TryUse(game.Player.Participant));
                        }
                    }
                    foreach(var device in Object.FindObjectsByType<PrototypeInteractable>(FindObjectsSortMode.None))
                    {
                        if(device.Kind!=PrototypeInteractionKind.KeySocket) continue;
                        game.Player.Teleport(device.transform.position+Vector3.back*2);
                        Assert.IsTrue(device.TryUse(game.Player.Participant));
                    }
                    game.Objective.RequiredBox.transform.position=game.Objective.BoxSocket;
                    game.Objective.RequiredBox.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
                    Physics.SyncTransforms(); Pads(game,false);
                    var end=Time.time+40; var atEdge=false;
                    while(!game.Objective.Completed && Time.time<end)
                    {
                        var shouldDodge=game.Objective.BossWarning || game.Objective.BossDanger;
                        if(shouldDodge!=atEdge) { atEdge=shouldDodge; Pads(game,atEdge); }
                        yield return null;
                    }
                    Assert.IsTrue(game.Objective.Completed,$"Puzzle chapter {chapter} stage {stage}: {game.Objective.Description}");
                    if(stage==6) Assert.AreEqual(3,game.Objective.BossRounds);
                    foreach(var actor in game.Participants)
                    {
                        Assert.IsTrue(actor.IsAlive,$"Unexpected down in chapter {chapter} stage {stage}");
                        Place(game,actor,game.Exit.EntryPoint.position); game.Exit.RegisterArrival(actor);
                    }
                    Assert.IsTrue(game.Exit.IsSettled); Assert.AreEqual(4,game.Exit.ArrivedCount);
                    yield return new WaitForSeconds(3.3f); yield return null;
                }
                Assert.AreEqual(PrototypeChapterCatalog.Get(chapter).StorySceneName,SceneManager.GetActiveScene().name);
                GameObject.Find("Continue").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return null; yield return null;
                Assert.AreEqual("PrototypeWaitingRoom",SceneManager.GetActiveScene().name);
            }
            Assert.AreEqual(127,PrototypeSave.Progress.completedMask); Assert.AreEqual(7,PrototypeSave.Progress.records.Count);
            Assert.AreEqual(7,PrototypeSave.Progress.unlockedChapter);
        }
    }
}
