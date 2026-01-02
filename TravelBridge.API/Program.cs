using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
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
using TravelBridge.API.Models.Apis;

var builder = WebApplication.CreateSlimBuilder(args);
string? connectionString = builder.Configuration.GetConnectionString("MariaDBConnection");

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log.txt",              // Single base filename
        fileSizeLimitBytes: 50 * 1024 * 1024, // 50 MB
        rollOnFileSizeLimit: true,            // Enable rolling when size exceeds
        retainedFileCountLimit: 10            // Keep only last 10 files (optional)
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

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    // Read request body
    context.Request.EnableBuffering();
    var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
    context.Request.Body.Position = 0;

    logger.LogInformation("Request {Method} {Path} - Body: {Body}", context.Request.Method, context.Request.Path, requestBody);

    // Capture the response body
    var originalBody = context.Response.Body;
    using var newBody = new MemoryStream();
    context.Response.Body = newBody;

    await next(); // Call the next middleware / endpoint

    // Read response body
    newBody.Seek(0, SeekOrigin.Begin);
    var responseBody = await new StreamReader(newBody).ReadToEndAsync();
    newBody.Seek(0, SeekOrigin.Begin);

    logger.LogInformation("Response {StatusCode} - Body: {Body}", context.Response.StatusCode, responseBody);

    // Copy the response back to the original stream
    await newBody.CopyToAsync(originalBody);
    context.Response.Body = originalBody;
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

await app.RunAsync();