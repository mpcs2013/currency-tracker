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
}
