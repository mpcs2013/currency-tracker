using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyTracker.Api.ErrorHandling;

/// <summary>
/// <see cref="IExceptionHandler"/> mapping <see cref="ValidationException"/>
/// (thrown by the Wolverine FluentValidation middleware) to an RFC 9457
/// ProblemDetails body with status 400 and an <c>errors</c> extension
/// listing each failure's property name and message.
/// </summary>
/// <remarks>
/// Registered before <see cref="GlobalExceptionHandler"/> in
/// <c>Program.cs</c> so this handler's specific mapping wins over the
/// generic fallback. Returns <c>false</c> for any exception type other
/// than <see cref="ValidationException"/>, allowing the chain to continue.
/// </remarks>
public sealed partial class ValidationExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ValidationExceptionHandler> logger
) : IExceptionHandler
{
    /// <summary>
    /// Try to handle <paramref name="exception"/> as a validation failure.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="exception">Thrown exception.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the exception was a <see cref="ValidationException"/>
    /// and a 400 ProblemDetails body has been written; <c>false</c> otherwise.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is not ValidationException validation)
        {
            return false;
        }

        LogValidationFailed(logger, httpContext.Request.Path, validation.Errors.Count());

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var errors = validation
            .Errors.GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Detail = "One or more validation errors occurred.",
        };
        problem.Extensions["errors"] = errors;

        return await problemDetails.TryWriteAsync(
            new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem }
        );
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Validation failed for {RequestPath}: {ErrorCount} error(s)"
    )]
    static partial void LogValidationFailed(ILogger logger, PathString requestPath, int errorCount);
}
