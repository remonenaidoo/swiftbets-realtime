using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.Contracts.Messaging;
using SwiftBets.Realtime.Application.Routing;
using SwiftBets.Realtime.Domain;

namespace SwiftBets.Realtime.Infrastructure.Messaging;

public sealed class LiveObserver<T>(PushDeltas push, Func<T, Route> route) : IEventHandler<T>
    where T : IEventContract
{
    public Task HandleAsync(ConsumedEvent<T> message, CancellationToken cancellationToken) =>
        push.PushAsync(route(message.Envelope.Payload), message.Envelope.OccurredAt, cancellationToken);
}
