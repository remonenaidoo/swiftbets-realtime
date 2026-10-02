using SwiftBets.Contracts.Money;
using System.Text.Json;
using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Payout;
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

    private static CouponSettledV2 Settled() =>
        new(Guid.NewGuid(), Owner, 1, CouponOutcome.Won, new Money(100, "ZAR"), new Money(250, "ZAR"), [], DateTimeOffset.UtcNow);

    [Fact]
    public void A_v2_settlement_keeps_its_delta_type() => DeltaRouter.Settled(Settled()).Type.ShouldBe("coupon-settled");

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

    [Fact]
    public void A_paid_win_reaches_the_public_ticker_with_a_masked_account_and_no_punter_id()
    {
        var punter = Guid.NewGuid();
        var paid = new PayoutCompletedV1(Guid.NewGuid(), punter, 1, new Money(2_500, "ZAR"), new Money(2_500, "ZAR"), DateTimeOffset.UtcNow);

        var route = DeltaRouter.PublicWin(paid).ShouldNotBeNull();
        var json = JsonSerializer.Serialize(route.Payload);

        (route.Groups.ShouldHaveSingleItem(), route.Type).ShouldBe((LiveGroups.Offer, "win"));
        json.ShouldContain("****" + punter.ToString("N")[^4..]);
        json.ShouldNotContain(punter.ToString());
        json.ShouldNotContain(punter.ToString("N"));
    }

    [Fact]
    public void A_clawback_or_nothing_to_pay_never_reaches_the_public_ticker() =>
        DeltaRouter.PublicWin(new PayoutCompletedV1(Guid.NewGuid(), Guid.NewGuid(), 2, new Money(-500, "ZAR"), new Money(0, "ZAR"), DateTimeOffset.UtcNow)).ShouldBeNull();
}
