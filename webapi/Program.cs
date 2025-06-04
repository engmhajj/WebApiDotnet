using System.Text;
using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;
using webapi.Authority;
using webapi.Data;
using webapi.Data.Seeding;
using webapi.Filters;
using webapi.Filters.OperationFilter;
using webapi.Middlewares;
using webapi.Models;
using webapi.Services;

// For production environments, it's recommended to avoid storing sensitive information like client secrets directly in the appsettings files. Instead, consider using:
//
// Environment Variables:
// Set sensitive values as environment variables and access them in your application.
//
// Azure Key Vault:
// If deploying to Azure, use Azure Key Vault to securely store and access secrets.
//
// Docker Secrets:
// When deploying with Docker, use Docker secrets to manage sensitive data.

var builder = WebApplication.CreateBuilder(args);

// (*^▽^*)═══════════════════════════════════════════════(^▽^*)
// ❖          // Load configuration files          ❖
// (^▽^*)═══════════════════════════════════════════════(*^▽^*)
builder
    .Configuration.SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true
    )
    .AddEnvironmentVariables();

// (*^▽^*)═══════════════════════════════════════════════════════════════════════════════════════(^▽^*)
// ❖          //NOTE: -------------------- Add versioning -------------------- //          ❖
// (^▽^*)═══════════════════════════════════════════════════════════════════════════════════════(*^▽^*)
//
// builder.Services.AddApiVersioning(options =>
// {
//     options.AssumeDefaultVersionWhenUnspecified = true;
//     options.DefaultApiVersion = new ApiVersion(1, 0);
//     options.ReportApiVersions = true;
//     // options.ApiVersionReader = new HeaderApiVersionReader("X-API-Versioning");
//
//     // You can choose how to specify the version
//     options.ApiVersionReader = ApiVersionReader.Combine(
//         new HeaderApiVersionReader("X-API-Versioning"),
//         new QueryStringApiVersionReader("api-version"),
//         new HeaderApiVersionReader("X-Version"),
//         new MediaTypeApiVersionReader("ver")

//     );
// });

// (*^▽^*)═══════════════════════════════════════════════════════════════════════════════════════════════(^▽^*)
// ❖          // // -------------------- Configuration & Services -------------------- //          ❖
// (^▽^*)═══════════════════════════════════════════════════════════════════════════════════════════════(*^▽^*)

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ShirtStoreManagementSQLite"))
);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
builder.Services.Configure<AppCredential>(builder.Configuration.GetSection("AppCredential"));

builder.Services.AddScoped<IAppRepository, AppRepository>();
builder.Services.AddSingleton<IFallbackAppProvider, FallbackAppProvider>();

builder.Services.AddScoped<IAuthenticator, Authenticator>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<UserRepository>();

builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<JwtService>();

builder.Services.AddScoped<DbSeeder>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHttpContextAccessor();

builder.Host.UseSerilog(
    (context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .WriteTo.Console()
            .WriteTo.File(
                "logs/webapi.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7
            );
    }
);

builder
    .Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("Keys"))
    .SetApplicationName("MVCWebApp");

builder
    .Services.AddAuthentication("Bearer")
    .AddJwtBearer(
        "Bearer",
        options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters =
                new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = builder.Configuration["JwtOptions:Issuer"],
                    ValidAudience = builder.Configuration["JwtOptions:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["JwtOptions:SecretKey"]
                                ?? throw new InvalidOperationException("Missing SecretKey")
                        )
                    ),
                };
        }
    );

builder.Services.AddControllers();
builder.Services.AddAuthorization();

// (*^▽^*)══════════════════════════════════════════════════════════(^▽^*)
// ❖          //Adding Swagger for API documentation          ❖
// (^▽^*)══════════════════════════════════════════════════════════(*^▽^*)

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });
    c.OperationFilter<AuthorizationHeaderOperationFilter>();

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Scheme = "Bearer",
            BearerFormat = "JWT",
            Type = SecuritySchemeType.Http,
            In = ParameterLocation.Header,
            Description =
                "Enter 'Bearer' [space] and then your token below.\nExample: \"Bearer abcdef12345\"",
        }
    );

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );

    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });
    // Optionally, add health check endpoint to Swagger UI
    c.DocumentFilter<HealthCheckDocumentFilter>();
});

// (*^▽^*)═══════════════════════════════════════════════════════════════════════════(^▽^*)
// ❖          //1- Adding caching service for frequently accessed data.          ❖
// (^▽^*)═══════════════════════════════════════════════════════════════════════════(*^▽^*)

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "MyApp_";
});

// (*^▽^*)════════════════════════════════════════════════(^▽^*)
// ❖          // Added for Testing purpose          ❖
// (^▽^*)════════════════════════════════════════════════(*^▽^*)
// builder.Services.AddScoped<IBackgroundJobWrapper, BackgroundJobWrapper>();
// builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

// (*^▽^*)══════════════════════════════════════════════════════════════(^▽^*)
// ❖          //2- Adding Health check and monitor service          ❖
// (^▽^*)══════════════════════════════════════════════════════════════(*^▽^*)

builder
    .Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("Database")
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
        name: "Redis"
    );

// (*^▽^*)════════════════════════════════════════════════════════(^▽^*)
// ❖          //3- Adding Rate Limiting / Throttling          ❖
// (^▽^*)════════════════════════════════════════════════════════(*^▽^*)

builder.Services.AddMemoryCache();

builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// (*^▽^*)══════════════════════════════════════════════════════════════(^▽^*)
// ❖     ✅ Best Queuing Solution: RabbitMQ with MassTransit     ❖
// (^▽^*)══════════════════════════════════════════════════════════════(*^▽^*)

// builder.Services.AddMassTransit(x =>
// {
//     x.AddConsumer<PaymentConsumer>();
//
//     x.UsingRabbitMq(
//         (context, cfg) =>
//         {
//             cfg.Host(
//                 "rabbitmq",
//                 h =>
//                 {
//                     h.Username("guest");
//                     h.Password("guest");
//                 }
//             );
//
//             cfg.ReceiveEndpoint(
//                 "payment-queue",
//                 e =>
//                 {
//                     e.ConfigureConsumer<PaymentConsumer>(context);
//                 }
//             );
//         }
//     );
// });

// (*^▽^*)═══════════════════════════════════════════════════════════════(^▽^*)
// ❖          //4. Background Jobs / Queues with Hangfire - Adding queuing serice, benefit queue
// persist, bad queue need to call db which is slower          ❖
// (^▽^*)═══════════════════════════════════════════════════════════════(*^▽^*)
// 🆚 Why Not Hangfire?
// Hangfire stores jobs in DB, which can be slower for high-volume, critical tasks.
//
// It’s not naturally distributed—multiple instances can fight over jobs.
//
// It’s great for simple background jobs, but not robust for mission-critical queues like payments in a growing system.
// builder.Services.AddHangfire(config =>
//     config
//         .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
//         .UseSimpleAssemblyNameTypeSerializer()
//         .UseRecommendedSerializerSettings()
//         .UseSQLiteStorage("Data Source=hangfire.db;")
// );
//
// builder.Services.AddHangfireServer();

// (*^▽^*)═══════════════════════════════════════════════════════════════════════(^▽^*)
// ❖     Another queuing service designed by me we can remove hangfire but use in memory on restart
// all data will be lost ❖
// (^▽^*)═══════════════════════════════════════════════════════════════════════(*^▽^*)
//
// builder.Services.AddSingleton<ITaskQueue, BackgroundTaskQueue>();
// builder.Services.AddHostedService<QueuedHostedService>();
//
// (*^▽^*)═════════════════════════════════════════════════════════════════════════════(^▽^*)
// ❖          // -------------------- Build App -------------------- //          ❖
// (^▽^*)═════════════════════════════════════════════════════════════════════════════(*^▽^*)

var app = builder.Build();

app.MapHealthChecks("/health");
app.UseRouting();

// app.UseEndpoints(endpoints =>
// {
//     endpoints.MapHangfireDashboard("/hangfire");
//     endpoints.MapControllers();
// });
app.UseHttpMetrics(); // Collect HTTP metrics for Prometheus

app.UseIpRateLimiting();

// app.UseHangfireDashboard("/hangfire"); // optional dashboard UI at /hangfire

app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/health");
    endpoints.MapMetrics(); // Prometheus metrics endpoint
});

// (*^▽^*)═══════════════════════════════════════════════════════════════════════════════════════════(^▽^*)
// ❖          // -------------------- Migrate & Seed Database -------------------- //          ❖
// (^▽^*)═══════════════════════════════════════════════════════════════════════════════════════════(*^▽^*)

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var seeder = services.GetRequiredService<DbSeeder>();
    var env = services.GetRequiredService<IWebHostEnvironment>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    if (env.IsDevelopment() || env.IsStaging())
    {
        logger.LogInformation(
            "Running database seeding for environment: {Environment}",
            env.EnvironmentName
        );
        await seeder.SeedAsync();
    }
    else
    {
        logger.LogInformation("Skipping database seeding in production environment.");
        // Optionally, you can still run safe parts of seeding here or perform limited operations
    }
}

// (*^▽^*)══════════════════════════════════════════════════════════════════════════════(^▽^*)
// ❖          // -------------------- Middleware -------------------- //          ❖
// (^▽^*)══════════════════════════════════════════════════════════════════════════════(*^▽^*)

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "Handled {RequestMethod} {RequestPath} with {StatusCode}";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestPath", httpContext.Request.Path);
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
        diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DocumentTitle = "Auth API Documentation";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API V1");
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.MapControllers();
app.Run();
