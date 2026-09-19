using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    [Serializable] public sealed class PrototypeNetworkHello { public string protocol, name, expectedServerId; }

    /// <summary>Opt-in loopback transport. No UGS/Steam/PlayFab accounts, no public listen interface.</summary>
    public sealed class PrototypeNetworkTransport : MonoBehaviour
    {
        private const string CommandMessage = "slime.command.v1", SnapshotMessage = "slime.snapshot.v1";
        private const string InputMessage = "slime.input.v1", WorldMessage = "slime.world.v1";
        private const string WaitingInputMessage = "slime.waiting-input.v1";
        private const int PacketLimit = 8192;
        private const int WorldPacketLimit = 32768;
        private NetworkManager _manager;
        private PrototypeNetworkRoom _room;
        private readonly Dictionary<ulong, string> _approvedNames = new Dictionary<ulong, string>();
        private readonly Dictionary<ulong, string> _errors = new Dictionary<ulong, string>();
        private readonly Dictionary<ulong, int> _requestsThisSecond = new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, int> _inputsThisSecond = new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, int> _waitingInputsThisSecond = new Dictionary<ulong, int>();
        private float _nextSnapshot, _nextRateWindow;
        private long _sequence;
        private bool _closing;
        private string _expectedServerId = "";
        public bool IsServer { get; private set; }
        public bool Connected => _manager != null && _manager.IsConnectedClient;
        public bool ConnectionEnded { get; private set; }
        public string ServerId => _room?.ServerId ?? Snapshot?.serverId;
        public PrototypeNetworkSnapshot Snapshot { get; private set; }
        public string Status { get; private set; } = "연결 준비";
        public int AcceptedCommands { get; private set; }
        public int RejectedCommands { get; private set; }
        public int RejectedJoins { get; private set; }
        public int AcceptedInputs { get; private set; }
        public int RejectedInputs { get; private set; }
        public int AcceptedWaitingInputs { get; private set; }
        public int RejectedWaitingInputs { get; private set; }
        public bool ServerWaiting => IsServer && _room != null && !_room.RunReserved;
        public long ServerWaitingEpoch => _room?.WaitingEpoch ?? 0;
        public Func<PrototypeNetworkWaitingState> CaptureWaiting;
        public Func<ulong,PrototypeNetworkCommand,string> ServerStationGuard;
        public PrototypeNetworkWorldState WorldState { get; private set; }
        public bool ServerStoryReady => IsServer && _room != null && _room.AllStoryReady;
        public PrototypeNetworkStartMember[] StartingRoster() => IsServer && _room != null ? _room.StartingRoster() : Array.Empty<PrototypeNetworkStartMember>();
        public PrototypeNetworkStartMember[] WaitingRoster() => IsServer && _room != null ? _room.ConnectedRoster() : Array.Empty<PrototypeNetworkStartMember>();
        public event Action<PrototypeNetworkSnapshot> SnapshotReceived;
        public event Action<PrototypeNetworkWorldState> WorldReceived;
        public event Action<ulong, PrototypeNetworkPlayerInput> InputReceived;
        public event Action<ulong, PrototypeNetworkWaitingInput> WaitingInputReceived;
        public event Action<ulong> ServerPeerLeft;

        public bool StartNetwork(bool server, ushort port, int capacity, string playerName, string protocol = PrototypeNetworkRoom.Protocol, string instanceId = "")
        {
            IsServer = server;
            _expectedServerId = server ? "" : instanceId ?? "";
            var obj = new GameObject("Local dedicated network manager");
            var transport = obj.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", port, "127.0.0.1");
            transport.ConnectTimeoutMS = 1000; transport.MaxConnectAttempts = 5; transport.DisconnectTimeoutMS = 3000;
            _manager = obj.AddComponent<NetworkManager>();
            _manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = false,
                ForceSamePrefabs = false, ConnectionApproval = true, TickRate = 30,
                ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new PrototypeNetworkHello { protocol = protocol, name = playerName, expectedServerId = _expectedServerId })) };
            _manager.OnClientConnectedCallback += OnConnected; _manager.OnClientDisconnectCallback += OnDisconnected;
            if (server)
            {
                _room = new PrototypeNetworkRoom(capacity,instanceId); _manager.ConnectionApprovalCallback = Approve;
            }
            var started = server ? _manager.StartServer() : _manager.StartClient();
            if (!started) { Status = "시작 실패 · 포트 사용 여부와 로그를 확인하세요."; return false; }
            _manager.CustomMessagingManager.RegisterNamedMessageHandler(CommandMessage, OnCommand);
            _manager.CustomMessagingManager.RegisterNamedMessageHandler(SnapshotMessage, OnSnapshot);
            _manager.CustomMessagingManager.RegisterNamedMessageHandler(InputMessage, OnInput);
            _manager.CustomMessagingManager.RegisterNamedMessageHandler(WorldMessage, OnWorld);
            _manager.CustomMessagingManager.RegisterNamedMessageHandler(WaitingInputMessage, OnWaitingInput);
            Status = server ? "전용 서버 실행 · 127.0.0.1:" + port : "로컬 서버 접속 중 · 127.0.0.1:" + port;
            return true;
        }
        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = false; response.CreatePlayerObject = false; response.Pending = false;
            try
            {
                if (request.Payload == null || request.Payload.Length > 256) { response.Reason = "invalid_handshake"; RejectedJoins++; return; }
                var hello = JsonUtility.FromJson<PrototypeNetworkHello>(Encoding.UTF8.GetString(request.Payload));
                if (hello == null) { response.Reason = "invalid_handshake"; RejectedJoins++; return; }
                if (!_room.CanJoin(hello.protocol, out var reason,hello.expectedServerId))
                { response.Reason = reason; RejectedJoins++; return; }
                // Reserve a slot during approval, not on a later callback: simultaneous joins cannot exceed capacity.
                if (!_room.Join(request.ClientNetworkId, hello.name, out reason)) { response.Reason = reason; RejectedJoins++; return; }
                _approvedNames[request.ClientNetworkId] = hello.name; response.Approved = true;
            }
            catch (ArgumentException) { response.Reason = "invalid_handshake"; RejectedJoins++; }
        }
        private void OnConnected(ulong client)
        {
            if (IsServer) SendSnapshot(client);
            else Status = "로컬 전용 서버 연결됨 · 계정 인증 없는 개발 시험";
        }
        private void OnDisconnected(ulong client)
        {
            if (IsServer)
            { _room.Leave(client); _approvedNames.Remove(client); _errors.Remove(client); _requestsThisSecond.Remove(client); _inputsThisSecond.Remove(client); _waitingInputsThisSecond.Remove(client); ServerPeerLeft?.Invoke(client); }
            else if (!_closing) { ConnectionEnded = true; Status = "연결 종료: " + _manager.DisconnectReason; }
        }
        private void Update()
        {
            if (!IsServer || _room == null || _manager == null || !_manager.IsListening) return;
            _room.Tick(Time.unscaledDeltaTime);
            if (Time.unscaledTime >= _nextRateWindow) { _requestsThisSecond.Clear(); _inputsThisSecond.Clear(); _waitingInputsThisSecond.Clear(); _nextRateWindow = Time.unscaledTime + 1; }
            if (Time.unscaledTime < _nextSnapshot) return;
            _nextSnapshot = Time.unscaledTime + .05f;
            foreach (var client in _manager.ConnectedClientsIds) SendSnapshot(client);
            Snapshot = CaptureRoom(ulong.MaxValue);
        }
        public void Send(string kind, int chapter = 1, bool ready = false)
        {
            if (!Connected || IsServer) return;
            SendCommand(new PrototypeNetworkCommand { kind = kind, chapter = chapter, ready = ready, sequence = ++_sequence, runId = Snapshot?.runId });
        }
        public void SendCommand(PrototypeNetworkCommand command)
        {
            if (Connected && !IsServer) SendJson(CommandMessage, NetworkManager.ServerClientId, JsonUtility.ToJson(command));
        }
        public void SendPlayerInput(PrototypeNetworkPlayerInput input)
        {
            if (Connected && !IsServer) SendJson(InputMessage, NetworkManager.ServerClientId, JsonUtility.ToJson(input));
        }
        public void SendWaitingInput(PrototypeNetworkWaitingInput input)
        {
            if (Connected && !IsServer) SendJson(WaitingInputMessage, NetworkManager.ServerClientId, JsonUtility.ToJson(input));
        }
        public void ReportWaitingVerdict(string error)
        {
            if (!IsServer) return;
            if (string.IsNullOrEmpty(error)) AcceptedWaitingInputs++; else RejectedWaitingInputs++;
        }
        private void OnWaitingInput(ulong sender, FastBufferReader reader)
        {
            if (!IsServer || !_approvedNames.ContainsKey(sender)) return;
            _waitingInputsThisSecond.TryGetValue(sender,out var count); _waitingInputsThisSecond[sender] = ++count;
            if (count > PrototypeNetworkInputBuffer.RequestsPerSecond)
            { RejectedWaitingInputs++; _approvedNames.Remove(sender); _manager.DisconnectClient(sender,"input_rate_limit"); return; }
            if (!ReadJson(reader,out var json)) { ReportWaitingVerdict("malformed_input"); return; }
            try
            {
                var packet = JsonUtility.FromJson<PrototypeNetworkWaitingInput>(json);
                if (WaitingInputReceived == null) ReportWaitingVerdict("no_waiting_room");
                else WaitingInputReceived.Invoke(sender,packet);
            }
            catch (ArgumentException) { ReportWaitingVerdict("malformed_input"); }
        }
        public void ReportInputVerdict(ulong sender, string error)
        {
            if (!IsServer) return;
            if (string.IsNullOrEmpty(error)) AcceptedInputs++; else RejectedInputs++;
            _errors[sender] = error ?? "";
        }
        private void OnInput(ulong sender, FastBufferReader reader)
        {
            if (!IsServer || !_approvedNames.ContainsKey(sender)) return;
            _inputsThisSecond.TryGetValue(sender, out var count); _inputsThisSecond[sender] = ++count;
            if (count > PrototypeNetworkInputBuffer.RequestsPerSecond)
            { RejectedInputs++; _approvedNames.Remove(sender); _manager.DisconnectClient(sender, "input_rate_limit"); return; }
            if (!ReadJson(reader, out var json)) { ReportInputVerdict(sender, "malformed_input"); return; }
            try
            {
                var input = JsonUtility.FromJson<PrototypeNetworkPlayerInput>(json);
                if (InputReceived == null) ReportInputVerdict(sender, "no_active_world");
                else InputReceived.Invoke(sender, input);
            }
            catch (ArgumentException) { ReportInputVerdict(sender, "malformed_input"); }
        }
        private void OnWorld(ulong sender, FastBufferReader reader)
        {
            if (IsServer || sender != NetworkManager.ServerClientId || !ReadJson(reader, out var json, WorldPacketLimit)) return;
            try
            {
                var state = JsonUtility.FromJson<PrototypeNetworkWorldState>(json);
                if (state == null || !state.IsValid() || (WorldState != null && state.sequence <= WorldState.sequence)) return;
                WorldState = state; WorldReceived?.Invoke(state);
            }
            catch (ArgumentException) { Status = "서버 월드 상태 형식 오류"; }
        }
        public void BroadcastWorld(PrototypeNetworkWorldState state, PrototypeNetworkStartMember[] bindings)
        {
            if (!IsServer || _manager == null || !_manager.IsListening) return;
            WorldState = state;
            foreach (var client in _manager.ConnectedClientsIds)
            {
                state.yourActor = -1;
                for (var i = 0; i < bindings.Length; i++) if (bindings[i].connection == client) { state.yourActor = i; break; }
                SendJson(WorldMessage, client, JsonUtility.ToJson(state), WorldPacketLimit);
            }
            state.yourActor = -1;
        }
        public void ServerBeginStory() { if (IsServer) _room.BeginStory(); }
        public void ServerReturnToRoom() { if (IsServer) _room.ReturnToWaitingRoom(); }
        private void OnCommand(ulong sender, FastBufferReader reader)
        {
            if (!IsServer || !_approvedNames.ContainsKey(sender)) return;
            _requestsThisSecond.TryGetValue(sender, out var count); _requestsThisSecond[sender] = ++count;
            if (count > 30)
            {
                RejectedCommands++; _approvedNames.Remove(sender); // Ignore the rest of this peer's queued burst.
                _manager.DisconnectClient(sender, "rate_limit"); return;
            }
            if (!ReadJson(reader, out var json)) { RejectedCommands++; _errors[sender] = "malformed_packet"; return; }
            try
            {
                var command = JsonUtility.FromJson<PrototypeNetworkCommand>(json);
                if (_room.Apply(sender, command, out var error, ServerStationGuard)) AcceptedCommands++; else RejectedCommands++;
                _errors[sender] = error; SendSnapshot(sender);
            }
            catch (ArgumentException) { RejectedCommands++; _errors[sender] = "malformed_command"; }
        }
        private void OnSnapshot(ulong sender, FastBufferReader reader)
        {
            if (IsServer || sender != NetworkManager.ServerClientId || !ReadJson(reader, out var json)) return;
            try
            {
                var state = JsonUtility.FromJson<PrototypeNetworkSnapshot>(json);
                if (state == null || state.peers == null || state.peers.Length > 4 || state.yourSlot < 0 || state.yourSlot >= 4) return;
                if (_expectedServerId.Length > 0 && state.serverId != _expectedServerId)
                { Status = "연결 종료: server_instance_mismatch"; ConnectionEnded = true; _manager.Shutdown(); return; }
                if (!state.NormalizeWaitingState()) return;
                Snapshot = state; _sequence = Math.Max(_sequence, state.lastSequence); SnapshotReceived?.Invoke(state);
            }
            catch (ArgumentException) { Status = "서버 상태 형식 오류"; }
        }
        private void SendSnapshot(ulong client)
        {
            if (!_manager.ConnectedClients.ContainsKey(client)) return;
            _errors.TryGetValue(client, out var error);
            SendJson(SnapshotMessage, client, JsonUtility.ToJson(CaptureRoom(client, error ?? "")));
        }
        private PrototypeNetworkSnapshot CaptureRoom(ulong client, string error = "")
        {
            var snapshot = _room.Snapshot(client,error);
            if (ServerWaiting) snapshot.waiting = CaptureWaiting?.Invoke();
            return snapshot;
        }
        private void SendJson(string message, ulong target, string json, int limit = PacketLimit)
        {
            var bytes = Encoding.UTF8.GetBytes(json); if (bytes.Length > limit) throw new InvalidOperationException("Network packet exceeds local protocol limit");
            using var writer = new FastBufferWriter(bytes.Length + 4, Allocator.Temp);
            writer.WriteValueSafe(bytes.Length); writer.WriteBytesSafe(bytes);
            _manager.CustomMessagingManager.SendNamedMessage(message, target, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }
        private static bool ReadJson(FastBufferReader reader, out string json, int limit = PacketLimit)
        {
            json = null;
            if (reader.Length < 4 || reader.Length > limit + 4) return false;
            reader.ReadValueSafe(out int length);
            if (length < 2 || length > limit || length != reader.Length - reader.Position) return false;
            var bytes = new byte[length]; reader.ReadBytesSafe(ref bytes, length);
            json = Encoding.UTF8.GetString(bytes); return true;
        }
        public void Close()
        {
            _closing = true;
            if (_manager != null) { _manager.Shutdown(); Status = "연결 종료"; }
        }
        private void OnDestroy() { Close(); if (_manager != null) Destroy(_manager.gameObject); }
    }
}
