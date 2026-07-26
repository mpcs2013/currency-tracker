# src/CurrencyTracker.Worker/Dockerfile
#
# Build context is the REPO ROOT:
#   docker build -f src/CurrencyTracker.Worker/Dockerfile -t currencytracker-worker:dev .
#
# Base image is aspnet (NOT runtime) even though the Worker serves no HTTP:
# ServiceDefaults carries <FrameworkReference Include="Microsoft.AspNetCore.App" />,
# and a framework reference resolves from the base image at run time.
# No EXPOSE and no port: the Worker Container App has ingress disabled (14.23).
# No HEALTHCHECK for the same reasons as the Api image (14.26).

# Pinned to an explicit patch, not the floating 10.0-noble tag — same reasoning
# as the Api image (14.26): AnalysisLevel=latest-recommended binds the analyzer
# set to the SDK, and TreatWarningsAsErrors is on, so an SDK refresh that adds
# one recommended rule would break this image on a day when no code changed.
FROM mcr.microsoft.com/dotnet/sdk:10.0.302-noble AS build
WORKDIR /src

# .editorconfig rides along for the *compile*, not the restore:
# AnalysisMode=AllEnabledByDefault turns on every CA rule, and this file is the
# only place CA1000/CA2007/IDE0058/CA1861 are set to none. Omit it and the
# Release build dies on Domain's Result.cs before it reaches anything else.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/CurrencyTracker.Domain/CurrencyTracker.Domain.csproj          src/CurrencyTracker.Domain/
COPY src/CurrencyTracker.Application/CurrencyTracker.Application.csproj src/CurrencyTracker.Application/
COPY src/CurrencyTracker.Infrastructure/CurrencyTracker.Infrastructure.csproj src/CurrencyTracker.Infrastructure/
COPY src/CurrencyTracker.ServiceDefaults/CurrencyTracker.ServiceDefaults.csproj src/CurrencyTracker.ServiceDefaults/
COPY src/CurrencyTracker.Worker/CurrencyTracker.Worker.csproj          src/CurrencyTracker.Worker/
RUN dotnet restore src/CurrencyTracker.Worker/CurrencyTracker.Worker.csproj

COPY src/ src/
RUN dotnet publish src/CurrencyTracker.Worker/CurrencyTracker.Worker.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CurrencyTracker.Worker.dll"]