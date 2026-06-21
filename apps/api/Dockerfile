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

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DATAPROTECTION_KEYS_PATH=/app/.aspnet/DataProtection-Keys

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://127.0.0.1:8080/ready || exit 1

# Entry point
ENTRYPOINT ["dotnet", "GameGuild.API.dll"]

#
#FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
#USER $APP_UID
#WORKDIR /app
#EXPOSE 8080
#EXPOSE 8081
#
#FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["cms.csproj", "./"]
#RUN dotnet restore "cms.csproj"
#COPY . .
#WORKDIR "/src/"
#RUN dotnet build "./cms.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./cms.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "GameGuild.CMS.dll"]
