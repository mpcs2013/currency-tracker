using CurrencyTracker.Api.Security;

namespace CurrencyTracker.Api;

/// <summary>
/// Composition-root wiring for the security headers: binds
/// <see cref="SecurityHeaderOptions"/>, configures the built-in HSTS
/// middleware from the same options, and exposes the middleware registration.
/// </summary>
public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Binds <see cref="SecurityHeaderOptions"/> and configures HSTS (max-age
    /// from config, subdomains included).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddSecurityHeaders(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var section = configuration.GetSection(SecurityHeaderOptions.SectionName);
        services.Configure<SecurityHeaderOptions>(section);

        var options = section.Get<SecurityHeaderOptions>() ?? new SecurityHeaderOptions();
        services.AddHsts(hsts =>
        {
            hsts.MaxAge = TimeSpan.FromDays(options.HstsMaxAgeDays);
            hsts.IncludeSubDomains = true;
        });

        return services;
    }

    /// <summary>
    /// Registers <see cref="SecurityHeadersMiddleware"/>. Call first, so the
    /// headers land on every response.
    /// </summary>
    /// <param name="app">The application pipeline.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
