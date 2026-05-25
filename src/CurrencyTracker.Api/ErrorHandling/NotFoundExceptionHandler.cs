using CurrencyTracker.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyTracker.Api.ErrorHandling;

/// <summary>
/// <see cref="IExceptionHandler"/> mapping <see cref="NotFoundException"/>
/// to an RFC 9457 ProblemDetails body with status 404. Exposes the
/// resource type and key as ProblemDetails extensions.
/// </summary>
public sealed partial class NotFoundExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<NotFoundExceptionHandler> logger
) : IExceptionHandler
{
    /// <summary>
    /// Try to handle <paramref name="exception"/> as a missing-resource failure.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is not NotFoundException notFound)
        {
            return false;
        }

        LogResourceNotFound(logger, notFound.Resource, notFound.Key, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource not found",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            Detail = notFound.Message,
        };
        problem.Extensions["resource"] = notFound.Resource;
        problem.Extensions["key"] = notFound.Key;

        return await problemDetails.TryWriteAsync(
            new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem }
        );
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Resource not found: {Resource} key={Key} path={RequestPath}"
    )]
    static partial void LogResourceNotFound(
        ILogger logger,
        string resource,
        string key,
        PathString requestPath
    );
}
