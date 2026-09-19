using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeExitScoringTests
    {
        private GameObject _exitObject;
        private GameObject[] _participantObjects;

        [SetUp]
        public void SetUp()
        {
            PrototypeSession.PartySize = 4;
            PrototypeSession.BeginChapter(1);
            _exitObject = new GameObject("Test Exit");
            _participantObjects = new GameObject[4];
            for (var index = 0; index < _participantObjects.Length; index++)
            {
                _participantObjects[index] = new GameObject("Test Participant " + (index + 1));
                var participant = _participantObjects[index].AddComponent<PrototypeParticipant>();
                participant.Configure(index, "Test " + (index + 1));
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_exitObject != null)
            {
                Object.DestroyImmediate(_exitObject);
            }

            if (_participantObjects == null)
            {
                return;
            }

            foreach (var participantObject in _participantObjects)
            {
                if (participantObject != null)
                {
                    Object.DestroyImmediate(participantObject);
                }
            }
        }

        [Test]
        public void FirstArrivalStartsWindowAndDuplicateArrivalDoesNotIncreaseCount()
        {
            var exit = ConfigureExit();
            var participant = _participantObjects[0].GetComponent<PrototypeParticipant>();

            exit.RegisterArrival(participant);
            exit.RegisterArrival(participant);

            Assert.IsTrue(exit.HasStartedSettlement);
            Assert.AreEqual(1, exit.ArrivedCount);
            Assert.IsFalse(exit.IsSettled);
            Assert.IsTrue(participant.HasEnteredExit);
        }

        [Test]
        public void DownedParticipantCannotStartOrJoinSettlement()
        {
            var exit = ConfigureExit();
            var participant = _participantObjects[0].GetComponent<PrototypeParticipant>();
            participant.MarkDown();

            exit.RegisterArrival(participant);

            Assert.IsFalse(exit.HasStartedSettlement);
            Assert.AreEqual(0, exit.ArrivedCount);
        }

        [Test]
        public void StageStartRosterSettlesEarlyWhenAllFourArrive()
        {
            var exit = ConfigureExit();

            foreach (var participantObject in _participantObjects)
            {
                exit.RegisterArrival(participantObject.GetComponent<PrototypeParticipant>());
            }

            Assert.IsTrue(exit.IsSettled);
            Assert.AreEqual(4, exit.ArrivedCount);
            Assert.Greater(exit.TeamScore, 0);
            StringAssert.Contains("전원 도착", exit.SettlementReason);
        }

        [UnityTest]
        public IEnumerator SettlementClosesAfterFiveSecondWindow()
        {
            var exit = ConfigureExit();
            exit.RegisterArrival(_participantObjects[0].GetComponent<PrototypeParticipant>());

            yield return new WaitForSeconds(PrototypeExitScoring.ExitWindowSeconds + 0.15f);

            Assert.IsTrue(exit.IsSettled);
            Assert.AreEqual(1, exit.ArrivedCount);
            StringAssert.Contains("5초", exit.SettlementReason);
        }

        private PrototypeExitScoring ConfigureExit()
        {
            var exit = _exitObject.AddComponent<PrototypeExitScoring>();
            exit.Configure(_exitObject.transform, _participantObjects.Length, Time.time);
            return exit;
        }
    }
}
