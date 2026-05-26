using System.Reflection;
using CurrencyTracker.Api;
using CurrencyTracker.AppHost;
using CurrencyTracker.Application;
using CurrencyTracker.Domain;
using CurrencyTracker.Infrastructure;
using CurrencyTracker.Worker;

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
            .InAssembly(typeof(ApplicationAssemblyAnchor).Assembly)
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
            .InAssembly(typeof(InfrastructureAssemblyAnchor).Assembly)
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
            .InAssembly(typeof(ApiAssemblyAnchor).Assembly)
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
            .InAssembly(typeof(WorkerAssemblyAnchor).Assembly)
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
            typeof(DomainAssemblyAnchor).Assembly,
            typeof(ApplicationAssemblyAnchor).Assembly,
            typeof(InfrastructureAssemblyAnchor).Assembly,
            typeof(ApiAssemblyAnchor).Assembly,
            typeof(WorkerAssemblyAnchor).Assembly,
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

    /// <summary>
    /// AppHost is an Aspire orchestrator. It references Api and Worker so
    /// the Aspire SDK can generate <c>Projects.CurrencyTracker_Api</c> and
    /// <c>Projects.CurrencyTracker_Worker</c>; both references are
    /// orchestration-only and do not run in Azure (Phase 14 deploys Api
    /// and Worker directly). The architectural constraint is that AppHost
    /// must <em>not</em> reference any of the layer projects
    /// (<c>Application</c>, <c>Infrastructure</c>, <c>Domain</c>); doing so
    /// would let business code accumulate in the orchestrator and break
    /// the "AppHost is orchestration only" rule in ADR 0002.
    /// </summary>
    [Fact]
    public void AppHost_references_only_Api_and_Worker()
    {
        var result = Types
            .InAssembly(typeof(AppHostAssemblyAnchor).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "CurrencyTracker.Application",
                "CurrencyTracker.Infrastructure",
                "CurrencyTracker.Domain"
            )
            .GetResult();

        var failingTypeNames = result.FailingTypes is null
            ? "(none)"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));

        result
            .IsSuccessful.Should()
            .BeTrue(
                because: $"AppHost is an Aspire orchestrator; it must reference only "
                    + $"Api and Worker (so the Aspire SDK can generate Projects.* "
                    + $"handles), never the layer projects. Failing types in "
                    + $"CurrencyTracker.AppHost: {failingTypeNames}"
            );
    }
}
