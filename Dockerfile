# Use the official .NET 8.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG ASPNETCORE_ENVIRONMENT=Production
ARG DEPLOYMENT_MODE=STANDARD
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Use the official .NET 8.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG ASPNETCORE_ENVIRONMENT=Production
ARG DEPLOYMENT_MODE=STANDARD
WORKDIR /src

# Copy solution file first for better caching
COPY ["Masark.sln", "./"]

# Copy project files for dependency restoration (better layer caching)
COPY ["Masark.API/Masark.API.csproj", "Masark.API/"]
COPY ["Masark.Application/Masark.Application.csproj", "Masark.Application/"]
COPY ["Masark.Domain/Masark.Domain.csproj", "Masark.Domain/"]
COPY ["Masark.Infrastructure/Masark.Infrastructure.csproj", "Masark.Infrastructure/"]
COPY ["Masark.Tests.Unit/Masark.Tests.Unit.csproj", "Masark.Tests.Unit/"]
COPY ["Masark.Tests.Integration/Masark.Tests.Integration.csproj", "Masark.Tests.Integration/"]

# Restore dependencies (this layer will be cached if project files don't change)
RUN dotnet restore "Masark.API/Masark.API.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/Masark.API"
RUN dotnet build "Masark.API.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG ASPNETCORE_ENVIRONMENT=Production
ARG DEPLOYMENT_MODE=STANDARD
RUN dotnet publish "Masark.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-build /p:UseAppHost=false

# Final stage - runtime image
FROM base AS final
ARG BUILD_VERSION=latest
ARG BUILD_DATE
ARG VCS_REF
ARG ASPNETCORE_ENVIRONMENT=Production
ARG DEPLOYMENT_MODE=STANDARD

# Add metadata labels
LABEL maintainer="Masark Engine Team" \
      version="${BUILD_VERSION}" \
      build-date="${BUILD_DATE}" \
      vcs-ref="${VCS_REF}" \
      description="Masark Personality-Career Matching Engine" \
      org.opencontainers.image.title="Masark Engine" \
      org.opencontainers.image.description="World-class MBTI personality assessment and career matching platform" \
      org.opencontainers.image.version="${BUILD_VERSION}" \
      org.opencontainers.image.created="${BUILD_DATE}" \
      org.opencontainers.image.revision="${VCS_REF}" \
      org.opencontainers.image.vendor="Masark" \
      org.opencontainers.image.source="https://github.com/Alsairy/Masark_Engine"

WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' --shell /bin/bash --home /home/masark masark && \
    chown -R masark:masark /app

# Copy published application with proper ownership
COPY --from=publish --chown=masark:masark /app/publish .

# Switch to non-root user
USER masark

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
ENV DEPLOYMENT_MODE=${DEPLOYMENT_MODE}
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# Health check with configurable timeout for different environments
HEALTHCHECK --interval=30s --timeout=15s --start-period=90s --retries=5 \
  CMD curl -f http://localhost:8080/health || curl -f http://localhost:8080/api/system/health || exit 1

ENTRYPOINT ["dotnet", "Masark.API.dll"]
