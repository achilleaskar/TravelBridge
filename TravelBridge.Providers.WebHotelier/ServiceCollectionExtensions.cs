using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TravelBridge.Contracts.Providers;

namespace TravelBridge.Providers.WebHotelier;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers WebHotelier services including the HTTP client, client wrapper, and IHotelProvider implementation.
    /// </summary>
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
        });

        services.AddScoped<WebHotelierClient>();
        
        // Register IHotelProvider implementation for WebHotelier
        services.AddScoped<IHotelProvider, WebHotelierHotelProvider>();

        return services;
    }
}
