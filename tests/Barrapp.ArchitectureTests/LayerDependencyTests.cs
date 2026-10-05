using System.Reflection;
using NetArchTest.Rules;

namespace Barrapp.ArchitectureTests;

/// <summary>
/// Bloquea el cruce de capas de Clean Architecture. Ver <c>docs/architecture.md</c>.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Barrapp.Domain.Common.Result).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(Barrapp.Application.DependencyInjection).Assembly;

    private static readonly Assembly InfrastructureAssembly =
        typeof(Barrapp.Infrastructure.DependencyInjection).Assembly;

    private static readonly Assembly PersistenceAssembly =
        typeof(Barrapp.Persistence.DependencyInjection).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_other_layers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Barrapp.Application",
                "Barrapp.Infrastructure",
                "Barrapp.Persistence",
                "Barrapp.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Domain_does_not_depend_on_external_packages()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "MediatR",
                "FluentValidation",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_does_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Barrapp.Infrastructure",
                "Barrapp.Persistence",
                "Barrapp.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_does_not_depend_on_aspnetcore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_persistence_or_api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Barrapp.Persistence",
                "Barrapp.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Persistence_does_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(PersistenceAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Barrapp.Infrastructure",
                "Barrapp.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Nothing_depends_on_api()
    {
        var result = Types.InAssemblies(
                [DomainAssembly, ApplicationAssembly, InfrastructureAssembly, PersistenceAssembly])
            .Should()
            .NotHaveDependencyOn("Barrapp.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(TestResult result) =>
        result.FailingTypeNames is null
            ? "Sin detalle."
            : "Tipos que rompen la regla: " + string.Join(", ", result.FailingTypeNames);
}
