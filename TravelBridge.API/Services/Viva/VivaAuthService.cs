using System.Text.Json;
using System.Text;
using TravelBridge.API.Models.Apis;
using Microsoft.Extensions.Options;

namespace TravelBridge.API.Services.Viva
{
    public class VivaAuthService
    {
        private readonly IOptions<VivaApiOptions> options;
        private readonly HttpClient _httpClient;

        private DateTime _tokenExpiry = DateTime.MinValue; // Store token expiration time
        private string? _accessToken;
        public VivaAuthService(IOptions<VivaApiOptions> options, HttpClient httpClient)
        {
            this.options = options;
            _httpClient = httpClient;
        }


        public async Task<string> GetAccessTokenAsync()
        {
            // Check if token is still valid
            if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry > DateTime.UtcNow)
            {
                return _accessToken; // ✅ Return cached token
            }

            var requestUrl = options.Value.AuthUrl;

            string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Value.ApiKey}:{options.Value.ApiSecret}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("Authorization", $"Basic {base64Credentials}");
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OAuth2 Token Request Failed: {response.StatusCode} - {responseContent}");
            }

            using var document = JsonDocument.Parse(responseContent);
            _accessToken = document.RootElement.GetProperty("access_token").GetString();
            int expiresIn = document.RootElement.GetProperty("expires_in").GetInt32();

            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Subtract 1 minute buffer

            return _accessToken ?? throw new Exception("Access token not found");
        }
    }
}
