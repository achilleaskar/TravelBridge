using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using TravelBridge.API.Endpoints;
using TravelBridge.API.Models.Apis;
using TravelBridge.API.Services.ExternalServices;
using TravelBridge.API.Services.WebHotelier;

var builder = WebApplication.CreateSlimBuilder(args);

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

//builder.Services.Configure<GzipCompressionProviderOptions>(options =>
//{
//    options.Level = CompressionLevel.Fastest; // or CompressionLevel.Optimal
//});

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

// Bind HereMapsApi section to HereMapsApiOptions
builder.Services.Configure<HereMapsApiOptions>(builder.Configuration.GetSection("HereMapsApi"));
// Register HttpClient with BaseAddress from configuration
builder.Services.AddHttpClient("HereMapsApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<HereMapsApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl); // Use BaseUrl from appsettings.json
});

// Bind MapBox section to MapBoxApiOptions
builder.Services.Configure<MapBoxApiOptions>(builder.Configuration.GetSection("MapBoxApi"));
// Register HttpClient with BaseAddress from configuration
builder.Services.AddHttpClient("MapBoxApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<MapBoxApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl); // Use BaseUrl from appsettings.json
});

// Bind WebHotelierApi options
builder.Services.Configure<WebHotelierApiOptions>(builder.Configuration.GetSection("WebHotelierApi"));

// Configure HttpClient with Basic Authentication
builder.Services.AddHttpClient("WebHotelierApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<WebHotelierApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.Add("Accept-Language", "el");

    // Add Basic Authentication header
    var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
});

#endregion HttpClients

builder.Services.AddScoped<HereMapsService>();
builder.Services.AddScoped<MapBoxService>();
builder.Services.AddScoped<WebHotelierPropertiesService>();
builder.Services.AddScoped<SearchPluginEndpoints>();
builder.Services.AddScoped<HotelEndpoint>();

#region Register Endpoint Groups

// Register your service
var app = builder.Build();

app.UseResponseCompression();

// Use the CORS policy
app.UseCors("AllowAll");

// Create a scope for the DI container
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;

    var searchEndpoints = serviceProvider.GetRequiredService<SearchPluginEndpoints>();
    searchEndpoints.MapEndpoints(app);

    var hotelEndpoints = serviceProvider.GetRequiredService<HotelEndpoint>();
    hotelEndpoints.MapEndpoints(app);
}

#endregion Register Endpoint Groups

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.RunAsync();