using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeNetworkProgressTests
    {
        [SetUp] public void Setup()
        {
            PrototypeSave.RootOverride = Path.GetFullPath(Path.Combine(Application.dataPath,"..","Temp","NetworkProgressTests",Guid.NewGuid().ToString("N")));
            PrototypeSave.Reload();
        }
        [TearDown] public void Cleanup() { PrototypeSession.Practice = false; PrototypeSave.RootOverride = null; }
        private static PrototypeNetworkWorldState Completed(int count = 4)
        {
            var state = new PrototypeNetworkWorldState
            {
                runId = Guid.NewGuid().ToString("N"), phase = "Story", chapter = 3, stage = 6, sequence = 1,
                yourActor = 0, roles = count, score = 1234, elapsed = 98.5f, arrivals = 1, settled = true,
                actors = new PrototypeNetworkActorState[count], chapterCompleted = true
            };
            for (var i = 0; i < count; i++) state.actors[i] = new PrototypeNetworkActorState
            {
                id = i, slot = i, name = "Peer " + i, cameraHeight = 1.55f, connected = true,
                alive = i == 0, exited = i == 0, items = Array.Empty<PrototypeItemKind>()
            };
            state.completion = new PrototypeChapterCompletion { runId = state.runId, chapter = state.chapter,
                participants = count, score = state.score, seconds = state.elapsed, scoreRuleVersion = "fixture-rules", gameBuildVersion = "fixture-build" };
            Assert.IsTrue(state.IsValid()); return state;
        }
        [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void ServerResultWinsOverClientGlobalsAndUsesFixedStartingCount(int count)
        {
            PrototypeSession.PartySize = 2; PrototypeSession.BeginChapter(7); PrototypeSession.Practice = true;
            var state = Completed(count); state.actors[count - 1].connected = false;
            var projection = new PrototypeNetworkProgress(); Assert.IsTrue(projection.Apply(state));
            Assert.AreEqual(4, PrototypeSave.Progress.unlockedChapter); Assert.AreEqual(4, PrototypeSave.Progress.completedMask);
            Assert.AreEqual(count == 4 ? 1 : 0, PrototypeSave.Progress.records.Count);
            if (count == 4)
            {
                var record = PrototypeSave.Progress.records[0]; Assert.AreEqual(1234, record.score); Assert.AreEqual(98.5f, record.seconds);
                Assert.AreEqual(4, record.participants); Assert.AreEqual("fixture-rules", record.scoreRuleVersion);
                Assert.AreEqual("fixture-build", record.gameBuildVersion);
            }
            PrototypeSave.Reload(); Assert.AreEqual(4, PrototypeSave.Progress.completedMask);
        }
        [Test] public void RepeatedStoryAndWaitingSnapshotsDoNotWriteOrDuplicateRecords()
        {
            var state = Completed(); var projection = new PrototypeNetworkProgress(); Assert.IsTrue(projection.Apply(state));
            using (File.Open(Path.Combine(PrototypeSave.Root,"progress.json"),FileMode.Open,FileAccess.Read,FileShare.None))
            {
                for (var i = 0; i < 20; i++) { state.sequence++; state.phase = i < 10 ? "Story" : "WaitingRoom"; Assert.IsTrue(projection.Apply(state)); }
                Assert.AreEqual("", PrototypeSave.Error); Assert.IsFalse(PrototypeSave.HasPendingProgress);
            }
            Assert.AreEqual(1, projection.AppliedCompletions); Assert.AreEqual(1, PrototypeSave.Progress.records.Count);
            // A fresh connection/process can receive the same result; the file's run ID still deduplicates it.
            PrototypeSave.Reload(); Assert.IsTrue(new PrototypeNetworkProgress().Apply(state)); Assert.AreEqual(1, PrototypeSave.Progress.records.Count);
        }
        [Test] public void CompletionMustMatchServerRunFinalStageScoreTimeAndRoster()
        {
            Action<PrototypeNetworkWorldState>[] mutations = {
                s => s.stage = 5, s => s.phase = "Playing", s => s.phase = "Lobby", s => s.settled = false,
                s => s.arrivals = 0, s => s.completion.runId = "another-run", s => s.completion.chapter = 2,
                s => s.completion.participants = 2, s => s.completion.score++, s => s.completion.seconds++,
                s => s.completion.seconds = float.NaN, s => s.completion.scoreRuleVersion = "", s => s.completion = null,
                s => s.yourActor = -1
            };
            foreach (var mutate in mutations)
            {
                var state = Completed(); mutate(state); Assert.IsFalse(new PrototypeNetworkProgress().Apply(state));
                Assert.AreEqual(0, PrototypeSave.Progress.completedMask);
            }
            Assert.IsFalse(File.Exists(Path.Combine(PrototypeSave.Root,"progress.json")));
        }
        [Test] public void ConflictingRepeatCannotRewriteAnAcceptedTwoOrFourPlayerResult()
        {
            foreach (var count in new[] { 2, 4 })
            {
                var state = Completed(count); var projection = new PrototypeNetworkProgress(); Assert.IsTrue(projection.Apply(state));
                state.score++; state.completion.score++; Assert.IsFalse(projection.Apply(state));
                Assert.AreEqual(1, projection.AppliedCompletions);
            }
            Assert.AreEqual(1234, PrototypeSave.Progress.records[0].score);
        }
        [Test] public void LockedFileKeepsPendingResultForExplicitRetryWithoutDuplication()
        {
            Assert.IsTrue(PrototypeSave.WriteProgress()); var state = Completed(); var projection = new PrototypeNetworkProgress();
            using (File.Open(Path.Combine(PrototypeSave.Root,"progress.json"),FileMode.Open,FileAccess.Read,FileShare.None))
            {
                Assert.IsTrue(projection.Apply(state)); Assert.IsTrue(PrototypeSave.HasPendingProgress); Assert.IsNotEmpty(PrototypeSave.Error);
                Assert.IsTrue(projection.Apply(state)); Assert.AreEqual(1, PrototypeSave.Progress.records.Count);
            }
            Assert.IsTrue(PrototypeSave.WriteProgress()); PrototypeSave.Reload();
            Assert.AreEqual(1, PrototypeSave.Progress.records.Count); Assert.AreEqual(4, PrototypeSave.Progress.completedMask);
            Assert.IsFalse(PrototypeSave.HasPendingProgress);
        }
        [Test] public void FutureFileIsNeverOverwrittenByANetworkCompletion()
        {
            Directory.CreateDirectory(PrototypeSave.Root); var path = Path.Combine(PrototypeSave.Root,"progress.json");
            const string future = "{\"schemaVersion\":999,\"unlockedChapter\":7}";
            File.WriteAllText(path,future); PrototypeSave.Reload();
            Assert.IsTrue(new PrototypeNetworkProgress().Apply(Completed())); Assert.IsTrue(PrototypeSave.ReadProtected);
            Assert.AreEqual(future,File.ReadAllText(path)); Assert.IsTrue(PrototypeSave.HasPendingProgress);
        }
        [Test] public void DiscoveredMemoriesSurviveWipeWithoutAwardingCompletion()
        {
            var state = Completed(); state.phase = "Playing"; state.stage = 1; state.chapterCompleted = false;
            state.journal = new[] { "[임시 기억] 서버 발견" }; var projection = new PrototypeNetworkProgress();
            Assert.IsTrue(projection.Apply(state)); state.phase = "Lobby"; state.journal = Array.Empty<string>();
            Assert.IsTrue(projection.Apply(state)); PrototypeSave.Reload();
            Assert.AreEqual(1,PrototypeSave.Progress.memories.Count); Assert.AreEqual(0,PrototypeSave.Progress.completedMask);
            Assert.AreEqual(0,PrototypeSave.Progress.records.Count);
        }
    }
}
