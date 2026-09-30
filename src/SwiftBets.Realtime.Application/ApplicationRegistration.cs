using Microsoft.Extensions.DependencyInjection;
using SwiftBets.Realtime.Application.Routing;

namespace SwiftBets.Realtime.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddRealtimeApplication(this IServiceCollection services) => services.AddSingleton<PushDeltas>();
}
