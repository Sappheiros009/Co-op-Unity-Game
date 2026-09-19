using System;
using System.Collections.Generic;

namespace SlimeCoop.Prototype
{
    public readonly struct PrototypeNetworkStartMember
    {
        public readonly ulong connection;
        public readonly int slot;
        public readonly string name;
        public PrototypeNetworkStartMember(ulong connection, int slot, string name)
        { this.connection = connection; this.slot = slot; this.name = name; }
    }
    [Serializable] public sealed class PrototypeNetworkPeer
    {
        public int slot;
        public string name;
        public bool connected = true, ready, storyReady;
        public long lastSequence;
    }
    [Serializable] public sealed class PrototypeNetworkCommand
    {
        public long sequence;
        public string kind, runId;
        public int chapter;
        public bool ready;
    }
    [Serializable] public sealed class PrototypeNetworkSnapshot
    {
        public string serverId, runId, phase, lastError;
        public int yourSlot = -1, owner = -1, capacity, chapter, startingCount;
        public long revision, serverTicks, lastSequence;
        public long waitingEpoch;
        public PrototypeNetworkWaitingState waiting;
        public double serverSeconds;
        public PrototypeNetworkPeer[] peers;
        public bool NormalizeWaitingState()
        {
            // JsonUtility can round-trip a null serializable class as an empty object.
            // Reserved/story snapshots do not carry waiting poses; normalize before validation.
            if (phase != "WaitingRoom") { waiting = null; return true; }
            return waiting == null || (waiting.IsValid() && waiting.epoch == waitingEpoch);
        }
    }

    /// <summary>Server-owned room model. Transport connection IDs, never client-supplied slots, bind authority.</summary>
    public sealed class PrototypeNetworkRoom
    {
        public const string Protocol = "slime-local-room-v4";
        private readonly Dictionary<ulong, PrototypeNetworkPeer> _connections = new Dictionary<ulong, PrototypeNetworkPeer>();
        private readonly List<PrototypeNetworkPeer> _roster = new List<PrototypeNetworkPeer>();
        private PrototypeNetworkStartMember[] _startingRoster = Array.Empty<PrototypeNetworkStartMember>();
        public PrototypeNetworkStartMember[] StartingRoster() => (PrototypeNetworkStartMember[])_startingRoster.Clone();
        public PrototypeNetworkStartMember[] ConnectedRoster()
        {
            var members = new List<PrototypeNetworkStartMember>();
            foreach (var pair in _connections) members.Add(new PrototypeNetworkStartMember(pair.Key,pair.Value.slot,pair.Value.name));
            members.Sort((left,right) => left.slot.CompareTo(right.slot)); return members.ToArray();
        }
        public string ServerId { get; }
        public string RunId { get; private set; } = "";
        public int Capacity { get; }
        public int Owner { get; private set; } = -1;
        public int Chapter { get; private set; } = 1;
        public int StartingCount { get; private set; }
        public long WaitingEpoch { get; private set; } = 1;
        public long Revision { get; private set; }
        public long ServerTicks { get; private set; }
        public double ServerSeconds { get; private set; }
        public bool RunReserved => StartingCount > 0;
        public bool StoryActive { get; private set; }
        public bool AllStoryReady => StoryActive && ConnectedCount > 0 && !_roster.Exists(p => p.connected && !p.storyReady);
        public int ConnectedCount => _connections.Count;

        public PrototypeNetworkRoom(int capacity, string instanceId = "")
        {
            if (capacity < 2 || capacity > 4) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (!string.IsNullOrEmpty(instanceId) && !Guid.TryParseExact(instanceId,"N",out _))
                throw new ArgumentException("Invalid local server instance identity.",nameof(instanceId));
            ServerId = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
            Capacity = capacity;
        }
        public bool CanJoin(string protocol, out string error, string expectedServerId = "")
        {
            error = protocol != Protocol ? "version_mismatch" :
                !string.IsNullOrEmpty(expectedServerId) && expectedServerId != ServerId ? "server_instance_mismatch" :
                RunReserved ? "run_locked" : ConnectedCount >= Capacity ? "room_full" : "";
            return error.Length == 0;
        }
        public bool Join(ulong connection, string name, out string error)
        {
            if (_connections.ContainsKey(connection)) { error = "duplicate_connection"; return false; }
            if (!CanJoin(Protocol, out error)) return false;
            if (string.IsNullOrWhiteSpace(name) || name.Length > 24) { error = "invalid_name"; return false; }
            foreach (var c in name) if (char.IsControl(c) || c == '<' || c == '>') { error = "invalid_name"; return false; }
            var slot = 0;
            while (_roster.Exists(p => p.slot == slot)) slot++;
            var peer = new PrototypeNetworkPeer { slot = slot, name = name };
            _connections.Add(connection, peer); _roster.Add(peer);
            if (Owner < 0) Owner = slot;
            Revision++; return true;
        }
        public void Leave(ulong connection)
        {
            if (!_connections.TryGetValue(connection, out var peer)) return;
            peer.connected = false; _connections.Remove(connection);
            if (!RunReserved) _roster.Remove(peer);
            if (Owner == peer.slot)
            {
                Owner = -1;
                foreach (var member in _roster)
                    if (member.connected && (Owner < 0 || member.slot < Owner)) Owner = member.slot;
            }
            Revision++; // Keep server identity, run identity, fixed roster and clock.
        }
        public bool Apply(ulong connection, PrototypeNetworkCommand command, out string error, Func<ulong,PrototypeNetworkCommand,string> stationGuard = null)
        {
            error = "";
            if (!_connections.TryGetValue(connection, out var peer)) error = "unknown_connection";
            else if (command == null || command.sequence <= peer.lastSequence || command.sequence > peer.lastSequence + 1024) error = "invalid_sequence";
            else
            {
                peer.lastSequence = command.sequence;
                if (command.kind == "story_ready")
                {
                    if (!StoryActive || command.runId != RunId) error = "stale_story";
                    else peer.storyReady = command.ready;
                }
                else if (RunReserved) error = "run_locked";
                else if (command.kind == "ready")
                { error = stationGuard?.Invoke(connection,command) ?? ""; if (error.Length == 0) peer.ready = command.ready; }
                else if (command.kind == "chapter")
                {
                    if (peer.slot != Owner) error = "owner_only";
                    else if (command.chapter < 1 || command.chapter > 7) error = "invalid_chapter";
                    else
                    {
                        error = stationGuard?.Invoke(connection,command) ?? "";
                        if (error.Length == 0) { Chapter = command.chapter; foreach (var member in _roster) member.ready = false; }
                    }
                }
                else if (command.kind == "reserve")
                {
                    if (peer.slot != Owner) error = "owner_only";
                    else if (_connections.Count < 2 || _roster.Exists(p => !p.ready || !p.connected)) error = "party_not_ready";
                    else
                    {
                        error = stationGuard?.Invoke(connection,command) ?? "";
                        if (error.Length == 0)
                        { StartingCount = _roster.Count; RunId = Guid.NewGuid().ToString("N"); _startingRoster = ConnectedRoster(); }
                    }
                }
                else error = "unknown_command";
                Revision++;
            }
            return error.Length == 0;
        }
        public void BeginStory()
        {
            if (!RunReserved || StoryActive) return;
            StoryActive = true;
            foreach (var peer in _roster) peer.storyReady = false;
            Revision++;
        }
        public void ReturnToWaitingRoom()
        {
            StartingCount = 0; RunId = ""; StoryActive = false;
            WaitingEpoch++;
            _startingRoster = Array.Empty<PrototypeNetworkStartMember>();
            _roster.RemoveAll(p => !p.connected);
            foreach (var peer in _roster) { peer.ready = false; peer.storyReady = false; }
            Revision++;
        }
        public void Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 1) return;
            ServerTicks++; ServerSeconds += seconds;
        }
        public PrototypeNetworkSnapshot Snapshot(ulong connection, string error = "")
        {
            _connections.TryGetValue(connection, out var self);
            var peers = _roster.ConvertAll(p => new PrototypeNetworkPeer
            { slot = p.slot, name = p.name, connected = p.connected, ready = p.ready, storyReady = p.storyReady, lastSequence = p.lastSequence }).ToArray();
            return new PrototypeNetworkSnapshot { serverId = ServerId, runId = RunId, phase = StoryActive ? "Story" : RunReserved ? "Reserved" : "WaitingRoom",
                lastError = error, yourSlot = self == null ? -1 : self.slot, owner = Owner, capacity = Capacity, chapter = Chapter,
                startingCount = StartingCount, revision = Revision, serverTicks = ServerTicks, serverSeconds = ServerSeconds,
                lastSequence = self == null ? 0 : self.lastSequence, peers = peers, waitingEpoch = WaitingEpoch };
        }
    }
}
