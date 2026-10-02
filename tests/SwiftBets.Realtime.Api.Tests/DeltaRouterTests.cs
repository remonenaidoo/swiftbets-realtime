using SwiftBets.Contracts.Money;
using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Settlement;
using SwiftBets.Contracts.Trading;
using SwiftBets.Realtime.Application.Routing;
using SwiftBets.Realtime.Domain;

namespace SwiftBets.Realtime.Api.Tests;

public sealed class DeltaRouterTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    [Fact]
    public void Settlement_goes_to_operators_and_to_the_punter_who_owns_the_coupon() =>
        DeltaRouter.Settled(Settled()).Groups.ShouldBe([LiveGroups.Ops, LiveGroups.Punter(Owner)]);

    [Fact]
    public void Settlement_never_reaches_another_punter() =>
        DeltaRouter.Settled(Settled()).Groups.ShouldNotContain(LiveGroups.Punter(Guid.NewGuid()));

    private static CouponSettledV1 Settled() =>
        new(Guid.NewGuid(), Owner, 1, CouponOutcome.Won, new Money(100, "ZAR"), 2.5m, new Money(250, "ZAR"), DateTimeOffset.UtcNow);

    [Fact]
    public void A_rejected_manual_result_reaches_operators_only()
    {
        var route = DeltaRouter.ManualResultRejected(new ManualResultRejectedV1(Guid.NewGuid(), Guid.NewGuid(), ManualResultRejectedV1.CouponCashedOut, "cashed out", DateTimeOffset.UtcNow));

        route.Type.ShouldBe("manual-result-rejected");
        route.Groups.ShouldBe([LiveGroups.Ops]);
    }

    [Fact]
    public void A_market_status_change_is_not_sent_to_punters()
    {
        var route = DeltaRouter.MarketStatusChanged(new MarketStatusChangedV1("fx", "fx-1x2", MarketStatus.Suspended, "trader", "suspended", Guid.NewGuid(), DateTimeOffset.UtcNow));

        route.Groups.ShouldNotContain(g => g.StartsWith("punter:", StringComparison.Ordinal));
    }
}
