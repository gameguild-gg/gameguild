# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy source code for the API solution
COPY . .

# Restore dependencies for the actual API project
RUN dotnet restore "Source/GameGuild.API/GameGuild.API.csproj"

# Publish the application from restored assets without a redundant intermediate build
RUN dotnet publish "Source/GameGuild.API/GameGuild.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Stage 2: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Install curl for health checks and create non-root user
RUN apk add --no-cache curl \
    && addgroup -g 1001 -S appuser \
    && adduser -S appuser -G appuser -u 1001

WORKDIR /app

# Prepare a writable location for persisted Data Protection keys before any
# named volume is mounted at runtime.
RUN mkdir -p /app/.aspnet/DataProtection-Keys \
    && chown -R appuser:appuser /app

# Copy published application with proper ownership
COPY --from=build --chown=appuser:appuser /app/publish .

# Switch to non-root user
USER appuser

# Expose the same internal port Coolify uses for routed application services.
EXPOSE 3000

# Set environment variables
ENV ASPNETCORE_HTTP_PORTS=3000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DATAPROTECTION_KEYS_PATH=/app/.aspnet/DataProtection-Keys

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://127.0.0.1:3000/live || exit 1

# Entry point
ENTRYPOINT ["dotnet", "GameGuild.API.dll"]
