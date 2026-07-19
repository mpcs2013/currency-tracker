// src/CurrencyTracker.Api/RateLimiting/RateLimitingExtensions.cs
using System.Globalization;
using System.Threading.RateLimiting;
using CurrencyTracker.Api.RateLimiting;

namespace CurrencyTracker.Api;

/// <summary>
/// Composition-root wiring for the Api's global rate limiter: a per-IP
/// fixed window chained with a per-user token bucket, with rejections
/// funnelled through <see cref="IProblemDetailsService"/> so a 429 carries
/// the same problem+json/traceId contract as every other error.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Registers the global chained rate limiter using values bound from the
    /// <see cref="RateLimitingOptions.SectionName"/> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var options =
            configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    http.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(
                        NumberFormatInfo.InvariantInfo
                    );
                }

                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                // Reuse the Phase 6 funnel: WriteAsync runs CustomizeProblemDetails,
                // which stamps traceId + instance onto the payload.
                var problemDetails =
                    http.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetails.WriteAsync(
                    new ProblemDetailsContext
                    {
                        HttpContext = http,
                        ProblemDetails =
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Too many requests",
                            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.29",
                            Detail =
                                "Rate limit exceeded. Retry after the interval indicated by the Retry-After header.",
                        },
                    }
                );
            };

            limiter.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                // Per-IP fixed window: the coarse flood backstop.
                PartitionedRateLimiter.Create<HttpContext, string>(http =>
                {
                    if (IsHealthProbe(http))
                    {
                        return RateLimitPartition.GetNoLimiter("health");
                    }

                    var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        ip,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = options.PerIp.PermitLimit,
                            Window = TimeSpan.FromSeconds(options.PerIp.WindowSeconds),
                            QueueLimit = 0,
                        }
                    );
                }),
                // Per-user token bucket: fair-use ceiling per authenticated caller,
                // falling back to IP for anonymous requests.
                PartitionedRateLimiter.Create<HttpContext, string>(http =>
                {
                    if (IsHealthProbe(http))
                    {
                        return RateLimitPartition.GetNoLimiter("health");
                    }

                    var key =
                        http.User.Identity?.Name
                        ?? http.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous";
                    return RateLimitPartition.GetTokenBucketLimiter(
                        key,
                        _ => new TokenBucketRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            TokenLimit = options.PerUser.TokenLimit,
                            TokensPerPeriod = options.PerUser.TokensPerPeriod,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(
                                options.PerUser.ReplenishmentPeriodSeconds
                            ),
                            QueueLimit = 0,
                        }
                    );
                })
            );
        });

        return services;
    }

    // Probes must never be throttled: an orchestrator hits /health/* on a
    // tight interval, and a rate-limited probe reads as an outage.
    private static bool IsHealthProbe(HttpContext http) =>
        http.Request.Path.StartsWithSegments("/health")
        || http.Request.Path.StartsWithSegments("/alive");
}
