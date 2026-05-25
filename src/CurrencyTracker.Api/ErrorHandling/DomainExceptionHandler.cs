using CurrencyTracker.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyTracker.Api.ErrorHandling;

/// <summary>
/// <see cref="IExceptionHandler"/> mapping <see cref="DomainException"/>
/// (and all subclasses from <c>CurrencyTracker.Domain.Exceptions</c>)
/// to an RFC 9457 ProblemDetails body with status 422.
/// </summary>
public sealed partial class DomainExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<DomainExceptionHandler> logger
) : IExceptionHandler
{
    /// <summary>
    /// Try to handle <paramref name="exception"/> as a domain-rule violation.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is not DomainException domain)
        {
            return false;
        }

        LogDomainViolation(logger, httpContext.Request.Path, domain.Message, domain);

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Domain rule violation",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.21",
            Detail = domain.Message,
        };

        return await problemDetails.TryWriteAsync(
            new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem }
        );
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Domain rule violation at {RequestPath}: {Message}"
    )]
    static partial void LogDomainViolation(
        ILogger logger,
        PathString requestPath,
        string message,
        Exception domain
    );
}
