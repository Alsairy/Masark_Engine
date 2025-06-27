# Use the official .NET 8.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official .NET 8.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Masark.API/Masark.API.csproj", "Masark.API/"]
COPY ["Masark.Application/Masark.Application.csproj", "Masark.Application/"]
COPY ["Masark.Domain/Masark.Domain.csproj", "Masark.Domain/"]
COPY ["Masark.Infrastructure/Masark.Infrastructure.csproj", "Masark.Infrastructure/"]

RUN dotnet restore "Masark.API/Masark.API.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/Masark.API"
RUN dotnet build "Masark.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Masark.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage - runtime image
FROM base AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' --shell /bin/bash --home /home/masark masark
USER masark

# Copy published application
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Masark.API.dll"]
