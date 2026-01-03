using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<VivaService> _logger;

        public VivaService(VivaAuthService authService, IOptions<VivaApiOptions> options, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<VivaService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("VivaApi");
            this.authService = authService;
            this.options = options;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<string> GetPaymentCode(VivaPaymentRequest request)
        {
            _logger.LogInformation("GetPaymentCode started - Amount: {Amount}, CustomerEmail: {Email}", 
                request.Amount, request.Customer?.Email);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Get the origin from the request
                var origin = _httpContextAccessor.HttpContext?.Request.Headers["Origin"].ToString();
                var referer = _httpContextAccessor.HttpContext?.Request.Headers["Referer"].ToString();
                
                // Check if the caller is from travelproject.gr
                bool isTravelProject = (!string.IsNullOrEmpty(origin) && origin.Contains("travelproject.gr", StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(referer) && referer.Contains("travelproject.gr", StringComparison.OrdinalIgnoreCase));

                _logger.LogDebug("GetPaymentCode: IsTravelProject: {IsTravelProject}, Origin: {Origin}, Referer: {Referer}", 
                    isTravelProject, origin, referer);

                // Use the appropriate source code
                request.SourceCode = isTravelProject
                 ? options.Value.SourceCodeTravelProject 
                 : options.Value.SourceCode;

                _logger.LogDebug("GetPaymentCode: Fetching access token from Viva");
                var accessToken = await authService.GetAccessTokenAsync();

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("GetPaymentCode: Sending payment order request to Viva");
                var response = await _httpClient.PostAsync("/checkout/v2/orders", httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GetPaymentCode failed: StatusCode: {StatusCode}, Error: {Error}", 
                        response.StatusCode, error);
                    throw new Exception($"Error creating payment order: {response.StatusCode} - {error}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(responseContent);
                var orderCode = document.RootElement.GetProperty("orderCode").GetInt64().ToString();

                stopwatch.Stop();
                _logger.LogInformation("GetPaymentCode completed - OrderCode: {OrderCode}, Amount: {Amount} in {ElapsedMs}ms", 
                    orderCode, request.Amount, stopwatch.ElapsedMilliseconds);

                return orderCode;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetPaymentCode failed - Amount: {Amount} after {ElapsedMs}ms", 
                    request.Amount, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<bool> ValidatePayment(string orderCode, string tid, decimal totalAmount, decimal? prepayAmount)
        {
            _logger.LogInformation("ValidatePayment started - OrderCode: {OrderCode}, Tid: {Tid}, TotalAmount: {TotalAmount}, PrepayAmount: {PrepayAmount}", 
                orderCode, tid, totalAmount, prepayAmount);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogDebug("ValidatePayment: Fetching access token from Viva");
                var accessToken = await authService.GetAccessTokenAsync();

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogDebug("ValidatePayment: Retrieving transaction details for Tid: {Tid}", tid);
                var response = await _httpClient.GetAsync($"/checkout/v2/transactions/{tid}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("ValidatePayment: Failed to retrieve transaction - StatusCode: {StatusCode}, Error: {Error}", 
                        response.StatusCode, error);
                    throw new Exception($"Error retrieving transaction: {response.StatusCode} - {error}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(responseContent);
                var OrderCode = document.RootElement.GetProperty("orderCode").GetInt64().ToString();
                var Amount = document.RootElement.GetProperty("amount").GetDecimal();
                var Status = document.RootElement.GetProperty("statusId").GetString();

                _logger.LogDebug("ValidatePayment: Transaction details - ReturnedOrderCode: {OrderCode}, Amount: {Amount}, Status: {Status}", 
                    OrderCode, Amount, Status);

                // Validate the transaction details
                var isValid = OrderCode == orderCode && (Amount == totalAmount || Amount == (prepayAmount ?? -1)) && Status == "F";

                stopwatch.Stop();

                if (isValid)
                {
                    _logger.LogInformation("ValidatePayment succeeded - OrderCode: {OrderCode}, Tid: {Tid}, Amount: {Amount} in {ElapsedMs}ms", 
                        orderCode, tid, Amount, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning("ValidatePayment failed validation - OrderCode match: {OrderCodeMatch}, Amount match: {AmountMatch} (Expected: {ExpectedAmount}, Got: {ActualAmount}), Status: {Status}", 
                        OrderCode == orderCode, 
                        Amount == totalAmount || Amount == (prepayAmount ?? -1),
                        prepayAmount ?? totalAmount,
                        Amount,
                        Status);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "ValidatePayment failed - OrderCode: {OrderCode}, Tid: {Tid} after {ElapsedMs}ms", 
                    orderCode, tid, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
