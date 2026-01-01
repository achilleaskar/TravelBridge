using TravelBridge.API.Endpoints;

namespace TravelBridge.API.Helpers.Extensions
{
    /// <summary>
    /// Extension methods for endpoint registration.
    /// Provides a clean, standard pattern for mapping endpoints.
    /// </summary>
    public static class EndpointExtensions
    {
        /// <summary>
        /// Maps all API endpoints for the application.
        /// </summary>
        public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapSearchPluginEndpoints();
            app.MapHotelEndpoints();
            app.MapReservationEndpoints();
            
            return app;
        }

        /// <summary>
        /// Maps search plugin endpoints.
        /// </summary>
        public static IEndpointRouteBuilder MapSearchPluginEndpoints(this IEndpointRouteBuilder app)
        {
            using var scope = app.ServiceProvider.CreateScope();
            var endpoints = scope.ServiceProvider.GetRequiredService<SearchPluginEndpoints>();
            endpoints.MapEndpoints(app);
            return app;
        }

        /// <summary>
        /// Maps hotel endpoints.
        /// </summary>
        public static IEndpointRouteBuilder MapHotelEndpoints(this IEndpointRouteBuilder app)
        {
            using var scope = app.ServiceProvider.CreateScope();
            var endpoints = scope.ServiceProvider.GetRequiredService<HotelEndpoint>();
            endpoints.MapEndpoints(app);
            return app;
        }

        /// <summary>
        /// Maps reservation endpoints.
        /// </summary>
        public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder app)
        {
            using var scope = app.ServiceProvider.CreateScope();
            var endpoints = scope.ServiceProvider.GetRequiredService<ReservationEndpoints>();
            endpoints.MapEndpoints(app);
            return app;
        }
    }
}
