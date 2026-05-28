using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// Development-only hosted service that applies pending EF Core migrations
/// at host startup. Registered by <see cref="DependencyInjection.AddInfrastructureDevelopment"/>;
/// the gate in the Api's <c>Program.cs</c> only calls that extension when
/// <c>builder.Environment.IsDevelopment()</c>. Production migrations are
/// a Phase 14 deploy-pipeline concern.
/// </summary>
internal sealed partial class MigrationRunner : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MigrationRunner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationRunner"/> class.
    /// </summary>
    /// <param name="services">The service provider for resolving dependencies.</param>
    /// <param name="logger">The logger instance.</param>
    public MigrationRunner(IServiceProvider services, ILogger<MigrationRunner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogApplyingMigrations(_logger);
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        LogMigrationsApplied(_logger);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Applying database migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Database migrations applied."
    )]
    private static partial void LogMigrationsApplied(ILogger logger);
}
