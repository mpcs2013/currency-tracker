namespace CurrencyTracker.Application.UnitTests;

public sealed class SmokeTests
{
    [Fact]
    public void Application_project_references_should_copy_application_and_domain_assemblies_to_the_test_output()
    {
        // Arrange
        var applicationAssemblyPath = Path.Combine(
            AppContext.BaseDirectory,
            "CurrencyTracker.Application.dll"
        );
        var domainAssemblyPath = Path.Combine(
            AppContext.BaseDirectory,
            "CurrencyTracker.Domain.dll"
        );

        // Act
        var applicationAssemblyExists = File.Exists(applicationAssemblyPath);
        var domainAssemblyExists = File.Exists(domainAssemblyPath);

        // Assert
        applicationAssemblyExists.Should().BeTrue();
        domainAssemblyExists.Should().BeTrue();
    }

    [Fact]
    public void NSubstitute_should_track_received_calls()
    {
        // Arrange
        var disposable = Substitute.For<IDisposable>();

        // Act
        disposable.Dispose();

        // Assert
        disposable.Received(1).Dispose();
    }
}
