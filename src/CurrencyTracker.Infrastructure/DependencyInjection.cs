using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        builder.Services.AddDbContext<Persistence.ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString)
        );

        builder.Services.AddScoped<ICurrencyRepository, Persistence.EfCurrencyRepository>();
        builder.Services.AddScoped<IExchangeRateRepository, Persistence.EfExchangeRateRepository>();
        builder.Services.AddScoped<IUnitOfWork, Persistence.EfUnitOfWork>();

        return builder;
    }
}
