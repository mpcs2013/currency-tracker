namespace CurrencyTracker.Api.Health;

/// <summary>
/// Composition-root registration for the Api's app-specific readiness
/// checks. Kept out of <c>ServiceDefaults</c> (which stays generic): the
/// Postgres and Redis dependencies are known only here, where the API host
/// composes them. Both checks are tagged <c>ready</c> so the
/// <c>/health/ready</c> endpoint can select them by tag.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds the Postgres and Redis readiness checks (tag <c>ready</c>) to the
    /// health-check set already seeded with the generic <c>self</c>/<c>live</c>
    /// check by <c>ServiceDefaults</c>.
    /// </summary>
    /// <param name="builder">The API host builder.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static IHostApplicationBuilder AddApiReadinessChecks(
        this IHostApplicationBuilder builder
    )
    {
        builder
            .Services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

        return builder;
    }
}
