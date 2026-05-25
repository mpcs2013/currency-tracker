using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyTracker.Api.ErrorHandling;

/// in <c>Development</c> the exception's <see cref="Exception.Message"/>
/// is exposed; in any other environment the field reads the literal
/// <c>"An unexpected error occurred."</c>. The environment check uses
/// <see cref="HostEnvironmentEnvExtensions.IsDevelopment"/>. Stack
/// traces never appear in the response body — they are logged
/// server-side only.
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger
) : IExceptionHandler
{
    /// <summary>
    /// Handle <paramref name="exception"/> as a generic 500 fallback.
    /// Always returns <c>true</c> — this is the last handler in the chain.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        LogUnhandledException(logger, httpContext.Request.Path, exception);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var detail = environment.IsDevelopment()
            ? exception.Message
            : "An unexpected error occurred.";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal server error",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Detail = detail,
        };

        return await problemDetails.TryWriteAsync(
            new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem }
        );
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Unhandled exception at {RequestPath}"
    )]
    static partial void LogUnhandledException(
        ILogger logger,
        PathString requestPath,
        Exception exception
    );
}
