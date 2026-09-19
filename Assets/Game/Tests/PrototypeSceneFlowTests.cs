using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeSceneFlowTests
    {
        [UnitySetUp] public IEnumerator Setup()
        {
            PrototypeSave.RootOverride = Path.GetFullPath(Path.Combine(Application.dataPath,"..","Temp","PrototypeSceneTests",Guid.NewGuid().ToString("N")));
            PrototypeSave.Reload(); PrototypeSession.PartySize = 4; PrototypeSession.Practice = false;
            PrototypeSession.AllChaptersForTesting = true; PrototypeSession.BeginChapter(1);
            yield return SceneManager.LoadSceneAsync("PrototypeLobby"); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            PrototypeUi.CloseModal(); PrototypeCapsulePlayer.SetCursor(false);
            yield return SceneManager.LoadSceneAsync("PrototypeLobby");
            PrototypeSave.RootOverride = null; PrototypeSave.Reload();
        }
        [UnityTest] public IEnumerator LobbyButtonLoadsWaitingRoomWithSingleEventSystemAndKoreanText()
        {
            var start = GameObject.Find("StartGame").GetComponent<UnityEngine.UI.Button>();
            Assert.IsNotNull(start); start.onClick.Invoke(); yield return null; yield return null;
            Assert.AreEqual("PrototypeWaitingRoom", SceneManager.GetActiveScene().name);
            Assert.AreEqual(1, Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(GameObject.Find("Chapter7").GetComponent<PrototypeWaitingRoomStation>());
            Assert.IsNotNull(Object.FindFirstObjectByType<PrototypeWaitingRoomWalker>());
            Assert.IsTrue(PrototypeUi.Font.HasCharacters("슬라임 협동게임 출구 품질", out _, true, true));
        }
        [UnityTest] public IEnumerator AllSevenScenesHaveCorrectRosterPartsObjectivesAndQualityInvariant()
        {
            for (var chapter = 1; chapter <= 7; chapter++)
            {
                PrototypeSession.BeginChapter(chapter);
                yield return SceneManager.LoadSceneAsync(PrototypeChapterCatalog.Get(chapter).SceneName); yield return null;
                var game = Object.FindFirstObjectByType<PrototypeGame>();
                Assert.AreEqual(chapter, game.ChapterNumber); Assert.AreEqual(4, game.Participants.Count);
                Assert.AreEqual(4, game.Objective.RequiredRoles); Assert.IsFalse(game.Exit.IsGateOpen);
                var parts = game.Player.transform.Find("CharacterParts");
                foreach (var name in new[]{"Body_Torso","Head","Hand_Left","Hand_Right","Foot_Left","Foot_Right","Necklace"}) Assert.IsNotNull(parts.Find(name), name);
                var original = PrototypeSettings.Copy(); var low = PrototypeSettings.Copy(); low.quality = "낮음";
                var colliderCount = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length;
                PrototypeSettings.Apply(low, false);
                Assert.AreEqual(colliderCount, Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(4, game.Objective.RequiredRoles); Assert.IsFalse(game.Exit.IsGateOpen);
                PrototypeSettings.Apply(original, false);
            }
        }
        [UnityTest] public IEnumerator WaitingRoomPartyStationCyclesTwoThreeFourAndKeepsWorldVisible()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeWaitingRoom"); yield return null;
            var room = Object.FindFirstObjectByType<PrototypeWaitingRoomController>();
            var station = GameObject.Find("PartyStation").GetComponent<PrototypeWaitingRoomStation>();
            room.Walker.Teleport(station.transform.position + Vector3.right * 2);
            room.Walker.AimAt(station.FocusPoint); Physics.SyncTransforms();
            PrototypeSession.PartySize = 4;
            foreach (var expected in new[] { 2, 3, 4, 2 }) { Assert.IsTrue(room.TryUseStation(station)); Assert.AreEqual(expected, PrototypeSession.PartySize); }
            Assert.IsNull(GameObject.Find("ChapterSelection")); Assert.IsNull(GameObject.Find("PartyPanel"));
            Assert.IsFalse(room.Walker.ViewCamera.orthographic);
        }
        [UnityTest] public IEnumerator LocalTeammatesPhysicallyTraverseSeededCorridorsToAssignedPads()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeChapter01"); yield return null;
            var game = Object.FindFirstObjectByType<PrototypeGame>(); game.CommandTeam(true);
            var deadline = Time.time + 30;
            var all = false;
            while (Time.time < deadline)
            {
                all = true;
                foreach (var bot in game.Teammates)
                {
                    var pad = game.Objective.Pads[bot.Participant.ParticipantId].position;
                    if (Vector3.Distance(bot.transform.position, pad) > 1.4f) all = false;
                }
                if (all) break;
                yield return null;
            }
            foreach (var bot in game.Teammates)
                Assert.Less(Vector3.Distance(bot.transform.position,game.Objective.Pads[bot.Participant.ParticipantId].position),1.4f,bot.name + " " + bot.transform.position);
        }
        [UnityTest] public IEnumerator PuzzleGateSettlementAndNextStageRevivalFlow()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeChapter01"); yield return null;
            var game = Object.FindFirstObjectByType<PrototypeGame>();
            foreach (var device in Object.FindObjectsByType<PrototypeInteractable>(FindObjectsSortMode.None))
            {
                if (device.Kind != PrototypeInteractionKind.Region) continue;
                game.Player.Teleport(device.transform.position + Vector3.back * 2);
                Assert.IsTrue(device.TryUse(game.Player.Participant));
            }
            game.Player.Teleport(game.Objective.Pads[0].position + Vector3.up * .1f);
            foreach (var bot in game.Teammates) { bot.SetOrder(PrototypeBotOrder.Hold); bot.Teleport(game.Objective.Pads[bot.Participant.ParticipantId].position + Vector3.up * .1f); }
            yield return new WaitForSeconds(PrototypeTuning.Current.puzzleHoldSeconds + .5f);
            Assert.IsTrue(game.Exit.IsGateOpen);
            game.Teammates[0].Participant.MarkDown();
            game.Player.Teleport(game.Exit.EntryPoint.position);
            game.Exit.RegisterArrival(game.Player.Participant);
            Assert.IsTrue(game.Exit.HasStartedSettlement); Assert.IsFalse(game.Exit.IsSettled);
            yield return new WaitForSeconds(8.3f);
            var next = Object.FindFirstObjectByType<PrototypeGame>();
            Assert.AreEqual(2, PrototypeSession.Stage); Assert.IsTrue(next.Teammates[0].Participant.IsAlive);
            Assert.Greater(PrototypeSession.RunScore,0); Assert.AreEqual(4,next.Objective.RequiredRoles);
        }
        [UnityTest] public IEnumerator ActualWipeReturnsLobbyAndPreservesUnlocks()
        {
            PrototypeSave.Progress.unlockedChapter = 4; Assert.IsTrue(PrototypeSave.WriteProgress());
            yield return SceneManager.LoadSceneAsync("PrototypeChapter01"); yield return null;
            var game=Object.FindFirstObjectByType<PrototypeGame>();
            foreach(var actor in game.Participants) actor.MarkDown();
            yield return new WaitForSeconds(3.4f);
            Assert.AreEqual("PrototypeLobby",SceneManager.GetActiveScene().name);
            Assert.AreEqual(0,PrototypeSession.RunScore); Assert.AreEqual(1,PrototypeSession.Stage);
            Assert.AreEqual(4,PrototypeSave.Progress.unlockedChapter);
        }
        [UnityTest] public IEnumerator EachStoryInterludeReturnsWaitingRoom()
        {
            for(var chapter=1;chapter<=7;chapter++)
            {
                yield return SceneManager.LoadSceneAsync(PrototypeChapterCatalog.Get(chapter).StorySceneName); yield return null;
                var story=Object.FindFirstObjectByType<PrototypeStoryInterludeController>(); Assert.AreEqual(chapter,story.CompletedChapter);
                GameObject.Find("Continue").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return null; yield return null;
                Assert.AreEqual("PrototypeWaitingRoom",SceneManager.GetActiveScene().name);
            }
        }
    }
}
