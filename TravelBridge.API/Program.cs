using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using TravelBridge.API.DataBase;
using TravelBridge.API.Endpoints;
using TravelBridge.API.Repositories;
using TravelBridge.API.Services;
using TravelBridge.Geo.Mapbox;
using TravelBridge.Geo.HereMaps;
using TravelBridge.Payments.Viva.Models.Apis;
using TravelBridge.Payments.Viva.Services.Viva;
using TravelBridge.API.Models.WebHotelier;
using TravelBridge.Providers.WebHotelier;
using TravelBridge.Providers.Abstractions;
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
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin() // Allow any origin
              .AllowAnyMethod() // Allow any HTTP method (GET, POST, etc.)
              .AllowAnyHeader(); // Allow any header
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

#region HttpClients

// Register providers using extension methods
builder.Services.AddHereMaps(builder.Configuration);
builder.Services.AddMapBox(builder.Configuration);
builder.Services.AddWebHotelier(builder.Configuration);

// Register HotelProviderResolver - uses IEnumerable<IHotelProvider> to discover all providers
builder.Services.AddScoped<HotelProviderResolver>();

// Bind Viva section to VivaApiOptions
builder.Services.Configure<VivaApiOptions>(builder.Configuration.GetSection("VivaApi"));

// Bind TestCard section to TestCardOptions
builder.Services.Configure<TestCardOptions>(builder.Configuration.GetSection("TestCard"));

// Register HttpClient with BaseAddress from configuration
builder.Services.AddHttpClient("VivaApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<VivaApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl); // Use BaseUrl from appsettings.json
});

#endregion HttpClients
builder.Services.AddSingleton<SmtpEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<WebHotelierPropertiesService>();
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

// Use the CORS policy
app.UseCors("AllowAll");

// Lightweight request logging middleware - logs only essential info, no body content
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var requestId = Guid.NewGuid().ToString("N")[..8];
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Log request start with key identifiers only
    using (Serilog.Context.LogContext.PushProperty("RequestId", requestId))
    {
        logger.LogInformation(
            "REQ {RequestId} {Method} {Path}{QueryString}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);

        try
        {
            await next();
            
            stopwatch.Stop();
            logger.LogInformation(
                "RES {RequestId} {StatusCode} in {ElapsedMs}ms",
                requestId,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "ERR {RequestId} {Method} {Path} failed after {ElapsedMs}ms: {ErrorMessage}",
                requestId,
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
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

app.UseSwagger();
app.UseSwaggerUI();

// Ensure Serilog flushes on shutdown
app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

await app.RunAsync();