using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeWaitingRoomTests
    {
        private PrototypeWaitingRoomController _room;
        [UnitySetUp] public IEnumerator Setup()
        {
            PrototypeUi.CloseModal(); PrototypeCapsulePlayer.SetCursor(false);
            PrototypeSave.RootOverride = Path.GetFullPath(Path.Combine(Application.dataPath,"..","Temp","PrototypeWaitingTests",Guid.NewGuid().ToString("N")));
            PrototypeSave.Reload(); PrototypeSession.PartySize = 4; PrototypeSession.AllChaptersForTesting = true;
            PrototypeSession.BeginChapter(1);
            yield return SceneManager.LoadSceneAsync("PrototypeWaitingRoom"); yield return null;
            _room = Object.FindFirstObjectByType<PrototypeWaitingRoomController>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            PrototypeUi.CloseModal(); PrototypeCapsulePlayer.SetCursor(false);
            yield return SceneManager.LoadSceneAsync("PrototypeLobby");
            PrototypeSave.RootOverride = null; PrototypeSave.Reload();
        }
        private PrototypeWaitingRoomStation Station(string name) => GameObject.Find(name).GetComponent<PrototypeWaitingRoomStation>();
        private void Approach(PrototypeWaitingRoomStation station)
        {
            _room.Walker.Teleport(station.transform.position - station.transform.forward * 2);
            _room.Walker.AimAt(station.FocusPoint); Physics.SyncTransforms();
        }
        [UnityTest] public IEnumerator RoomHasSevenWorldStationsOneFirstPersonCameraAndSeparateCharacterParts()
        {
            Assert.AreEqual(16,_room.Stations.Count); Assert.AreEqual(0,_room.SelectedChapter);
            for (var i=1;i<=7;i++) { Assert.AreEqual(i,Station("Chapter"+i).Chapter); Assert.IsNotNull(Station("Chapter"+i).GetComponentInChildren<Collider>()); }
            Assert.AreEqual(1,Array.FindAll(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None),c=>c.enabled&&c.gameObject.activeInHierarchy).Length);
            Assert.AreEqual(1,Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length);
            Assert.IsFalse(_room.Walker.ViewCamera.orthographic); Assert.IsNull(GameObject.Find("ChapterSelection"));
            var parts = _room.Walker.transform.Find("CharacterParts");
            foreach (var part in new[]{"Body_Torso","Head","Hand_Left","Hand_Right","Foot_Left","Foot_Right","Necklace"}) Assert.IsNotNull(parts.Find(part));
            Assert.IsFalse(parts.Find("Head").gameObject.activeSelf); yield return null;
        }
        [UnityTest] public IEnumerator MovementUsesCharacterCollisionAndDoesNotEscapeRoomWalls()
        {
            var start = _room.Walker.transform.position;
            _room.Walker.transform.rotation = Quaternion.identity;
            for (var i=0;i<40;i++) _room.Walker.StepMotor(new PrototypeMotorInput{move=Vector2.up},.02f);
            Assert.Greater(_room.Walker.transform.position.z,start.z+2);
            _room.Walker.Teleport(new Vector3(0,.12f,-5)); Physics.SyncTransforms();
            for (var i=0;i<220;i++) _room.Walker.StepMotor(new PrototypeMotorInput{move=Vector2.down},.02f);
            Assert.Greater(_room.Walker.transform.position.z,-9.5f); Assert.Greater(_room.Walker.transform.position.y,-.1f);
            var safe = _room.Walker.transform.position;
            _room.Walker.StepMotor(new PrototypeMotorInput{move=new Vector2(float.NaN,0)},.02f);
            Assert.AreEqual(safe,_room.Walker.transform.position); yield return null;
        }
        [UnityTest] public IEnumerator StationCaptionsFaceWalkerWithoutRotatingDevicesOrWrappingWords()
        {
            foreach (var position in new[] { PrototypeWaitingRoomWalker.Spawn, new Vector3(8,.12f,-5), new Vector3(-8,.12f,4) })
            {
                _room.Walker.Teleport(position);
                yield return null; yield return null;
                foreach (var station in _room.Stations)
                {
                    var label = station.transform.Find("StationLabel").GetComponent<TextMeshPro>();
                    var away = label.transform.position - _room.Walker.ViewCamera.transform.position; away.y = 0;
                    Assert.Greater(Vector3.Dot(label.transform.forward,away.normalized),.999f,station.name);
                    Assert.AreEqual(TextWrappingModes.NoWrap,label.textWrappingMode,station.name);
                    if (station.Kind == PrototypeWaitingStationKind.Chapter) Assert.LessOrEqual(label.GetPreferredValues(label.text).x * label.transform.localScale.x,2.26f,station.name);
                    Assert.Greater(Vector3.Dot(label.transform.up,Vector3.up),.999f,station.name);
                }
                Assert.Less(Quaternion.Angle(Station("PartyStation").transform.rotation,Quaternion.Euler(0,90,0)),.01f);
                Assert.Less(Quaternion.Angle(Station("BackStation").transform.rotation,Quaternion.Euler(0,-90,0)),.01f);
            }
            var ready = Station("ReadyStation").transform.Find("StationLabel").GetComponent<TextMeshPro>();
            Assert.AreEqual("준비 장치\n출발",ready.text);
            Assert.AreEqual(1.35f,ready.transform.localPosition.y,.01f);
        }
        [UnityTest] public IEnumerator SelectingChapterDoesNotStartUntilNearbyReadyStationIsUsed()
        {
            var ready=Station("ReadyStation"); Approach(ready); Assert.IsFalse(_room.TryUseStation(ready));
            var chapter=Station("Chapter7"); Approach(chapter); Assert.IsTrue(_room.TryUseStation(chapter));
            Assert.AreEqual(7,_room.SelectedChapter); Assert.AreEqual("PrototypeWaitingRoom",SceneManager.GetActiveScene().name);
            Assert.IsFalse(_room.TryUseStation(ready));
            Approach(ready); Assert.IsTrue(_room.TryUseStation(ready)); yield return null; yield return null;
            Assert.AreEqual("PrototypeChapter07",SceneManager.GetActiveScene().name);
        }
        [UnityTest] public IEnumerator StationRejectsDistanceBackFacingOcclusionAndModalInput()
        {
            var chapter=Station("Chapter1");
            for (var repeat=0;repeat<12;repeat++)
            {
                _room.Walker.Teleport(PrototypeWaitingRoomWalker.Spawn); Assert.IsFalse(_room.TryUseStation(chapter));
                Approach(chapter); _room.Walker.transform.Rotate(0,180,0); Assert.IsFalse(_room.TryUseStation(chapter));
                Approach(chapter);
                var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.name="QA Waiting Occluder";
                blocker.transform.position=chapter.transform.position+Vector3.back+Vector3.up;
                blocker.transform.localScale=new Vector3(2,3,.3f); Physics.SyncTransforms();
                Assert.IsFalse(_room.TryUseStation(chapter)); Object.Destroy(blocker); yield return null;
                PrototypeSettingsPanel.Open(_room.transform); Assert.IsFalse(_room.TryUseStation(chapter)); PrototypeUi.CloseModal();
                yield return null;
                var camera=_room.Walker.ViewCamera.transform; var toward=chapter.FocusPoint-camera.position;
                Physics.Raycast(camera.position,toward.normalized,out var hit,toward.magnitude,~0,QueryTriggerInteraction.Ignore);
                Assert.IsTrue(_room.TryUseStation(chapter),$"repeat={repeat}; pos={_room.Walker.transform.position}; camera={camera.position}; " +
                    $"station={chapter.transform.position}; dot={Vector3.Dot(camera.forward,toward.normalized)}; hit={hit.collider?.name}; modal={PrototypeUi.IsModalOpen}");
            }
        }
        [UnityTest] public IEnumerator LockedChapterAndTrialToggleCannotLaunchUnselectedLockedChapter()
        {
            PrototypeSave.Progress.unlockedChapter=1; PrototypeSession.AllChaptersForTesting=false;
            var chapter=Station("Chapter7"); Approach(chapter); Assert.IsFalse(_room.TryUseStation(chapter));
            var trial=Station("TrialModeStation"); Approach(trial); Assert.IsTrue(_room.TryUseStation(trial));
            Approach(chapter); Assert.IsTrue(_room.TryUseStation(chapter)); Assert.AreEqual(7,_room.SelectedChapter);
            Approach(trial); Assert.IsTrue(_room.TryUseStation(trial)); Assert.AreEqual(0,_room.SelectedChapter);
            var ready=Station("ReadyStation"); Approach(ready); Assert.IsFalse(_room.TryUseStation(ready)); yield return null;
        }
        [UnityTest] public IEnumerator SideStationsRetainSpecialtyJournalAndTwoDimensionalLobby()
        {
            var specialty=Station("SpecialtyStation"); var original=PrototypeSession.SelectedSpecialty;
            Approach(specialty); Assert.IsTrue(_room.TryUseStation(specialty)); Assert.AreNotEqual(original,PrototypeSession.SelectedSpecialty);
            var memories=Station("MemoriesStation"); Approach(memories); Assert.IsTrue(_room.TryUseStation(memories)); Assert.IsTrue(PrototypeUi.IsModalOpen);
            PrototypeUi.CloseModal(); yield return null;
            var back=Station("BackStation"); Approach(back); Assert.IsTrue(_room.TryUseStation(back)); yield return null; yield return null;
            Assert.AreEqual("PrototypeLobby",SceneManager.GetActiveScene().name);
            Assert.IsTrue(Object.FindFirstObjectByType<Camera>().orthographic);
        }
    }
}
