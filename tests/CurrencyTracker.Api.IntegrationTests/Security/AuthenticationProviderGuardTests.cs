using CurrencyTracker.Api.Security;

namespace CurrencyTracker.Api.IntegrationTests.Security;

/// <summary>
/// Tests for the boot-time assertion that a declared identity provider agrees
/// with the authority the host was actually given. The guard exists so a
/// mismatched deployment dies at startup instead of booting green and rejecting
/// every token at request time. Pure function, so no host is booted here — the
/// tests live in this project only because the guard is <c>internal</c> and this
/// is the assembly <c>InternalsVisibleTo</c> names.
/// </summary>
public sealed class AuthenticationProviderGuardTests
{
    private const string EntraAuthority =
        "https://login.microsoftonline.com/04b94fa0-2449-42e3-b19d-3275d586556a/v2.0";

    private const string KeycloakAuthority = "https://localhost:8080/realms/currency-tracker";

    [Fact]
    public void Validate_WithNoDeclaredProvider_AcceptsAnyAuthority()
    {
        // Arrange & Act
        var act = () => AuthenticationProviderGuard.Validate(provider: null, KeycloakAuthority);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithEntraIdDeclaredAndAnEntraAuthority_Passes()
    {
        // Arrange & Act
        var act = () => AuthenticationProviderGuard.Validate("EntraId", EntraAuthority);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(KeycloakAuthority)]
    [InlineData("http://login.microsoftonline.com/tenant/v2.0")] // not HTTPS
    [InlineData("https://login.microsoftonline.com/tenant")] // no /v2.0
    [InlineData("not-a-uri")]
    public void Validate_WithEntraIdDeclaredAndAMismatchedAuthority_FailsFastNamingBoth(
        string authority
    )
    {
        // Arrange & Act
        var act = () => AuthenticationProviderGuard.Validate("EntraId", authority);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*EntraId*")
            .And.Message.Should()
            .Contain(authority);
    }

    [Fact]
    public void Validate_IsCaseInsensitiveAboutTheDeclaredProvider()
    {
        // Arrange & Act
        var act = () => AuthenticationProviderGuard.Validate("entraid", EntraAuthority);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithAnUnknownDeclaredProvider_AssertsNothing()
    {
        // Arrange & Act
        var act = () => AuthenticationProviderGuard.Validate("Keycloak", KeycloakAuthority);

        // Assert
        act.Should().NotThrow();
    }
}
