namespace SwiftBets.Realtime.Domain;

/// <summary>
/// One pushed change. <see cref="Sequence"/> increases by one per group, so a client that sees a gap knows it missed
/// something and re-reads over HTTP instead of trusting a partial picture.
/// </summary>
public sealed record LiveDelta(string Group, long Sequence, string Type, DateTimeOffset OccurredAt, object Payload);
