using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SlimeCoop.Prototype
{
    /// <summary>Explicit opt-in QA which uses the ordinary 2D lobby's real buttons, not the network boot shortcut.</summary>
    public sealed class PrototypeLocalLobbyQa : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string runTag,role,status="RUNNING",phase="Lobby",serverId="",serverLog="",saveRoot="",startedUtc,finishedUtc;
            public int processId,serverProcessId,capacity;
            public List<string> checks=new List<string>(),errors=new List<string>(),captures=new List<string>(),expectedTransportErrors=new List<string>();
        }
        private readonly Report _report=new Report();
        private string _output,_folder,_port;
        private float _deadline,_nextWrite,_quitAt=-1;
        private bool _finished,_finishing,_expectMissingServer;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs(); var role=Arg(args,"--local-lobby-qa","");
            if(role!="creator" && role!="guest" && role!="failure") return;
            var folder=Arg(args,"--lobby-qa-folder",""); var tag=Arg(args,"--lobby-qa-run","");
            if(folder.Length==0 || !Guid.TryParseExact(tag,"N",out _)) throw new ArgumentException("Lobby QA requires an isolated folder and run ID.");
            var obj=new GameObject("Explicit ordinary lobby QA"); DontDestroyOnLoad(obj);
            var qa=obj.AddComponent<PrototypeLocalLobbyQa>(); qa._folder=Path.GetFullPath(folder); Directory.CreateDirectory(qa._folder);
            qa._output=Path.Combine(qa._folder,"result.json"); qa._port=Arg(args,"--lobby-qa-port","17997");
            qa._report.role=role; qa._report.runTag=tag; qa._report.capacity=int.Parse(Arg(args,"--lobby-qa-capacity","2"));
            qa._report.processId=System.Diagnostics.Process.GetCurrentProcess().Id; qa._report.startedUtc=DateTime.UtcNow.ToString("o");
            PrototypeSave.RootOverride=Path.Combine(qa._folder,"Save",tag); PrototypeSave.Reload();
            Application.runInBackground=true; QualitySettings.vSyncCount=0; Application.targetFrameRate=30;
            qa._deadline=Time.realtimeSinceStartup+100; Application.logMessageReceived+=qa.OnLog;
        }
        private static string Arg(string[] args,string key,string fallback)
        { for(var i=0;i+1<args.Length;i++) if(args[i]==key) return args[i+1]; return fallback; }
        private static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException("Lobby QA: "+message); }
        private static void Click(string name)
        {
            var obj=GameObject.Find(name); Require(obj!=null,"Missing button "+name);
            var button=obj.GetComponent<UnityEngine.UI.Button>(); Require(button!=null && button.interactable,"Inactive button "+name); button.onClick.Invoke();
        }
        private void Check(string label) { if(!_report.checks.Contains(label)) _report.checks.Add(label); }
        private IEnumerator Start()
        {
            yield return null; yield return null;
            Require(SceneManager.GetActiveScene().name=="PrototypeLobby","Did not start from normal lobby");
            Require(FindFirstObjectByType<PrototypeNetworkTransport>()==null,"CLI network shortcut active");
            Require(Camera.main!=null && Camera.main.orthographic,"Initial lobby is not 2D");
            Check("ordinary_2d_lobby_no_network_shortcut");
            if(_report.role=="creator" || _report.role=="failure") yield return Capture("01-lobby");
            OpenMenu(); var panel=FindFirstObjectByType<PrototypeLocalNetworkPanel>();
            if(_report.role=="failure")
            {
                Click("JoinLocalRoom"); yield return null; Click("CancelLocalRoom");
                for(var i=0;i<5;i++) yield return null;
                Require(FindFirstObjectByType<PrototypeNetworkTransport>()==null && Camera.main.orthographic,"Cancel leaked a session");
                Check("pending_join_cancelled_to_2d"); OpenMenu(); panel=FindFirstObjectByType<PrototypeLocalNetworkPanel>();
                _expectMissingServer=true; Click("JoinLocalRoom"); yield return null;
                while(panel!=null && panel.Busy) yield return null;
                Require(panel!=null && panel.Status.Contains("연결하지 못했습니다"),"No readable timeout recovery");
                _expectMissingServer=false;
                Check("connection_failure_has_retry_and_cancel"); yield return Capture("02-failed-join");
                Click("CancelLocalRoom"); yield return null; OpenMenu(); panel=FindFirstObjectByType<PrototypeLocalNetworkPanel>();
                Require(!panel.Busy,"Retry menu blocked"); Click("CancelLocalRoom"); yield return null;
                Check("retry_menu_reopened"); Finish(); yield break;
            }
            if(_report.role=="creator") yield return Capture("02-create-menu");
            Click(_report.role=="creator"?"CreateLocalRoom":"JoinLocalRoom");
            PrototypeNetworkBootstrap session=null;
            while((session=FindFirstObjectByType<PrototypeNetworkBootstrap>())==null || !session.ClientReady)
            {
                if(panel!=null && panel.CreatedServerProcessId>0)
                { _report.serverProcessId=panel.CreatedServerProcessId; _report.serverLog=panel.ServerLogPath; }
                Require(panel==null || panel.Busy,"Connection failed: "+(panel!=null?panel.Status:"")); yield return null;
            }
            var network=session.Network; _report.serverId=network.Snapshot.serverId;
            _report.saveRoot=PrototypeSave.Root;
            _report.phase="WaitingRoom"; Check("real_menu_connection_approved");
            while(Connected(network.Snapshot)<_report.capacity) yield return null;
            ValidateWaiting(session); Check("all_real_clients_visible_in_3d"); yield return Capture("03-waiting-room");
            if(_report.role=="creator")
            {
                Require(_report.serverProcessId>0 && _report.serverProcessId!=_report.processId,"Server is not a separate process");
                Check("separate_dedicated_server_process"); yield return new WaitForSecondsRealtime(2);
                yield return LeaveThroughMenu(); Check("creator_returned_to_2d_lobby");
                yield return new WaitForSecondsRealtime(.8f);
                OpenMenu(); Click("JoinLocalRoom");
                while((session=FindFirstObjectByType<PrototypeNetworkBootstrap>())==null || !session.ClientReady) yield return null;
                network=session.Network;
                Require(network.Snapshot.serverId==_report.serverId,"Server identity changed on rejoin");
                Require(PrototypeSave.Root==_report.saveRoot,"Rejoin lost named test profile");
                Require(network.Snapshot.owner!=network.Snapshot.yourSlot,"Rejoining creator stole owner role");
                ValidateWaiting(session); Check("rejoined_same_server_without_stealing_owner");
                _report.phase="Rejoined"; yield return Capture("04-rejoined-room");
                while(Connected(network.Snapshot)>1) yield return null;
                Require(network.Snapshot.owner==network.Snapshot.yourSlot,"Last participant did not inherit room role");
                Check("server_survived_other_clients_leaving"); yield return LeaveThroughMenu();
                Check("final_client_left_to_2d_lobby");
            }
            else
            {
                var originalOwner=network.Snapshot.owner; var ticks=network.Snapshot.serverTicks;
                while(network.Snapshot.owner==originalOwner) yield return null;
                Require(network.Connected && network.Snapshot.serverId==_report.serverId && network.Snapshot.serverTicks>ticks,"Owner leaving stopped server");
                Check("owner_departure_kept_server_and_clock");
                while(Connected(network.Snapshot)<_report.capacity) yield return null;
                Require(network.Snapshot.serverId==_report.serverId,"Rejoin changed server");
                Check("creator_rejoined_same_room"); yield return new WaitForSecondsRealtime(3);
                yield return LeaveThroughMenu(); Check("guest_left_to_2d_lobby");
            }
            Require(FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"EventSystem leaked");
            yield return Capture("05-returned-lobby"); Finish();
        }
        private void OpenMenu()
        {
            Click("LocalMultiplayer"); GameObject.Find("PlayerName").GetComponent<TMP_InputField>().text=_report.role+"-"+_report.processId;
            GameObject.Find("Port").GetComponent<TMP_InputField>().text=_port;
            while(!GameObject.Find("Capacity").GetComponentInChildren<TMP_Text>().text.Contains(_report.capacity+"명")) Click("Capacity");
        }
        private static int Connected(PrototypeNetworkSnapshot snapshot)
        { var count=0; if(snapshot!=null) foreach(var peer in snapshot.peers) if(peer.connected) count++; return count; }
        private static void ValidateWaiting(PrototypeNetworkBootstrap session)
        {
            var waiting=session.GetComponent<PrototypeNetworkWaitingClient>();
            Require(waiting!=null && waiting.Visible && waiting.Walker!=null,"Missing first-person waiting replica");
            Require(!waiting.Walker.ViewCamera.orthographic,"Waiting camera is not 3D");
            Require(waiting.ActorCount==Connected(session.Network.Snapshot),"Fake/missing participants");
            Require(FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Multiple EventSystems");
            var cameras=0; foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None)) if(camera.isActiveAndEnabled) cameras++;
            var listeners=0; foreach(var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) if(listener.isActiveAndEnabled) listeners++;
            Require(cameras==1 && Camera.main==waiting.Walker.ViewCamera,"Missing/extra enabled first-person camera");
            Require(listeners==1,"Missing/extra enabled audio listener");
            Require(FindFirstObjectByType<PrototypeLobbyController>()==null,"Old 2D lobby still active");
        }
        private IEnumerator LeaveThroughMenu()
        {
            Click("Leave"); yield return null; Click("ConfirmLeave");
            for(var i=0;i<5;i++) yield return null;
            Require(SceneManager.GetActiveScene().name=="PrototypeLobby" && Camera.main!=null && Camera.main.orthographic,"Did not return to 2D lobby");
            Require(FindFirstObjectByType<PrototypeNetworkBootstrap>()==null && FindFirstObjectByType<PrototypeNetworkTransport>()==null,"Network survived menu leave");
        }
        private IEnumerator Capture(string name)
        {
            yield return PrototypeFrameCapture.Save(Path.Combine(_folder,name+".png"),error=>
            { if(error.Length>0) _report.errors.Add(error); else _report.captures.Add(name); });
        }
        private void OnLog(string text,string stack,LogType type)
        {
            // NGO logs an Error for the deliberately absent server. Keep it as explicit negative-test evidence, not a blanket suppression.
            if(_report.role=="failure" && _expectMissingServer && type==LogType.Error && text=="Failed to connect to server.")
            { _report.expectedTransportErrors.Add(text); return; }
            if((type==LogType.Exception || type==LogType.Error || type==LogType.Assert) && _report.errors.Count<8) _report.errors.Add(text);
        }
        private void Update()
        {
            if(_finished) { if(Time.realtimeSinceStartup>=_quitAt) Application.Quit(_report.status=="PASSED"?0:2); return; }
            if(_finishing) { Finish(); return; }
            if(_report.errors.Count>0 || Time.realtimeSinceStartup>_deadline)
            { if(_report.errors.Count==0) _report.errors.Add("lobby_qa_timeout:"+_report.phase); StopAllCoroutines(); Finish(); return; }
            if(Time.realtimeSinceStartup>=_nextWrite) { _nextWrite=Time.realtimeSinceStartup+.25f; Write(); }
        }
        private void Finish()
        {
            _finishing=true;
            _report.status=_report.errors.Count==0?"PASSED":"FAILED"; _report.finishedUtc=DateTime.UtcNow.ToString("o");
            if(!Write()) return;
            _finished=true; _quitAt=Time.realtimeSinceStartup+.4f;
        }
        private bool Write()
        {
            try
            {
                var temporary=_output+".tmp"; File.WriteAllText(temporary,JsonUtility.ToJson(_report,true));
                if(File.Exists(_output)) File.Replace(temporary,_output,null); else File.Move(temporary,_output); return true;
            }
            catch(IOException) { return false; }
        }
        private void OnDestroy() { Application.logMessageReceived-=OnLog; }
    }
}
