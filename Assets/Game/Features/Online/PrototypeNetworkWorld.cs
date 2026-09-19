using System;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Dedicated-process chapter owner. Only server ticks advance players; packets supply intent.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PrototypeNetworkWorld : MonoBehaviour
    {
        private PrototypeNetworkTransport _network;
        private PrototypeNetworkInputBuffer _inputs;
        private PrototypeNetworkStartMember[] _bindings = Array.Empty<PrototypeNetworkStartMember>();
        private Transform[] _props = Array.Empty<Transform>();
        private string _runId = "", _phase = "WaitingRoom";
        private int _chapter, _stage;
        private float _stageStarted, _sendAt;
        private long _sequence, _ticks;
        private PrototypeChapterCompletion _completion;
        public PrototypeGame ServerGame { get; private set; }
        public string Phase => _phase;
        public long SimulationTicks => _ticks;

        public void Configure(PrototypeNetworkTransport network)
        {
            _network = network;
            _network.InputReceived += OnInput;
            _network.ServerPeerLeft += OnPeerLeft;
        }
        private void Update()
        {
            if (_network == null || !_network.IsServer) return;
            var room = _network.Snapshot;
            if (room != null && room.phase == "Reserved" && room.runId != _runId)
            {
                _bindings = _network.StartingRoster();
                if (_bindings.Length < 2 || _bindings.Length > 4) return;
                var connections = new ulong[_bindings.Length];
                for (var i = 0; i < _bindings.Length; i++) connections[i] = _bindings[i].connection;
                _runId = room.runId; _chapter = room.chapter; _stage = 1; _ticks = 0; _completion = null;
                var ownerActor = Array.FindIndex(_bindings, member => member.slot == room.owner);
                if (ownerActor < 0) return;
                PrototypeSession.BeginNetworkChapter(_chapter, _runId, _bindings.Length, 1839, ownerActor);
                _inputs = new PrototypeNetworkInputBuffer(_runId, _stage, connections);
                // A peer can leave between reservation and this frame. Keep its original actor slot disconnected.
                for (var i = 0; i < _bindings.Length; i++)
                    foreach (var peer in room.peers)
                        if (peer.slot == _bindings[i].slot && !peer.connected) OnPeerLeft(_bindings[i].connection);
                BuildStage();
            }
            if (_phase == "Story" && _network.ServerStoryReady)
            {
                _phase = "WaitingRoom"; _network.ServerReturnToRoom();
            }
        }
        private void BuildStage()
        {
            ClearGame();
            _phase = "Playing"; _stageStarted = Time.time;
            var root = new GameObject("Server Chapter " + _chapter + " Stage " + _stage);
            root.transform.SetParent(transform, false);
            ServerGame = root.AddComponent<PrototypeGame>();
            ServerGame.ConfigureChapter(_chapter);
            var names = new string[_bindings.Length];
            for (var i = 0; i < names.Length; i++) names[i] = _bindings[i].name;
            ServerGame.ConfigureServerPlayers(names);
            ServerGame.ServerStageEnded += OnStageEnded;
            _props = Array.Empty<Transform>();
        }
        private void FixedUpdate()
        {
            if (_phase != "Playing" || ServerGame == null || ServerGame.Exit == null || _inputs == null) return;
            var now = Time.realtimeSinceStartupAsDouble;
            foreach (var player in ServerGame.ControlledPlayers)
                player.StepServerInput(_inputs.Consume(player.Participant.ParticipantId, now), Time.fixedDeltaTime);
            _ticks++;
        }
        private void LateUpdate()
        {
            if (_network == null || !_network.IsServer || ServerGame == null || ServerGame.Exit == null || Time.unscaledTime < _sendAt) return;
            _sendAt = Time.unscaledTime + .05f;
            _network.BroadcastWorld(Capture(), _bindings);
        }
        private void OnInput(ulong sender, PrototypeNetworkPlayerInput packet)
        {
            var error = "no_active_world";
            if (_phase == "Playing" && _inputs != null)
                _inputs.Submit(sender, packet, Time.realtimeSinceStartupAsDouble, out error);
            _network.ReportInputVerdict(sender, error);
        }
        private void OnPeerLeft(ulong connection)
        {
            if (_inputs == null || !_inputs.TryGetActor(connection, out var actor)) return;
            _inputs.Disconnect(connection); PrototypeSession.Disconnect(actor);
            if (ServerGame == null) return;
            foreach (var player in ServerGame.ControlledPlayers)
                if (player.Participant.ParticipantId == actor)
                { player.Participant.SetConnected(false); player.Interaction.Drop(); }
        }
        private void OnStageEnded(bool won)
        {
            if (_phase != "Playing") return;
            if (won && PrototypeSession.AdvanceStage())
            {
                _stage = PrototypeSession.Stage; _inputs.AdvanceStage(_stage); BuildStage(); return;
            }
            ServerGame.gameObject.SetActive(false);
            ClearLure();
            if (won)
            {
                _completion = PrototypeChapterCompletion.FromSession();
                _phase = "Story"; _network.ServerBeginStory();
            }
            else { _phase = "Lobby"; _network.ServerReturnToRoom(); }
        }
        public PrototypeNetworkWorldState Capture()
        {
            if (ServerGame == null || ServerGame.Exit == null) return null;
            if (_props.Length == 0) _props = PrototypeNetworkWorldState.DynamicProps(ServerGame);
            var game = ServerGame; var exit = game.Exit; var objective = game.Objective;
            var state = new PrototypeNetworkWorldState
            {
                runId = _runId, chapter = _chapter, stage = _stage, seed = PrototypeSession.Seed,
                phase = _phase, sequence = ++_sequence, simulationTick = _ticks,
                score = PrototypeSession.RunScore, elapsed = PrototypeSession.RunSeconds + (_phase == "Playing" && !exit.IsSettled ? Mathf.Max(0, Time.time - _stageStarted) : 0),
                arrivals = exit.ArrivedCount, exitRemaining = exit.SecondsRemaining, settled = exit.IsSettled,
                roles = objective.RequiredRoles, activeRoles = objective.ActiveRoles, objective = objective.Description,
                status = _phase == "Lobby" ? PrototypeSession.LastResult : game.StatusMessage,
                bossWarning = objective.BossWarning, bossDanger = objective.BossDanger,
                lureActive = PrototypeLure.Active != null, lurePosition = PrototypeLure.Active == null ? Vector3.zero : PrototypeLure.Active.transform.position,
                actors = new PrototypeNetworkActorState[_bindings.Length], props = new PrototypeNetworkPose[_props.Length],
                pickups = new PrototypeNetworkPickupState[game.Map.Pickups.Count], journal = PrototypeSession.Journal.ToArray()
            };
            state.chapterCompleted = _completion != null;
            state.completion = _completion?.Copy();
            for (var i = 0; i < state.actors.Length; i++)
            {
                var player = game.ControlledPlayers[i]; var actor = player.Participant;
                var items = new PrototypeItemKind[actor.Inventory.Items.Count];
                for (var j = 0; j < items.Length; j++) items[j] = actor.Inventory.Items[j];
                var pitch = Mathf.DeltaAngle(0, player.ViewCamera.transform.localEulerAngles.x);
                state.actors[i] = new PrototypeNetworkActorState
                {
                    id = i, slot = _bindings[i].slot, name = actor.DisplayName,
                    position = player.transform.position, yaw = player.transform.eulerAngles.y, pitch = Mathf.Clamp(pitch, -82, 82),
                    cameraHeight = player.ViewCamera.transform.localPosition.y, health = actor.Health, stamina = player.Stamina,
                    connected = actor.IsConnected, alive = actor.IsAlive, exited = actor.HasEnteredExit, crouching = actor.IsCrouching,
                    necklace = player.NecklaceActive, climbing = player.IsClimbing, items = items,
                    selected = player.Interaction.SelectedSlot, prompt = player.Interaction.Prompt, rescue = Mathf.Clamp01(player.Interaction.RescueProgress),
                    guidance = objective.GuidanceTarget(player.transform.position)
                };
            }
            for (var i = 0; i < _props.Length; i++) state.props[i] = PrototypeNetworkPose.Read(_props[i]);
            for (var i = 0; i < state.pickups.Length; i++)
                state.pickups[i] = new PrototypeNetworkPickupState { kind = game.Map.Pickups[i].Item, pose = PrototypeNetworkPose.Read(game.Map.Pickups[i].transform) };
            return state;
        }
        private static void ClearLure()
        {
            if (PrototypeLure.Active == null) return;
            PrototypeLure.Active.gameObject.SetActive(false); Destroy(PrototypeLure.Active.gameObject);
        }
        private void ClearGame()
        {
            if (ServerGame != null)
            {
                ServerGame.ServerStageEnded -= OnStageEnded;
                ServerGame.gameObject.SetActive(false); Destroy(ServerGame.gameObject); ServerGame = null;
            }
            ClearLure();
        }
        private void OnDestroy()
        {
            if (_network != null) { _network.InputReceived -= OnInput; _network.ServerPeerLeft -= OnPeerLeft; }
            ClearGame();
        }
    }
}
