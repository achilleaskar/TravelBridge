using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using TravelBridge.API.Models.Apis;
using TravelBridge.API.Models.ExternalModels;

namespace TravelBridge.API.Services.Viva
{
    public class VivaService
    {
        private readonly VivaAuthService authService;
        private readonly IOptions<VivaApiOptions> options;
        private readonly HttpClient _httpClient;

        public VivaService(VivaAuthService authService, IOptions<VivaApiOptions> options, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("VivaApi");
            this.authService = authService;
            this.options = options;
        }

        public async Task<string> GetPaymentCode(VivaPaymentRequest request)
        {
            request.SourceCode = options.Value.SourceCode;
            var accessToken = await authService.GetAccessTokenAsync(); // ✅ Auto-fetch token

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/checkout/v2/orders", httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating payment order: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseContent);
            var orderCode = document.RootElement.GetProperty("orderCode").GetInt64().ToString();

            return orderCode;
        }

        internal async Task<bool> ValidatePayment(string orderCode, string tid, Models.DB.Reservation reservation)
        {
            var accessToken = await authService.GetAccessTokenAsync(); // Fetch the access token

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.GetAsync($"/checkout/v2/transactions/{tid}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error retrieving transaction: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseContent);
            var OrderCode = document.RootElement.GetProperty("orderCode").GetInt64().ToString();
            var Amount = document.RootElement.GetProperty("amount").GetDecimal();
            var Status = document.RootElement.GetProperty("statusId").GetString();

            // Validate the transaction details
            return OrderCode == orderCode && (Amount == reservation.TotalAmount || Amount == (reservation.PartialPayment?.prepayAmount ?? -1)) && Status == "F";
        }
    }
}
