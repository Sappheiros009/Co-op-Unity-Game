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
    public sealed class PrototypeMotorTests
    {
        private PrototypeGame _game;
        private PrototypeCapsulePlayer _player;
        [UnitySetUp] public IEnumerator Setup()
        {
            PrototypeSave.RootOverride = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "PrototypeMotorTests", Guid.NewGuid().ToString("N")));
            PrototypeSave.Reload(); PrototypeSession.PartySize = 4; PrototypeSession.Practice = false; PrototypeSession.BeginChapter(1);
            yield return SceneManager.LoadSceneAsync("PrototypeChapter01"); yield return null;
            _game = Object.FindFirstObjectByType<PrototypeGame>(); _player = _game.Player;
            _player.enabled = false; _player.Interaction.enabled = false;
            foreach (var bot in _game.Teammates) { bot.SetOrder(PrototypeBotOrder.Hold); bot.enabled = false; }
            PrototypeCapsulePlayer.SetCursor(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            PrototypeUi.CloseModal(); PrototypeCapsulePlayer.SetCursor(false);
            yield return SceneManager.LoadSceneAsync("PrototypeLobby");
            PrototypeSave.RootOverride = null; PrototypeSave.Reload();
        }
        private void Step(PrototypeMotorInput input, int count = 1)
        {
            for (var i = 0; i < count; i++) _player.StepMotor(input, 1f / 60);
            Physics.SyncTransforms();
        }
        [UnityTest] public IEnumerator MotorSprintIsForwardOnlyAndConsumesStamina()
        {
            _player.Teleport(new Vector3(0, .05f, -5)); Step(default, 5);
            var before = _player.transform.position; var stamina = _player.Stamina;
            Step(new PrototypeMotorInput { move = Vector2.up, sprint = true }, 20);
            Assert.That(_player.transform.position.z - before.z, Is.EqualTo(PrototypeTuning.Current.sprintSpeed / 3).Within(.15));
            Assert.Less(_player.Stamina, stamina);
            _player.Teleport(new Vector3(0, .05f, -5)); Step(default, 5); before = _player.transform.position;
            Step(new PrototypeMotorInput { move = Vector2.left, sprint = true }, 20);
            Assert.That(before.x - _player.transform.position.x, Is.EqualTo(PrototypeTuning.Current.walkSpeed / 3).Within(.15));
            yield return null;
        }
        [UnityTest] public IEnumerator LowCeilingPreventsStandingUntilClear()
        {
            Step(new PrototypeMotorInput { crouch = true });
            _player.Teleport(new Vector3(6, .04f, 1)); Physics.SyncTransforms();
            Step(default, 5);
            Assert.IsTrue(_player.Participant.IsCrouching);
            Assert.That(_player.GetComponent<CharacterController>().height, Is.EqualTo(1));
            _player.Teleport(new Vector3(3, .04f, -2)); Step(default, 5);
            Assert.IsFalse(_player.Participant.IsCrouching);
            Assert.That(_player.GetComponent<CharacterController>().height, Is.EqualTo(1.8f));
            yield return null;
        }
        [UnityTest] public IEnumerator MarkedWallClimbsRightAndJumpsAwayWithoutImmediateReattach()
        {
            _player.Teleport(new Vector3(-7, .1f, 1)); Physics.SyncTransforms();
            Step(new PrototypeMotorInput { climb = true, move = Vector2.up }, 20);
            Assert.IsTrue(_player.IsClimbing); Assert.Greater(_player.transform.position.y, .6f);
            var x = _player.transform.position.x;
            Step(new PrototypeMotorInput { climb = true, move = Vector2.right }, 10);
            Assert.Greater(_player.transform.position.x, x + .3f);
            _player.ViewCamera.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var z = _player.transform.position.z;
            Step(new PrototypeMotorInput { climb = true, jump = true });
            Step(new PrototypeMotorInput { climb = true }, 8);
            Assert.IsFalse(_player.IsClimbing); Assert.Less(_player.transform.position.z, z - .3f);
            yield return null;
        }
        [UnityTest] public IEnumerator HoldingUpMantlesOntoClearLedge()
        {
            _player.Teleport(new Vector3(-7, .1f, 1)); Physics.SyncTransforms();
            Step(new PrototypeMotorInput { climb = true, move = Vector2.up }, 95);
            Assert.Greater(_player.transform.position.y, 3.4f, "Did not reach the top of the marked wall");
            Assert.Greater(_player.transform.position.z, 1.5f, "Did not move onto the ledge");
            Assert.IsFalse(_player.IsMantling);
            yield return null;
        }
        [UnityTest] public IEnumerator ModalDoesNotFreezeAirbornePlayer()
        {
            _player.Teleport(new Vector3(0, 4, -5)); _player.enabled = true;
            PrototypeSettingsPanel.Open(_game.transform);
            yield return new WaitForSeconds(1);
            Assert.Less(_player.transform.position.y, .5f);
            Assert.IsTrue(PrototypeUi.IsModalOpen);
        }
        [UnityTest] public IEnumerator MarkedBoxCornerCanBeTraversedWithoutDropping()
        {
            _player.Teleport(new Vector3(-5.6f,.8f,1)); Physics.SyncTransforms();
            Step(new PrototypeMotorInput { climb=true });
            for(var i=0;i<40;i++)
            {
                Step(new PrototypeMotorInput { climb=true,move=Vector2.right });
                Assert.IsTrue(_player.IsClimbing,$"Lost corner at step {i}: {_player.transform.position}");
            }
            Assert.Greater(_player.transform.position.z,1.6f);
            yield return null;
        }
        [UnityTest] public IEnumerator BufferedJumpBeforeLandingIsConsumedOnLanding()
        {
            _player.Teleport(new Vector3(0,.22f,-5)); Physics.SyncTransforms();
            Step(new PrototypeMotorInput { jump=true });
            var maxY=_player.transform.position.y; var end=Time.time+.8f;
            while(Time.time<end)
            { yield return null; _player.StepMotor(default,Time.deltaTime); maxY=Mathf.Max(maxY,_player.transform.position.y); }
            Assert.Greater(maxY,.9f,"The buffered jump was not performed on landing");
        }
        [UnityTest] public IEnumerator CoyoteWindowAllowsBriefGapButNotExpiredGap()
        {
            var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.transform.position=new Vector3(0,4.75f,-5); platform.transform.localScale=new Vector3(2,.5f,2);
            _player.Teleport(new Vector3(0,5.05f,-5)); Physics.SyncTransforms(); Step(default,5);
            platform.SetActive(false); Physics.SyncTransforms(); yield return null;
            Step(new PrototypeMotorInput { jump=true }); Step(default,5);
            Assert.Greater(_player.transform.position.y,5.4f);
            platform.SetActive(true); _player.Teleport(new Vector3(0,5.05f,-5)); Physics.SyncTransforms(); Step(default,5);
            platform.SetActive(false); Physics.SyncTransforms();
            // Keep stepping the motor as normal play does. Pausing it would retain the previous Move's isGrounded flag.
            var deadline=Time.time+.2f;
            while(Time.time<deadline) { yield return null; _player.StepMotor(default,Time.deltaTime); }
            Assert.IsFalse(_player.GetComponent<CharacterController>().isGrounded);
            Step(new PrototypeMotorInput { jump=true }); Step(default,5);
            Assert.Less(_player.transform.position.y,5.1f);
        }
        [UnityTest] public IEnumerator OpposingPushesCancelAndSameActorRequestsDoNotStack()
        {
            var box=GameObject.Find("Pushable_HeavyBox").GetComponent<PrototypeCarryable>();
            var body=box.GetComponent<Rigidbody>(); body.useGravity=false; body.linearVelocity=Vector3.zero;
            var origin=box.transform.position;
            for(var frame=0;frame<12;frame++)
            {
                for(var duplicate=0;duplicate<8;duplicate++) box.Push(Vector3.right,0);
                box.Push(Vector3.left,1); yield return new WaitForFixedUpdate();
            }
            Assert.That(Vector3.Distance(origin,box.transform.position),Is.LessThan(.01f));
            for(var frame=0;frame<12;frame++)
            { box.Push(Vector3.right,0); box.Push(Vector3.zero,1); yield return new WaitForFixedUpdate(); }
            Assert.Greater(box.transform.position.x,origin.x+.04f);
        }
        [Test] public void KeyboardAdapterReadsRealInputSystemState()
        {
            var previous = Keyboard.current; var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(PrototypeInput.Binding("Forward"), PrototypeInput.Binding("Sprint")));
                InputSystem.Update();
                Assert.IsTrue(PrototypeInput.Held("Forward")); Assert.IsTrue(PrototypeInput.Held("Sprint"));
                Assert.IsFalse(PrototypeInput.Held("Backward"));
            }
            finally { InputSystem.RemoveDevice(keyboard); if (previous != null && previous.added) previous.MakeCurrent(); }
        }
    }
}
