using NUnit.Framework;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeNetworkRoomTests
    {
        private static PrototypeNetworkCommand Command(long sequence, string kind, int chapter = 1, bool ready = true)
            => new PrototypeNetworkCommand { sequence = sequence, kind = kind, chapter = chapter, ready = ready };
        [TestCase(2)] [TestCase(3)] [TestCase(4)] public void CapacityAndFixedStartRosterSurviveOwnerDeparture(int count)
        {
            var room = new PrototypeNetworkRoom(count);
            for (var i = 0; i < count; i++)
            { Assert.IsTrue(room.Join((ulong)(100+i), "Client" + i, out _)); Assert.IsTrue(room.Apply((ulong)(100+i), Command(1,"ready"), out _)); }
            Assert.IsFalse(room.Join(999,"Overflow",out var reason)); Assert.AreEqual("room_full",reason);
            Assert.IsTrue(room.Apply(100,Command(2,"reserve"),out _)); var run = room.RunId; var server = room.ServerId;
            room.Tick(.2); room.Leave(100); room.Tick(.2);
            var snapshot = room.Snapshot(101);
            Assert.AreEqual(1,room.Owner); Assert.AreEqual(count,room.StartingCount); Assert.AreEqual(count,snapshot.peers.Length);
            Assert.IsFalse(snapshot.peers[0].connected); Assert.AreEqual(run,room.RunId); Assert.AreEqual(server,room.ServerId);
            Assert.AreEqual(.4,room.ServerSeconds,.0001); Assert.IsFalse(room.Join(999,"Late",out reason)); Assert.AreEqual("run_locked",reason);
        }
        [Test] public void NonOwnerCannotSelectChapterOrReserveAndReplayedCommandsAreRejected()
        {
            var room = new PrototypeNetworkRoom(4); room.Join(10,"First",out _); room.Join(20,"Second",out _);
            Assert.IsFalse(room.Apply(20,Command(1,"chapter",7),out var error)); Assert.AreEqual("owner_only",error);
            Assert.AreEqual(1,room.Chapter); Assert.IsFalse(room.Apply(999,Command(1,"ready"),out error)); Assert.AreEqual("unknown_connection",error);
            Assert.IsTrue(room.Apply(10,Command(1,"chapter",3),out _));
            Assert.IsFalse(room.Apply(10,Command(1,"chapter",6),out error)); Assert.AreEqual("invalid_sequence",error); Assert.AreEqual(3,room.Chapter);
            Assert.IsFalse(room.Apply(20,Command(2,"reserve"),out error)); Assert.AreEqual("owner_only",error);
        }
        [Test] public void StartRequiresAtLeastTwoReadyPlayersAndChapterChangeClearsConsent()
        {
            var room = new PrototypeNetworkRoom(4); room.Join(10,"First",out _); room.Apply(10,Command(1,"ready"),out _);
            Assert.IsFalse(room.Apply(10,Command(2,"reserve"),out _)); room.Join(20,"Second",out _); room.Apply(20,Command(1,"ready"),out _);
            Assert.IsTrue(room.Apply(10,Command(3,"chapter",4),out _));
            Assert.IsFalse(room.Snapshot(10).peers[0].ready); Assert.IsFalse(room.Apply(10,Command(4,"reserve"),out _));
        }
        [Test] public void BadProtocolNamesSequencesAndMutableSnapshotsCannotAlterAuthority()
        {
            var room = new PrototypeNetworkRoom(2);
            Assert.IsFalse(room.CanJoin("future",out var error)); Assert.AreEqual("version_mismatch",error);
            Assert.IsFalse(room.Join(10,"<b>fake</b>",out _)); Assert.IsFalse(room.Join(10,"fake\nname",out _));
            Assert.IsTrue(room.Join(10,"Client",out _));
            Assert.IsFalse(room.Apply(10,Command(0,"ready"),out _)); Assert.IsFalse(room.Apply(10,Command(50000,"ready"),out _));
            var snapshot=room.Snapshot(10); snapshot.peers[0].ready=true;
            Assert.IsFalse(room.Snapshot(10).peers[0].ready); room.Tick(double.NaN); Assert.AreEqual(0,room.ServerSeconds);
        }
        [TestCase(2)] [TestCase(3)] [TestCase(4)] public void StoryRequiresEveryConnectedVoteAndDepartureNeverShrinksExitRoster(int count)
        {
            var room = new PrototypeNetworkRoom(count);
            for (var i=0;i<count;i++) { room.Join((ulong)(10+i),"Slime"+i,out _); room.Apply((ulong)(10+i),Command(1,"ready"),out _); }
            Assert.IsTrue(room.Apply(10,Command(2,"reserve"),out _)); var run=room.RunId;
            room.BeginStory(); Assert.AreEqual("Story",room.Snapshot(10).phase); Assert.IsFalse(room.AllStoryReady);
            for (var i=0;i<count-1;i++)
                Assert.IsTrue(room.Apply((ulong)(10+i),new PrototypeNetworkCommand{sequence=10,kind="story_ready",runId=run,ready=true},out _));
            Assert.IsFalse(room.AllStoryReady,"Owner/majority consent cannot skip the last connected participant.");
            Assert.IsFalse(room.Apply((ulong)(9+count),new PrototypeNetworkCommand{sequence=10,kind="story_ready",runId="old-run",ready=true},out var reason));
            Assert.AreEqual("stale_story",reason); Assert.IsFalse(room.AllStoryReady);
            room.Leave((ulong)(9+count)); Assert.IsTrue(room.AllStoryReady);
            Assert.AreEqual(count,room.StartingCount); Assert.AreEqual(count,room.StartingRoster().Length); Assert.AreEqual(run,room.RunId);
            room.ReturnToWaitingRoom(); Assert.AreEqual("WaitingRoom",room.Snapshot(10).phase);
            Assert.AreEqual(count-1,room.Snapshot(10).peers.Length); Assert.IsFalse(room.AllStoryReady);
            foreach (var peer in room.Snapshot(10).peers) { Assert.IsFalse(peer.ready); Assert.IsFalse(peer.storyReady); }
        }
        [Test] public void StoryVoteCanBeWithdrawnAndCannotBeCarriedIntoNewRun()
        {
            var room = new PrototypeNetworkRoom(2); room.Join(10,"A",out _); room.Join(20,"B",out _);
            room.Apply(10,Command(1,"ready"),out _); room.Apply(20,Command(1,"ready"),out _); room.Apply(10,Command(2,"reserve"),out _);
            var first=room.RunId;
            Assert.IsFalse(room.Apply(10,new PrototypeNetworkCommand{sequence=3,kind="story_ready",runId=first,ready=true},out _));
            room.BeginStory();
            room.Apply(10,new PrototypeNetworkCommand{sequence=4,kind="story_ready",runId=first,ready=true},out _);
            room.Apply(20,new PrototypeNetworkCommand{sequence=2,kind="story_ready",runId=first,ready=true},out _);
            Assert.IsTrue(room.AllStoryReady);
            room.Apply(20,new PrototypeNetworkCommand{sequence=3,kind="story_ready",runId=first,ready=false},out _); Assert.IsFalse(room.AllStoryReady);
            room.ReturnToWaitingRoom(); room.Apply(10,Command(5,"ready"),out _); room.Apply(20,Command(4,"ready"),out _);
            room.Apply(10,Command(6,"reserve"),out _); room.BeginStory(); Assert.AreNotEqual(first,room.RunId);
            Assert.IsFalse(room.Apply(10,new PrototypeNetworkCommand{sequence=7,kind="story_ready",runId=first,ready=true},out _));
            Assert.IsFalse(room.AllStoryReady);
        }
        [Test] public void FixedConnectionBindingsRemainSortedIndependentAndRetainedAcrossDeparture()
        {
            var room=new PrototypeNetworkRoom(4); room.Join(10,"A",out _); room.Join(20,"B",out _); room.Join(30,"C",out _);
            room.Leave(20); // Slots 0 and 2, not a contiguous actor array.
            room.Apply(10,Command(1,"ready"),out _); room.Apply(30,Command(1,"ready"),out _); room.Apply(10,Command(2,"reserve"),out _);
            var bindings=room.StartingRoster(); Assert.AreEqual(2,bindings.Length);
            Assert.AreEqual(0,bindings[0].slot); Assert.AreEqual(2,bindings[1].slot); Assert.AreEqual(30ul,bindings[1].connection);
            bindings[0]=new PrototypeNetworkStartMember(999,3,"mutated");
            room.Leave(10); Assert.AreEqual(10ul,room.StartingRoster()[0].connection); Assert.AreEqual(2,room.Owner);
            var input=new PrototypeNetworkInputBuffer(room.RunId,1,new[]{room.StartingRoster()[0].connection,room.StartingRoster()[1].connection});
            Assert.IsTrue(input.TryGetActor(30,out var actor)); Assert.AreEqual(1,actor);
        }
    }
}
