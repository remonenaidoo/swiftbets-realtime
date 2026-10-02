using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.Contracts.Messaging;
using SwiftBets.Realtime.Application.Routing;
using SwiftBets.Realtime.Domain;

namespace SwiftBets.Realtime.Infrastructure.Messaging;

/// <summary>Pushes every route an event produces; a null route means this audience gets nothing.</summary>
public sealed class LiveObserver<T>(PushDeltas push, Func<T, IEnumerable<Route?>> routes) : IEventHandler<T>
    where T : IEventContract
{
    public async Task HandleAsync(ConsumedEvent<T> message, CancellationToken cancellationToken)
    {
        foreach (var route in routes(message.Envelope.Payload))
        {
            if (route is not null)
            {
                await push.PushAsync(route, message.Envelope.OccurredAt, cancellationToken);
            }
        }
    }
}
