using SwiftBets.BuildingBlocks.Observability;
using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Realtime.Application;
using SwiftBets.Realtime.Infrastructure;

if (HealthProbe.TryRun(args) is { } probeExitCode)
{
    return probeExitCode;
}

var builder = WebApplication.CreateBuilder(args);
builder.AddSwiftBetsObservability("swiftbets-realtime");
builder.Services.AddSwiftBetsWeb();
builder.Services.AddRealtimeApplication();
builder.Services.AddRealtimeInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseSwiftBetsObservability();
app.UseSwiftBetsWeb();
app.MapSwiftBetsOperationalEndpoints();
app.MapGet("/", () => Results.Ok(new { service = "swiftbets-realtime" })).ExcludeFromDescription();

await app.RunAsync();
return 0;

public partial class Program;
