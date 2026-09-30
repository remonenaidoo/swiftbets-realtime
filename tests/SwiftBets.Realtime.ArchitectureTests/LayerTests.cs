using System.Reflection;
using NetArchTest.Rules;

namespace SwiftBets.Realtime.ArchitectureTests;

public sealed class LayerTests
{
    private static readonly Assembly Domain = typeof(SwiftBets.Realtime.Domain.DomainAssembly).Assembly;
    private static readonly Assembly Application = typeof(SwiftBets.Realtime.Application.ApplicationRegistration).Assembly;

    [Fact]
    public void Domain_depends_on_nothing_else_in_the_solution() =>
        Types.InAssembly(Domain).ShouldNot().HaveDependencyOnAny("SwiftBets.Realtime.Application", "SwiftBets.Realtime.Infrastructure", "SwiftBets.BuildingBlocks", "Microsoft.AspNetCore", "Dapper")
            .GetResult().IsSuccessful.ShouldBeTrue();

    [Fact]
    public void Application_does_not_depend_on_infrastructure() =>
        Types.InAssembly(Application).ShouldNot().HaveDependencyOnAny("SwiftBets.Realtime.Infrastructure", "Dapper", "Microsoft.Data.SqlClient", "Confluent.Kafka", "StackExchange.Redis", "Npgsql")
            .GetResult().IsSuccessful.ShouldBeTrue();

    [Fact]
    public void Domain_assembly_references_no_other_project() =>
        Domain.GetReferencedAssemblies().Select(a => a.Name).ShouldNotContain(n => n!.StartsWith("SwiftBets.", StringComparison.Ordinal));
}
