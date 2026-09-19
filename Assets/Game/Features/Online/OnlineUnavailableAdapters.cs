using System.Threading;
using System.Threading.Tasks;

namespace SlimeCoop.Online
{
    /// <summary>Explicit fail-closed adapters used until the approved Steam/PlayFab SDK boundary exists.</summary>
    public sealed class UnconfiguredSteamIdentityProvider : IOnlineIdentityProvider
    {
        public Task<OnlineResult<OnlineIdentity>> AuthenticateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(OnlineResult<OnlineIdentity>.Failure(
                OnlineFailureCode.NotConfigured, "steam_provider_not_configured"));
        }
    }

    public sealed class UnconfiguredPlayFabSessionBackend : IOnlineSessionBackend
    {
        public Task<OnlineResult<OnlineServerAllocation>> AllocateServerAsync(
            OnlineSessionRequest request,
            OnlineIdentity identity,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(OnlineResult<OnlineServerAllocation>.Failure(
                OnlineFailureCode.NotConfigured, "playfab_mps_provider_not_configured"));
        }
    }

    public sealed class UnconfiguredServerTransport : IOnlineServerTransport
    {
        public event System.Action<OnlineServerSnapshot> SnapshotReceived { add { } remove { } }
        public event System.Action<OnlineServerEvent> EventReceived { add { } remove { } }
        public event System.Action<string> Disconnected { add { } remove { } }

        public Task<OnlineResult> ConnectAsync(OnlineServerAllocation allocation, CancellationToken cancellationToken)
        {
            return Task.FromResult(OnlineResult.Failure(
                OnlineFailureCode.NotConfigured, "server_transport_not_configured"));
        }

        public Task<OnlineResult> SendCommandAsync(OnlineCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(OnlineResult.Failure(
                OnlineFailureCode.NotConfigured, "server_transport_not_configured"));
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }
    }
}

