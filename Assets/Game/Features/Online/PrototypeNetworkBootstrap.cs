using System;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlimeCoop.Prototype
{
    /// <summary>Loopback session entry from the 2D menu or explicit QA flags; never starts a player-host.</summary>
    public sealed class PrototypeNetworkBootstrap : MonoBehaviour
    {
        private PrototypeNetworkTransport _network;
        private bool _server, _hadClient;
        private float _emptySince = -1;
        private string _qa="", _output="", _runTag="", _name="Slime", _protocol=PrototypeNetworkRoom.Protocol, _capture="";
        private string _instanceId="",_readyFile="";
        private ushort _port;
        private int _capacity;
        private bool _menuEntry,_clientReady,_failed;
        private float _joinDeadline,_startupTimeout,_startedAt;
        private Action<string> _joinFailed;
        private Action _joined;
        public PrototypeNetworkTransport Network => _network;
        public bool ClientReady => _clientReady;

        public static bool TryJoinFromLobby(PrototypeLocalConnection request, string expectedServerId, Action joined,
            Action<string> failed, out PrototypeNetworkBootstrap session)
        {
            session=null;
            if(request==null || NetworkManager.Singleton!=null || FindFirstObjectByType<PrototypeNetworkBootstrap>()!=null) return false;
            var obj=new GameObject("Local network session"); DontDestroyOnLoad(obj);
            session=obj.AddComponent<PrototypeNetworkBootstrap>(); session._menuEntry=true;
            session._port=request.Port; session._capacity=request.Capacity; session._name=request.Name;
            session._instanceId=expectedServerId??""; session._joined=joined; session._joinFailed=failed;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var args = Environment.GetCommandLineArgs();
            var mode = Argument(args, "--slime-network", "");
            if (mode != "server" && mode != "client") return;
            var obj = new GameObject("Local network session"); DontDestroyOnLoad(obj);
            var app = obj.AddComponent<PrototypeNetworkBootstrap>();
            app._server = mode == "server";
            app._port = ushort.TryParse(Argument(args,"--network-port","7777"),out var port) && port >= 1024 ? port : (ushort)7777;
            app._capacity = int.TryParse(Argument(args,"--network-capacity","4"),out var capacity) ? Mathf.Clamp(capacity,2,4) : 4;
            app._name = Argument(args,"--network-name","Slime");
            app._protocol = Argument(args,"--network-protocol",PrototypeNetworkRoom.Protocol);
            app._qa = Argument(args,"--network-qa","");
            app._output = Argument(args,"--network-output","");
            app._runTag = Argument(args,"--network-run-tag","");
            app._capture = Argument(args,"--network-capture","");
            app._instanceId=Argument(args,"--network-server-id","");
            app._readyFile=Argument(args,"--network-ready-file","");
            app._startupTimeout=int.TryParse(Argument(args,"--network-startup-timeout","0"),out var timeout)?Mathf.Clamp(timeout,0,120):0;
            var saveRoot = Argument(args,"--network-save-root","");
            if (saveRoot.Length > 0) PrototypeSave.RootOverride = Path.GetFullPath(saveRoot);
            Application.runInBackground = true; QualitySettings.vSyncCount = 0; Application.targetFrameRate = 30;
            SceneManager.sceneLoaded += app.OnFirstScene;
        }
        private static string Argument(string[] args, string key, string fallback)
        { for (var i=0;i+1<args.Length;i++) if(args[i]==key) return args[i+1]; return fallback; }
        private void OnFirstScene(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnFirstScene;
            ClearScene(scene);
        }
        private static void ClearScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects()) { root.SetActive(false); Destroy(root); }
        }
        private void Start()
        {
            _startedAt=Time.realtimeSinceStartup; _joinDeadline=_startedAt+12;
            Application.runInBackground=true;
            _network = gameObject.AddComponent<PrototypeNetworkTransport>();
            if (_qa.Length > 0)
                gameObject.AddComponent<PrototypeNetworkQa>().Configure(_network,_qa,_output,_runTag,_capacity,_capture);
            if (!_network.StartNetwork(_server,_port,_capacity,_name,_protocol,_instanceId))
            {
                if(_menuEntry) AbortJoin("시작 실패");
                else if(_server && _qa.Length==0) Application.Quit(2);
                return;
            }
            if (_server)
            {
                gameObject.AddComponent<PrototypeNetworkWaitingServer>().Configure(_network);
                gameObject.AddComponent<PrototypeNetworkWorld>().Configure(_network);
                if(_readyFile.Length>0) File.WriteAllText(_readyFile,_network.ServerId);
            }
            else if(!_menuEntry) ConfigureClient();
        }
        private void ConfigureClient()
        {
            var world=gameObject.AddComponent<PrototypeNetworkClientWorld>(); world.Configure(_network,_qa.Length==0);
            var waiting=gameObject.AddComponent<PrototypeNetworkWaitingClient>(); waiting.Configure(_network,world,_qa.Length==0,ExitToLobby);
            if(_network.Snapshot!=null) waiting.ApplySnapshot(_network.Snapshot);
        }
        public void CancelJoin()
        {
            if(!_menuEntry || _clientReady) return;
            _joinFailed=null; _joined=null; _network?.Close(); gameObject.SetActive(false); Destroy(gameObject);
        }
        private void AbortJoin(string reason)
        {
            if(_failed) return; _failed=true;
            var callback=_joinFailed; CancelJoin(); callback?.Invoke(PrototypeLocalConnection.ExplainFailure(reason));
        }
        public void ExitToLobby()
        {
            PrototypeUi.CloseModal(); _network?.Close();
            gameObject.SetActive(false); Destroy(gameObject);
            SceneManager.LoadScene("PrototypeLobby");
        }
        private void Update()
        {
            if(_network==null || _qa.Length>0 || _failed) return;
            if(!_server)
            {
                if(_menuEntry && !_clientReady)
                {
                    if(_network.ConnectionEnded || Time.realtimeSinceStartup>=_joinDeadline) { AbortJoin(_network.Status); return; }
                    var room=_network.Snapshot;
                    if(!_network.Connected || room?.phase!="WaitingRoom" || room.waiting==null || !room.waiting.IsValid()
                        || Array.Find(room.waiting.actors,p=>p.slot==room.yourSlot)==null) return;
                    _clientReady=true; _joined?.Invoke(); _joined=null; _joinFailed=null;
                    PrototypeUi.CloseModal(); ClearScene(SceneManager.GetActiveScene()); ConfigureClient();
                }
                else
                {
                    if(_network.Connected) _clientReady=true;
                    if(!_network.Connected && (_clientReady || _network.ConnectionEnded || Time.realtimeSinceStartup>=_joinDeadline))
                    {
                        _failed=true; PrototypeCapsulePlayer.SetCursor(false);
                        var panel=PrototypeUi.Modal(transform,"연결이 종료되었습니다");
                        PrototypeUi.Text(panel,"Reason",PrototypeLocalConnection.ExplainFailure(_network.Status),.05f,.23f,.9f,.3f,24);
                        PrototypeUi.Button(panel,"ReturnToLobby","2D 로비로 돌아가기",.25f,.72f,.5f,.13f,ExitToLobby);
                    }
                }
                return;
            }
            if(_network.Snapshot==null) return;
            var connected=0; foreach(var peer in _network.Snapshot.peers) if(peer.connected) connected++;
            if(connected>0) { _hadClient=true; _emptySince=-1; }
            else if(_hadClient)
            { if(_emptySince<0) _emptySince=Time.unscaledTime; if(Time.unscaledTime-_emptySince>5) Application.Quit(); }
            else if(_startupTimeout>0 && Time.realtimeSinceStartup-_startedAt>=_startupTimeout) Application.Quit();
        }
        private void OnDestroy() { SceneManager.sceneLoaded-=OnFirstScene; }
    }
}
