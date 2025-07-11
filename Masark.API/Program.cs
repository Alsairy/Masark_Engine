using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Masark.Application.Handlers.Commands;
using Masark.Application.Services;
using Masark.Application.Interfaces;
using Masark.Infrastructure.Identity;
using Masark.Infrastructure.Services;
using Masark.Infrastructure.Middleware;
using Masark.Infrastructure.Authorization;
// using Masark.AssessmentModule.Extensions;
// using Masark.CareerModule.Extensions;
// using Masark.ReportingModule.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Masark.Infrastructure.Repositories;
using Masark.Infrastructure.Options;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
// using HealthChecks.UI.Client;
using Masark.Infrastructure.HealthChecks;

public partial class Program 
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder);
        var app = builder.Build();
        ConfigureApp(app);
        RunApp(app);
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.Console()
            .WriteTo.File("logs/masark-.txt", rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext()
            .CreateLogger();

        builder.Host.UseSerilog();

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Configure database based on environment with connection pooling optimization
        if (builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));
        }
        else
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=masark.db";
            
            builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
            {
                options.UseSqlite(connectionString, sqliteOptions =>
                {
                    sqliteOptions.CommandTimeout(30);
                });
                options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
                options.EnableDetailedErrors(builder.Environment.IsDevelopment());
                options.EnableServiceProviderCaching();
                options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.SensitiveDataLoggingEnabledWarning));
            }, poolSize: 64); // Optimized pool size for SQLite concurrent access
        }

        // Configure Redis distributed caching
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "MasarkEngine";
        });

        // Configure comprehensive health checks
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database", tags: new[] { "ready", "db" })
            .AddRedis(redisConnectionString, "redis", tags: new[] { "ready", "cache" })
            .AddCheck<ApplicationInsightsHealthCheck>("application_insights", tags: new[] { "ready", "monitoring" })
            .AddCheck<JwtTokenHealthCheck>("jwt_token_service", tags: new[] { "ready", "auth" })
            .AddCheck<CachingServiceHealthCheck>("caching_service", tags: new[] { "ready", "cache" })
            .AddCheck<LocalizationServiceHealthCheck>("localization_service", tags: new[] { "ready", "localization" })
            .AddCheck<SecurityMonitoringHealthCheck>("security_monitoring", tags: new[] { "ready", "security" })
            .AddCheck<PerformanceMonitoringHealthCheck>("performance_monitoring", tags: new[] { "ready", "performance" })
            .AddCheck<MemoryHealthCheck>("memory_usage", tags: new[] { "ready", "system" })
            .AddCheck<DiskSpaceHealthCheck>("disk_space", tags: new[] { "ready", "system" })
            .AddCheck<CpuUsageHealthCheck>("cpu_usage", tags: new[] { "ready", "system" });

        builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "MasarkEngine-SuperSecure-JWT-Secret-Key-2024-Production-Ready";
var key = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MasarkEngine",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MasarkEngine",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("USER", "ADMIN"));
    
    options.AddPolicy("ManageUsers", policy => 
        policy.RequireRole("ADMIN")
              .RequireClaim("permission", "user.manage"));
    
    options.AddPolicy("ManageSystem", policy => 
        policy.RequireRole("ADMIN")
              .RequireClaim("permission", "system.manage"));
    
    options.AddPolicy("ViewReports", policy => 
        policy.RequireRole("ADMIN", "MANAGER")
              .RequireClaim("permission", "reports.view"));
    
    options.AddPolicy("ManageReports", policy => 
        policy.RequireRole("ADMIN")
              .RequireClaim("permission", "reports.manage"));
    
    options.AddPolicy("ManageAssessments", policy => 
        policy.RequireRole("ADMIN", "ASSESSMENT_MANAGER")
              .RequireClaim("permission", "assessment.manage"));
    
    options.AddPolicy("ViewAssessmentResults", policy => 
        policy.RequireRole("ADMIN", "MANAGER", "COUNSELOR")
              .RequireClaim("permission", "assessment.results.view"));
    
    options.AddPolicy("TakeAssessment", policy => 
        policy.RequireRole("USER", "ADMIN")
              .RequireClaim("permission", "assessment.take"));
    
    options.AddPolicy("ManageCareers", policy => 
        policy.RequireRole("ADMIN", "CAREER_MANAGER")
              .RequireClaim("permission", "career.manage"));
    
    options.AddPolicy("ViewCareerData", policy => 
        policy.RequireRole("USER", "ADMIN", "MANAGER", "COUNSELOR")
              .RequireClaim("permission", "career.view"));
    
    options.AddPolicy("ManageApiKeys", policy => 
        policy.RequireRole("ADMIN")
              .RequireClaim("permission", "api.keys.manage"));
    
    options.AddPolicy("ViewApiUsage", policy => 
        policy.RequireRole("ADMIN", "MANAGER")
              .RequireClaim("permission", "api.usage.view"));
    
    options.AddPolicy("AccessTenantData", policy => 
        policy.RequireAssertion(context =>
        {
            var tenantClaim = context.User.FindFirst("tenant_id");
            var requestTenant = context.Resource as string;
            return tenantClaim != null && (requestTenant == null || tenantClaim.Value == requestTenant);
        }));
    
    options.AddPolicy("RequireMFA", policy => 
        policy.RequireRole("ADMIN")
              .RequireClaim("mfa_verified", "true"));
    
    options.AddPolicy("BusinessHoursOnly", policy => 
        policy.RequireAssertion(context =>
        {
            var now = DateTime.UtcNow;
            var businessStart = new TimeSpan(6, 0, 0); // 6 AM UTC
            var businessEnd = new TimeSpan(22, 0, 0);   // 10 PM UTC
            return now.TimeOfDay >= businessStart && now.TimeOfDay <= businessEnd;
        }));
    
    options.AddPolicy("TrustedNetworkOnly", policy => 
        policy.RequireAssertion(context =>
        {
            var httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;
            if (httpContext == null) return false;
            
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
            var trustedNetworks = new[] { "127.0.0.1", "::1" }; // Add your trusted IPs
            return trustedNetworks.Contains(clientIp);
        }));
});

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(StartAssessmentSessionHandler).Assembly));

builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

builder.Services.AddScoped<IPersonalityScoringService, PersonalityScoringService>();
builder.Services.AddScoped<IEnhancedPersonalityScoringService, EnhancedPersonalityScoringService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>(); // Stateless, can be singleton
builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>(); // Singleton for DbContextPool compatibility
builder.Services.AddScoped<IPersonalityRepository, PersonalityRepository>(); // Scoped for DB context
builder.Services.AddScoped<ICareerMatchingService, CareerMatchingService>();
builder.Services.AddSingleton<ICachingService, CachingService>(); // Singleton for better cache performance
builder.Services.AddSingleton<ILocalizationService, LocalizationService>(); // Stateless, can be singleton

builder.Services.AddSingleton<PerformanceMonitoringService>();
builder.Services.AddHostedService<PerformanceMonitoringService>(provider => 
    provider.GetRequiredService<PerformanceMonitoringService>());
builder.Services.AddSingleton<IPerformanceMonitoringService>(provider => 
    provider.GetRequiredService<PerformanceMonitoringService>());

builder.Services.AddScoped<IReportGenerationService, ReportGenerationService>();
builder.Services.AddScoped<IAssessmentStateMachineService, AssessmentStateMachineService>();
builder.Services.AddSingleton<ISecurityMonitoringService, SecurityMonitoringService>(); // Singleton for monitoring
builder.Services.AddSingleton<ICdnService, CdnService>(); // Stateless, can be singleton
builder.Services.AddScoped<DatabaseSeeder>(); // Scoped for DB operations

// builder.Services.AddAssessmentModule();
// builder.Services.AddCareerModule();
// builder.Services.AddReportingModule();

// builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>(); // Stateless
// builder.Services.AddSingleton<IAuthorizationHandler, TenantAccessAuthorizationHandler>(); // Stateless
// builder.Services.AddSingleton<IAuthorizationHandler, MfaAuthorizationHandler>(); // Stateless

builder.Services.AddEncryptionServices(builder.Configuration);

if (builder.Environment.IsProduction())
{
    builder.Services.AddScoped<IKeyVaultService, AzureKeyVaultService>();
}
else
{
    builder.Services.AddScoped<IKeyVaultService, LocalKeyVaultService>();
}

// builder.Services.AddFluentValidationAutoValidation();
// builder.Services.AddValidatorsFromAssemblyContaining<Masark.Application.Validators.Assessment.StartAssessmentSessionCommandValidator>();

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true;
    options.EnableAuthenticationTrackingJavaScript = false;
    options.EnableDependencyTrackingTelemetryModule = true;
    options.EnablePerformanceCounterCollectionModule = true;
    options.EnableRequestTrackingTelemetryModule = true;
    options.EnableEventCounterCollectionModule = true;
});

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ApiRateLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });
    
    options.AddFixedWindowLimiter("AssessmentRateLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
    
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add service validation to ensure proper DI configuration
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CachingOptions>()
    .Bind(builder.Configuration.GetSection("Caching"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IServiceValidator, ServiceValidator>();

        // Register custom health checks
        builder.Services.AddSingleton<ApplicationInsightsHealthCheck>();
        builder.Services.AddSingleton<JwtTokenHealthCheck>();
        builder.Services.AddSingleton<CachingServiceHealthCheck>();
        builder.Services.AddSingleton<LocalizationServiceHealthCheck>();
        builder.Services.AddSingleton<SecurityMonitoringHealthCheck>();
        builder.Services.AddSingleton<PerformanceMonitoringHealthCheck>();
        builder.Services.AddSingleton<MemoryHealthCheck>();
        builder.Services.AddSingleton<DiskSpaceHealthCheck>();
        builder.Services.AddSingleton<CpuUsageHealthCheck>();

// Configure ASP.NET Core Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Configure supported cultures for localization
var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("en-US"),
    new CultureInfo("ar"),
    new CultureInfo("ar-SA"),
    new CultureInfo("es"),
    new CultureInfo("es-ES"),
    new CultureInfo("zh"),
    new CultureInfo("zh-CN")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en", "en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    
    // Configure culture providers in order of priority
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
    
    // Configure culture detection options
    options.ApplyCurrentCultureToResponseHeaders = true;
    options.FallBackToParentCultures = true;
    options.FallBackToParentUICultures = true;
});

    }

    private static void ConfigureApp(WebApplication app)
    {

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseRequestLocalization();
app.UseRtlSupport();

app.UseResponseCaching();

// Configure static files with CDN-optimized headers
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 365; // 1 year
        ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={durationInSeconds}");
        ctx.Context.Response.Headers.Append("Expires", DateTime.UtcNow.AddYears(1).ToString("R"));
        
        ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        ctx.Context.Response.Headers.Append("X-Frame-Options", "DENY");
        
        var etag = $"\"{ctx.Context.Request.Path.Value?.GetHashCode():X}\"";
        ctx.Context.Response.Headers.Append("ETag", etag);
        
        ctx.Context.Response.Headers.Append("Vary", "Accept-Encoding");
    }
});
app.UseRateLimiter();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthentication();
app.UseZeroTrust();
app.UseXssProtection();
app.UseSqlInjectionPrevention();
app.UseAuthorization();

// Configure comprehensive health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration,
                description = entry.Value.Description,
                data = entry.Value.Data,
                tags = entry.Value.Tags,
                exception = entry.Value.Exception?.Message
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            readyChecks = report.Entries.Where(e => e.Value.Tags.Contains("ready")).Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration,
                description = entry.Value.Description,
                tags = entry.Value.Tags
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            message = "Application is running"
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }
});

app.MapHealthChecks("/health/db", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            databaseChecks = report.Entries.Where(e => e.Value.Tags.Contains("db")).Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration,
                description = entry.Value.Description
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }
});

app.MapHealthChecks("/health/cache", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("cache"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            cacheChecks = report.Entries.Where(e => e.Value.Tags.Contains("cache")).Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration,
                description = entry.Value.Description,
                data = entry.Value.Data
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }
});

app.MapHealthChecks("/health/system", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("system"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            systemChecks = report.Entries.Where(e => e.Value.Tags.Contains("system")).Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration,
                description = entry.Value.Description,
                data = entry.Value.Data
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }
});

app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
        }

        using (var scope = app.Services.CreateScope())
        {
            var serviceValidator = scope.ServiceProvider.GetRequiredService<IServiceValidator>();
            var validationResult = serviceValidator.ValidateServicesAsync(scope.ServiceProvider).Result;
            
            if (!validationResult)
            {
                Log.Fatal("Service validation failed during startup. Application will not start.");
                throw new InvalidOperationException("Service validation failed during startup");
            }
            
            Log.Information("Service validation completed successfully");
        }

        var skipSeeding = app.Configuration.GetValue<bool>("SkipDatabaseSeeding", false);
        if (!skipSeeding)
        {
            using (var scope = app.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                seeder.SeedAsync().Wait();
            }
        }
    }

    private static void RunApp(WebApplication app)
    {

        try
        {
            Log.Information("Starting Masark Engine API");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
