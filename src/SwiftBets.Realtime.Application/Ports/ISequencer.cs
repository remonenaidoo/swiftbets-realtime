namespace SwiftBets.Realtime.Application.Ports;

/// <summary>Hands out the next sequence number for a group, shared by every realtime replica.</summary>
public interface ISequencer
{
    Task<long> NextAsync(string group, CancellationToken cancellationToken);
}
