using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace TravelBridge.Geo.HereMaps;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHereMaps(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HereMapsApiOptions>(configuration.GetSection("HereMapsApi"));

        services.AddHttpClient("HereMapsApi", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<HereMapsApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        })
        .AddPolicyHandler((sp, _) => GetRetryPolicy(sp, "HereMapsApi"));

        services.AddScoped<HereMapsService>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider serviceProvider, string clientName)
    {
        var logger = serviceProvider.GetService<ILogger<HereMapsService>>();

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
