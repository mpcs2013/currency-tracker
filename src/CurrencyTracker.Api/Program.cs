using CurrencyTracker.Application;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.UseWolverine(opts =>
{
    opts.ApplicationAssembly = typeof(ApplicationAssemblyAnchor).Assembly;
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
