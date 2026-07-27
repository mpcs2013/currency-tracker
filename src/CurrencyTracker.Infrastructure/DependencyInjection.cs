using CurrencyTracker.Application.Abstractions.Alerts;
using CurrencyTracker.Application.Abstractions.Caching;
using CurrencyTracker.Application.Abstractions.Notifications;
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
        // 14.44 — one data source for the whole host, and the only place the
        // connection string is read. Registered as a singleton so the container
        // disposes it at shutdown: UseNpgsql(DbDataSource) treats the instance
        // as externally owned and will not dispose it. In Azure this carries an
        // Entra token password provider; locally it is Aspire's string, as-is.
        var dataSource = ApplicationDataSource.Create(builder.Configuration);
        builder.Services.AddSingleton(dataSource);

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(dataSource).UseSnakeCaseNamingConvention()
        );

        // 14.45 — Redis distributed cache. Locally the endpoint is Aspire's
        // plaintext container (WithReference(cache), Phase 7.5) and the
        // connection string is used as-is. In Azure it is Managed Redis with
        // access keys disabled, so the connection must present an Entra token
        // from its FIRST attempt — hence the async factory below: the token
        // extension is awaitable and the cache invokes the factory lazily, once.
        //
        // Never hardcode "localhost:6379". IsNullOrWhiteSpace, not null: an
        // empty string is a configuration value (14.46, docs/configuration.md).
        var cacheEndpoint = builder.Configuration.GetConnectionString(
            CacheConnection.ConnectionStringName
        );

        if (string.IsNullOrWhiteSpace(cacheEndpoint))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings__{CacheConnection.ConnectionStringName} is not configured. "
                    + "Locally it is injected by the Aspire AppHost (WithReference(cache), "
                    + "Phase 7.5); in Azure it is a Key Vault reference (Phase 14.43)."
            );
        }

        var useManagedIdentity = builder.Configuration.GetValue<bool>(
            ApplicationDataSource.ManagedIdentityKey
        );

        if (useManagedIdentity)
        {
            // The Worker never opens a cache connection — 14.24 grants the
            // Managed Redis access policy to the Api ONLY, deliberately, because
            // grants follow code. This factory's laziness is what makes that
            // asymmetry survivable rather than a boot failure in the Worker.
            builder.Services.AddStackExchangeRedisCache(options =>
                options.ConnectionMultiplexerFactory = () =>
                    CacheConnection.ConnectAsync(cacheEndpoint, useManagedIdentity: true)
            );
        }
        else
        {
            builder.Services.AddStackExchangeRedisCache(options =>
                options.Configuration = cacheEndpoint
            );
        }

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
        builder.Services.AddSingleton<IAlertNotifier, Notifications.LogAlertNotifier>();

        builder.Services.AddScoped<IAlertRuleEvaluator, Alerts.EfAlertRuleEvaluator>();
        builder.Services.AddScoped<IExchangeRateProvider, FrankfurterExchangeRateProvider>();
        builder.Services.AddScoped<ICurrencyRepository, Persistence.EfCurrencyRepository>();
        builder.Services.AddScoped<IExchangeRateRepository, Persistence.EfExchangeRateRepository>();
        builder.Services.AddScoped<IAlertRepository, Persistence.EfAlertRepository>();
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
