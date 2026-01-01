using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using TravelBridge.Payments.Viva.Models.Apis;
using TravelBridge.Payments.Viva.Models.ExternalModels;

namespace TravelBridge.Payments.Viva.Services.Viva
{
    public class VivaService
    {
        private readonly VivaAuthService authService;
        private readonly IOptions<VivaApiOptions> options;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VivaService(VivaAuthService authService, IOptions<VivaApiOptions> options, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient("VivaApi");
            this.authService = authService;
            this.options = options;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GetPaymentCode(VivaPaymentRequest request)
        {
            // Get the origin from the request
            var origin = _httpContextAccessor.HttpContext?.Request.Headers["Origin"].ToString();
            var referer = _httpContextAccessor.HttpContext?.Request.Headers["Referer"].ToString();
            
            // Check if the caller is from travelproject.gr
            bool isTravelProject = (!string.IsNullOrEmpty(origin) && origin.Contains("travelproject.gr", StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(referer) && referer.Contains("travelproject.gr", StringComparison.OrdinalIgnoreCase));

            // Use the appropriate source code
            request.SourceCode = isTravelProject
             ? options.Value.SourceCodeTravelProject 
             : options.Value.SourceCode;

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

        public async Task<bool> ValidatePayment(string orderCode, string tid, decimal totalAmount, decimal? prepayAmount)
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
            return OrderCode == orderCode && (Amount == totalAmount || Amount == (prepayAmount ?? -1)) && Status == "F";
        }
    }
}
