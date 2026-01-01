using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using TravelBridge.API.Models.Apis;
using TravelBridge.Core.Interfaces;
using TravelBridge.Infrastructure.Integrations.Viva;

namespace TravelBridge.API.Services.Viva
{
    public class VivaService : IPaymentProvider
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

        #region IPaymentProvider Implementation

        public int ProviderId => (int)Models.PaymentProvider.Viva;
        public string ProviderName => "Viva Wallet";

        public async Task<string> CreatePaymentOrderAsync(PaymentOrderRequest request, CancellationToken cancellationToken = default)
        {
            var vivaRequest = new VivaPaymentRequest
            {
                Amount = (int)request.Amount,
                CustomerTrns = request.Description ?? "",
                Customer = new VivaCustomer
                {
                    Email = request.CustomerEmail,
                    FullName = request.CustomerFullName,
                    Phone = request.CustomerPhone ?? ""
                },
                MerchantTrns = request.MerchantReference ?? ""
            };

            return await GetPaymentCode(vivaRequest);
        }

        public async Task<PaymentValidationResult> ValidatePaymentAsync(string orderCode, string transactionId, decimal expectedAmount, CancellationToken cancellationToken = default)
        {
            try
            {
                var accessToken = await authService.GetAccessTokenAsync();

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.GetAsync($"/checkout/v2/transactions/{transactionId}", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    return PaymentValidationResult.Failure($"Error retrieving transaction: {response.StatusCode} - {error}");
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(responseContent);
                var retrievedOrderCode = document.RootElement.GetProperty("orderCode").GetInt64().ToString();
                var amount = document.RootElement.GetProperty("amount").GetDecimal();
                var status = document.RootElement.GetProperty("statusId").GetString();

                // Validate the transaction details
                bool isValid = retrievedOrderCode == orderCode && status == "F";

                if (isValid)
                {
                    return PaymentValidationResult.Success(retrievedOrderCode, transactionId, amount, status ?? "F");
                }
                else
                {
                    return PaymentValidationResult.Failure($"Validation failed: OrderCode match={retrievedOrderCode == orderCode}, Status={status}");
                }
            }
            catch (Exception ex)
            {
                return PaymentValidationResult.Failure($"Exception during validation: {ex.Message}");
            }
        }

        #endregion IPaymentProvider Implementation

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

            var accessToken = await authService.GetAccessTokenAsync();

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
            var accessToken = await authService.GetAccessTokenAsync();

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
