using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace CurrencyTracker.Api.IntegrationTests.Auth;

/// <summary>
/// Test-host plumbing that re-points the JwtBearer scheme at
/// <see cref="TestJwt"/>'s in-memory signing key, so the real validation
/// pipeline accepts tokens this suite mints — without any live identity
/// provider. Production configuration is untouched; this only runs in the
/// test host.
/// </summary>
public static class TestAuthHost
{
    /// <summary>
    /// Configures the web host so the JwtBearer scheme validates against the
    /// test signing key and issuer. Setting <c>options.Configuration</c>
    /// short-circuits OIDC metadata discovery, so the (bogus) test Authority
    /// is never contacted over the network.
    /// </summary>
    /// <param name="builder">The Alba/WebApplicationFactory web host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IWebHostBuilder UseTestJwtBearer(this IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.Configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = TestJwt.Issuer,
                    };
                    options.Configuration.SigningKeys.Add(TestJwt.SecurityKey);

                    // Keep strict validation ON, just bound to the test issuer/key.
                    options.TokenValidationParameters.ValidIssuer = TestJwt.Issuer;
                    options.TokenValidationParameters.IssuerSigningKey = TestJwt.SecurityKey;
                }
            )
        );
}
