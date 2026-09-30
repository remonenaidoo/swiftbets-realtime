using Microsoft.Extensions.DependencyInjection;

namespace SwiftBets.Realtime.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddRealtimeApplication(this IServiceCollection services) => services;
}
