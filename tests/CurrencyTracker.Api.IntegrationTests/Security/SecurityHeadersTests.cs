using CurrencyTracker.Api.IntegrationTests.Rates;

namespace CurrencyTracker.Api.IntegrationTests.Security;

/// <summary>
/// Proves the 13.10 static header set is present on every response. HSTS is
/// not asserted here: UseHsts() is non-dev and HTTPS-only, and the Alba
/// host runs HTTP in Development — HSTS is verified manually over HTTPS.
/// </summary>
public sealed class SecurityHeadersTests(RatesApiFixture fixture) : IClassFixture<RatesApiFixture>
{
    [Fact]
    public async Task Every_response_carries_the_static_security_headers()
    {
        // Act
        var result = await fixture.Host.Scenario(scenario =>
        {
            scenario.Get.Url("/health/live");
            scenario.StatusCodeShouldBe(200);
        });

        // Assert
        var headers = result.Context.Response.Headers;
        headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        headers["Referrer-Policy"].ToString().Should().NotBeNullOrEmpty();
        headers["Content-Security-Policy"].ToString().Should().Contain("default-src");
    }
}
