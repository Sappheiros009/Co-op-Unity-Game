using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeRulesTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private string _saveRoot;
        [SetUp] public void Setup()
        {
            PrototypeSession.PartySize = 4; PrototypeSession.Practice = false; PrototypeSession.BeginChapter(1);
            _saveRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "PrototypeRules", Guid.NewGuid().ToString("N")));
            PrototypeSave.RootOverride = _saveRoot; PrototypeSave.Reload();
        }
        [TearDown] public void Cleanup()
        {
            foreach (var obj in _objects) if (obj != null) Object.DestroyImmediate(obj);
            _objects.Clear(); PrototypeSave.RootOverride = null; PrototypeSave.Reload();
        }
        private PrototypeParticipant Actor(int id)
        {
            var obj = new GameObject("RulesActor"); _objects.Add(obj);
            var actor = obj.AddComponent<PrototypeParticipant>(); actor.Configure(id, "시험"); return actor;
        }
        private PrototypeExitScoring Exit(params PrototypeParticipant[] actors)
        {
            var obj = new GameObject("RulesExit"); _objects.Add(obj);
            var exit = obj.AddComponent<PrototypeExitScoring>(); exit.Configure(obj.transform, 4, 10);
            if (actors.Length > 0) exit.BindRoster(actors);
            return exit;
        }
        [Test] public void ClosedGateAndDistantArrivalAreRejected()
        {
            var actor = Actor(0); var exit = Exit(actor); exit.SetGateOpen(false);
            Assert.IsFalse(exit.TryRegisterArrival(actor, 12)); exit.SetGateOpen(true);
            actor.transform.position = Vector3.one * 20; Assert.IsFalse(exit.TryRegisterArrival(actor, 12));
            Assert.IsFalse(exit.HasStartedSettlement);
        }
        [Test] public void SameIdFromDifferentObjectIsNotRosterMember()
        {
            var real = Actor(0); var fake = Actor(0); var exit = Exit(real);
            Assert.IsFalse(exit.TryRegisterArrival(fake, 12)); Assert.IsTrue(exit.TryRegisterArrival(real, 12));
        }
        [Test] public void DeadlineCannotExtendBeforeNextUpdate()
        {
            var first = Actor(0); var late = Actor(1); var exit = Exit(first, late);
            Assert.IsTrue(exit.TryRegisterArrival(first, 12));
            Assert.IsFalse(exit.TryRegisterArrival(late, 17));
            Assert.AreEqual(1, exit.ArrivedCount); Assert.AreEqual(7, exit.SettlementElapsed, .001f);
            Assert.AreEqual(930, exit.TeamScore);
        }
        [Test] public void DeadOrDisconnectedRosterDoesNotCloseWindowEarly()
        {
            var first = Actor(0); var down = Actor(1); var disconnected = Actor(2); var other = Actor(3);
            var exit = Exit(first, down, disconnected, other); down.MarkDown(); disconnected.SetConnected(false); other.MarkDown();
            exit.TryRegisterArrival(first, 12); Assert.IsFalse(exit.IsSettled); Assert.AreEqual(4, exit.StageStartParticipantCount);
            exit.Tick(17); Assert.IsTrue(exit.IsSettled); Assert.AreEqual(1, exit.ArrivedCount);
        }
        [Test] public void ExitParticipantIsSafeAndCannotReenter()
        {
            var actor = Actor(0); var exit = Exit(actor); exit.TryRegisterArrival(actor, 12); actor.Damage(200); actor.MarkDown();
            Assert.IsTrue(actor.IsAlive); Assert.IsFalse(exit.TryRegisterArrival(actor, 13)); Assert.AreEqual(1, exit.ArrivedCount);
        }
        [Test] public void InventoryIsBoundedAndTransferIsAtomic()
        {
            var a = new PrototypeInventory(3); var b = new PrototypeInventory(2);
            a.Add(PrototypeItemKind.Key); b.Add(PrototypeItemKind.Medkit); b.Add(PrototypeItemKind.Rope);
            Assert.IsFalse(a.TransferTo(b, PrototypeItemKind.Key)); Assert.IsTrue(a.Has(PrototypeItemKind.Key));
            b.Consume(PrototypeItemKind.Medkit); Assert.IsTrue(a.TransferTo(b, PrototypeItemKind.Key));
            Assert.IsFalse(a.Has(PrototypeItemKind.Key)); Assert.IsFalse(a.TransferTo(b, PrototypeItemKind.Key));
        }
        [Test] public void RequestValidationRejectsSpoofDuplicateAndNonFinitePosition()
        {
            var actor = Actor(0);
            Assert.IsFalse(PrototypeSession.ValidateRequest("r1", 1, actor, Vector3.zero, 3, out _));
            Assert.IsTrue(PrototypeSession.ValidateRequest("r1", 0, actor, Vector3.zero, 3, out _));
            Assert.IsFalse(PrototypeSession.ValidateRequest("r1", 0, actor, Vector3.zero, 3, out _));
            Assert.IsFalse(PrototypeSession.ValidateRequest("r2", 0, actor, new Vector3(float.NaN,0,0), 3, out _));
        }
        [Test] public void RoomOwnerTransferRetainsStageScoreAndRoster()
        {
            PrototypeSession.CompleteStage(123, 45); PrototypeSession.Disconnect(0);
            Assert.AreEqual(1, PrototypeSession.RoomOwner); Assert.AreEqual(123, PrototypeSession.RunScore);
            Assert.AreEqual(4, PrototypeSession.StartingCount); Assert.AreEqual(45, PrototypeSession.RunSeconds);
            Assert.IsTrue(PrototypeSession.Reconnect(0)); Assert.AreEqual(1, PrototypeSession.RoomOwner);
        }
        [Test] public void SecurityKickRequiresEvidenceAndDoesNotBanTeammates()
        {
            Assert.IsFalse(PrototypeSession.ConfirmCheatForSimulation(1, ""));
            Assert.IsTrue(PrototypeSession.ConfirmCheatForSimulation(1, "reviewed-local-test"));
            Assert.IsFalse(PrototypeSession.Reconnect(1)); Assert.IsTrue(PrototypeSession.IsConnected(2));
        }
        [Test] public void WipeClearsRunButPreservesPermanentProgress()
        {
            PrototypeSave.Progress.unlockedChapter = 5; Assert.IsTrue(PrototypeSave.WriteProgress());
            PrototypeSession.CompleteStage(300, 25); PrototypeSession.Wipe();
            Assert.AreEqual(0, PrototypeSession.RunScore); Assert.AreEqual(1, PrototypeSession.Stage);
            PrototypeSave.Reload(); Assert.AreEqual(5, PrototypeSave.Progress.unlockedChapter);
        }
        [Test] public void ChapterSaveIsIdempotentAndUsesStartCount()
        {
            for (var i=1;i<=6;i++) { PrototypeSession.CompleteStage(100,10); if (i<6) Assert.IsTrue(PrototypeSession.AdvanceStage()); }
            PrototypeSession.Disconnect(3); PrototypeSave.CompleteChapter(); PrototypeSave.CompleteChapter();
            Assert.AreEqual(1, PrototypeSave.Progress.records.Count); Assert.AreEqual(600, PrototypeSave.Progress.records[0].score);
            Assert.AreEqual(2, PrototypeSave.Progress.unlockedChapter);
        }
        [Test] public void CorruptOrFutureSaveCannotOverwriteOriginal()
        {
            Directory.CreateDirectory(_saveRoot); var path = Path.Combine(_saveRoot,"progress.json");
            const string original = "{\"schemaVersion\":999,\"unlockedChapter\":7}"; File.WriteAllText(path,original);
            PrototypeSave.Reload(); Assert.AreEqual(1, PrototypeSave.Progress.unlockedChapter);
            Assert.IsFalse(PrototypeSave.WriteProgress()); Assert.AreEqual(original,File.ReadAllText(path));
        }
        [Test] public void SeedsAreRepeatableAndConnectionsAreWithinRoomDoorBounds()
        {
            for(var seed=0;seed<300;seed++)
            {
                var a=new PrototypeRoomLayout(seed); var b=new PrototypeRoomLayout(seed);
                Assert.IsTrue(a.Validate()); CollectionAssert.AreEqual(a.CorridorX,b.CorridorX);
                CollectionAssert.AreEqual(a.ItemPositions,b.ItemPositions);
            }
        }
        [Test] public void KeyRemapRejectsDuplicatesAndReservedKeys()
        {
            var data=new PrototypeSettingsData();
            Assert.IsFalse(PrototypeInput.Rebind(data,"Jump",UnityEngine.InputSystem.Key.W));
            Assert.IsFalse(PrototypeInput.Rebind(data,"Jump",UnityEngine.InputSystem.Key.Escape));
            Assert.IsTrue(PrototypeInput.Rebind(data,"Jump",UnityEngine.InputSystem.Key.R));
        }
        [Test] public void RankingNeverMixesChaptersOrRuleVersionsAndBreaksTiesByTime()
        {
            var records = PrototypeSave.Progress.records;
            records.Add(new PrototypeTeamRecord { chapter=1,participants=4,score=100,seconds=20,scoreRuleVersion="a" });
            records.Add(new PrototypeTeamRecord { chapter=1,participants=4,score=100,seconds=10,scoreRuleVersion="a" });
            records.Add(new PrototypeTeamRecord { chapter=1,participants=4,score=999,seconds=1,scoreRuleVersion="b" });
            records.Add(new PrototypeTeamRecord { chapter=2,participants=4,score=999,seconds=1,scoreRuleVersion="a" });
            records.Add(new PrototypeTeamRecord { chapter=1,participants=2,score=999,seconds=1,scoreRuleVersion="a" });
            var ranked=PrototypeSave.Ranking(1,"a"); Assert.AreEqual(2,ranked.Count); Assert.AreEqual(10,ranked[0].seconds);
        }
        [Test] public void QualityPreviewMatchesResolvedScaleWithoutMutatingAppliedSettings()
        {
            var original=PrototypeSettings.Copy(); var data=PrototypeSettings.Copy(); data.quality="낮음";
            try
            {
                PrototypeSettings.Apply(data,false); Assert.AreEqual(.7f,PrototypeSettings.ResolvedRenderScale(data));
                data.quality="높음"; Assert.AreEqual("낮음",PrototypeSettings.Current.quality);
            }
            finally { PrototypeSettings.Apply(original,false); }
        }
        [Test] public void NecklaceDirectionFollowsRelativeHeading()
        {
            Assert.AreEqual("↑ 앞",PrototypeStoryMemory.Compass(Vector3.forward,Vector3.forward));
            Assert.AreEqual("→ 오른쪽",PrototypeStoryMemory.Compass(Vector3.forward,Vector3.right));
            Assert.AreEqual("↓ 뒤",PrototypeStoryMemory.Compass(Vector3.right,Vector3.left));
        }
        [TestCase(0f,1f,true)] [TestCase(1f,1f,true)] [TestCase(1f,0f,false)] [TestCase(0f,-1f,false)]
        public void SprintOnlyWorksForward(float x,float y,bool expected)
        {
            Assert.AreEqual(expected,PrototypeMovementRules.CanSprint(new Vector2(x,y),true,false,100));
            Assert.IsFalse(PrototypeMovementRules.CanSprint(new Vector2(x,y),true,true,100));
            Assert.IsFalse(PrototypeMovementRules.CanSprint(new Vector2(x,y),true,false,0));
        }
        [Test] public void FrontFacingClimbRightMovesToWorldRight()
        { Assert.AreEqual(Vector3.right,PrototypeMovementRules.ClimbRight(Vector3.back)); }
        [TestCase(2)] [TestCase(3)] [TestCase(4)] public void EveryStartingPartySizeClosesOnlyAfterItsFullRoster(int count)
        {
            PrototypeSession.PartySize=count; PrototypeSession.BeginChapter(1);
            var exit=Exit(); exit.Configure(exit.transform,count,10);
            for(var i=0;i<count;i++)
            {
                Assert.IsTrue(exit.TryRegisterArrival(Actor(i),12));
                Assert.AreEqual(i==count-1,exit.IsSettled);
            }
            Assert.AreEqual(count,exit.ArrivedCount);
            Assert.AreEqual(980+(count-1)*250,exit.TeamScore);
        }
    }
}
