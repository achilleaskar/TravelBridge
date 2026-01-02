using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        });

        services.AddScoped<HereMapsService>();

        return services;
    }
}
