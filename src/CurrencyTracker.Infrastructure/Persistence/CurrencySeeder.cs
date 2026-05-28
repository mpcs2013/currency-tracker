using CurrencyTracker.Domain.Currencies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CurrencyTracker.Infrastructure.Persistence;

/// <summary>
/// Seeds the five common currencies (USD, EUR, CHF, GBP, JPY) on
/// host startup. Idempotent — the seeder skips work when the
/// currencies table is non-empty. Registered only when
/// <see cref="DependencyInjection.AddInfrastructureDevelopment"/> returns true;
/// production seeds are a Phase 14 deploy-pipeline concern.
/// </summary>
/// <remarks>
/// Uses the <see cref="LoggerMessageAttribute"/> source generator for the
/// two log sites so CA1848 (LoggerMessage-delegate performance) is
/// satisfied without per-call allocation. The class is <c>partial</c> so
/// the generator can emit the matching method bodies in a sibling file.
/// </remarks>
internal sealed partial class CurrencySeeder : IHostedService
{
    private static readonly (string Code, string Name, int NumericCode)[] SeedData =
    [
        ("USD", "United States Dollar", 840),
        ("EUR", "Euro", 978),
        ("CHF", "Swiss Franc", 756),
        ("GBP", "Pound Sterling", 826),
        ("JPY", "Japanese Yen", 392),
    ];

    private readonly IServiceProvider _services;
    private readonly ILogger<CurrencySeeder> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="CurrencySeeder"/>.
    /// </summary>
    /// <param name="services">Root service provider for scope creation.</param>
    /// <param name="logger">Logger.</param>
    public CurrencySeeder(IServiceProvider services, ILogger<CurrencySeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await dbContext.Currencies.AnyAsync(cancellationToken))
        {
            LogAlreadySeeded(_logger);
            return;
        }

        foreach (var (code, name, numericCode) in SeedData)
        {
            var currencyCode = CurrencyCode.Create(code).Value;
            var currency = Currency.Create(currencyCode, name, numericCode).Value;
            dbContext.Currencies.Add(currency);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogSeeded(_logger, SeedData.Length);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Information,
        Message = "Currencies already seeded; skipping."
    )]
    private static partial void LogAlreadySeeded(ILogger logger);

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Information,
        Message = "Seeded {Count} currencies."
    )]
    private static partial void LogSeeded(ILogger logger, int count);
}
