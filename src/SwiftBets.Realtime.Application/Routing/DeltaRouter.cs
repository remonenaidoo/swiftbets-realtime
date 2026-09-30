using SwiftBets.Contracts.Payout;
using SwiftBets.Contracts.Placement;
using SwiftBets.Contracts.Settlement;
using SwiftBets.Contracts.Steward;
using SwiftBets.Realtime.Domain;

namespace SwiftBets.Realtime.Application.Routing;

/// <summary>
/// Decides who sees what. Money events go to operators and to the punter they belong to, never to other punters;
/// Steward and reconciler events are operator-only.
/// </summary>
public static class DeltaRouter
{
    public static Route Placed(CouponPlacedV1 placed) =>
        new([LiveGroups.Ops, LiveGroups.Punter(placed.PunterId), .. placed.Legs.Select(l => l.FixtureId).Distinct().Select(LiveGroups.Fixture)], "coupon-placed", placed);

    public static Route Rejected(CouponRejectedV1 rejected) =>
        new([LiveGroups.Ops, LiveGroups.Punter(rejected.PunterId)], "coupon-rejected", rejected);

    public static Route Settled(CouponSettledV1 settled) =>
        new([LiveGroups.Ops, LiveGroups.Punter(settled.PunterId)], "coupon-settled", settled);

    public static Route Paid(PayoutCompletedV1 paid) =>
        new([LiveGroups.Ops, LiveGroups.Punter(paid.PunterId)], "payout-completed", paid);

    public static Route Stuck(StuckCouponV1 stuck) => new([LiveGroups.Ops], "stuck-coupon", stuck);

    public static Route PayoutDeadLettered(PayoutAttemptV1 attempt) => new([LiveGroups.Ops], "payout-dead-lettered", attempt);

    public static Route IncidentRaised(IncidentRaisedV1 raised) => new([LiveGroups.Ops], "incident-raised", raised);

    public static Route IncidentUpdated(IncidentUpdatedV1 updated) => new([LiveGroups.Ops], "incident-updated", updated);

    public static Route RemediationExecuted(RemediationExecutedV1 executed) => new([LiveGroups.Ops], "remediation-executed", executed);
}
