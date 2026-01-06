using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace TravelBridge.Geo.Mapbox;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMapBox(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MapBoxApiOptions>(configuration.GetSection("MapBoxApi"));

        services.AddHttpClient("MapBoxApi", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<MapBoxApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        })
        .AddPolicyHandler((sp, _) => GetRetryPolicy(sp, "MapBoxApi"));

        services.AddScoped<MapBoxService>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider serviceProvider, string clientName)
    {
        var logger = serviceProvider.GetService<ILogger<MapBoxService>>();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    logger?.LogWarning(
                        "HTTP {ClientName} retry {RetryAttempt} after {DelayMs}ms due to {StatusCode}",
                        clientName,
                        retryAttempt,
                        timespan.TotalMilliseconds,
                        outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message);
                });
    }
}
