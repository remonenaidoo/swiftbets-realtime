using SwiftBets.Contracts.Money;
using SwiftBets.Contracts.Settlement;
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
}
