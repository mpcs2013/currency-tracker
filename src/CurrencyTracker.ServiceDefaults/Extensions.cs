using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace CurrencyTracker.ServiceDefaults;

/// <summary>
/// Adds the standard Aspire service defaults — OpenTelemetry,
/// resilient HTTP defaults, service discovery, and default health
/// checks — to a host. Referenced by both
/// <c>CurrencyTracker.Api</c> and <c>CurrencyTracker.Worker</c>.
/// </summary>
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureSerilog(); // + 13.1

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Registers Serilog as the logging provider behind
    /// <c>ILogger&lt;T&gt;</c> for the host. One pipeline, three sinks:
    /// Console (newline-delimited compact JSON, always), Seq (when the
    /// <c>seq</c> connection string is present — the AppHost injects it
    /// in dev), and OTLP (when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is
    /// set — Aspire sets it locally; the Azure environment sets it in
    /// production). Levels and overrides are read from the host's
    /// <c>Serilog</c> configuration section. Existing
    /// <c>[LoggerMessage]</c> call sites are unaffected: only the
    /// provider behind the abstraction changes.
    /// </summary>
    public static TBuilder ConfigureSerilog<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSerilog(
            (services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(new CompactJsonFormatter());

                // Dev log server (13.7). Presence of the connection string
                // is the switch — never a hardcoded URL (Phase 7/11/12 rule).
                var seqServerUrl = builder.Configuration.GetConnectionString("seq");
                if (!string.IsNullOrWhiteSpace(seqServerUrl))
                {
                    loggerConfiguration.WriteTo.Seq(seqServerUrl);
                }

                // OTLP logs. The sink reads endpoint/protocol/headers from
                // the OTEL_EXPORTER_OTLP_* environment variables itself —
                // the same contract Aspire (dev) and the Phase 14 Azure
                // environment (prod) both fulfil.
                if (
                    !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"])
                )
                {
                    loggerConfiguration.WriteTo.OpenTelemetry();
                }
            }
        );

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(o =>
                        o.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !ctx.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
        );

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder
            .Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Health-check endpoints in non-Development environments have security implications.
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath);

            app.MapHealthChecks(
                AlivenessEndpointPath,
                new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") }
            );
        }

        return app;
    }
}
