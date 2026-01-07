using System.Threading.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;
using TravelBridge.API.DataBase;
using TravelBridge.API.Endpoints;
using TravelBridge.API.Providers;
using TravelBridge.API.Repositories;
using TravelBridge.API.Services;
using TravelBridge.Geo.Mapbox;
using TravelBridge.Geo.HereMaps;
using TravelBridge.Payments.Viva.Models.Apis;
using TravelBridge.Payments.Viva.Services.Viva;
using TravelBridge.API.Models.WebHotelier;
using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.WebHotelier;
using TravelBridge.API.Models.Apis;

var builder = WebApplication.CreateSlimBuilder(args);
string? connectionString = builder.Configuration.GetConnectionString("MariaDBConnection");

// Configure Serilog with daily rolling logs, 30 day retention
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/travelbridge-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 50 * 1024 * 1024,
        rollOnFileSizeLimit: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add Memory Cache for hotel/room info caching
builder.Services.AddMemoryCache();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddMySql(connectionString!, name: "database", tags: ["db", "mysql"]);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.Parse("10.11.10-MariaDB"),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
        }
    ));

// Add CORS services with environment-specific policies
builder.Services.AddCors(options =>
{
    // Development: Allow all origins
    options.AddPolicy("Development", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Production: Restrict to known origins
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
                "https://my-diakopes.gr",
                "https://www.my-diakopes.gr",
                "https://travelproject.gr",
                "https://www.travelproject.gr")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Add response compression services
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services
    .AddOpenApi()
    .AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "TravelBridge API",
            Version = "v1",
            Description = "Hotel booking API with WebHotelier and Viva Wallet integration."
        });
    }).Configure<RouteOptions>(options =>
    {
        options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
    });

#region HttpClients

// Register providers using extension methods (with Polly retry policies)
builder.Services.AddHereMaps(builder.Configuration);
builder.Services.AddMapBox(builder.Configuration);
builder.Services.AddWebHotelier(builder.Configuration);

// Register the provider resolver (resolves providers by ID)
builder.Services.AddScoped<IHotelProviderResolver, HotelProviderResolver>();

// Bind Viva section to VivaApiOptions
builder.Services.Configure<VivaApiOptions>(builder.Configuration.GetSection("VivaApi"));

// Bind TestCard section to TestCardOptions
builder.Services.Configure<TestCardOptions>(builder.Configuration.GetSection("TestCard"));

// Register Viva HttpClient with fast-fail retry policy (1 retry only for payments)
builder.Services.AddHttpClient("VivaApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<VivaApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
})
.AddPolicyHandler((sp, _) =>
{
    var logger = sp.GetService<ILogger<VivaService>>();
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 1,
            sleepDurationProvider: _ => TimeSpan.FromMilliseconds(100),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                logger?.LogWarning(
                    "Viva payment HTTP retry {RetryAttempt} after {DelayMs}ms due to {Reason}",
                    retryAttempt,
                    timespan.TotalMilliseconds,
                    outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message);
            });
});

#endregion HttpClients
builder.Services.AddSingleton<SmtpEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<WebHotelierPropertiesService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<SearchPluginEndpoints>();
builder.Services.AddScoped<HotelEndpoint>();
builder.Services.AddScoped<ReservationEndpoints>();

builder.Services.AddScoped<VivaService>();
builder.Services.AddScoped<VivaAuthService>();

builder.Services.AddScoped<ReservationsRepository>();

#region Register Endpoint Groups

// Register your service
var app = builder.Build();

app.UseResponseCompression();

// Use environment-specific CORS policy
var env = app.Environment;
app.UseCors(env.IsDevelopment() ? "Development" : "Production");

// Use Rate Limiting
app.UseRateLimiter();

// Lightweight request logging middleware with correlation ID
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    // Get or generate session ID from request header
    var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault() 
                    ?? Guid.NewGuid().ToString("N")[..8];
    var requestId = Guid.NewGuid().ToString("N")[..8];
    
    // Set response headers for tracking
    context.Response.Headers["X-Session-Id"] = sessionId;
    context.Response.Headers["X-Request-Id"] = requestId;
    
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Enrich log context with correlation IDs
    using (Serilog.Context.LogContext.PushProperty("SessionId", sessionId))
    using (Serilog.Context.LogContext.PushProperty("RequestId", requestId))
    {
        logger.LogInformation(
            "REQ {Method} {Path}{QueryString}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);

        try
        {
            await next();
            
            stopwatch.Stop();
            logger.LogInformation(
                "RES {StatusCode} in {ElapsedMs}ms",
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "ERR {Method} {Path} failed after {ElapsedMs}ms: {ErrorMessage}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }
});

// Map Health Check endpoint
app.MapHealthChecks("/health");

// Create a scope for the DI container
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;

    var searchEndpoints = serviceProvider.GetRequiredService<SearchPluginEndpoints>();
    searchEndpoints.MapEndpoints(app);

    var hotelEndpoints = serviceProvider.GetRequiredService<HotelEndpoint>();
    hotelEndpoints.MapEndpoints(app);

    var reservationEndpoints = serviceProvider.GetRequiredService<ReservationEndpoints>();
    reservationEndpoints.MapEndpoints(app);
}

#endregion Register Endpoint Groups

app.UseSwagger();
app.UseSwaggerUI();

// Ensure Serilog flushes on shutdown
app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

await app.RunAsync();