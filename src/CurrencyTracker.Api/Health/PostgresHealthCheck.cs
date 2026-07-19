// src/CurrencyTracker.Api/Health/PostgresHealthCheck.cs
using CurrencyTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CurrencyTracker.Api.Health;

/// <summary>
/// Readiness check that reports whether the Api can reach Postgres. Uses
/// <see cref="DatabaseFacade.CanConnectAsync(System.Threading.CancellationToken)"/>,
/// which opens and closes a connection and returns <c>false</c> (rather
/// than throwing) when the database is unreachable.
/// </summary>
/// <param name="dbContext">The application's EF Core context.</param>
internal sealed class PostgresHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken
    )
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("Postgres is reachable.")
            : HealthCheckResult.Unhealthy("Postgres is not reachable.");
    }
}
