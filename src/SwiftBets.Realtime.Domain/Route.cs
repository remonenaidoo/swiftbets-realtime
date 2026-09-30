namespace SwiftBets.Realtime.Domain;

/// <summary>Where one consumed event goes: the groups, and the delta type clients switch on.</summary>
public sealed record Route(IReadOnlyList<string> Groups, string Type, object Payload);
