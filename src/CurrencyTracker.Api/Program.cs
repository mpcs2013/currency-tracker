using System.Diagnostics;
using CurrencyTracker.Api.ErrorHandling;
using CurrencyTracker.Application;
using JasperFx;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

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
        ctx.ProblemDetails.Instance ??= $"{http.Request.Method} {http.Request.Path}";
        ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? http.TraceIdentifier;
    };
});

builder.Services.AddExceptionHandler<ValidationExceptionHandler>(); // ← added in 6.4
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // ← added in 6.4

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(); // ← added in 6.4

app.MapWolverineEndpoints();

app.UseHttpsRedirection();

//app.Run();

return await app.RunJasperFxCommands(args);
