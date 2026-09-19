using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlimeCoop.Prototype
{
    /// <summary>Scene-local orchestration. Gameplay rules remain in focused feature modules.</summary>
    public sealed class PrototypeGame : MonoBehaviour
    {
        [SerializeField] private int chapterNumber = 1;
        private readonly List<PrototypeTeammate> _teammates = new List<PrototypeTeammate>();
        private readonly List<PrototypeCapsuleMonster> _monsters = new List<PrototypeCapsuleMonster>();
        private readonly List<PrototypeParticipant> _participants = new List<PrototypeParticipant>();
        private readonly List<PrototypeCapsulePlayer> _controlledPlayers = new List<PrototypeCapsulePlayer>();
        private string[] _serverPlayerNames;
        private float _transitionAt = -1, _wipeAt = -1;
        public PrototypeCapsulePlayer Player { get; private set; }
        public PrototypeExitScoring Exit { get; private set; }
        public PrototypeMapBuilder Map { get; private set; }
        public PrototypeStageObjective Objective { get; private set; }
        public int ChapterNumber => chapterNumber;
        public PrototypeChapterDefinition Chapter => PrototypeChapterCatalog.Get(chapterNumber);
        public string ChapterDisplayName => Chapter.ChapterLabel;
        public string StatusMessage { get; private set; } = "";
        public IReadOnlyList<PrototypeTeammate> Teammates => _teammates;
        public IReadOnlyList<PrototypeParticipant> Participants => _participants;
        public IReadOnlyList<PrototypeCapsuleMonster> Monsters => _monsters;
        public IReadOnlyList<PrototypeCapsulePlayer> ControlledPlayers => _controlledPlayers;
        public bool HasNetworkPlayers => _serverPlayerNames != null;
        public bool IsReplica { get; private set; }
        public bool IsServerControlled => HasNetworkPlayers && !IsReplica;
        public event System.Action<bool> ServerStageEnded;
        public bool IsTransitioning => _transitionAt >= 0 || _wipeAt >= 0;
        public int StageNumber => Application.isPlaying ? PrototypeSession.Stage : 1;
        public void ConfigureChapter(int value) { chapterNumber = Mathf.Clamp(value, 1, 7); }
        public void ConfigureServerPlayers(string[] names)
        {
            if (_participants.Count > 0) throw new System.InvalidOperationException("Configure server participants before building the world.");
            if (names == null || names.Length < 2 || names.Length > 4) throw new System.ArgumentException("A server stage requires 2 to 4 participants.", nameof(names));
            foreach (var name in names)
                if (string.IsNullOrWhiteSpace(name) || name.Length > 24) throw new System.ArgumentException("Invalid participant name.", nameof(names));
            _serverPlayerNames = (string[])names.Clone();
            IsReplica = false;
        }
        public void ConfigureReplicaPlayers(string[] names) { ConfigureServerPlayers(names); IsReplica = true; }
        public string ServerPlayerName(int id) => _serverPlayerNames[id];
        private void Start()
        {
            if (IsReplica) return; // Display worlds are built and frozen by the network replica, never simulated here.
            if (IsServerControlled) PrototypeSession.PartySize = _serverPlayerNames.Length;
            if (PrototypeSession.Chapter != chapterNumber || (IsServerControlled && PrototypeSession.StartingCount != _serverPlayerNames.Length))
                PrototypeSession.BeginChapter(chapterNumber);
            Map = GetComponent<PrototypeMapBuilder>() ?? gameObject.AddComponent<PrototypeMapBuilder>();
            Map.Build(this);
            Exit.BindRoster(_participants);
            for (var i = 0; i < _participants.Count; i++)
                for (var j = i + 1; j < _participants.Count; j++)
                    Physics.IgnoreCollision(_participants[i].GetComponent<CharacterController>(), _participants[j].GetComponent<CharacterController>());
            if (!IsServerControlled) (GetComponent<PrototypeHud>() ?? gameObject.AddComponent<PrototypeHud>()).Configure(this);
            SetStatus(IsServerControlled ? "서버가 참가자별 입력을 받아 장치·구조·출구를 판정합니다." : "장치 조건을 충족해 출구를 여세요. T: 동료 협동 배치 / Y: 따라오기");
        }
        private void Update()
        {
            if (IsReplica) return;
            if (Exit == null || Player == null) return;
            foreach (var p in _participants) p.SetConnected(PrototypeSession.IsConnected(p.ParticipantId));
            if (Exit.IsSettled && _transitionAt < 0)
            {
                PrototypeSession.CompleteStage(Exit.TeamScore, Exit.SettlementElapsed);
                _transitionAt = Time.time + 3f;
                SetStatus($"구간 성공! {Exit.ArrivedCount}/{Exit.StageStartParticipantCount}명 도착 · 팀 {Exit.TeamScore}점 (시험 배점)");
            }
            if (_transitionAt >= 0 && Time.time >= _transitionAt) { Advance(); return; }
            if (!Exit.HasStartedSettlement && !IsTransitioning)
            {
                var alive = 0; var connected = 0;
                foreach (var p in _participants) if (p.IsConnected) { connected++; if (p.IsAlive) alive++; }
                if (alive == 0 && connected > 0) { _wipeAt = Time.time + 3; SetStatus("접속 중인 전원이 쓰러졌습니다. 런을 초기화하고 로비로 돌아갑니다."); }
            }
            if (_wipeAt >= 0 && Time.time >= _wipeAt)
            {
                _wipeAt = float.PositiveInfinity;
                PrototypeSession.Wipe();
                if (IsServerControlled) ServerStageEnded?.Invoke(false);
                else SceneManager.LoadScene("PrototypeLobby");
            }
        }
        private void Advance()
        {
            _transitionAt = float.PositiveInfinity;
            if (IsServerControlled) { ServerStageEnded?.Invoke(true); return; }
            if (PrototypeSession.Practice) { SceneManager.LoadScene("PrototypeWaitingRoom"); return; }
            if (PrototypeSession.AdvanceStage()) SceneManager.LoadScene(Chapter.SceneName);
            else { PrototypeSave.CompleteChapter(); SceneManager.LoadScene(Chapter.StorySceneName); }
        }
        public void RegisterWorld(PrototypeMapBuilder map, PrototypeCapsulePlayer player, PrototypeExitScoring exit)
        {
            Map = map; Player = player; Exit = exit;
            RegisterControlledPlayer(player);
        }
        public void RegisterControlledPlayer(PrototypeCapsulePlayer player)
        {
            if (player == null || _controlledPlayers.Contains(player)) return;
            _controlledPlayers.Add(player);
            if (!_participants.Contains(player.Participant)) _participants.Add(player.Participant);
        }
        public void RegisterObjective(PrototypeStageObjective value) { Objective = value; }
        public void RegisterTeammate(PrototypeTeammate value)
        {
            if (_teammates.Contains(value)) return;
            _teammates.Add(value); _participants.Add(value.Participant);
        }
        public void RegisterMonster(PrototypeCapsuleMonster value) { if (!_monsters.Contains(value)) _monsters.Add(value); }
        public void SetStatus(string value) { StatusMessage = value; }
        public void HandlePlayerDown() { SetStatus("쓰러졌습니다. 동료에게 구조를 요청하세요. Q: 구조 핑"); }
        public void CommandTeam(bool assist)
        {
            if (assist && Map.Raft != null && !Objective.RegionalReady && Player.transform.position.z < 34)
            {
                foreach (var teammate in _teammates) teammate.SetOrder(PrototypeBotOrder.RegionAssist);
                SetStatus("동료들이 부유물에 올라탑니다. 물속 잠금을 풀고 전원이 탑승한 뒤 조타 장치를 사용하세요.");
                return;
            }
            foreach (var teammate in _teammates) teammate.SetAssist(assist);
            SetStatus(assist ? "동료들이 각 협동 장치로 이동합니다. 마지막 장치는 직접 맡으세요." : "동료들이 따라옵니다.");
        }
    }
}
