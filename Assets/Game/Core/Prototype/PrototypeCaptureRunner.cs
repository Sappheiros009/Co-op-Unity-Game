using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlimeCoop.Prototype
{
    /// <summary>Explicit, opt-in standalone QA harness. Does not run in normal play.</summary>
    public sealed class PrototypeCaptureRunner : MonoBehaviour
    {
        [Serializable] private sealed class Result
        {
            public string editorVersion, device, buildVersion = "prototype-v2";
            public List<string> captures = new List<string>();
            public List<string> errors = new List<string>();
        }
        private readonly Result _result = new Result();
        private string _root;
        private float _deadline;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i=0;i+1<args.Length;i++)
            {
                if (args[i] != "--prototype-capture") continue;
                var obj=new GameObject("PrototypeCaptureRunner"); DontDestroyOnLoad(obj);
                var runner=obj.AddComponent<PrototypeCaptureRunner>();
                runner._root=Path.GetFullPath(args[i+1]); Directory.CreateDirectory(runner._root);
                runner._deadline=Time.realtimeSinceStartup+120;
                PrototypeSave.RootOverride=Path.Combine(runner._root,"IsolatedSave"); PrototypeSave.Reload();
                runner._result.editorVersion=Application.unityVersion; runner._result.device=SystemInfo.graphicsDeviceName;
                Application.logMessageReceived+=runner.OnLog;
                break;
            }
        }
        private void OnLog(string message,string stack,LogType kind)
        {
            if ((kind==LogType.Error || kind==LogType.Exception || kind==LogType.Assert) && _result.errors.Count<30)
                _result.errors.Add(message+"\n"+stack);
        }
        private IEnumerator Start()
        {
            if (_root==null) yield break;
            yield return new WaitForSeconds(2);
            yield return Capture("01-Lobby");
            GameObject.Find("StartGame").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitForSeconds(2);
            yield return Capture("02-WaitingRoom");
            var waitingRoom = FindAnyObjectByType<PrototypeWaitingRoomController>();
            UseWaitingStation(waitingRoom, "Chapter1");
            UseWaitingStation(waitingRoom, "ReadyStation");
            yield return new WaitForSeconds(2);
            yield return Capture("03-Chapter01");
            var game=FindAnyObjectByType<PrototypeGame>();
            game.CommandTeam(true);
            yield return new WaitForSeconds(2);
            PrototypeSettingsPanel.Open(game.transform);
            yield return new WaitForSeconds(.5f);
            yield return Capture("04-Settings"); PrototypeUi.CloseModal();
            PrototypeSession.BeginChapter(2);
            yield return SceneManager.LoadSceneAsync("PrototypeChapter02");
            yield return new WaitForSeconds(2);
            game=FindAnyObjectByType<PrototypeGame>(); game.Player.Teleport(new Vector3(0,.1f,18));
            yield return new WaitForSeconds(.5f);
            yield return Capture("05-LAVA");
            yield return SceneManager.LoadSceneAsync("PrototypeStoryInterlude_Chapter01");
            yield return new WaitForSeconds(2);
            yield return Capture("06-Story");
            PrototypeSession.BeginChapter(1);
            for(var stage=1;stage<6;stage++) { PrototypeSession.CompleteStage(100,10); PrototypeSession.AdvanceStage(); }
            yield return SceneManager.LoadSceneAsync("PrototypeChapter01"); yield return null;
            game=FindAnyObjectByType<PrototypeGame>(); game.Player.Teleport(new Vector3(0,.1f,40));
            while(!game.Objective.BossWarning) yield return null;
            yield return Capture("07-BossWarning");
            PrototypeSession.BeginChapter(4);
            yield return SceneManager.LoadSceneAsync("PrototypeChapter04"); yield return new WaitForSeconds(.5f);
            game=FindAnyObjectByType<PrototypeGame>(); game.Player.Teleport(new Vector3(0,.1f,18));
            yield return new WaitForSeconds(.5f); yield return Capture("08-CooperativeRaft");
            game.Player.Teleport(new Vector3(-7,3.6f,3)); game.Player.ToggleNecklace(); yield return new WaitForSeconds(.3f);
            PrototypeRecordsPanel.OpenJournal(game.transform); yield return null; yield return Capture("09-MemoryJournal");
            PrototypeUi.CloseModal();
            yield return SceneManager.LoadSceneAsync("PrototypeWaitingRoom"); yield return null;
            PrototypeRecordsPanel.OpenRanking(transform); yield return null; yield return Capture("10-TeamRecords");
            PrototypeUi.CloseModal();
            // Lock only the explicit QA save: exercise real IO failure, UI notice and retry, not a fake label.
            PrototypeSave.WriteProgress();
            using (var locked = new FileStream(Path.Combine(PrototypeSave.Root,"progress.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                PrototypeSave.Progress.unlockedChapter = 4;
                if (PrototypeSave.WriteProgress()) _result.errors.Add("QA save lock unexpectedly allowed replacement");
                yield return new WaitForSecondsRealtime(.3f); yield return Capture("11-SaveFailure");
            }
            PrototypeSettingsPanel.Open(transform); yield return null;
            GameObject.Find("Audio").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            GameObject.Find("RetryProgress").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            if (PrototypeSave.Error.Length > 0 || PrototypeSave.HasPendingProgress) _result.errors.Add("QA save retry failed");
            Finish();
        }
        private void Finish()
        {
            _deadline=0;
            File.WriteAllText(Path.Combine(_root,"result.json"),JsonUtility.ToJson(_result,true));
            Application.Quit(_result.errors.Count==0 ? 0 : 2);
        }
        private static void UseWaitingStation(PrototypeWaitingRoomController room, string stationName)
        {
            var station = GameObject.Find(stationName).GetComponent<PrototypeWaitingRoomStation>();
            // Bounded capture fixture positioning; the ordinary distance/facing/use path still decides the action.
            room.Walker.Teleport(station.transform.position - station.transform.forward * 2);
            room.Walker.AimAt(station.FocusPoint); Physics.SyncTransforms();
            if (!room.TryUseStation(station)) throw new InvalidOperationException("Waiting station interaction failed: " + stationName);
        }
        private void Update()
        { if (_deadline>0 && Time.realtimeSinceStartup>_deadline) { _result.errors.Add("QA capture deadline exceeded"); Finish(); } }
        private IEnumerator Capture(string name)
        {
            yield return PrototypeFrameCapture.Save(Path.Combine(_root,name+".png"),error=>
            {if(error.Length>0)_result.errors.Add(error+": "+name);else _result.captures.Add(name);});
        }
        private void OnDestroy() { Application.logMessageReceived-=OnLog; }
    }
}
