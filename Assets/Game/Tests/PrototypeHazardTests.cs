using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeHazardTests
    {
        private GameObject _root;
        private CharacterController _controller;
        private PrototypeParticipant _participant;
        private PrototypeHazard _hazard;
        [UnitySetUp] public IEnumerator Setup()
        {
            PrototypeSession.BeginNetworkChapter(1,"hazard-test",2,1839);
            _root=new GameObject("Hazard physics QA"); _root.transform.position=Vector3.right*1000;
            var game=_root.AddComponent<PrototypeGame>(); game.ConfigureServerPlayers(new[]{"Hazard participant","Other"}); game.enabled=false;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(_root.transform,false);
            floor.transform.localPosition=Vector3.down*.5f; floor.transform.localScale=new Vector3(10,1,10);
            var danger=GameObject.CreatePrimitive(PrimitiveType.Cube); danger.transform.SetParent(_root.transform,false);
            danger.transform.localPosition=Vector3.up*.12f; danger.transform.localScale=new Vector3(4,.25f,4);
            danger.GetComponent<BoxCollider>().isTrigger=true;
            _hazard=danger.AddComponent<PrototypeHazard>(); _hazard.DamagePerSecond=120;
            var actor=new GameObject("Controller"); actor.transform.SetParent(_root.transform,false);
            actor.transform.localPosition=new Vector3(0,.15f,0);
            var player=actor.AddComponent<PrototypeCapsulePlayer>();
            player.Configure(game,actor.transform.position,Color.green,0,"Hazard participant",PrototypePlayerControl.Server);
            game.RegisterControlledPlayer(player); _controller=player.GetComponent<CharacterController>(); _participant=player.Participant;
            Physics.SyncTransforms(); yield return new WaitForFixedUpdate();
        }
        [UnityTearDown] public IEnumerator Cleanup() { Object.Destroy(_root); yield return null; }
        private IEnumerator Step(int ticks)
        {
            for(var i=0;i<ticks;i++) { _controller.Move(Vector3.down*.02f); yield return new WaitForFixedUpdate(); }
        }
        [UnityTest] public IEnumerator RealFloorVolumeDamagesAndDownsControllerWithoutRigidbodies()
        {
            Assert.IsNull(_hazard.GetComponent<Rigidbody>());
            Assert.IsNull(_controller.GetComponent<Rigidbody>());
            yield return Step(8); Assert.Less(_participant.Health,100); Assert.Greater(_participant.Health,0);
            yield return Step(60); Assert.AreEqual(0,_participant.Health); Assert.IsFalse(_participant.IsAlive);
        }
        [UnityTest] public IEnumerator LeavingHazardStopsDamage()
        {
            yield return Step(8); Assert.Less(_participant.Health,100);
            _controller.enabled=false; _controller.transform.localPosition=new Vector3(4,.15f,0); _controller.enabled=true;
            Physics.SyncTransforms(); yield return Step(2);
            var health=_participant.Health; yield return Step(15); Assert.AreEqual(health,_participant.Health);
        }
        [UnityTest] public IEnumerator SafeExitParticipantCannotBeDamagedByHazard()
        {
            _participant.MarkEnteredExit(); var protectedHealth=_participant.Health;
            yield return Step(20); Assert.AreEqual(protectedHealth,_participant.Health);
        }
        [UnityTest] public IEnumerator FeetAboveShallowVolumeTakeNoDamage()
        {
            _controller.enabled=false; _controller.transform.localPosition=new Vector3(0,1,0); _controller.enabled=true;
            Physics.SyncTransforms(); var health=_participant.Health;
            yield return new WaitForSeconds(.3f); Assert.AreEqual(health,_participant.Health);
        }
    }
}
