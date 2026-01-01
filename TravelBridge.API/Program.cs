using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using System.Threading.RateLimiting;
using TravelBridge.API.DataBase;
using TravelBridge.API.Endpoints;
using TravelBridge.API.Middleware;
using TravelBridge.API.Models.Apis;
using TravelBridge.API.Repositories;
using TravelBridge.API.Services;
using TravelBridge.API.Services.ExternalServices;
using TravelBridge.API.Services.Viva;
using TravelBridge.API.Services.WebHotelier;

var builder = WebApplication.CreateSlimBuilder(args);
string? connectionString = builder.Configuration.GetConnectionString("MariaDBConnection");

// Configure Serilog with correlation ID enrichment
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext() // Required for SessionId/RequestId enrichment
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SessionId}/{RequestId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log.txt",              // Single base filename
        fileSizeLimitBytes: 50 * 1024 * 1024, // 50 MB
        rollOnFileSizeLimit: true,            // Enable rolling when size exceeds
        retainedFileCountLimit: 10,           // Keep only last 10 files
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SessionId}/{RequestId}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.Parse("10.11.10-MariaDB"),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
        }
    ));

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(
                "https://my-diakopes.gr",
                "https://www.my-diakopes.gr",
                "https://travelproject.gr",
                "https://www.travelproject.gr"
              )
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Separate policy for development (localhost)
    options.AddPolicy("Development", policy =>
    {
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add response compression services
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Enable compression for HTTPS requests
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services
    .AddOpenApi()
    .AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Sample API",
            Version = "v1",
            Description = "API to demonstrate Swagger integration."
        });
    }).Configure<RouteOptions>(options =>
    {
        options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
    });

#region Retry Policies

// Retry policy for WebHotelier, MapBox, HereMaps: 3 retries with 100ms, 250ms, 500ms delays
static IAsyncPolicy<HttpResponseMessage> GetStandardRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => retryAttempt switch
        {
            1 => TimeSpan.FromMilliseconds(100),
            2 => TimeSpan.FromMilliseconds(250),
            3 => TimeSpan.FromMilliseconds(500),
            _ => TimeSpan.FromMilliseconds(500)
        });
}

// Retry policy for Viva: 1 retry with 100ms delay (payments should fail fast)
static IAsyncPolicy<HttpResponseMessage> GetVivaRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(1, _ => TimeSpan.FromMilliseconds(100));
}

#endregion Retry Policies

#region HttpClients

// Bind Pricing options
builder.Services.Configure<PricingOptions>(builder.Configuration.GetSection("Pricing"));

// Bind HereMapsApi section to HereMapsApiOptions
builder.Services.Configure<HereMapsApiOptions>(builder.Configuration.GetSection("HereMapsApi"));
// Register HttpClient with BaseAddress from configuration
builder.Services.AddHttpClient("HereMapsApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<HereMapsApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
})
.AddPolicyHandler(GetStandardRetryPolicy());

// Bind MapBox section to MapBoxApiOptions
builder.Services.Configure<MapBoxApiOptions>(builder.Configuration.GetSection("MapBoxApi"));
// Register HttpClient with BaseAddress from configuration
builder.Services.AddHttpClient("MapBoxApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<MapBoxApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
})
.AddPolicyHandler(GetStandardRetryPolicy());

// Bind Viva section to VivaApiOptions
builder.Services.Configure<VivaApiOptions>(builder.Configuration.GetSection("VivaApi"));

// Register HttpClient with BaseAddress from configuration (1 retry for payments)
builder.Services.AddHttpClient("VivaApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<VivaApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
})
.AddPolicyHandler(GetVivaRetryPolicy());


// Bind WebHotelierApi options
builder.Services.Configure<WebHotelierApiOptions>(builder.Configuration.GetSection("WebHotelierApi"));
// Configure HttpClient with Basic Authentication (3 retries)
builder.Services.AddHttpClient("WebHotelierApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<WebHotelierApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.Add("Accept-Language", "el");

    // Add Basic Authentication header
    var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
})
.AddPolicyHandler(GetStandardRetryPolicy());

#endregion HttpClients
builder.Services.AddSingleton<SmtpEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<HereMapsService>();
builder.Services.AddScoped<MapBoxService>();
builder.Services.AddScoped<WebHotelierPropertiesService>();
builder.Services.AddScoped<SearchPluginEndpoints>();
builder.Services.AddScoped<HotelEndpoint>();
builder.Services.AddScoped<ReservationEndpoints>();

builder.Services.AddScoped<VivaService>();
builder.Services.AddScoped<VivaAuthService>();

builder.Services.AddScoped<ReservationsRepository>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddMySql(connectionString!, name: "database", tags: ["db", "mysql"]);

// Add memory cache for hotel/room info caching
builder.Services.AddMemoryCache();

// Add rate limiting (100 requests per minute per IP)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0  // No queuing, reject immediately
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

#region Register Endpoint Groups

// Register your service
var app = builder.Build();

// Initialize static pricing configuration
var pricingOptions = app.Services.GetRequiredService<IOptions<PricingOptions>>().Value;
PricingConfig.Initialize(pricingOptions);

app.UseResponseCompression();

// Use the CORS policy based on environment
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}
else
{
    app.UseCors("AllowedOrigins");
}

// Add correlation ID middleware (must be before request logging)
app.UseCorrelationId();

// Add rate limiting middleware
app.UseRateLimiter();

// Request/Response logging middleware (conditional - only log bodies in Development or on errors)
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var sessionId = context.Items["SessionId"]?.ToString() ?? "-";
    var requestId = context.Items["RequestId"]?.ToString() ?? "-";

    // Log request (without body in production for performance)
    if (app.Environment.IsDevelopment())
    {
        context.Request.EnableBuffering();
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;
        logger.LogInformation("Request {Method} {Path} - Body: {Body}", context.Request.Method, context.Request.Path, requestBody);
    }
    else
    {
        logger.LogInformation("Request {Method} {Path}", context.Request.Method, context.Request.Path);
    }

    // Capture the response body only in Development
    if (app.Environment.IsDevelopment())
    {
        var originalBody = context.Response.Body;
        using var newBody = new MemoryStream();
        context.Response.Body = newBody;

        await next();

        newBody.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(newBody).ReadToEndAsync();
        newBody.Seek(0, SeekOrigin.Begin);

        logger.LogInformation("Response {StatusCode} - Body: {Body}", context.Response.StatusCode, responseBody);

        await newBody.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }
    else
    {
        await next();
        logger.LogInformation("Response {StatusCode}", context.Response.StatusCode);
    }
});

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

// Map health check endpoint
app.MapHealthChecks("/health");

app.UseSwagger();
app.UseSwaggerUI();

await app.RunAsync();