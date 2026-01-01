using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TravelBridge.Core.Entities;
using TravelBridge.Core.Interfaces;

namespace TravelBridge.Infrastructure.Integrations.Viva
{
    /// <summary>
    /// Configuration options for Viva API.
    /// </summary>
    public class VivaOptions
    {
        public string BaseUrl { get; set; } = "";
        public string AuthUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string ApiSecret { get; set; } = "";
        public string SourceCode { get; set; } = "";
        public string SourceCodeTravelProject { get; set; } = "";
    }

    /// <summary>
    /// Viva payment request model.
    /// </summary>
    public class VivaPaymentRequest
    {
        public int Amount { get; set; }
        public string CustomerTrns { get; set; } = "";
        public VivaCustomer Customer { get; set; } = new();
        public string SourceCode { get; set; } = "";
        public string MerchantTrns { get; set; } = "";
        public bool DisableCash { get; set; } = true;
        public int PaymentTimeout { get; } = 300;
        public string DynamicDescriptor { get; } = "My Diakopes";
        public List<string> Tags { get; set; } = ["my-diakopes tag"];
    }

    public class VivaCustomer
    {
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? CountryCode { get; set; }
        public string? RequestLang { get; set; }
    }

    /// <summary>
    /// Viva authentication service.
    /// </summary>
    public class VivaAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly VivaOptions _options;
        private string? _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public VivaAuthService(IHttpClientFactory httpClientFactory, IOptions<VivaOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient();
            _options = options.Value;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
                return _accessToken;

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ApiKey}:{_options.ApiSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, _options.AuthUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);

            return _accessToken!;
        }
    }

    /// <summary>
    /// Viva payment service.
    /// Implements IPaymentProvider for provider-agnostic payment operations.
    /// </summary>
    public class VivaPaymentService : IPaymentProvider
    {
        private readonly HttpClient _httpClient;
        private readonly VivaAuthService _authService;
        private readonly VivaOptions _options;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public VivaPaymentService(
            IHttpClientFactory httpClientFactory,
            VivaAuthService authService,
            IOptions<VivaOptions> options,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _httpClient = httpClientFactory.CreateClient("VivaApi");
            _authService = authService;
            _options = options.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        public int ProviderId => (int)PaymentProvider.Viva;
        public string ProviderName => "Viva Wallet";

        public async Task<string> CreatePaymentOrderAsync(PaymentOrderRequest request, CancellationToken cancellationToken = default)
        {
            var sourceCode = GetSourceCode();

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
                MerchantTrns = request.MerchantReference ?? "",
                SourceCode = sourceCode
            };

            return await GetPaymentCodeAsync(vivaRequest);
        }

        /// <summary>
        /// Gets a payment code from Viva for the given request.
        /// </summary>
        public async Task<string> GetPaymentCodeAsync(VivaPaymentRequest request)
        {
            // Set source code if not already set
            if (string.IsNullOrEmpty(request.SourceCode))
            {
                request.SourceCode = GetSourceCode();
            }

            var accessToken = await _authService.GetAccessTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/checkout/v2/orders", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating payment order: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement.GetProperty("orderCode").GetInt64().ToString();
        }

        public async Task<PaymentValidationResult> ValidatePaymentAsync(
            string orderCode, 
            string transactionId, 
            decimal expectedAmount, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var accessToken = await _authService.GetAccessTokenAsync();

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.GetAsync($"/checkout/v2/transactions/{transactionId}", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    return PaymentValidationResult.Failure($"Error retrieving transaction: {response.StatusCode} - {error}");
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseContent);
                
                var retrievedOrderCode = doc.RootElement.GetProperty("orderCode").GetInt64().ToString();
                var amount = doc.RootElement.GetProperty("amount").GetDecimal();
                var status = doc.RootElement.GetProperty("statusId").GetString();

                bool isValid = retrievedOrderCode == orderCode && status == "F";

                if (isValid)
                    return PaymentValidationResult.Success(retrievedOrderCode, transactionId, amount, status ?? "F");
                else
                    return PaymentValidationResult.Failure($"Validation failed: OrderCode match={retrievedOrderCode == orderCode}, Status={status}");
            }
            catch (Exception ex)
            {
                return PaymentValidationResult.Failure($"Exception during validation: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates a payment against expected amounts (total or partial prepay).
        /// Used for reservation-based validation.
        /// </summary>
        /// <param name="orderCode">The order code from Viva</param>
        /// <param name="transactionId">The transaction ID from Viva callback</param>
        /// <param name="totalAmount">The total reservation amount</param>
        /// <param name="prepayAmount">Optional partial prepay amount</param>
        /// <returns>True if payment is valid</returns>
        public async Task<bool> ValidatePaymentAmountAsync(string orderCode, string transactionId, decimal totalAmount, decimal? prepayAmount = null)
        {
            var accessToken = await _authService.GetAccessTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.GetAsync($"/checkout/v2/transactions/{transactionId}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error retrieving transaction: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            
            var retrievedOrderCode = doc.RootElement.GetProperty("orderCode").GetInt64().ToString();
            var amount = doc.RootElement.GetProperty("amount").GetDecimal();
            var status = doc.RootElement.GetProperty("statusId").GetString();

            // Validate: order code matches, status is "F" (finalized), and amount matches either total or prepay
            bool amountValid = amount == totalAmount || (prepayAmount.HasValue && amount == prepayAmount.Value);
            return retrievedOrderCode == orderCode && amountValid && status == "F";
        }

        private string GetSourceCode()
        {
            var origin = _httpContextAccessor?.HttpContext?.Request.Headers["Origin"].ToString();
            var referer = _httpContextAccessor?.HttpContext?.Request.Headers["Referer"].ToString();

            bool isTravelProject = 
                (!string.IsNullOrEmpty(origin) && origin.Contains("travelproject.gr", StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(referer) && referer.Contains("travelproject.gr", StringComparison.OrdinalIgnoreCase));

            return isTravelProject ? _options.SourceCodeTravelProject : _options.SourceCode;
        }
    }
}
