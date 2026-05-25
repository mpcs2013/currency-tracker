using CurrencyTracker.Application;
using Wolverine;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

builder.UseWolverine(opts =>
{
    opts.ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly;
});

builder.Services.AddOpenApi();
builder.Services.AddWolverineHttp();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapWolverineEndpoints();

app.UseHttpsRedirection();

app.Run();
