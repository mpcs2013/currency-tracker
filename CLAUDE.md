# Currency Tracker — Project Guide for Claude

## Stack
- .NET 10, Aspire 13, C# (nullable enabled, implicit usings)
- Messaging: Wolverine  |  Data: EF Core 10 + Redis  |  Auth: Keycloak
- Tests: xUnit v3, FluentAssertions, NSubstitute, NetArchTest

## Architecture — Clean Architecture, respect the dependency rule
- Domain: entities, value objects, domain exceptions. No infrastructure refs.
- Application: use cases, Wolverine handlers, ports/interfaces.
- Infrastructure: EF Core, Redis, Keycloak, external adapters.
- Api/Host: Aspire wiring, endpoints, ProblemDetails mapping.
- Never let inner layers depend on outer layers. NetArchTest enforces this — keep it green.

## Layout
- /src/Api, /src/Application, /src/Domain, /src/Infrastructure — .NET 10 Clean Architecture
- /web — React + TypeScript (Vite), talks to the API over REST

## Frontend conventions
- TypeScript strict; functional components + hooks.
- Keep API types in sync with the backend DTOs/ProblemDetails contract.
- Lint clean (ESLint) and formatted (Prettier) before commit.

## Backend conventions
- (as in the main project guide — Clean Architecture, xUnit v3, ProblemDetails)

## Error handling
- Typed domain exception hierarchy -> RFC 9457 ProblemDetails at the boundary.
- Don't swallow exceptions; map them.

## Build & test commands
- Build:  dotnet build
- Test:   dotnet test
- Run:    dotnet run --project <AppHost>   (Aspire orchestrates dependencies)

## Conventions
- Match existing patterns before introducing new ones.
- New behavior needs xUnit v3 tests with FluentAssertions; mock with NSubstitute.
- After any change, run the build and the full test suite and report exact errors.
- Prefer small, reviewable diffs.

## Guardrails
- Do not add NuGet packages without flagging why.
- Do not weaken or skip NetArchTest rules to make a build pass.
- Ask before schema/migration changes.