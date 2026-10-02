using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Payout;
using SwiftBets.Contracts.Placement;
using SwiftBets.Contracts.Settlement;
using SwiftBets.Contracts.Steward;
using SwiftBets.Contracts.Trading;
using SwiftBets.Realtime.Domain;

namespace SwiftBets.Realtime.Application.Routing;

/// <summary>
/// Decides who sees what. Money events go to operators and to the punter they belong to, never to other punters;
/// Steward and reconciler events are operator-only.
/// </summary>
public static class DeltaRouter
{
    /// <summary>A full fixture snapshot (prices and status): public, so it goes to everyone.</summary>
    public static Route FixtureChanged(FixtureChangedV1 fixture) =>
        new([LiveGroups.Offer, LiveGroups.Fixture(fixture.FixtureId)], "fixture-changed", fixture);

    /// <summary>Carries the V1 summary fields (bet type, total odds, stake) the operator feed shows, plus the V2 bets.</summary>
    public static Route Placed(CouponPlacedV2 placed) =>
        new([LiveGroups.Ops, LiveGroups.Punter(placed.PunterId), .. placed.Legs.Select(l => l.FixtureId).Distinct().Select(LiveGroups.Fixture)], "coupon-placed", new
        {
            placed.CouponId,
            placed.PunterId,
            BetType = placed.Bets.Count != 1 || placed.Bets[0].Lines != 1 || placed.Legs.Any(l => l.IsBanker) ? "system" : placed.Legs.Count == 1 ? "single" : "accumulator",
            TotalOdds = placed.TotalStake.MinorUnits == 0 ? 0m : decimal.Round((decimal)placed.PotentialPayout.MinorUnits / placed.TotalStake.MinorUnits, 2, MidpointRounding.ToZero),
            Stake = placed.TotalStake,
            placed.PotentialPayout,
            placed.Legs,
            placed.Bets,
            placed.PlacedAt,
        });

    public static Route Rejected(CouponRejectedV1 rejected) =>
        new([LiveGroups.Ops, LiveGroups.Punter(rejected.PunterId)], "coupon-rejected", rejected);

    public static Route Settled(CouponSettledV2 settled) =>
        new([LiveGroups.Ops, LiveGroups.Punter(settled.PunterId)], "coupon-settled", settled);

    public static Route Paid(PayoutCompletedV1 paid) =>
        new([LiveGroups.Ops, LiveGroups.Punter(paid.PunterId)], "payout-completed", paid);

    /// <summary>A win for the public recent-winners ticker: the amount and a masked account, never the punter's id.</summary>
    public static Route? PublicWin(PayoutCompletedV1 paid) =>
        paid.Delta.MinorUnits <= 0
            ? null
            : new([LiveGroups.Offer], "win", new
            {
                paid.CouponId,
                Account = "****" + paid.PunterId.ToString("N")[^4..],
                Payout = paid.Delta,
                PaidAt = paid.CompletedAt,
            });

    public static Route Stuck(StuckCouponV1 stuck) => new([LiveGroups.Ops], "stuck-coupon", stuck);

    public static Route PayoutDeadLettered(PayoutAttemptV1 attempt) => new([LiveGroups.Ops], "payout-dead-lettered", attempt);

    /// <summary>A market suspended or reopened outside the feed: operators see it on the trading view.</summary>
    public static Route MarketStatusChanged(MarketStatusChangedV1 change) => new([LiveGroups.Ops], "market-status-changed", change);

    /// <summary>Settlement refused a trader's result for one coupon, for example because it was cashed out.</summary>
    public static Route ManualResultRejected(ManualResultRejectedV1 rejected) => new([LiveGroups.Ops], "manual-result-rejected", rejected);

    public static Route IncidentRaised(IncidentRaisedV1 raised) => new([LiveGroups.Ops], "incident-raised", raised);

    public static Route IncidentUpdated(IncidentUpdatedV1 updated) => new([LiveGroups.Ops], "incident-updated", updated);

    public static Route RemediationExecuted(RemediationExecutedV1 executed) => new([LiveGroups.Ops], "remediation-executed", executed);
}
