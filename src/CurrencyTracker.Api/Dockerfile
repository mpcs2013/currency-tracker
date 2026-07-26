# src/CurrencyTracker.Api/Dockerfile
#
# Build context is the REPO ROOT:
#   docker build -f src/CurrencyTracker.Api/Dockerfile -t currencytracker-api:dev .
#
# Stage 1 compiles with the full SDK; stage 2 runs on the chiseled ASP.NET
# runtime: no shell, no package manager, non-root `app` user, port 8080
# (matching modules/container-app-api target_port).
#
# There is deliberately NO HEALTHCHECK instruction: Azure Container Apps
# ignores it (health = platform probes per the Phase 13.B contract), and
# chiseled ships no process capable of running one. See Phase 14.D notes.

# Pinned to an explicit patch, not the floating 10.0-noble tag. AnalysisLevel=
# latest-recommended binds the analyzer set to the SDK, and TreatWarningsAsErrors
# is on — a monthly SDK refresh that adds one recommended rule would break this
# image on a day when no code changed. 10.0.302 is what 10.0-noble resolves to
# today, so this pins current behaviour rather than changing it.
FROM mcr.microsoft.com/dotnet/sdk:10.0.302-noble AS build
WORKDIR /src

# Restore layer: manifests only, so source edits don't invalidate the
# NuGet download. Central Package Management means the props files ARE
# the version manifest — they must be in this layer.
#
# .editorconfig rides along for the *compile*, not the restore:
# AnalysisMode=AllEnabledByDefault turns on every CA rule, and this file is the
# only place CA2007/IDE0058/CA1000/CA1861 are set to none. Omit it and the
# Release build dies on hundreds of ConfigureAwait errors — src/ has just five
# ConfigureAwait calls in total.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/CurrencyTracker.Domain/CurrencyTracker.Domain.csproj          src/CurrencyTracker.Domain/
COPY src/CurrencyTracker.Application/CurrencyTracker.Application.csproj src/CurrencyTracker.Application/
COPY src/CurrencyTracker.Infrastructure/CurrencyTracker.Infrastructure.csproj src/CurrencyTracker.Infrastructure/
COPY src/CurrencyTracker.ServiceDefaults/CurrencyTracker.ServiceDefaults.csproj src/CurrencyTracker.ServiceDefaults/
COPY src/CurrencyTracker.Api/CurrencyTracker.Api.csproj                src/CurrencyTracker.Api/
RUN dotnet restore src/CurrencyTracker.Api/CurrencyTracker.Api.csproj

# Source layer + publish. --no-restore keeps the layers honest: if the
# restore layer didn't cover a dependency, the build fails loudly here.
COPY src/ src/
RUN dotnet publish src/CurrencyTracker.Api/CurrencyTracker.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
EXPOSE 8080

# Chiseled already defaults to the non-root `app` user; state it by UID
# anyway so runAsNonRoot-style admission checks can verify without
# starting the container (a name in USER is unverifiable metadata).
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CurrencyTracker.Api.dll"]