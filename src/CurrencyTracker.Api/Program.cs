using System.Diagnostics;
using CurrencyTracker.Api.ErrorHandling;
using CurrencyTracker.Application;
using CurrencyTracker.Application.Abstractions.Persistence;
using CurrencyTracker.Application.Abstractions.Providers;
using CurrencyTracker.Application.Messaging;
using CurrencyTracker.Infrastructure;
using CurrencyTracker.ServiceDefaults;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ServiceDefaults wires the generic OTel instrumentation but can't see the
// app's telemetry scope. Register the ingestion meter + source here so the
// rates.ingested counter and the ingest.daily_rates span are exported.
builder
    .Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter(IngestionTelemetry.SourceName))
    .WithTracing(tracing => tracing.AddSource(IngestionTelemetry.SourceName));

builder.AddInfrastructure();

if (builder.Environment.IsDevelopment())
{
    builder.AddInfrastructureDevelopment();
}

builder.UseWolverine(opts =>
{
    opts.ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly;
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly); // scan the Api for [Wolverine*] endpoints
    opts.UseFluentValidation();

    // The ingestion handler depends on internal adapters (the Frankfurter
    // provider and the EF repositories / unit-of-work). Wolverine 6 can't
    // inline-construct internal types, and ServiceLocationPolicy.NotAllowed
    // (the 6.0 default) forbids the service-locator fallback. Opt these
    // specific ports into service location so the adapters stay internal
    // sealed, per the cross-layer guardrails.
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateProvider>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IExchangeRateRepository>();
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IUnitOfWork>();
});

builder.Services.AddOpenApi();
builder.Services.AddWolverineHttp();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        var http = ctx.HttpContext;
        var traceId = Activity.Current?.Id ?? http.TraceIdentifier;
        ctx.ProblemDetails.Instance ??= $"{http.Request.Method} {http.Request.Path}";
        ctx.ProblemDetails.Extensions["traceId"] = traceId;
    };
});

builder.Services.AddExceptionHandler<ValidationExceptionHandler>(); // ← added in 6.4
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>(); // ← added in 6.6
builder.Services.AddExceptionHandler<DomainExceptionHandler>(); // ← added in 6.6
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // ← added in 6.4

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler(); // ← added in 6.4
app.MapDefaultEndpoints();
app.MapWolverineEndpoints();

app.UseHttpsRedirection();

app.Run();

/// <summary>
/// Marker type for Alba's <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
