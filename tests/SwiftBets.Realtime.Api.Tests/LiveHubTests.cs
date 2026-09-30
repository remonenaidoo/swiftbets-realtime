using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SwiftBets.BuildingBlocks.Testing;
using SwiftBets.BuildingBlocks.Web;
using SwiftBets.Contracts.Steward;
using SwiftBets.Realtime.Application.Ports;
using SwiftBets.Realtime.Application.Routing;
using SwiftBets.Realtime.Api.Hubs;

namespace SwiftBets.Realtime.Api.Tests;

public sealed class LiveHubTests : IClassFixture<LiveHubTests.Factory>
{
    private readonly Factory _factory;

    public LiveHubTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Operator_receives_steward_deltas_with_a_sequence()
    {
        await using var connection = Connect(TestJwt.Issue(Guid.NewGuid().ToString(), Roles.Operator));
        var received = new TaskCompletionSource<JsonElement>();
        connection.On<JsonElement>(LiveHub.DeltaMethod, delta => received.TrySetResult(delta));
        await connection.StartAsync(TestContext.Current.CancellationToken);

        var raised = new IncidentRaisedV1(Guid.NewGuid(), "stuckCoupon", "c1", "stuck", DateTimeOffset.UtcNow);
        await _factory.Services.GetRequiredService<PushDeltas>().PushAsync(DeltaRouter.IncidentRaised(raised), raised.OpenedAt, TestContext.Current.CancellationToken);

        var delta = await received.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        delta.GetProperty("type").GetString().ShouldBe("incident-raised");
        delta.GetProperty("group").GetString().ShouldBe("ops");
        delta.GetProperty("sequence").GetInt64().ShouldBeGreaterThan(0);
        delta.GetProperty("payload").GetProperty("incidentId").GetGuid().ShouldBe(raised.IncidentId);
    }

    [Fact]
    public async Task Connection_without_a_token_is_refused()
    {
        await using var connection = Connect(token: null);

        await Should.ThrowAsync<HttpRequestException>(() => connection.StartAsync(TestContext.Current.CancellationToken));
    }

    private HubConnection Connect(string? token) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, LiveHub.Path), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                if (token is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                }
            })
            .Build();

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Kafka:BootstrapServers", "127.0.0.1:1");
            builder.UseSetting("ConnectionStrings:Redis", "127.0.0.1:1,connectTimeout=200,abortConnect=false");
            builder.UseSetting("Jwt:Authority", "https://identity.test");
            builder.UseSetting("Realtime:RunConsumers", "false");
            builder.UseSetting("Realtime:RedisBackplane", "false");
            builder.ConfigureTestServices(services =>
            {
                services.UseTestJwt();
                services.AddSingleton<ISequencer, CountingSequencer>();
            });
        }
    }

    private sealed class CountingSequencer : ISequencer
    {
        private long _next;

        public Task<long> NextAsync(string group, CancellationToken cancellationToken) => Task.FromResult(Interlocked.Increment(ref _next));
    }
}
