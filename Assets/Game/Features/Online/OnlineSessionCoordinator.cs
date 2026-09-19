using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SlimeCoop.Online
{
    /// <summary>
    /// Service-neutral client coordinator. It never calculates gameplay results and never treats local loopback as Steam or PlayFab.
    /// </summary>
    public sealed class OnlineSessionCoordinator : IDisposable
    {
        private readonly HashSet<string> _sentRequestIds = new HashSet<string>(StringComparer.Ordinal);
        private IOnlineIdentityProvider _identityProvider;
        private IOnlineSessionBackend _backend;
        private IOnlineServerTransport _transport;
        private OnlineIdentity _identity;
        private OnlineServerAllocation _allocation;
        private OnlineServerSnapshot _lastSnapshot;
        private string[] _lockedRoster;
        private long _lastServerSequence = -1;
        private bool _disposed;

        public OnlineConnectionState State { get; private set; } = OnlineConnectionState.Unconfigured;
        public OnlineIdentity Identity => CopyIdentity(_identity);
        public OnlineServerSnapshot LastSnapshot => _lastSnapshot;

        public event Action<OnlineConnectionState> StateChanged;
        public event Action<OnlineServerSnapshot> SnapshotApplied;
        public event Action<OnlineServerEvent> ServerEventReceived;
        public event Action<string> Disconnected;

        public OnlineResult Configure(
            IOnlineIdentityProvider identityProvider,
            IOnlineSessionBackend backend,
            IOnlineServerTransport transport)
        {
            if (_disposed) return OnlineResult.Failure(OnlineFailureCode.SessionEnded, "coordinator_disposed");
            if (State == OnlineConnectionState.Connected || State == OnlineConnectionState.ConnectingServer)
                return OnlineResult.Failure(OnlineFailureCode.InvalidArgument, "cannot_reconfigure_connected_session");
            if (identityProvider == null || backend == null || transport == null)
                return OnlineResult.Failure(OnlineFailureCode.NotConfigured, "online_provider_missing");

            UnsubscribeTransport();
            _identityProvider = identityProvider;
            _backend = backend;
            _transport = transport;
            _transport.SnapshotReceived += HandleSnapshot;
            _transport.EventReceived += HandleServerEvent;
            _transport.Disconnected += HandleDisconnected;
            _identity = null;
            _allocation = null;
            _lastSnapshot = null;
            _lockedRoster = null;
            _lastServerSequence = -1;
            _sentRequestIds.Clear();
            SetState(OnlineConnectionState.Configured);
            return OnlineResult.Success();
        }

        public async Task<OnlineResult<OnlineIdentity>> AuthenticateAsync(CancellationToken cancellationToken)
        {
            if (_disposed) return OnlineResult<OnlineIdentity>.Failure(OnlineFailureCode.SessionEnded, "coordinator_disposed");
            if (State != OnlineConnectionState.Configured && State != OnlineConnectionState.Disconnected && State != OnlineConnectionState.Failed)
                return OnlineResult<OnlineIdentity>.Failure(OnlineFailureCode.InvalidArgument, "invalid_authentication_state");

            SetState(OnlineConnectionState.Authenticating);
            var result = await _identityProvider.AuthenticateAsync(cancellationToken);
            if (!result.Succeeded || result.Value == null || !result.Value.IsValid())
            {
                SetState(OnlineConnectionState.Failed);
                return result.Succeeded
                    ? OnlineResult<OnlineIdentity>.Failure(OnlineFailureCode.AuthenticationFailed, "invalid_identity_result")
                    : result;
            }

            _identity = CopyIdentity(result.Value);
            SetState(OnlineConnectionState.Authenticated);
            return OnlineResult<OnlineIdentity>.Success(CopyIdentity(_identity));
        }

        public async Task<OnlineResult<OnlineServerAllocation>> JoinOrAllocateAsync(
            OnlineSessionRequest request,
            CancellationToken cancellationToken)
        {
            if (_disposed) return OnlineResult<OnlineServerAllocation>.Failure(OnlineFailureCode.SessionEnded, "coordinator_disposed");
            if (State != OnlineConnectionState.Authenticated || _identity == null)
                return OnlineResult<OnlineServerAllocation>.Failure(OnlineFailureCode.Unauthorized, "identity_required");
            if (request == null)
                return OnlineResult<OnlineServerAllocation>.Failure(OnlineFailureCode.InvalidArgument, "invalid_session_request");
            if (!request.IsValid(out var reason))
                return OnlineResult<OnlineServerAllocation>.Failure(OnlineFailureCode.InvalidArgument, reason);

            SetState(OnlineConnectionState.AllocatingServer);
            var result = await _backend.AllocateServerAsync(request, CopyIdentity(_identity), cancellationToken);
            if (!result.Succeeded || result.Value == null || !result.Value.IsValid())
            {
                SetState(OnlineConnectionState.Failed);
                return result.Succeeded
                    ? OnlineResult<OnlineServerAllocation>.Failure(OnlineFailureCode.ServerAllocationFailed, "invalid_server_allocation")
                    : result;
            }

            _allocation = CopyAllocation(result.Value);
            SetState(OnlineConnectionState.ConnectingServer);
            var connected = await _transport.ConnectAsync(_allocation, cancellationToken);
            if (!connected.Succeeded)
            {
                SetState(OnlineConnectionState.Failed);
                return OnlineResult<OnlineServerAllocation>.Failure(connected.FailureCode, connected.Message);
            }

            SetState(OnlineConnectionState.Connected);
            return OnlineResult<OnlineServerAllocation>.Success(CopyAllocation(_allocation));
        }

        public async Task<OnlineResult> SendCommandAsync(OnlineCommand command, CancellationToken cancellationToken)
        {
            var validation = ValidateCommand(command);
            if (!validation.Succeeded) return validation;
            if (!_sentRequestIds.Add(command.RequestId))
                return OnlineResult.Failure(OnlineFailureCode.DuplicateCommand, "duplicate_request_id");

            var result = await _transport.SendCommandAsync(command, cancellationToken);
            if (!result.Succeeded && result.FailureCode == OnlineFailureCode.SessionEnded)
                SetState(OnlineConnectionState.Disconnected);
            return result;
        }

        public async Task DisconnectAsync()
        {
            if (_disposed || _transport == null) return;
            await _transport.DisconnectAsync();
            SetState(OnlineConnectionState.Disconnected);
        }

        public OnlineResult ApplyServerSnapshot(OnlineServerSnapshot snapshot)
        {
            if (_disposed) return OnlineResult.Failure(OnlineFailureCode.SessionEnded, "coordinator_disposed");
            if (snapshot == null)
                return OnlineResult.Failure(OnlineFailureCode.InvalidArgument, "invalid_server_snapshot");
            if (!snapshot.IsValid(out var reason))
                return OnlineResult.Failure(OnlineFailureCode.InvalidArgument, reason);
            if (_allocation != null && (snapshot.SessionId != _allocation.SessionId || snapshot.ServerId != _allocation.ServerId))
                return OnlineResult.Failure(OnlineFailureCode.Unauthorized, "server_identity_mismatch");
            if (_lastSnapshot != null && snapshot.Sequence <= _lastServerSequence)
                return OnlineResult.Failure(OnlineFailureCode.StaleState, "stale_server_snapshot");
            if (!RosterRemainsStable(snapshot.StartingRoster, out reason))
                return OnlineResult.Failure(OnlineFailureCode.InvalidArgument, reason);

            _lastServerSequence = snapshot.Sequence;
            _lastSnapshot = CopySnapshot(snapshot);
            if (State == OnlineConnectionState.ConnectingServer) SetState(OnlineConnectionState.Connected);
            SnapshotApplied?.Invoke(_lastSnapshot);
            return OnlineResult.Success();
        }

        public OnlineResult ApplyServerEvent(OnlineServerEvent serverEvent)
        {
            if (_disposed) return OnlineResult.Failure(OnlineFailureCode.SessionEnded, "coordinator_disposed");
            if (serverEvent == null || !serverEvent.IsValid())
                return OnlineResult.Failure(OnlineFailureCode.InvalidArgument, "invalid_server_event");
            if (_allocation != null && (serverEvent.SessionId != _allocation.SessionId || serverEvent.ServerId != _allocation.ServerId))
                return OnlineResult.Failure(OnlineFailureCode.Unauthorized, "event_session_mismatch");
            if (_lastSnapshot != null && !string.IsNullOrEmpty(serverEvent.RunId) && serverEvent.RunId != _lastSnapshot.RunId)
                return OnlineResult.Failure(OnlineFailureCode.StaleState, "event_run_mismatch");
            if (serverEvent.Sequence <= _lastServerSequence)
                return OnlineResult.Failure(OnlineFailureCode.StaleState, "stale_server_event");

            _lastServerSequence = serverEvent.Sequence;
            ServerEventReceived?.Invoke(serverEvent);
            if (serverEvent.Type == OnlineServerEventType.SessionKicked)
                SetState(OnlineConnectionState.Disconnected);
            return OnlineResult.Success();
        }

        private OnlineResult ValidateCommand(OnlineCommand command)
        {
            if (_disposed) return OnlineResult.Failure(OnlineFailureCode.SessionEnded, "coordinator_disposed");
            if (State != OnlineConnectionState.Connected || _identity == null || _allocation == null)
                return OnlineResult.Failure(OnlineFailureCode.Unauthorized, "connected_identity_required");
            if (command == null || !command.IsValid())
                return OnlineResult.Failure(OnlineFailureCode.InvalidArgument, "invalid_online_command");
            if (command.SessionId != _allocation.SessionId || command.ActorPlayerId != _identity.BackendPlayerId)
                return OnlineResult.Failure(OnlineFailureCode.Unauthorized, "command_identity_mismatch");
            if (_lastSnapshot != null && command.Stage != 0 && command.Stage != _lastSnapshot.Stage)
                return OnlineResult.Failure(OnlineFailureCode.StaleState, "command_stage_mismatch");
            return OnlineResult.Success();
        }

        private bool RosterRemainsStable(string[] roster, out string reason)
        {
            reason = string.Empty;
            if (_lockedRoster == null)
            {
                if (roster == null || roster.Length == 0) return true;
                _lockedRoster = (string[])roster.Clone();
                return true;
            }

            if (roster == null || roster.Length != _lockedRoster.Length)
            {
                reason = "starting_roster_changed";
                return false;
            }

            var expected = new HashSet<string>(_lockedRoster, StringComparer.Ordinal);
            foreach (var playerId in roster)
                if (!expected.Remove(playerId))
                {
                    reason = "starting_roster_changed";
                    return false;
                }
            if (expected.Count != 0) reason = "starting_roster_changed";
            return reason.Length == 0;
        }

        private static OnlineServerSnapshot CopySnapshot(OnlineServerSnapshot snapshot)
        {
            return new OnlineServerSnapshot
            {
                SessionId = snapshot.SessionId,
                ServerId = snapshot.ServerId,
                RunId = snapshot.RunId,
                Phase = snapshot.Phase,
                OwnerPlayerId = snapshot.OwnerPlayerId,
                StartingRoster = snapshot.StartingRoster == null ? Array.Empty<string>() : (string[])snapshot.StartingRoster.Clone(),
                ConnectedPlayers = snapshot.ConnectedPlayers == null ? Array.Empty<string>() : (string[])snapshot.ConnectedPlayers.Clone(),
                Sequence = snapshot.Sequence,
                Chapter = snapshot.Chapter,
                Stage = snapshot.Stage
            };
        }

        private static OnlineIdentity CopyIdentity(OnlineIdentity identity)
        {
            if (identity == null) return null;
            return new OnlineIdentity
            {
                SteamId = identity.SteamId,
                BackendPlayerId = identity.BackendPlayerId,
                DisplayName = identity.DisplayName
            };
        }

        private static OnlineServerAllocation CopyAllocation(OnlineServerAllocation allocation)
        {
            if (allocation == null) return null;
            return new OnlineServerAllocation
            {
                SessionId = allocation.SessionId,
                ServerId = allocation.ServerId,
                ConnectionPayload = allocation.ConnectionPayload,
                ProtocolVersion = allocation.ProtocolVersion
            };
        }

        private void HandleSnapshot(OnlineServerSnapshot snapshot) => ApplyServerSnapshot(snapshot);
        private void HandleServerEvent(OnlineServerEvent serverEvent) => ApplyServerEvent(serverEvent);
        private void HandleDisconnected(string reason)
        {
            SetState(OnlineConnectionState.Disconnected);
            Disconnected?.Invoke(reason ?? string.Empty);
        }

        private void SetState(OnlineConnectionState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }

        private void UnsubscribeTransport()
        {
            if (_transport == null) return;
            _transport.SnapshotReceived -= HandleSnapshot;
            _transport.EventReceived -= HandleServerEvent;
            _transport.Disconnected -= HandleDisconnected;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnsubscribeTransport();
            _identityProvider = null;
            _backend = null;
            _transport = null;
            _identity = null;
            _allocation = null;
            _lastSnapshot = null;
            _lockedRoster = null;
            _sentRequestIds.Clear();
            SetState(OnlineConnectionState.Disconnected);
        }
    }
}

