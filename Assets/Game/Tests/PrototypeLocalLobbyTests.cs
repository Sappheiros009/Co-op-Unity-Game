using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace SlimeCoop.Prototype.Tests
{
    public sealed class PrototypeLocalLobbyTests
    {
        [TestCase("","7797",4)] [TestCase("<b>name</b>","7797",4)] [TestCase("bad\nname","7797",4)]
        [TestCase("Slime","1023",4)] [TestCase("Slime","65536",4)] [TestCase("Slime","+7797",4)]
        [TestCase("Slime","host:7797",4)] [TestCase("Slime","7797",1)] [TestCase("Slime","7797",5)]
        public void InvalidMenuInputNeverProducesConnectionRequest(string name,string port,int capacity)
        {
            Assert.IsFalse(PrototypeLocalConnection.TryCreate(name,port,capacity,out var request,out var reason));
            Assert.IsNull(request); Assert.IsNotEmpty(reason);
        }
        [TestCase("1024",2)] [TestCase("65535",4)] [TestCase("7797",3)]
        public void ValidMenuInputPreservesPortCapacityAndTrimmedKoreanName(string port,int count)
        {
            Assert.IsTrue(PrototypeLocalConnection.TryCreate("  슬라임  ",port,count,out var request,out var error),error);
            Assert.AreEqual("슬라임",request.Name); Assert.AreEqual(ushort.Parse(port),request.Port); Assert.AreEqual(count,request.Capacity);
        }
        [Test] public void CreatingClientCannotAccidentallyJoinDifferentServerOnSamePort()
        {
            var expected=Guid.NewGuid().ToString("N"); var room=new PrototypeNetworkRoom(4,expected);
            Assert.AreEqual(expected,room.ServerId);
            Assert.IsTrue(room.CanJoin(PrototypeNetworkRoom.Protocol,out _,expected));
            Assert.IsTrue(room.CanJoin(PrototypeNetworkRoom.Protocol,out _)); // Explicit Join does not claim to have created the server.
            Assert.IsFalse(room.CanJoin(PrototypeNetworkRoom.Protocol,out var reason,Guid.NewGuid().ToString("N")));
            Assert.AreEqual("server_instance_mismatch",reason); Assert.AreEqual(0,room.ConnectedCount);
            Assert.Throws<ArgumentException>(()=>new PrototypeNetworkRoom(4,"not-an-instance"));
        }
        [Test] public void KnownConnectionFailuresHaveReadableRecoveryInstructions()
        {
            foreach(var reason in new[]{"version_mismatch","room_full","run_locked","server_instance_mismatch","invalid_name","timeout"})
            { var message=PrototypeLocalConnection.ExplainFailure("연결 종료: "+reason); Assert.Greater(message.Length,12); Assert.IsFalse(message.Contains(reason)); }
        }
        [Test] public void NamedTestProfilesAreStableExclusiveAndSeparateFromOrdinarySave()
        {
            var root=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Temp","PrototypeLocalProfiles",Guid.NewGuid().ToString("N")));
            Assert.IsTrue(PrototypeLocalProfile.TryAcquire(root,"슬라임",out var first,out var error),error);
            string savedRoot;
            using(first)
            {
                savedRoot=first.Root; Assert.IsTrue(savedRoot.StartsWith(root+Path.DirectorySeparatorChar));
                Assert.IsFalse(PrototypeLocalProfile.TryAcquire(root,"슬라임",out _,out _));
                Assert.IsTrue(PrototypeLocalProfile.TryAcquire(root,"다른 슬라임",out var other,out error),error);
                using(other) Assert.AreNotEqual(first.Root,other.Root);
            }
            Assert.IsTrue(PrototypeLocalProfile.TryAcquire(root,"슬라임",out var reopened,out error),error);
            using(reopened) Assert.AreEqual(savedRoot,reopened.Root);
        }
        [UnityTest] public IEnumerator MenuRejectsBadInputAndCancelRetryRetainsTwoDimensionalLobby()
        {
            PrototypeSave.RootOverride=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Temp","PrototypeLocalLobbyTests",Guid.NewGuid().ToString("N")));
            PrototypeSave.Reload();
            yield return SceneManager.LoadSceneAsync("PrototypeLobby"); yield return null;
            try
            {
                GameObject.Find("LocalMultiplayer").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return null;
                var panel=Object.FindFirstObjectByType<PrototypeLocalNetworkPanel>(); Assert.IsNotNull(panel);
                Assert.IsTrue(Camera.main.orthographic); Assert.AreEqual(1,Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
                var name=GameObject.Find("PlayerName").GetComponent<TMP_InputField>();
                var port=GameObject.Find("Port").GetComponent<TMP_InputField>();
                Assert.IsFalse(name.richText); Assert.IsNotNull(name.textViewport); Assert.AreEqual(24,name.characterLimit);
                Assert.Greater(((RectTransform)name.transform).rect.width,0);
                Assert.IsTrue(name.targetGraphic.raycastTarget);
                name.text="<bad>"; GameObject.Find("JoinLocalRoom").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.IsFalse(panel.Busy); Assert.IsTrue(panel.Status.Contains("기호"));
                Assert.IsNull(Object.FindFirstObjectByType<PrototypeNetworkBootstrap>());
                name.text="슬라임"; port.text="123"; GameObject.Find("CreateLocalRoom").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.IsFalse(panel.Busy); Assert.IsTrue(panel.Status.Contains("1024")); Assert.AreEqual(0,panel.CreatedServerProcessId);
                port.text="17997";
                GameObject.Find("JoinLocalRoom").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.IsTrue(panel.Busy); Assert.IsFalse(name.interactable);
                yield return null; yield return null; yield return null;
                Assert.IsNotNull(Object.FindFirstObjectByType<PrototypeNetworkBootstrap>());
                GameObject.Find("CancelLocalRoom").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                for(var i=0;i<5;i++) yield return null;
                Assert.IsNull(Object.FindFirstObjectByType<PrototypeNetworkBootstrap>()); Assert.IsNull(NetworkManager.Singleton);
                Assert.IsFalse(PrototypeUi.IsModalOpen); Assert.IsTrue(Camera.main.orthographic);
                Assert.IsTrue(GameObject.Find("LastResult").GetComponent<TMP_Text>().text.Contains("슬라임"));
                Assert.IsNotNull(GameObject.Find("StartGame"));
                GameObject.Find("LocalMultiplayer").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return null;
                panel=Object.FindFirstObjectByType<PrototypeLocalNetworkPanel>(); Assert.IsFalse(panel.Busy);
                GameObject.Find("Capacity").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.IsTrue(GameObject.Find("Capacity").GetComponentInChildren<TMP_Text>().text.Contains("2명"));
                panel.Cancel(); yield return null;
                Assert.AreEqual("PrototypeLobby",SceneManager.GetActiveScene().name);
                Assert.AreEqual(1,Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
            }
            finally
            {
                Object.FindFirstObjectByType<PrototypeLocalNetworkPanel>()?.Cancel();
                PrototypeUi.CloseModal(); PrototypeCapsulePlayer.SetCursor(false);
                PrototypeLocalProfile.Release(); PrototypeSave.RootOverride=null; PrototypeSave.Reload();
            }
        }
    }
}
