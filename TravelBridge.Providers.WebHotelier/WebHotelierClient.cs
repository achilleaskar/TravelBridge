using System.Net.Http.Headers;
using System.Text.Json;
using TravelBridge.Providers.WebHotelier.Models.Responses;

namespace TravelBridge.Providers.WebHotelier;

/// <summary>
/// HTTP client for WebHotelier API calls.
/// This is a pure HTTP client that handles all WebHotelier API communication.
/// </summary>
public class WebHotelierClient
{
    private readonly HttpClient _httpClient;

    public WebHotelierClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("WebHotelierApi");
    }

    /// <summary>
    /// Search for properties by name
    /// </summary>
    public async Task<WHHotel[]> SearchPropertiesAsync(string propertyName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"property?name={Uri.EscapeDataString(propertyName)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<WHPropertiesResponse>(jsonString);
        
        return result?.data?.hotels ?? [];
    }

    /// <summary>
    /// Get all properties
    /// </summary>
    public async Task<WHHotel[]> GetAllPropertiesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("property", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<WHPropertiesResponse>(jsonString);
        
        return result?.data?.hotels ?? [];
    }

    /// <summary>
    /// Get multi-property availability
    /// </summary>
    public async Task<WHMultiAvailabilityResponse?> GetAvailabilityAsync(WHAvailabilityRequest request, string party, CancellationToken cancellationToken = default)
    {
        var url = $"availability?party={party}" +
            $"&checkin={Uri.EscapeDataString(request.CheckIn)}" +
            $"&checkout={Uri.EscapeDataString(request.CheckOut)}" +
            $"&lat={Uri.EscapeDataString(request.Lat)}" +
            $"&lon={Uri.EscapeDataString(request.Lon)}" +
            $"&lat1={Uri.EscapeDataString(request.BottomLeftLatitude)}" +
            $"&lat2={Uri.EscapeDataString(request.TopRightLatitude)}" +
            $"&lon1={Uri.EscapeDataString(request.BottomLeftLongitude)}" +
            $"&lon2={Uri.EscapeDataString(request.TopRightLongitude)}" +
            $"&sort_by={Uri.EscapeDataString(request.SortBy)}" +
            $"&sort_order={Uri.EscapeDataString(request.SortOrder)}&&payments=1";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<WHMultiAvailabilityResponse>(jsonString);
    }

    /// <summary>
    /// Get single property availability
    /// </summary>
    public async Task<WHSingleAvailabilityData?> GetSingleAvailabilityAsync(string propertyId, string checkIn, string checkOut, string party, CancellationToken cancellationToken = default)
    {
        var url = $"availability/{propertyId}?party={party}" +
            $"&checkin={Uri.EscapeDataString(checkIn)}" +
            $"&checkout={Uri.EscapeDataString(checkOut)}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<WHSingleAvailabilityData>(jsonString);
    }

    /// <summary>
    /// Get flexible calendar for alternatives
    /// </summary>
    public async Task<WHAlternativeDaysData?> GetFlexibleCalendarAsync(string propertyId, string party, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var url = $"availability/{propertyId}/flexible-calendar?party={party}" +
            $"&startDate={startDate:yyyy-MM-dd}" +
            $"&endDate={endDate:yyyy-MM-dd}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<WHAlternativeDaysData>(jsonString);
    }

    /// <summary>
    /// Get hotel info
    /// </summary>
    public async Task<WHHotelInfoResponse?> GetHotelInfoAsync(string hotelId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/property/{hotelId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<WHHotelInfoResponse>(jsonString);
    }

    /// <summary>
    /// Get room info
    /// </summary>
    public async Task<WHRoomInfoResponse?> GetRoomInfoAsync(string hotelId, string roomCode, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/room/{hotelId}/{roomCode}", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<WHRoomInfoResponse>(jsonString);
    }

    /// <summary>
    /// Create booking
    /// </summary>
    public async Task<WHBookingResponse?> CreateBookingAsync(string hotelCode, Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        var url = $"/book/{hotelCode}";
        var content = new FormUrlEncodedContent(parameters);

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        
        var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Booking failed: {response.StatusCode} - {jsonString}");
        }
        
        return JsonSerializer.Deserialize<WHBookingResponse>(jsonString);
    }

    /// <summary>
    /// Cancel booking
    /// </summary>
    public async Task<bool> CancelBookingAsync(int reservationId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/purge/{reservationId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
