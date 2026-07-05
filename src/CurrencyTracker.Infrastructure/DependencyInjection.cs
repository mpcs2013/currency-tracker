using CurrencyTracker.Application.Abstractions.Alerts;
using CurrencyTracker.Application.Abstractions.Caching;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Application.Abstractions.Security;
using CurrencyTracker.Application.Abstractions.Time;
using CurrencyTracker.Infrastructure.Caching;
using CurrencyTracker.Infrastructure.Persistence;
using CurrencyTracker.Infrastructure.Providers;
using CurrencyTracker.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace CurrencyTracker.Infrastructure;

/// <summary>
/// Composition-root registration for the Infrastructure layer. This is
/// the single public seam the Api and Worker hosts use to wire
/// Infrastructure's adapters; the adapters themselves
/// (<c>EfCurrencyRepository</c>, <c>EfExchangeRateRepository</c>,
/// <c>EfUnitOfWork</c>) stay <c>internal</c> and are never named
/// outside this assembly.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the <see cref="Persistence.ApplicationDbContext"/> and the
    /// three Phase 4 persistence ports against their EF Core adapters.
    /// </summary>
    /// <param name="builder">The host application builder (Api or Worker).</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>currencytracker</c> connection string is not
    /// configured — typically because the host was started without the
    /// Aspire AppHost injecting <c>ConnectionStrings__currencytracker</c>.
    /// </exception>
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString =
            builder.Configuration.GetConnectionString("currencytracker")
            ?? throw new InvalidOperationException(
                "The 'currencytracker' connection string is not configured. "
                    + "Are you running through the Aspire AppHost?"
            );

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention()
        );

        // Redis distributed cache. The connection string is injected by the
        // AppHost via WithReference(cache) (Phase 7.5) as ConnectionStrings__cache.
        // Fail fast if it's absent — same discipline as the Postgres connection
        // string in Phase 8. Never hardcode "localhost:6379".
        var cacheConnection =
            builder.Configuration.GetConnectionString("cache")
            ?? throw new InvalidOperationException(
                "ConnectionStrings__cache is not configured. Run via the AppHost "
                    + "(WithReference(cache), Phase 7.5) so the connection string is injected."
            );
        builder.Services.AddStackExchangeRedisCache(options =>
            options.Configuration = cacheConnection
        );
        builder.Services.AddSingleton<ICacheService, RedisCacheService>();

        builder
            .Services.AddOptions<FrankfurterOptions>()
            .Bind(builder.Configuration.GetSection(FrankfurterOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                o => o.BaseUrl is { IsAbsoluteUri: true, Scheme: "https" },
                "Frankfurter:BaseUrl must be an absolute https URI."
            )
            .Validate(o => o.Timeout > TimeSpan.Zero, "Frankfurter:Timeout must be positive.")
            .ValidateOnStart();

        builder
            .Services.AddHttpClient<FrankfurterClient>(
                (sp, client) =>
                {
                    var opts = sp.GetRequiredService<IOptions<FrankfurterOptions>>().Value;
                    client.BaseAddress = opts.BaseUrl;
                    client.Timeout = opts.Timeout;
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
                    client.MaxResponseContentBufferSize = 256 * 1024;
                }
            )
            .AddResilienceHandler(
                "frankfurter",
                static pipeline =>
                {
                    pipeline.AddRetry(
                        new HttpRetryStrategyOptions
                        {
                            MaxRetryAttempts = 3,
                            BackoffType = DelayBackoffType.Exponential,
                            UseJitter = true,
                            Delay = TimeSpan.FromMilliseconds(500),
                        }
                    );
                    pipeline.AddCircuitBreaker(
                        new HttpCircuitBreakerStrategyOptions
                        {
                            FailureRatio = 0.5,
                            MinimumThroughput = 10,
                            SamplingDuration = TimeSpan.FromSeconds(30),
                            BreakDuration = TimeSpan.FromSeconds(15),
                        }
                    );
                    pipeline.AddTimeout(
                        new HttpTimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(3) }
                    );
                }
            );

        // Current-user adapter: projects HttpContext.User onto the Phase 4
        // ICurrentUser port. AddHttpContextAccessor supplies the ambient context;
        // the adapter is internal, so it can only be registered here (ADR 0006).
        // In the Worker (no HttpContext) this resolves to an anonymous view.
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddScoped<ICurrentUser, Security.HttpContextCurrentUser>();
        builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        builder.Services.AddScoped<IAlertRuleEvaluator, Alerts.EfAlertRuleEvaluator>();
        builder.Services.AddScoped<IExchangeRateProvider, FrankfurterExchangeRateProvider>();
        builder.Services.AddScoped<ICurrencyRepository, Persistence.EfCurrencyRepository>();
        builder.Services.AddScoped<IExchangeRateRepository, Persistence.EfExchangeRateRepository>();
        builder.Services.AddScoped<IUnitOfWork, Persistence.EfUnitOfWork>();

        return builder;
    }

    /// <summary>
    /// Registers development-only hosted services — currently the
    /// <see cref="Persistence.MigrationRunner"/>, which applies pending
    /// migrations at startup. Call this from the host only when
    /// <c>IHostEnvironment.IsDevelopment()</c> is true; production
    /// migrations are applied by the deploy pipeline (Phase 14).
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static IHostApplicationBuilder AddInfrastructureDevelopment(
        this IHostApplicationBuilder builder
    )
    {
        builder.Services.AddHostedService<Persistence.MigrationRunner>();
        builder.Services.AddHostedService<Persistence.CurrencySeeder>();
        return builder;
    }
}
