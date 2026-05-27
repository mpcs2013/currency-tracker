using System.Net;
using Aspire.Hosting.Testing;
using FluentAssertions;
using Xunit;

namespace CurrencyTracker.AppHost.SmokeTests;

/// <summary>
/// Smoke tests that boot the full AppHost graph (Api + Worker + Postgres
/// + Redis) via <see cref="DistributedApplicationTestingBuilder"/> and
/// confirm the Api's <c>/alive</c> endpoint returns 200 once all
/// resources are healthy. The CI <c>aspire-smoke</c> job runs this on
/// Linux only (Docker is required).
/// </summary>
public sealed class AppHostAliveSmokeTests
{
    /// <summary>
    /// Boot the AppHost, poll <c>/alive</c> until 200, assert body.
    /// </summary>
    [Fact]
    public async Task Api_alive_endpoint_returns_200_Healthy_after_apphost_starts()
    {
        var ct = TestContext.Current?.CancellationToken ?? CancellationToken.None;

        // Arrange
        var appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.CurrencyTracker_AppHost>(
                ct
            );

        await using var app = await appHost.BuildAsync(ct);
        await app.StartAsync(ct);

        using var httpClient = app.CreateHttpClient("api");

        // Act — poll /alive for up to 90 seconds (image pulls on first run).
        var deadline = DateTime.UtcNow.AddSeconds(90);
        HttpResponseMessage? response = null;
        var body = string.Empty;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                response = await httpClient.GetAsync("/alive", ct);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    body = await response.Content.ReadAsStringAsync(ct);
                    break;
                }
            }
            catch (HttpRequestException)
            {
                // Api process not ready yet; keep polling.
            }

            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be("Healthy");
    }
}
