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
    public sealed class PrototypeInteractionTests
    {
        private PrototypeGame _game;
        private PrototypeCapsulePlayer _player;
        private PrototypeParticipant _ally;
        private Keyboard _keyboard, _previousKeyboard;
        private InputSettings.UpdateMode _previousUpdateMode;
        private InputSettings.EditorInputBehaviorInPlayMode _previousEditorInput;
        private InputSettings.BackgroundBehavior _previousBackground;
        private GameObject _blocker;
        [UnitySetUp] public IEnumerator Setup()
        {
            PrototypeSave.RootOverride = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "PrototypeInteraction", Guid.NewGuid().ToString("N")));
            PrototypeSession.SelectedSpecialty = PrototypeSpecialty.Scout;
            PrototypeSession.PartySize = 4; PrototypeSession.Practice = false; PrototypeSession.BeginChapter(1);
            yield return SceneManager.LoadSceneAsync("PrototypeChapter01"); yield return null;
            _game = Object.FindFirstObjectByType<PrototypeGame>(); _player = _game.Player;
            _player.enabled = false; _player.Interaction.enabled = false; _player.Teleport(new Vector3(0, .05f, -5));
            foreach (var bot in _game.Teammates)
            { bot.enabled = false; bot.SetOrder(PrototypeBotOrder.Hold); bot.Teleport(new Vector3(6, .05f, -6 + bot.Participant.ParticipantId)); }
            _ally = _game.Teammates[0].Participant; _game.Teammates[0].Teleport(new Vector3(0, .05f, -3));
            _player.ViewCamera.transform.LookAt(_ally.transform.position + Vector3.up * .7f);
            while (_player.Participant.Inventory.Items.Count > 0)
                _player.Participant.Inventory.Consume(_player.Participant.Inventory.Items[0]);
            Physics.SyncTransforms();
            _previousUpdateMode = InputSystem.settings.updateMode;
            _previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            _previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            _previousKeyboard = Keyboard.current; _keyboard = InputSystem.AddDevice<Keyboard>(); _keyboard.MakeCurrent();
            PrototypeCapsulePlayer.SetCursor(true);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
            if (_previousKeyboard != null && _previousKeyboard.added) _previousKeyboard.MakeCurrent();
            InputSystem.settings.updateMode = _previousUpdateMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = _previousEditorInput;
            InputSystem.settings.backgroundBehavior = _previousBackground;
            if (_blocker != null) Object.Destroy(_blocker);
            PrototypeUi.CloseModal(); PrototypeCapsulePlayer.SetCursor(false); PrototypeSession.SelectedSpecialty = PrototypeSpecialty.Healer;
            yield return SceneManager.LoadSceneAsync("PrototypeLobby"); PrototypeSave.RootOverride = null;
        }
        private IEnumerator Hold(float seconds, params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); InputSystem.Update();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); InputSystem.Update();
            var input = PrototypeInteraction.ReadInput();
            Assert.AreEqual(Array.IndexOf(keys, PrototypeInput.Binding("Interact")) >= 0, input.interactHeld);
            if (Array.IndexOf(keys, PrototypeInput.Binding("Use")) >= 0)
                Assert.IsTrue(input.usePressed, $"Use pressed edge missing: held={PrototypeInput.Held("Use")}; key={PrototypeInput.Binding("Use")}; keyboard={Keyboard.current?.deviceId}");
            // Batch mode has no focused Game window: inject input ownership, never weaken the runtime cursor/modal gate.
            input.hasControl = true;
            var elapsed = 0f;
            do
            {
                _player.Interaction.StepInteraction(input, Time.deltaTime); elapsed += Time.deltaTime;
                input.interactPressed = false; input.usePressed = false; input.dropPressed = false; input.selectedSlot = -1;
                yield return null;
            } while (elapsed < seconds);
        }
        private string Diagnostic()
        {
            var camera = _player.ViewCamera.transform;
            Physics.Raycast(camera.position, camera.forward, out var hit, PrototypeTuning.Current.interactionRange, ~0, QueryTriggerInteraction.Ignore);
            return $"cursor={Cursor.lockState}; canAct={_player.Participant.CanAct}; modal={PrototypeUi.IsModalOpen}; " +
                $"keyboard={Keyboard.current?.deviceId}/{_keyboard.deviceId}; E={PrototypeInput.Held("Interact")}; prompt={_player.Interaction.Prompt}; " +
                $"camera={camera.position}/{camera.forward}; ally={_ally.transform.position}; hit={hit.collider?.name}";
        }
        [UnityTest] public IEnumerator RescueReleaseCancelsAndCompletionConsumesOneKitWithTimedProtection()
        {
            _ally.MarkDown(); _player.Participant.Inventory.Add(PrototypeItemKind.Medkit);
            yield return Hold(.7f, PrototypeInput.Binding("Interact"));
            Assert.Greater(_player.Interaction.RescueProgress, .1f, Diagnostic()); Assert.IsFalse(_ally.IsAlive);
            yield return Hold(.1f); Assert.AreEqual(0, _player.Interaction.RescueProgress);
            Assert.IsTrue(_player.Participant.Inventory.Has(PrototypeItemKind.Medkit));
            yield return Hold(PrototypeTuning.Current.reviveSeconds + .12f, PrototypeInput.Binding("Interact"));
            Assert.IsTrue(_ally.IsAlive); Assert.IsFalse(_player.Participant.Inventory.Has(PrototypeItemKind.Medkit));
            Assert.AreEqual(PrototypeTuning.Current.reviveHealth, _ally.Health);
            _ally.Damage(10); Assert.AreEqual(PrototypeTuning.Current.reviveHealth, _ally.Health);
            yield return Hold(PrototypeTuning.Current.reviveProtection + .1f);
            _ally.Damage(10); Assert.AreEqual(PrototypeTuning.Current.reviveHealth - 10, _ally.Health);
        }
        [UnityTest] public IEnumerator WallOrLostLineOfSightCancelsRescueWithoutConsumingKit()
        {
            _ally.MarkDown(); _player.Participant.Inventory.Add(PrototypeItemKind.Medkit);
            yield return Hold(.5f, PrototypeInput.Binding("Interact")); Assert.Greater(_player.Interaction.RescueProgress, 0, Diagnostic());
            _blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); _blocker.name = "Test rescue LOS blocker";
            _blocker.transform.position = new Vector3(0, 1, -4); _blocker.transform.localScale = new Vector3(3, 3, .3f);
            Physics.SyncTransforms(); yield return Hold(.2f, PrototypeInput.Binding("Interact"));
            Assert.AreEqual(0, _player.Interaction.RescueProgress); Assert.IsFalse(_ally.IsAlive);
            Assert.IsTrue(_player.Participant.Inventory.Has(PrototypeItemKind.Medkit));
            _blocker.SetActive(false); Physics.SyncTransforms();
            yield return Hold(.5f, PrototypeInput.Binding("Interact")); Assert.That(_player.Interaction.RescueProgress, Is.InRange(.05f, .3f));
        }
        [UnityTest] public IEnumerator NonHealerWithoutKitCannotRescueAndMedkitOnlyHealsValidWoundedTarget()
        {
            _ally.MarkDown(); yield return Hold(.2f, PrototypeInput.Binding("Interact"));
            Assert.AreEqual(0, _player.Interaction.RescueProgress); Assert.IsFalse(_ally.IsAlive);
            _ally.ResetParticipant(); _player.Participant.Inventory.Add(PrototypeItemKind.Medkit);
            yield return Hold(.05f, PrototypeInput.Binding("Use")); Assert.IsTrue(_player.Participant.Inventory.Has(PrototypeItemKind.Medkit));
            yield return Hold(.05f); _ally.Damage(40);
            yield return Hold(.05f, PrototypeInput.Binding("Use"));
            Assert.AreEqual(100, _ally.Health, Diagnostic()); Assert.IsFalse(_player.Participant.Inventory.Has(PrototypeItemKind.Medkit));
        }
        [UnityTest] public IEnumerator HealerWithoutKitUsesLongerRescueDuration()
        {
            PrototypeSession.SelectedSpecialty = PrototypeSpecialty.Healer; _player.Participant.Configure(0, "시험 힐러");
            _ally.MarkDown();
            yield return Hold(PrototypeTuning.Current.reviveSeconds + .1f, PrototypeInput.Binding("Interact"));
            Assert.IsFalse(_ally.IsAlive); Assert.Greater(_player.Interaction.RescueProgress, .45f);
            yield return Hold(PrototypeTuning.Current.healerReviveSeconds - PrototypeTuning.Current.reviveSeconds + .1f, PrototypeInput.Binding("Interact"));
            Assert.IsTrue(_ally.IsAlive); Assert.AreEqual(0, _player.Participant.Inventory.Items.Count);
        }
        [UnityTest] public IEnumerator InputOwnershipLossAndModalBothCancelRescue()
        {
            _ally.MarkDown(); _player.Participant.Inventory.Add(PrototypeItemKind.Medkit);
            yield return Hold(.4f, PrototypeInput.Binding("Interact"));
            _player.Interaction.StepInteraction(new PrototypeInteractionInput { hasControl = false, interactHeld = true, selectedSlot = -1 }, .1f);
            Assert.AreEqual(0, _player.Interaction.RescueProgress);
            yield return Hold(.4f, PrototypeInput.Binding("Interact"));
            PrototypeSettingsPanel.Open(_game.transform);
            _player.Interaction.StepInteraction(new PrototypeInteractionInput { hasControl = true, interactHeld = true, selectedSlot = -1 }, .1f);
            Assert.AreEqual(0, _player.Interaction.RescueProgress); Assert.IsFalse(_ally.IsAlive);
            Assert.IsTrue(_player.Participant.Inventory.Has(PrototypeItemKind.Medkit));
        }
        [UnityTest] public IEnumerator PullRequiresUnobstructedLandingSpace()
        {
            _player.Teleport(new Vector3(0, 1.7f, -4.5f));
            _player.ViewCamera.transform.LookAt(_ally.transform.position + Vector3.up * .7f);
            var destination = _player.transform.position - _player.transform.forward * 1.1f;
            _blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); _blocker.name = "Test pull landing blocker";
            _blocker.transform.position = destination + Vector3.up; _blocker.transform.localScale = new Vector3(1, 2, 1);
            var origin = _ally.transform.position; Physics.SyncTransforms();
            yield return Hold(.05f, PrototypeInput.Binding("Interact")); Assert.AreEqual(origin, _ally.transform.position);
            StringAssert.Contains("공간", _game.StatusMessage);
            _blocker.SetActive(false); Physics.SyncTransforms();
            yield return Hold(.05f, PrototypeInput.Binding("Interact"));
            Assert.Less(Vector3.Distance(destination, _ally.transform.position), .05f);
        }
        [Test] public void InvalidHealingCannotCreateAnAliveZeroHealthParticipant()
        {
            _ally.Damage(40); _ally.Heal(-100); _ally.Heal(float.NaN); _ally.Heal(float.PositiveInfinity);
            Assert.IsTrue(_ally.IsAlive); Assert.AreEqual(60, _ally.Health);
            _ally.Heal(500); Assert.AreEqual(100, _ally.Health);
        }
    }
}
