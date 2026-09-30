using SwiftBets.Realtime.Application.Ports;
using SwiftBets.Realtime.Domain;

namespace SwiftBets.Realtime.Application.Routing;

public sealed class PushDeltas(ISequencer sequencer, ILiveBroadcaster broadcaster)
{
    public async Task PushAsync(Route route, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        foreach (var group in route.Groups)
        {
            var sequence = await sequencer.NextAsync(group, cancellationToken);
            await broadcaster.SendAsync(new LiveDelta(group, sequence, route.Type, occurredAt, route.Payload), cancellationToken);
        }
    }
}
