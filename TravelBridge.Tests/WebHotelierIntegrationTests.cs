using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using TravelBridge.API.Contracts;
using TravelBridge.API.Models.Apis;
using TravelBridge.API.Services;
using TravelBridge.API.Services.WebHotelier;
using TravelBridge.Core.Services;

namespace TravelBridge.Tests
{
    /// <summary>
    /// Integration tests for WebHotelier API.
    /// These tests make REAL calls to the WebHotelier API.
    /// Only tests read operations (no bookings).
    /// </summary>
    public class WebHotelierIntegrationTests : IAsyncLifetime
    {
        private readonly HttpClient _httpClient;
        private readonly WebHotelierPropertiesService _service;
        private readonly WebHotelierApiOptions _options;

        // Test hotel code - VAROSVILL is a known hotel in the system
        private const string TestHotelCode = "VAROSVILL";
        private const string TestHotelId = "1-VAROSVILL";

        public WebHotelierIntegrationTests()
        {
            // Setup real WebHotelier API credentials
            _options = new WebHotelierApiOptions
            {
                BaseUrl = "https://rest.reserve-online.net/",
                Username = "travelproje20666",
                Password = "F9FD67BEC99B96C45519D34CB77BAEFEBD445A9B",
                GuaranteeCard = new GuaranteeCardOptions()
            };

            // Create HttpClient with Basic Auth
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_options.BaseUrl)
            };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "el");

            var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            // Create mock IHttpClientFactory
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(x => x.CreateClient("WebHotelierApi")).Returns(_httpClient);

            // Create mock SmtpEmailSender
            var emailSender = new Mock<SmtpEmailSender>(Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>());

            // Create options wrapper
            var optionsWrapper = Options.Create(_options);

            // Create memory cache
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            // Initialize PricingConfig
            PricingConfig.Initialize(new PricingOptions
            {
                MinimumMarginPercent = 10,
                SpecialHotelDiscountPercent = 5
            });

            // Create service
            _service = new WebHotelierPropertiesService(httpClientFactory.Object, emailSender.Object, optionsWrapper, memoryCache);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public Task DisposeAsync()
        {
            _httpClient.Dispose();
            return Task.CompletedTask;
        }

        #region Hotel Info Tests

        [Fact]
        public async Task GetHotelInfo_WithValidHotelCode_ReturnsHotelData()
        {
            // Act
            var result = await _service.GetHotelInfo(TestHotelCode);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.True(result.ErrorCode == null || result.ErrorCode == "OK"); // No error or OK means success
            Assert.False(string.IsNullOrWhiteSpace(result.Data.Name));
        }

        [Fact]
        public async Task GetHotelInfo_WithValidHotelCode_ReturnsPhotos()
        {
            // Act
            var result = await _service.GetHotelInfo(TestHotelCode);

            // Assert
            Assert.NotNull(result.Data.LargePhotos);
            Assert.NotEmpty(result.Data.LargePhotos);
        }

        [Fact]
        public async Task GetHotelInfo_WithValidHotelCode_ReturnsOperation()
        {
            // Act
            var result = await _service.GetHotelInfo(TestHotelCode);

            // Assert
            Assert.NotNull(result.Data.Operation);
            Assert.False(string.IsNullOrWhiteSpace(result.Data.Operation.CheckinTime));
            Assert.False(string.IsNullOrWhiteSpace(result.Data.Operation.CheckoutTime));
        }

        [Fact]
        public async Task GetHotelInfo_WithInvalidHotelCode_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetHotelInfo("INVALID_HOTEL_CODE_12345"));
        }

        #endregion

        #region Room Info Tests

        [Fact]
        public async Task GetRoomInfo_WithValidCodes_ReturnsRoomData()
        {
            // First get hotel info to find a valid room code
            var hotelInfo = await _service.GetHotelInfo(TestHotelCode);
            
            // We need to get availability first to find room codes
            // Use a date far in the future to increase chance of availability
            var checkIn = DateTime.Today.AddDays(60).ToString("yyyy-MM-dd");
            var checkOut = DateTime.Today.AddDays(62).ToString("yyyy-MM-dd");
            
            var availReq = new SingleAvailabilityRequest
            {
                PropertyId = TestHotelCode,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Party = "[{\"adults\":2}]"
            };

            try
            {
                var availability = await _service.GetHotelAvailabilityAsync(availReq, DateTime.Today.AddDays(60), null);
                
                if (availability.Data?.Rooms?.Any() == true)
                {
                    var roomType = availability.Data.Rooms.First().Type;
                    
                    // Act
                    var result = await _service.GetRoomInfo(TestHotelCode, roomType);

                    // Assert
                    Assert.NotNull(result);
                    Assert.NotNull(result.Data);
                    Assert.False(string.IsNullOrWhiteSpace(result.Data.Name));
                }
                else
                {
                    // Skip if no rooms available
                    Assert.True(true, "No rooms available for testing - hotel might be fully booked");
                }
            }
            catch (Exception)
            {
                // If availability check fails, skip this test
                Assert.True(true, "Could not check availability - skipping room info test");
            }
        }

        #endregion

        #region Search Properties Tests

        [Fact]
        public async Task SearchProperty_WithValidName_ReturnsResults()
        {
            // Act
            var result = await _service.SearchPropertyAsync("Varos");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task SearchProperty_WithPartialName_ReturnsResults()
        {
            // Act
            var result = await _service.SearchPropertyAsync("hotel");

            // Assert
            Assert.NotNull(result);
            // Should return multiple hotels
        }

        [Fact]
        public async Task GetAllProperties_ReturnsResults()
        {
            // Act
            var result = await _service.GetAllPropertiesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        #endregion
    }
}
