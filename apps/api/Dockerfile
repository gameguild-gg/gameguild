# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file first for dependency restoration
COPY ["GameGuild.API.csproj", "./"]

# Restore dependencies
RUN dotnet restore "GameGuild.API.csproj"

# Copy source code
COPY . .

# Build the application
RUN dotnet build "GameGuild.API.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "GameGuild.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# Install curl for health checks and create non-root user
RUN apk add --no-cache curl \
    && addgroup -g 1001 -S appuser \
    && adduser -S appuser -G appuser -u 1001

WORKDIR /app

# Copy published application with proper ownership
COPY --from=build --chown=appuser:appuser /app/publish .

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "GameGuild.API.dll"]
