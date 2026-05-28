using System.Diagnostics;
using CurrencyTracker.Api.ErrorHandling;
using CurrencyTracker.Application;
using CurrencyTracker.Infrastructure;
using CurrencyTracker.ServiceDefaults;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddInfrastructure();

if (builder.Environment.IsDevelopment())
{
    builder.AddInfrastructureDevelopment();
}

builder.UseWolverine(opts =>
{
    opts.ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly;
    opts.UseFluentValidation();
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
