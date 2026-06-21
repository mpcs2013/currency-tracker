using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CurrencyTracker.Api.IntegrationTests.Auth;

/// <summary>
/// Mints signed JWTs for the integration tests against a fixed in-memory
/// signing key, and exposes the key + issuer so the test host can be told
/// to trust it (see <see cref="TestAuthHost"/>). Validation in the API
/// stays fully on — issuer, audience, signature, lifetime, 30s skew — it
/// just validates against this key instead of a live Keycloak JWKS, which
/// keeps the suite hermetic and fast.
/// </summary>
public static class TestJwt
{
    /// <summary>The issuer the test tokens carry and the host is told to accept.</summary>
    public const string Issuer = "https://test.local/realms/currency-tracker";

    /// <summary>
    /// The audience the test tokens carry. Must equal the API's configured
    /// <c>Authentication:Audience</c> (the fixture sets it to this value).
    /// </summary>
    public const string Audience = "currency-tracker-api";

    /// <summary>The symmetric signing key shared by the minter and the host's validation.</summary>
    public static readonly SymmetricSecurityKey SecurityKey = new(
        System.Text.Encoding.UTF8.GetBytes("currency-tracker-integration-test-signing-key-32b")
    )
    {
        KeyId = "test-key",
    };

    private static readonly SigningCredentials Credentials = new(
        SecurityKey,
        SecurityAlgorithms.HmacSha256
    );

    /// <summary>
    /// Mints a signed bearer token for a caller with the supplied realm roles.
    /// </summary>
    /// <param name="roles">The roles to place in the multivalued <c>roles</c>
    /// claim (empty for an authenticated-but-role-less caller).</param>
    /// <returns>A compact JWT string suitable for an <c>Authorization: Bearer</c> header.</returns>
    public static string ForRoles(params string[] roles)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = Credentials,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(),
                ["preferred_username"] = "integration-tester",
                ["roles"] = roles,
            },
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
