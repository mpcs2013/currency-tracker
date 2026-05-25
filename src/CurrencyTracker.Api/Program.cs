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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapWolverineEndpoints();

app.UseHttpsRedirection();

//app.Run();

return await app.RunJasperFxCommands(args);
