using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeServerPlayerTests
    {
        private PrototypeGame _game;
        private GameObject _blocker;
        [UnitySetUp] public IEnumerator Setup()
        {
            PrototypeSave.RootOverride = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "PrototypeServerPlayer", Guid.NewGuid().ToString("N")));
            PrototypeUi.CloseModal(); PrototypeSession.Practice = false;
            PrototypeSession.SelectedSpecialty = PrototypeSpecialty.Scout;
            PrototypeSession.PartySize = 4; PrototypeSession.BeginChapter(1);
            yield return SceneManager.LoadSceneAsync("PrototypeLobby");
            CreateWorld(new[] { "서버 참가자 A", "서버 참가자 B", "서버 참가자 C", "서버 참가자 D" });
            yield return null; yield return null;
            Assert.AreEqual(4, _game.ControlledPlayers.Count);
            Physics.SyncTransforms();
        }
        private void CreateWorld(string[] names)
        {
            _game = new GameObject("Server controlled test world").AddComponent<PrototypeGame>();
            _game.ConfigureChapter(1); _game.ConfigureServerPlayers(names);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            PrototypeUi.CloseModal(); PrototypeCapsulePlayer.SetCursor(false);
            if (_blocker != null) Object.Destroy(_blocker);
            PrototypeSession.SelectedSpecialty = PrototypeSpecialty.Healer;
            PrototypeSession.PartySize = 4;
            yield return SceneManager.LoadSceneAsync("PrototypeLobby");
            PrototypeSave.RootOverride = null; PrototypeSave.Reload();
        }
        private PrototypePlayerInput AimAt(PrototypeCapsulePlayer player, Vector3 point)
        {
            var direction = point - player.ViewCamera.transform.position;
            var input = PrototypePlayerInput.Neutral(Mathf.Repeat(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 360),
                -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg);
            input.hasControl = input.interaction.hasControl = true;
            return input;
        }
        private void PrepareRescue(out PrototypeCapsulePlayer rescuer, out PrototypeCapsulePlayer ally)
        {
            rescuer = _game.ControlledPlayers[0]; ally = _game.ControlledPlayers[1];
            rescuer.Teleport(new Vector3(0, .05f, -5)); ally.Teleport(new Vector3(0, .05f, -3));
            _game.ControlledPlayers[2].Teleport(new Vector3(6, .05f, -5));
            _game.ControlledPlayers[3].Teleport(new Vector3(-6, .05f, -5));
            ally.Participant.MarkDown(); rescuer.Participant.Inventory.Add(PrototypeItemKind.Medkit);
            Physics.SyncTransforms();
        }
        [UnityTest] public IEnumerator TwoThreeAndFourPlayerWorldsContainNoBotsOrActivePlayerCameras()
        {
            for (var count = 2; count <= 4; count++)
            {
                Object.Destroy(_game.gameObject); yield return null;
                var names = new string[count]; for (var i = 0; i < count; i++) names[i] = "Participant " + i;
                CreateWorld(names); names[0] = "mutated caller array";
                yield return null;
                Assert.AreEqual(count, _game.Participants.Count); Assert.AreEqual(count, _game.ControlledPlayers.Count);
                Assert.AreEqual(0, _game.Teammates.Count); Assert.AreEqual(count, _game.Objective.RequiredRoles);
                Assert.AreEqual(count, _game.Exit.StageStartParticipantCount);
                Assert.IsNull(_game.GetComponent<PrototypeHud>());
                Assert.AreEqual(0, _game.GetComponentsInChildren<AudioListener>(true).Length);
                for (var i = 0; i < count; i++)
                {
                    var player = _game.ControlledPlayers[i];
                    Assert.AreEqual(i, player.Participant.ParticipantId); Assert.AreEqual("Participant " + i, player.Participant.DisplayName);
                    Assert.AreEqual(PrototypePlayerControl.Server, player.Control); Assert.IsFalse(player.ReadsLocalInput);
                    Assert.IsFalse(player.ViewCamera.enabled); Assert.AreEqual("Untagged", player.ViewCamera.tag);
                    Assert.IsTrue(player.transform.Find("CharacterParts/Head").gameObject.activeSelf);
                }
            }
        }
        [UnityTest] public IEnumerator InputMovesOnlyItsBoundActorAndUsesThatActorsStamina()
        {
            var moving = _game.ControlledPlayers[1]; var idle = _game.ControlledPlayers[0];
            var original = moving.transform.position; var idlePosition = idle.transform.position;
            var frame = PrototypePlayerInput.Neutral(0, 0); frame.hasControl = true;
            frame.motor.move = Vector2.up; frame.motor.sprint = true;
            for (var i = 0; i < 12; i++) Assert.IsTrue(moving.StepServerInput(frame, 1f / 30));
            Assert.Greater(moving.transform.position.z, original.z + 2);
            Assert.Less(moving.Stamina, PrototypeTuning.Current.staminaMaximum);
            Assert.AreEqual(idlePosition, idle.transform.position); Assert.AreEqual(PrototypeTuning.Current.staminaMaximum, idle.Stamina);
            yield return null;
        }
        [UnityTest] public IEnumerator ServerActorsDoNotPollTheHostKeyboardOrAdvanceWithoutATick()
        {
            var previous = Keyboard.current; var keyboard = InputSystem.AddDevice<Keyboard>();
            var positions = new Vector3[4]; for (var i = 0; i < 4; i++) positions[i] = _game.ControlledPlayers[i].transform.position;
            try
            {
                keyboard.MakeCurrent(); PrototypeCapsulePlayer.SetCursor(true);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(PrototypeInput.Binding("Forward"), PrototypeInput.Binding("Sprint")));
                InputSystem.Update(); yield return null; yield return null;
                for (var i = 0; i < 4; i++) Assert.AreEqual(positions[i], _game.ControlledPlayers[i].transform.position);
            }
            finally { InputSystem.RemoveDevice(keyboard); if (previous != null && previous.added) previous.MakeCurrent(); }
        }
        [UnityTest] public IEnumerator FixedTickServerMotorTakesActualHazardDamage()
        {
            var player=_game.ControlledPlayers[0]; player.Teleport(new Vector3(8.6f,.15f,49));
            var driver=_game.gameObject.AddComponent<PrototypeHazardServerMotorProbe>(); driver.Player=player;
            yield return new WaitForSeconds(.35f);
            Assert.Less(player.Health,100,"Server motor participant must take damage inside the real floor volume.");
            yield return new WaitForSeconds(1.1f);
            Assert.IsFalse(player.Participant.IsAlive);
            Assert.IsFalse(_game.IsTransitioning,"One downed player must not wipe the surviving party.");
        }
        [UnityTest] public IEnumerator ServerRescueIgnoresHostModalButHonorsPlayersReleasedControl()
        {
            PrepareRescue(out var player, out var ally);
            var frame = AimAt(player, ally.transform.position + Vector3.up * .7f); frame.interaction.interactHeld = true;
            PrototypeSettingsPanel.Open(_game.transform);
            for (var i = 0; i < 10; i++) Assert.IsTrue(player.StepServerInput(frame, .05f));
            Assert.Greater(player.Interaction.RescueProgress, .1f, player.Interaction.Prompt);
            StringAssert.StartsWith("{key:Interact}",player.Interaction.Prompt);
            Assert.IsTrue(player.StepServerInput(PrototypePlayerInput.Neutral(), .05f));
            Assert.AreEqual(0, player.Interaction.RescueProgress); Assert.IsFalse(ally.Participant.IsAlive);
            Assert.IsTrue(player.Participant.Inventory.Has(PrototypeItemKind.Medkit));
            for (var i = 0; i < 65; i++) Assert.IsTrue(player.StepServerInput(frame, .05f));
            Assert.IsTrue(ally.Participant.IsAlive); Assert.IsFalse(player.Participant.Inventory.Has(PrototypeItemKind.Medkit));
            Assert.IsTrue(PrototypeUi.IsModalOpen);
            yield return null;
        }
        [UnityTest] public IEnumerator PullMovesAnActualControlledPlayerOnlyWhenLandingIsClear()
        {
            PrepareRescue(out var player, out var ally); ally.Participant.ResetParticipant();
            player.Teleport(new Vector3(0, 1.7f, -4.5f));
            var frame = AimAt(player, ally.transform.position + Vector3.up * .7f); frame.interaction.interactPressed = true;
            var destination = player.transform.position - Vector3.forward * 1.1f;
            _blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _blocker.transform.position = destination + Vector3.up; _blocker.transform.localScale = new Vector3(1, 2, 1);
            var origin = ally.transform.position; Physics.SyncTransforms();
            Assert.IsTrue(player.StepServerInput(frame, .02f)); Assert.AreEqual(origin, ally.transform.position);
            StringAssert.Contains("공간", _game.StatusMessage);
            _blocker.SetActive(false); player.Teleport(new Vector3(0, 1.7f, -4.5f)); Physics.SyncTransforms();
            Assert.IsTrue(player.StepServerInput(frame, .02f));
            Assert.Less(Vector3.Distance(destination, ally.transform.position), .05f);
            yield return null;
        }
        [UnityTest] public IEnumerator DisconnectedOrExitedPlayerCannotMoveOrConsumeItems()
        {
            var player = _game.ControlledPlayers[2];
            player.Participant.Damage(40); player.Participant.Inventory.Add(PrototypeItemKind.Medkit);
            var position = player.transform.position;
            var frame = PrototypePlayerInput.Neutral(); frame.hasControl = frame.interaction.hasControl = true;
            frame.motor.move = Vector2.up; frame.interaction.selectedSlot = 0; frame.interaction.usePressed = true;
            PrototypeSession.Disconnect(2); player.Participant.SetConnected(false);
            Assert.IsTrue(player.StepServerInput(frame, .05f)); Assert.AreEqual(position, player.transform.position);
            Assert.AreEqual(60, player.Health); Assert.IsTrue(player.Participant.Inventory.Has(PrototypeItemKind.Medkit));
            PrototypeSession.Reconnect(2); player.Participant.SetConnected(true); player.Participant.MarkEnteredExit();
            Assert.IsTrue(player.StepServerInput(frame, .05f)); Assert.AreEqual(position, player.transform.position);
            Assert.AreEqual(60, player.Health); Assert.IsTrue(player.Participant.Inventory.Has(PrototypeItemKind.Medkit));
            yield return null;
        }
        [UnityTest] public IEnumerator InvalidInputAndDeltaDoNotMutatePhysicsOrHealth()
        {
            var player = _game.ControlledPlayers[1]; var origin = player.transform.position;
            var invalid = PrototypePlayerInput.Neutral(); invalid.hasControl = true; invalid.yaw = float.NaN;
            Assert.IsFalse(player.StepServerInput(invalid, .05f));
            invalid = PrototypePlayerInput.Neutral(); invalid.motor.move = new Vector2(float.PositiveInfinity, 1);
            Assert.IsFalse(player.StepServerInput(invalid, .05f));
            foreach (var dt in new[] { float.NaN, float.PositiveInfinity, -.1f, 0, 1 })
                Assert.IsFalse(player.StepServerInput(PrototypePlayerInput.Neutral(), dt));
            Assert.AreEqual(origin, player.transform.position); Assert.AreEqual(100, player.Health);
            player.StepMotor(new PrototypeMotorInput { move = Vector2.up }, float.NaN);
            Assert.AreEqual(origin, player.transform.position);
            yield return null;
        }
        [UnityTest] public IEnumerator ReplicaCannotRunSimulationAndServerExitDoesNotReleaseAnotherCursorsOwnership()
        {
            var obj = new GameObject("Test replica"); obj.transform.SetParent(_game.transform);
            var replica = obj.AddComponent<PrototypeCapsulePlayer>();
            replica.Configure(_game, new Vector3(7, .05f, -5), Color.cyan, 3, "Replica", PrototypePlayerControl.Replica);
            var position = replica.transform.position;
            replica.StepMotor(new PrototypeMotorInput { move = Vector2.up }, .05f);
            Assert.IsFalse(replica.StepServerInput(PrototypePlayerInput.Neutral(), .05f));
            Assert.AreEqual(position, replica.transform.position);
            PrototypeCapsulePlayer.SetCursor(true); var before = Cursor.lockState;
            var player = _game.ControlledPlayers[0]; player.Teleport(_game.Exit.EntryPoint.position);
            _game.Exit.SetGateOpen(true); Assert.IsTrue(_game.Exit.TryRegisterArrival(player.Participant, Time.time));
            Assert.AreEqual(before, Cursor.lockState); Assert.AreEqual(4, _game.Exit.StageStartParticipantCount);
            yield return null;
        }
        [UnityTest] public IEnumerator ServerStageCompletionSignalsCoordinatorOnceWithoutLoadingLocalScenes()
        {
            var calls = 0; var success = false; var scene = SceneManager.GetActiveScene().name;
            _game.ServerStageEnded += cleared => { calls++; success = cleared; };
            _game.Exit.SetGateOpen(true);
            foreach (var player in _game.ControlledPlayers)
            { player.Teleport(_game.Exit.EntryPoint.position); Assert.IsTrue(_game.Exit.TryRegisterArrival(player.Participant, Time.time)); }
            yield return new WaitForSeconds(3.3f);
            Assert.AreEqual(1, calls); Assert.IsTrue(success); Assert.AreEqual(scene, SceneManager.GetActiveScene().name);
            Assert.Greater(PrototypeSession.RunScore, 0);
            yield return null; Assert.AreEqual(1, calls);
        }
    }
}
