namespace SwiftBets.Realtime.Domain;

/// <summary>
/// The hub's groups. Operators join <see cref="Ops"/>; every signed-in user joins their own punter group; any client may
/// watch a fixture. Group names are the unit of ordering: each group has its own sequence.
/// </summary>
public static class LiveGroups
{
    public const string Ops = "ops";

    /// <summary>Every connection: public prices and fixture status for the betting site.</summary>
    public const string Offer = "offer";

    public static string Punter(Guid punterId) => $"punter:{punterId}";

    public static string Fixture(string fixtureId) => $"fixture:{fixtureId}";
}
