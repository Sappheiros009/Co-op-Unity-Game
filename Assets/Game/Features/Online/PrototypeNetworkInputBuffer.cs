using System;
using System.Collections.Generic;

namespace SlimeCoop.Prototype
{
    [Serializable]
    public sealed class PrototypeNetworkPlayerInput
    {
        public string runId;
        public int stage;
        public long sequence;
        public PrototypePlayerInput input;
    }

    /// <summary>
    /// Server-side inbox. The transport's approved connection selects the actor; packets contain no actor ID.
    /// Receive frequency cannot advance physics. The server consumes one held input per fixed simulation tick.
    /// </summary>
    public sealed class PrototypeNetworkInputBuffer
    {
        public const double SilenceTimeoutSeconds = .25;
        public const int RequestsPerSecond = 60;
        private readonly Dictionary<ulong, int> _actors = new Dictionary<ulong, int>();
        private readonly PrototypeNetworkInputChannel[] _channels;
        public string RunId { get; }
        public int Stage { get; private set; }
        public int StartingCount => _channels.Length;

        public PrototypeNetworkInputBuffer(string runId, int stage, IReadOnlyList<ulong> connections)
        {
            if (string.IsNullOrWhiteSpace(runId) || runId.Length > 64) throw new ArgumentException("Invalid run ID.", nameof(runId));
            if (stage < 1 || stage > 6) throw new ArgumentOutOfRangeException(nameof(stage));
            if (connections == null || connections.Count < 2 || connections.Count > 4)
                throw new ArgumentException("A fixed starting roster requires 2 to 4 connections.", nameof(connections));
            RunId = runId; Stage = stage;
            _channels = new PrototypeNetworkInputChannel[connections.Count];
            for (var i = 0; i < connections.Count; i++)
            {
                if (_actors.ContainsKey(connections[i])) throw new ArgumentException("Duplicate connection.", nameof(connections));
                _actors.Add(connections[i], i);
                _channels[i] = new PrototypeNetworkInputChannel();
            }
        }

        public bool TryGetActor(ulong connection, out int actor)
        {
            if (_actors.TryGetValue(connection, out actor) && _channels[actor].Connected) return true;
            actor = -1; return false;
        }
        public bool Submit(ulong connection, PrototypeNetworkPlayerInput packet, double serverNow, out string error)
        {
            error = "";
            if (!TryGetActor(connection, out var actor)) { error = "unknown_connection"; return false; }
            var stateError = packet == null || packet.runId != RunId || packet.stage != Stage ? "stale_stage" : "";
            return _channels[actor].Submit(packet?.sequence ?? 0, packet?.input ?? PrototypePlayerInput.Neutral(), serverNow, out error, stateError);
        }
        public PrototypePlayerInput Consume(int actor, double serverNow)
        {
            if (actor < 0 || actor >= _channels.Length) throw new ArgumentOutOfRangeException(nameof(actor));
            return _channels[actor].Consume(serverNow);
        }
        public long LastSequence(int actor) => _channels[actor].Sequence;
        public void Disconnect(ulong connection)
        {
            if (!_actors.TryGetValue(connection, out var actor)) return;
            var channel = _channels[actor]; channel.Connected = false; channel.Clear();
        }
        public void AdvanceStage(int stage)
        {
            if (stage != Stage + 1 || stage > 6) throw new ArgumentOutOfRangeException(nameof(stage));
            Stage = stage;
            foreach (var channel in _channels)
            {
                channel.Clear();
                // The connection sequence and fixed roster survive stage/owner changes.
            }
        }
    }
}
