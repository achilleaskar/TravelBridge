using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using TravelBridge.Providers.Abstractions;

namespace TravelBridge.Providers.WebHotelier;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebHotelier(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WebHotelierApiOptions>(configuration.GetSection("WebHotelierApi"));

        services.AddHttpClient("WebHotelierApi", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WebHotelierApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Accept-Language", "el");

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        })
        .AddPolicyHandler((sp, _) => GetRetryPolicy(sp, "WebHotelierApi"));

        services.AddScoped<WebHotelierClient>();
        
        // Register the WebHotelier provider implementation
        services.AddScoped<IHotelProvider, WebHotelierHotelProvider>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider serviceProvider, string clientName)
    {
        var logger = serviceProvider.GetService<ILogger<WebHotelierClient>>();

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
