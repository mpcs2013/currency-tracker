using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;

namespace CurrencyTracker.Api.IntegrationTests.RateLimiting;

/// <summary>
/// Proves the 13.9 contract: exceeding the limit yields 429
/// application/problem+json with a traceId. The path is unauthenticated —
/// the global limiter counts every request before routing resolves an
/// endpoint, so the 401s along the way still consume the per-IP budget.
/// </summary>
public sealed class RateLimitingTests(RateLimitingFixture fixture)
    : IClassFixture<RateLimitingFixture>
{
    [Fact]
    public async Task Exceeding_the_limit_returns_429_problem_json_with_traceId()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );

        // Act — fire past the permit limit; capture the first rejection.
        HttpResponseMessage? rejected = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var response = await client.GetAsync(
                "/api/v1/rates/latest?base=EUR",
                cancellationToken
            );
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rejected = response;
                break;
            }
        }

        // Assert
        rejected.Should().NotBeNull();
        rejected!.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        rejected.Headers.RetryAfter.Should().NotBeNull();

        var body = await rejected.Content.ReadAsStringAsync(cancellationToken);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(429);
        root.GetProperty("title").GetString().Should().Be("Too many requests");
        root.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }
}
