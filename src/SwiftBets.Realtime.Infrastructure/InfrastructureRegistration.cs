using SwiftBets.Contracts.Risk;
using SwiftBets.Contracts.Casino;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftBets.BuildingBlocks.Core;
using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.BuildingBlocks.Redis;
using SwiftBets.Contracts.Messaging;
using SwiftBets.Contracts.Offer;
using SwiftBets.Contracts.Payout;
using SwiftBets.Contracts.Placement;
using SwiftBets.Contracts.Settlement;
using SwiftBets.Contracts.Steward;
using SwiftBets.Contracts.Trading;
using SwiftBets.Realtime.Application.Ports;
using SwiftBets.Realtime.Application.Routing;
using SwiftBets.Realtime.Domain;
using SwiftBets.Realtime.Infrastructure.Messaging;

namespace SwiftBets.Realtime.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddRealtimeInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKafkaMessaging(configuration);
        services.AddSwiftBetsRedis(Required(configuration, "ConnectionStrings:Redis"));
        services.AddFaultInjection(configuration);
        services.AddSingleton<ISequencer, RedisSequencer>();

        if (configuration.GetValue("Realtime:RunConsumers", true))
        {
            // Live views only care about now: new groups start at the latest offset (D68). The group id is per
            // deployment, not per replica, so each event is pushed once and the Redis backplane fans it out.
            Observe<FixtureChangedV1>(services, Topics.FixtureChanged, DeltaRouter.FixtureChanged);
            Observe<CouponPlacedV2>(services, Topics.CouponPlacedV2, DeltaRouter.Placed);
            Observe<CouponRejectedV1>(services, Topics.CouponRejected, DeltaRouter.Rejected);
            Observe<CouponSettledV2>(services, Topics.CouponSettledV2, DeltaRouter.Settled);
            Observe<PayoutCompletedV1>(services, Topics.PayoutCompleted, p => [DeltaRouter.Paid(p), DeltaRouter.PublicWin(p)]);
            Observe<LiabilityChangedV1>(services, Topics.LiabilityChanged, DeltaRouter.Liability);
            Observe<RiskAlertV1>(services, Topics.RiskAlert, DeltaRouter.Alert);
            Observe<CasinoTransactionV1>(services, Topics.CasinoTransaction, DeltaRouter.CasinoMoved);
            Observe<StuckCouponV1>(services, Topics.StuckCoupon, DeltaRouter.Stuck);
            Observe<PayoutAttemptV1>(services, Topics.PayoutDeadLetter, DeltaRouter.PayoutDeadLettered);
            Observe<MarketStatusChangedV1>(services, Topics.MarketStatusChanged, DeltaRouter.MarketStatusChanged);
            Observe<ManualResultRejectedV1>(services, Topics.ManualResultRejected, DeltaRouter.ManualResultRejected);
            Observe<IncidentRaisedV1>(services, Topics.IncidentRaised, DeltaRouter.IncidentRaised);
            Observe<IncidentUpdatedV1>(services, Topics.IncidentUpdated, DeltaRouter.IncidentUpdated);
            Observe<RemediationExecutedV1>(services, Topics.RemediationExecuted, DeltaRouter.RemediationExecuted);
        }

        return services;
    }

    private static void Observe<T>(IServiceCollection services, string topic, Func<T, Route> route)
        where T : IEventContract => Observe<T>(services, topic, e => [route(e)]);

    private static void Observe<T>(IServiceCollection services, string topic, Func<T, IEnumerable<Route?>> routes)
        where T : IEventContract
    {
        services.AddScoped<IEventHandler<T>>(sp => new LiveObserver<T>(sp.GetRequiredService<PushDeltas>(), routes));
        services.AddSingleton<IHostedService>(sp => new KafkaConsumerHost<T>(
            new ConsumerRegistration(topic, $"swiftbets.realtime.{topic}", StartAtLatest: true),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IEventPublisher>(),
            sp.GetRequiredService<IOptions<KafkaOptions>>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<KafkaConsumerHost<T>>>()));
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value ? value : throw new InvalidOperationException($"Configuration '{key}' is required.");
}
