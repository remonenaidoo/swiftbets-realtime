using StackExchange.Redis;
using SwiftBets.Realtime.Application.Ports;

namespace SwiftBets.Realtime.Infrastructure;

/// <summary>INCR per group: atomic across replicas, so a client sees one gap-free sequence whichever replica pushed.</summary>
public sealed class RedisSequencer(IConnectionMultiplexer redis) : ISequencer
{
    public Task<long> NextAsync(string group, CancellationToken cancellationToken) => redis.GetDatabase().StringIncrementAsync($"rt:seq:{group}");
}
