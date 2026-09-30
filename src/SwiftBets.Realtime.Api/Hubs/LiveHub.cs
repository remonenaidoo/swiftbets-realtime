using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Realtime.Domain;

namespace SwiftBets.Realtime.Api.Hubs;

/// <summary>
/// Clients never choose a punter or ops group: membership comes from the token. The only client-driven groups are
/// fixtures, which carry public prices and nothing about any punter.
/// </summary>
[Authorize]
public sealed class LiveHub : Hub
{
    public const string Path = "/hubs/live";
    public const string DeltaMethod = "delta";

    public override async Task OnConnectedAsync()
    {
        if (Guid.TryParse(Context.User?.FindFirst("sub")?.Value, out var subject))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, LiveGroups.Punter(subject));
        }

        if (Context.User?.IsInRole(Roles.Operator) == true || Context.User?.IsInRole(Roles.Admin) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, LiveGroups.Ops);
        }

        await base.OnConnectedAsync();
    }

    public Task WatchFixture(string fixtureId) =>
        IsFixtureId(fixtureId) ? Groups.AddToGroupAsync(Context.ConnectionId, LiveGroups.Fixture(fixtureId)) : throw new HubException("Invalid fixture id.");

    public Task UnwatchFixture(string fixtureId) =>
        IsFixtureId(fixtureId) ? Groups.RemoveFromGroupAsync(Context.ConnectionId, LiveGroups.Fixture(fixtureId)) : throw new HubException("Invalid fixture id.");

    private static bool IsFixtureId(string fixtureId) => fixtureId is { Length: > 0 and <= 64 } && fixtureId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
