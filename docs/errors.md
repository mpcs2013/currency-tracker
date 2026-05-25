# Error contract

## 1) The contract in one paragraph

`CurrencyTracker.Api` converts exceptions into RFC 9457-style `application/problem+json` responses via four `IExceptionHandler` implementations; handlers run from most specific to least specific, and every response includes `type`, `title`, `status`, `detail`, `instance`, plus a `traceId` extension added centrally in `Program.cs`.

## 2) The four exception families with status codes

This contract is owned by the API composition root (see the **API** row in [AGENTS.md#the-eleven-mindset-taxonomy](../AGENTS.md#the-eleven-mindset-taxonomy)).

| Exception family | HTTP status | Title | Type | Handler |
| --- | --- | --- | --- | --- |
| `FluentValidation.ValidationException` | 400 Bad Request | `Validation failed` | `https://tools.ietf.org/html/rfc9110#section-15.5.1` | [`ValidationExceptionHandler`](../src/CurrencyTracker.Api/ErrorHandling/ValidationExceptionHandler.cs) |
| `CurrencyTracker.Application.Exceptions.NotFoundException` | 404 Not Found | `Resource not found` | `https://tools.ietf.org/html/rfc9110#section-15.5.5` | [`NotFoundExceptionHandler`](../src/CurrencyTracker.Api/ErrorHandling/NotFoundExceptionHandler.cs) |
| `CurrencyTracker.Domain.Exceptions.DomainException` (and subclasses) | 422 Unprocessable Entity | `Domain rule violation` | `https://tools.ietf.org/html/rfc9110#section-15.5.21` | [`DomainExceptionHandler`](../src/CurrencyTracker.Api/ErrorHandling/DomainExceptionHandler.cs) |
| Any other `Exception` | 500 Internal Server Error | `Internal server error` | `https://tools.ietf.org/html/rfc9110#section-15.6.1` | [`GlobalExceptionHandler`](../src/CurrencyTracker.Api/ErrorHandling/GlobalExceptionHandler.cs) |

## 3) The response shape (RFC 9457 fields + extensions)

All error responses include these base fields:

- `type` (string)
- `title` (string)
- `status` (number)
- `detail` (string)
- `instance` (string, defaults to `"<HTTP method> <path>"`)

All responses also include `traceId` in `extensions`. Some families add extra extensions (`errors`, `resource`, `key`).

### Validation (400)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "POST /ping-via-bus",
  "errors": {
    "Message": [
      "Message must be 100 characters or fewer."
    ]
  },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
}
```

### Not found (404)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Resource not found",
  "status": 404,
  "detail": "ExchangeRate 'USD/EUR/2026-05-21' was not found.",
  "instance": "GET /throws-notfound",
  "resource": "ExchangeRate",
  "key": "USD/EUR/2026-05-21",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
}
```

### Domain rule violation (422)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.21",
  "title": "Domain rule violation",
  "status": 422,
  "detail": "test domain failure",
  "instance": "GET /throws-domain",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
}
```

### Generic 500 (Production)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal server error",
  "status": 500,
  "detail": "An unexpected error occurred.",
  "instance": "GET /throws-generic",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
}
```

### Generic 500 (Development)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal server error",
  "status": 500,
  "detail": "secret leaked at CurrencyTracker.Api.IntegrationTests",
  "instance": "GET /throws-generic",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
}
```

## 4) Production vs Development differences

From a security perspective (see the **Security** row in [AGENTS.md#the-eleven-mindset-taxonomy](../AGENTS.md#the-eleven-mindset-taxonomy)), only generic 500 `detail` changes by environment:

- **Development:** `detail` = raw `exception.Message`.
- **Non-development (Production, Testing, etc.):** `detail` = `An unexpected error occurred.`.
- Stack traces are never written into the response body; they remain server-side logs.

## 5) How to add a new exception type (five steps)

1. Choose the right family first: if your failure already fits Validation / NotFound / Domain / Generic, reuse it instead of adding a new type.
2. If needed, add the new exception class in the correct layer (`Application` or `Domain`) with a clear, stable message contract.
3. Add a dedicated `IExceptionHandler` in `src/CurrencyTracker.Api/ErrorHandling/` that sets status/title/type/detail and any required extensions.
4. Register the new handler in `src/CurrencyTracker.Api/Program.cs` **before** `GlobalExceptionHandler` so the specific mapping wins.
5. Add/extend contract tests in `tests/CurrencyTracker.Api.IntegrationTests/Errors/ProblemDetailsContractTests.cs` and update this document with the new family and example payload.

## 6) Where to read more

- RFC 9457: https://www.rfc-editor.org/rfc/rfc9457
- API implementation files:
  - [`ValidationExceptionHandler.cs`](../src/CurrencyTracker.Api/ErrorHandling/ValidationExceptionHandler.cs)
  - [`NotFoundExceptionHandler.cs`](../src/CurrencyTracker.Api/ErrorHandling/NotFoundExceptionHandler.cs)
  - [`DomainExceptionHandler.cs`](../src/CurrencyTracker.Api/ErrorHandling/DomainExceptionHandler.cs)
  - [`GlobalExceptionHandler.cs`](../src/CurrencyTracker.Api/ErrorHandling/GlobalExceptionHandler.cs)
  - [`Program.cs` (ProblemDetails customization + handler registration)](../src/CurrencyTracker.Api/Program.cs)
- Contract tests:
  - [`ProblemDetailsContractTests.cs`](../tests/CurrencyTracker.Api.IntegrationTests/Errors/ProblemDetailsContractTests.cs)
