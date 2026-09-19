using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SlimeCoop.Online
{
    public enum OnlineConnectionState
    {
        Unconfigured,
        Configured,
        Authenticating,
        Authenticated,
        AllocatingServer,
        ConnectingServer,
        Connected,
        Failed,
        Disconnected
    }

    public enum OnlineFailureCode
    {
        None,
        NotConfigured,
        InvalidArgument,
        AuthenticationFailed,
        ServerAllocationFailed,
        ConnectionFailed,
        VersionMismatch,
        Unauthorized,
        StaleState,
        DuplicateCommand,
        ServerUnavailable,
        SessionEnded
    }

    public enum OnlineServerEventType
    {
        RosterLocked,
        HostRoleTransferred,
        StageStarted,
        ExitWindowStarted,
        ExitSettlementClosed,
        StoryConsentChanged,
        RunWiped,
        SessionKicked
    }

    public sealed class OnlineResult
    {
        public bool Succeeded { get; private set; }
        public OnlineFailureCode FailureCode { get; private set; }
        public string Message { get; private set; }

        public static OnlineResult Success()
        {
            return new OnlineResult { Succeeded = true, FailureCode = OnlineFailureCode.None, Message = string.Empty };
        }

        public static OnlineResult Failure(OnlineFailureCode code, string message)
        {
            return new OnlineResult { Succeeded = false, FailureCode = code, Message = message ?? string.Empty };
        }
    }

    public sealed class OnlineResult<T>
    {
        public bool Succeeded { get; private set; }
        public OnlineFailureCode FailureCode { get; private set; }
        public string Message { get; private set; }
        public T Value { get; private set; }

        public static OnlineResult<T> Success(T value)
        {
            return new OnlineResult<T> { Succeeded = true, FailureCode = OnlineFailureCode.None, Message = string.Empty, Value = value };
        }

        public static OnlineResult<T> Failure(OnlineFailureCode code, string message)
        {
            return new OnlineResult<T> { Succeeded = false, FailureCode = code, Message = message ?? string.Empty };
        }
    }

    public sealed class OnlineIdentity
    {
        public string SteamId { get; set; }
        public string BackendPlayerId { get; set; }
        public string DisplayName { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SteamId)
                && !string.IsNullOrWhiteSpace(BackendPlayerId)
                && SteamId.Length <= 64
                && BackendPlayerId.Length <= 128;
        }
    }

    public sealed class OnlineSessionRequest
    {
        public string PartyReference { get; set; }
        public string BuildVersion { get; set; }
        public string ContentVersion { get; set; }
        public int RequestedCapacity { get; set; }
        public bool Practice { get; set; }

        public bool IsValid(out string reason)
        {
            reason = string.Empty;
            if (RequestedCapacity < 2 || RequestedCapacity > 4) reason = "capacity_out_of_range";
            else if (string.IsNullOrWhiteSpace(BuildVersion) || BuildVersion.Length > 64) reason = "invalid_build_version";
            else if (string.IsNullOrWhiteSpace(ContentVersion) || ContentVersion.Length > 64) reason = "invalid_content_version";
            else if (!string.IsNullOrEmpty(PartyReference) && PartyReference.Length > 128) reason = "invalid_party_reference";
            return reason.Length == 0;
        }
    }

    public sealed class OnlineServerAllocation
    {
        public string SessionId { get; set; }
        public string ServerId { get; set; }
        public string ConnectionPayload { get; set; }
        public string ProtocolVersion { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SessionId)
                && !string.IsNullOrWhiteSpace(ServerId)
                && !string.IsNullOrWhiteSpace(ConnectionPayload)
                && !string.IsNullOrWhiteSpace(ProtocolVersion)
                && SessionId.Length <= 128
                && ServerId.Length <= 128
                && ConnectionPayload.Length <= 4096;
        }
    }

    public sealed class OnlineCommand
    {
        public string RequestId { get; set; }
        public string SessionId { get; set; }
        public string RunId { get; set; }
        public string ActorPlayerId { get; set; }
        public string Kind { get; set; }
        public int Stage { get; set; }
        public long ClientSequence { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(RequestId)
                && !string.IsNullOrWhiteSpace(SessionId)
                && !string.IsNullOrWhiteSpace(ActorPlayerId)
                && !string.IsNullOrWhiteSpace(Kind)
                && RequestId.Length <= 96
                && SessionId.Length <= 128
                && ActorPlayerId.Length <= 128
                && Kind.Length <= 64
                && Stage >= 0
                && ClientSequence >= 0;
        }
    }

    public sealed class OnlineServerSnapshot
    {
        public string SessionId { get; set; }
        public string ServerId { get; set; }
        public string RunId { get; set; }
        public string Phase { get; set; }
        public string OwnerPlayerId { get; set; }
        public string[] StartingRoster { get; set; } = Array.Empty<string>();
        public string[] ConnectedPlayers { get; set; } = Array.Empty<string>();
        public long Sequence { get; set; }
        public int Chapter { get; set; }
        public int Stage { get; set; }

        public bool IsValid(out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(SessionId) || SessionId.Length > 128) reason = "invalid_session_id";
            else if (string.IsNullOrWhiteSpace(ServerId) || ServerId.Length > 128) reason = "invalid_server_id";
            else if (RunId == null || RunId.Length > 128) reason = "invalid_run_id";
            else if (Sequence < 0 || Chapter < 0 || Stage < 0) reason = "invalid_sequence_or_stage";
            else if (!ValidRoster(StartingRoster, true)) reason = "invalid_starting_roster";
            else if (!ValidRoster(ConnectedPlayers, false)) reason = "invalid_connected_roster";
            else if (!string.IsNullOrEmpty(OwnerPlayerId) && !Contains(ConnectedPlayers, OwnerPlayerId)) reason = "invalid_owner";
            return reason.Length == 0;
        }

        private static bool ValidRoster(string[] roster, bool startingRoster)
        {
            if (roster == null || roster.Length > 4) return false;
            if (startingRoster && roster.Length != 0 && roster.Length < 2) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in roster)
                if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || !ids.Add(id)) return false;
            return true;
        }

        private static bool Contains(string[] values, string expected)
        {
            if (values == null) return false;
            foreach (var value in values)
                if (string.Equals(value, expected, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    public sealed class OnlineServerEvent
    {
        public OnlineServerEventType Type { get; set; }
        public string SessionId { get; set; }
        public string ServerId { get; set; }
        public string RunId { get; set; }
        public string SubjectPlayerId { get; set; }
        public string Reason { get; set; }
        public long Sequence { get; set; }
        public int Stage { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SessionId)
                && SessionId.Length <= 128
                && !string.IsNullOrWhiteSpace(ServerId)
                && ServerId.Length <= 128
                && (RunId == null || RunId.Length <= 128)
                && Sequence >= 0
                && Stage >= 0;
        }
    }

    public interface IOnlineIdentityProvider
    {
        Task<OnlineResult<OnlineIdentity>> AuthenticateAsync(CancellationToken cancellationToken);
    }

    public interface IOnlineSessionBackend
    {
        Task<OnlineResult<OnlineServerAllocation>> AllocateServerAsync(
            OnlineSessionRequest request,
            OnlineIdentity identity,
            CancellationToken cancellationToken);
    }

    public interface IOnlineServerTransport
    {
        event Action<OnlineServerSnapshot> SnapshotReceived;
        event Action<OnlineServerEvent> EventReceived;
        event Action<string> Disconnected;

        Task<OnlineResult> ConnectAsync(OnlineServerAllocation allocation, CancellationToken cancellationToken);
        Task<OnlineResult> SendCommandAsync(OnlineCommand command, CancellationToken cancellationToken);
        Task DisconnectAsync();
    }
}

