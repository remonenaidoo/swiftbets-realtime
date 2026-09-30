using SwiftBets.Realtime.Domain;

namespace SwiftBets.Realtime.Application.Ports;

public interface ILiveBroadcaster
{
    Task SendAsync(LiveDelta delta, CancellationToken cancellationToken);
}
