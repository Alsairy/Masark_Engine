# Masark Engine - Developer Setup Guide

## 🚀 Quick Start for Developers

This guide will help you get the Masark Engine .NET 8.0 project up and running on your development machine.

## ✅ Prerequisites

### Required Software
- **.NET 8.0 SDK** (version 8.0.100 or later)
- **Visual Studio 2022** (17.8 or later) OR **Visual Studio Code** with C# extension
- **SQL Server** (LocalDB, Express, or full version)
- **Redis** (for caching - optional for development)
- **Node.js 20+** (for admin frontend)
- **Git** for version control

### Download .NET 8.0 SDK
If you don't have .NET 8.0 installed:
1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0
2. Download and install the latest .NET 8.0 SDK
3. Verify installation: `dotnet --version` (should show 8.0.x)

## 🔧 Project Setup

### 1. Clone the Repository
```bash
git clone <repository-url>
cd Masark_Engine
```

### 2. Verify .NET Version
```bash
dotnet --version
# Should output: 8.0.x (where x is the patch version)

dotnet --list-sdks
# Should show: 8.0.x [path]
```

### 3. Restore NuGet Packages
```bash
# From the root directory
dotnet restore

# This will restore packages for all projects:
# - Masark.API
# - Masark.Application  
# - Masark.Domain
# - Masark.Infrastructure
# - Masark.Tests.Unit
# - Masark.Tests.Integration
```

### 4. Build the Solution
```bash
# Build all projects
dotnet build

# Or build in Release mode
dotnet build -c Release
```

### 5. Database Setup

#### Option A: SQLite (Recommended for Development)
The project is configured to use SQLite by default in development mode.

```bash
# Navigate to the API project
cd Masark.API

# Create/update the database
dotnet ef database update --project ../Masark.Infrastructure

# Seed the database with initial data
dotnet run --seed-data
```

#### Option B: SQL Server
If you prefer SQL Server, update `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MasarkEngine;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

Then run:
```bash
dotnet ef database update --project ../Masark.Infrastructure
```

### 6. Run the Application
```bash
# From Masark.API directory
cd Masark.API
dotnet run

# Or use watch mode for development
dotnet watch run
```

The API will be available at:
- **HTTPS**: https://localhost:7001
- **HTTP**: http://localhost:5001
- **Swagger UI**: https://localhost:7001/swagger

## 🎨 Admin Frontend Setup

### 1. Navigate to Frontend Directory
```bash
cd admin-frontend
```

### 2. Install Dependencies
```bash
# Using npm
npm install

# Or using yarn
yarn install

# Or using pnpm (recommended)
pnpm install
```

### 3. Start Development Server
```bash
# Using npm
npm run dev

# Or using yarn
yarn dev

# Or using pnpm
pnpm run dev
```

The frontend will be available at: http://localhost:5173

## 🐳 Docker Setup (Alternative)

If you prefer using Docker:

### 1. Build and Run with Docker Compose
```bash
# Build and start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### 2. Individual Docker Build
```bash
# Build the API image
docker build -t masark-engine:latest .

# Run the container
docker run -p 5001:8080 -p 7001:8081 masark-engine:latest
```

## 🧪 Running Tests

### Unit Tests
```bash
# Run all unit tests
dotnet test Masark.Tests.Unit

# Run with coverage
dotnet test Masark.Tests.Unit --collect:"XPlat Code Coverage"
```

### Integration Tests
```bash
# Run integration tests
dotnet test Masark.Tests.Integration

# Run all tests
dotnet test
```

## 🔍 Common Issues & Solutions

### Issue 1: "SDK not found" Error
**Problem**: .NET 8.0 SDK not installed or not in PATH
**Solution**: 
1. Download and install .NET 8.0 SDK from Microsoft
2. Restart your terminal/IDE
3. Verify with `dotnet --version`

### Issue 2: Package Restore Fails
**Problem**: NuGet package restore errors
**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore --force
```

### Issue 3: Database Connection Issues
**Problem**: Cannot connect to database
**Solution**:
1. Check connection string in `appsettings.Development.json`
2. Ensure SQL Server/LocalDB is running
3. Try SQLite option for simpler setup

### Issue 4: Port Already in Use
**Problem**: Ports 5001/7001 are already in use
**Solution**:
1. Change ports in `launchSettings.json`
2. Or kill processes using those ports
3. Use `dotnet run --urls "https://localhost:7002;http://localhost:5002"`

### Issue 5: Redis Connection Errors
**Problem**: Redis connection failures
**Solution**:
1. Redis is optional for development
2. Comment out Redis configuration in `Program.cs`
3. Or install Redis locally/use Docker

## 🛠️ Development Tools

### Recommended Visual Studio Extensions
- **C# Dev Kit** (for VS Code)
- **REST Client** (for API testing)
- **GitLens** (for Git integration)
- **Thunder Client** (for API testing)

### Recommended Visual Studio Features
- **Live Unit Testing**
- **Code Coverage**
- **IntelliSense**
- **Debugging Tools**

## 📊 Project Structure

```
Masark_Engine/
├── Masark.API/              # Web API project
├── Masark.Application/      # Application layer (CQRS, Services)
├── Masark.Domain/          # Domain entities and business logic
├── Masark.Infrastructure/   # Data access and external services
├── Masark.Tests.Unit/      # Unit tests
├── Masark.Tests.Integration/ # Integration tests
├── admin-frontend/         # React TypeScript admin panel
├── deploy/                 # Deployment configurations
└── .github/               # CI/CD workflows
```

## 🔐 Environment Configuration

### Development Environment Variables
Create a `.env` file in the root directory:

```env
ASPNETCORE_ENVIRONMENT=Development
DEPLOYMENT_MODE=STANDARD
JWT_SECRET_KEY=your-super-secret-jwt-key-here
DATABASE_CONNECTION=Data Source=masark.db
REDIS_CONNECTION=localhost:6379
```

### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=masark.db"
  },
  "JwtSettings": {
    "SecretKey": "your-development-secret-key",
    "Issuer": "MasarkEngine",
    "Audience": "MasarkUsers",
    "ExpirationMinutes": 1440
  }
}
```

## 🚀 Getting Started Checklist

- [ ] Install .NET 8.0 SDK
- [ ] Clone the repository
- [ ] Run `dotnet --version` to verify .NET 8.0
- [ ] Run `dotnet restore` to restore packages
- [ ] Run `dotnet build` to build the solution
- [ ] Set up database (SQLite recommended for dev)
- [ ] Run `dotnet ef database update --project Masark.Infrastructure`
- [ ] Start the API with `dotnet run` from Masark.API directory
- [ ] Test API at https://localhost:7001/swagger
- [ ] Set up admin frontend with `pnpm install && pnpm run dev`
- [ ] Test frontend at http://localhost:5173

## 📞 Support

If you encounter issues:

1. **Check this guide** for common solutions
2. **Verify .NET 8.0** is properly installed
3. **Check logs** in the console output
4. **Review connection strings** in configuration files
5. **Try SQLite** instead of SQL Server for simpler setup

## 🎯 Next Steps

Once you have the project running:

1. **Explore the API** using Swagger UI
2. **Run the test suite** to ensure everything works
3. **Check the admin frontend** functionality
4. **Review the code structure** and architecture
5. **Start developing** new features

---

**Happy Coding! 🚀**

*This project uses .NET 8.0 with Clean Architecture, CQRS pattern, and modern development practices.*
