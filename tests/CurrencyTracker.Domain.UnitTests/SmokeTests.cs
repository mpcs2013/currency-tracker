namespace CurrencyTracker.Domain.UnitTests;

public sealed class SmokeTests
{
    [Fact]
    public void Domain_project_reference_should_copy_the_domain_assembly_to_the_test_output()
    {
        // Arrange
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, "CurrencyTracker.Domain.dll");

        // Act
        var exists = File.Exists(assemblyPath);

        // Assert
        exists.Should().BeTrue();
    }
}
