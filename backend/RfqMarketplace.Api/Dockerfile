# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Restore first, on its own layer: dependencies change far less often than source,
# so an edit to a .cs file does not invalidate the cached restore.
COPY RfqMarketplace.Api/RfqMarketplace.Api.csproj RfqMarketplace.Api/
RUN dotnet restore RfqMarketplace.Api/RfqMarketplace.Api.csproj

COPY RfqMarketplace.Api/ RfqMarketplace.Api/
RUN dotnet publish RfqMarketplace.Api/RfqMarketplace.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------------------------------------------------------------------------
# Runtime
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# curl is only here for the container healthcheck below.
RUN apk add --no-cache curl

# Run as a non-root user: a compromised process should not own the container.
# The .NET 8 images already ship an "app" user, so only create one if that ever changes.
RUN id -u app >/dev/null 2>&1 || (addgroup -S app && adduser -S -G app app)

COPY --from=build --chown=app:app /app/publish .

USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -fsS http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "RfqMarketplace.Api.dll"]
