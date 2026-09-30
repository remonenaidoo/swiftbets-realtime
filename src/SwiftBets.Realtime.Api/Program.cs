using Microsoft.AspNetCore.Authentication.JwtBearer;
using SwiftBets.BuildingBlocks.Observability;
using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Contracts.Serialization;
using SwiftBets.Realtime.Api.Hubs;
using SwiftBets.Realtime.Application;
using SwiftBets.Realtime.Application.Ports;
using SwiftBets.Realtime.Infrastructure;

if (HealthProbe.TryRun(args) is { } probeExitCode)
{
    return probeExitCode;
}

var builder = WebApplication.CreateBuilder(args);
builder.AddSwiftBetsObservability("swiftbets-realtime");
builder.Services.AddSwiftBetsWeb();
builder.Services.AddSwiftBetsJwtBearer(builder.Configuration);
builder.Services.AddAuthorization();

// Browsers reach the hub through the gateway, which turns the session cookie into a bearer header. Native clients
// connect directly, and WebSockets cannot carry headers, so the token may also arrive as access_token on hub paths only.
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var inner = options.Events.OnMessageReceived;
    options.Events.OnMessageReceived = async context =>
    {
        await inner(context);
        if (context.Token is null && context.HttpContext.Request.Path.StartsWithSegments("/hubs") && context.Request.Query["access_token"] is { Count: 1 } token)
        {
            context.Token = token;
        }
    };
});

var signalR = builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 4 * 1024)
    .AddJsonProtocol(options =>
    {
        foreach (var converter in ContractJson.Options.Converters)
        {
            options.PayloadSerializerOptions.Converters.Add(converter);
        }
    });
if (builder.Configuration.GetValue("Realtime:RedisBackplane", true) && builder.Configuration.GetConnectionString("Redis") is { Length: > 0 } redis)
{
    signalR.AddStackExchangeRedis(redis, options => options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("swiftbets-realtime"));
}

builder.Services.AddSingleton<ILiveBroadcaster, SignalRBroadcaster>();
builder.Services.AddRealtimeApplication();
builder.Services.AddRealtimeInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseSwiftBetsObservability();
app.UseSwiftBetsWeb();
app.UseAuthentication();
app.UseAuthorization();
app.MapSwiftBetsOperationalEndpoints();
app.MapHub<LiveHub>(LiveHub.Path);

await app.RunAsync();
return 0;

public partial class Program;
