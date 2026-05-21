using System.Reflection;
using CurrencyTracker.Domain;

namespace CurrencyTracker.Architecture.Tests;

/// <summary>
/// Architecture-boundary assertions that fail the build when the
/// Clean Architecture dependency direction is violated. Each [Fact]
/// inspects one layer's compiled IL and asserts the absence of
/// forbidden outbound references.
/// </summary>
public sealed class LayerBoundaryTests
{
    /// <summary>
    /// Domain is the pure layer. It must not depend on any other
    /// layer in the src/ tree. This is the strictest boundary and
    /// the most expensive to repair if violated, which is why it
    /// is the first architecture test in the project.
    /// </summary>
    [Fact]
    public void Domain_has_zero_outbound_layer_references()
    {
        var result = Types
            .InAssembly(typeof(DomainAssemblyAnchor).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "CurrencyTracker.Application",
                "CurrencyTracker.Infrastructure",
                "CurrencyTracker.Api",
                "CurrencyTracker.Worker"
            )
            .GetResult();

        var failingTypeNames = result.FailingTypes is null
            ? "(none)"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: $"Domain must be the pure layer with zero outbound references to other layers. "
                    + $"Failing types in CurrencyTracker.Domain: {failingTypeNames}"
            );
    }

    /// <summary>
    /// Application is allowed to depend only on Domain.
    /// It must not depend on Infrastructure, Api, or Worker.
    /// </summary>
    [Fact]
    public void Application_references_only_Domain()
    {
        var result = Types
            .InAssembly(typeof(CurrencyTracker.Application.ApplicationAssemblyAnchor).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "CurrencyTracker.Infrastructure",
                "CurrencyTracker.Api",
                "CurrencyTracker.Worker"
            )
            .GetResult();

        var failingTypeNames = result.FailingTypes is null
            ? "(none)"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: $"Application must only reference Domain and must not depend on Infrastructure, Api, or Worker. "
                    + $"Failing types in CurrencyTracker.Application: {failingTypeNames}"
            );
    }

    /// <summary>
    /// Infrastructure is allowed to depend on Application and Domain.
    /// It must not depend on Api or Worker.
    /// </summary>
    [Fact]
    public void Infrastructure_references_only_Application_and_Domain()
    {
        var result = Types
            .InAssembly(
                typeof(CurrencyTracker.Infrastructure.InfrastructureAssemblyAnchor).Assembly
            )
            .Should()
            .NotHaveDependencyOnAny("CurrencyTracker.Api", "CurrencyTracker.Worker")
            .GetResult();

        var failingTypeNames = result.FailingTypes is null
            ? "(none)"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: $"Infrastructure must only reference Application and Domain and must not depend on Api or Worker. "
                    + $"Failing types in CurrencyTracker.Infrastructure: {failingTypeNames}"
            );
    }

    /// <summary>
    /// Api must not reference Worker.
    /// </summary>
    [Fact]
    public void Api_does_not_reference_Worker()
    {
        var result = Types
            .InAssembly(typeof(CurrencyTracker.Api.ApiAssemblyAnchor).Assembly)
            .Should()
            .NotHaveDependencyOnAny("CurrencyTracker.Worker")
            .GetResult();

        var failingTypeNames = result.FailingTypes is null
            ? "(none)"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: $"Api must not reference Worker. "
                    + $"Failing types in CurrencyTracker.Api: {failingTypeNames}"
            );
    }

    /// <summary>
    /// Worker must not reference Api.
    /// </summary>
    [Fact]
    public void Worker_does_not_reference_Api()
    {
        var result = Types
            .InAssembly(typeof(CurrencyTracker.Worker.WorkerAssemblyAnchor).Assembly)
            .Should()
            .NotHaveDependencyOnAny("CurrencyTracker.Api")
            .GetResult();

        var failingTypeNames = result.FailingTypes is null
            ? "(none)"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: $"Worker must not reference Api. "
                    + $"Failing types in CurrencyTracker.Worker: {failingTypeNames}"
            );
    }

    /// <summary>
    /// Production src/ assemblies must not directly depend on test-only packages.
    /// </summary>
    [Fact]
    public void Src_projects_do_not_reference_test_packages()
    {
        IEnumerable<Assembly> srcAssemblies =
        [
            typeof(CurrencyTracker.Domain.DomainAssemblyAnchor).Assembly,
            typeof(CurrencyTracker.Application.ApplicationAssemblyAnchor).Assembly,
            typeof(CurrencyTracker.Infrastructure.InfrastructureAssemblyAnchor).Assembly,
            typeof(CurrencyTracker.Api.ApiAssemblyAnchor).Assembly,
            typeof(CurrencyTracker.Worker.WorkerAssemblyAnchor).Assembly,
        ];

        var result = Types
            .InAssemblies(srcAssemblies)
            .Should()
            .NotHaveDependencyOnAny(
                "xunit.v3",
                "Xunit",
                "FluentAssertions",
                "NSubstitute",
                "NetArchTest.Rules"
            )
            .GetResult();

        var failingTypeNames = result.FailingTypes is null
            ? "(none)"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: $"Src projects must not reference test-only packages or namespaces. "
                    + $"Failing types across src assemblies: {failingTypeNames}"
            );
    }
}
