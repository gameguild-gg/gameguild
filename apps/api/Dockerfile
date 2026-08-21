# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Devtron and the repository compose files build this image from the repository
# root, so keep every COPY source relative to that canonical build context.
# Copy project definition files first for dependency layer caching.
COPY apps/api/GameGuild.sln apps/api/Directory.Build.props apps/api/Directory.Packages.props apps/api/global.json ./

# Copy source projects (tests excluded - not needed for production image)
COPY apps/api/Source/ Source/

# Restore dependencies via the main project (all module projects are transitive)
RUN dotnet restore "Source/GameGuild.API/GameGuild.API.csproj" --disable-parallel

# Publish the application
RUN dotnet publish "Source/GameGuild.API/GameGuild.API.csproj" -m:1 -o /app/publish \
    -c Release \
    /p:UseAppHost=false \
    --no-restore

# Stage 2: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

WORKDIR /app

# Install health-check, PostgreSQL authentication, and localization dependencies.
RUN apk add --no-cache curl krb5-libs icu-libs icu-data-full openssl3 libssl3 \
    && addgroup -g 1001 -S appuser \
    && adduser -S appuser -G appuser -u 1001 \
    && chown -R appuser:appuser /app

# Copy published application with correct ownership
COPY --from=build --chown=appuser:appuser /app/publish .

USER appuser

EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://127.0.0.1:8080/live || exit 1

ENTRYPOINT ["dotnet", "GameGuild.API.dll"]
