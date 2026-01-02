using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        });

        services.AddScoped<MapBoxService>();

        return services;
    }
}
