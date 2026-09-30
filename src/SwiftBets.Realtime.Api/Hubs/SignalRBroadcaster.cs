using Microsoft.AspNetCore.SignalR;
using SwiftBets.Realtime.Application.Ports;
using SwiftBets.Realtime.Domain;

namespace SwiftBets.Realtime.Api.Hubs;

public sealed class SignalRBroadcaster(IHubContext<LiveHub> hub) : ILiveBroadcaster
{
    public Task SendAsync(LiveDelta delta, CancellationToken cancellationToken) =>
        hub.Clients.Group(delta.Group).SendAsync(LiveHub.DeltaMethod, delta, cancellationToken);
}
