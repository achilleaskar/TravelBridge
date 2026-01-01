using System.Reflection;
using TravelBridge.Core.Interfaces;

namespace TravelBridge.Tests
{
    /// <summary>
    /// Tests to enforce architectural rules and dependency constraints.
    /// These tests ensure the modular monolith boundaries are respected.
    /// </summary>
    public class ArchitectureTests
    {
        #region Core Layer Tests

        [Fact]
        public void Core_ShouldNotReference_Infrastructure()
        {
            // Arrange
            var coreAssembly = typeof(TravelBridge.Core.Services.PricingConfig).Assembly;
            var referencedAssemblies = coreAssembly.GetReferencedAssemblies();

            // Act & Assert
            Assert.DoesNotContain(referencedAssemblies, 
                a => a.Name?.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase) == true);
        }

        [Fact]
        public void Core_ShouldNotReference_API()
        {
            // Arrange
            var coreAssembly = typeof(TravelBridge.Core.Services.PricingConfig).Assembly;
            var referencedAssemblies = coreAssembly.GetReferencedAssemblies();

            // Act & Assert
            Assert.DoesNotContain(referencedAssemblies,
                a => a.Name?.Contains("TravelBridge.API", StringComparison.OrdinalIgnoreCase) == true);
        }

        [Fact]
        public void Core_ShouldNotReference_EntityFramework()
        {
            // Arrange
            var coreAssembly = typeof(TravelBridge.Core.Services.PricingConfig).Assembly;
            var referencedAssemblies = coreAssembly.GetReferencedAssemblies();

            // Act & Assert
            Assert.DoesNotContain(referencedAssemblies,
                a => a.Name?.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) == true);
        }

        [Fact]
        public void Core_ShouldNotReference_HttpClient()
        {
            // Arrange - Core should not have HTTP dependencies
            var coreAssembly = typeof(TravelBridge.Core.Services.PricingConfig).Assembly;
            var referencedAssemblies = coreAssembly.GetReferencedAssemblies();

            // Act & Assert
            Assert.DoesNotContain(referencedAssemblies,
                a => a.Name?.Equals("System.Net.Http", StringComparison.OrdinalIgnoreCase) == true);
        }

        #endregion

        #region Contracts Layer Tests

        [Fact]
        public void Contracts_ShouldNotReference_Infrastructure()
        {
            // Arrange
            var contractsAssembly = typeof(TravelBridge.Contracts.Requests.AvailabilitySearchRequest).Assembly;
            var referencedAssemblies = contractsAssembly.GetReferencedAssemblies();

            // Act & Assert
            Assert.DoesNotContain(referencedAssemblies,
                a => a.Name?.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase) == true);
        }

        [Fact]
        public void Contracts_ShouldNotReference_API()
        {
            // Arrange
            var contractsAssembly = typeof(TravelBridge.Contracts.Requests.AvailabilitySearchRequest).Assembly;
            var referencedAssemblies = contractsAssembly.GetReferencedAssemblies();

            // Act & Assert
            Assert.DoesNotContain(referencedAssemblies,
                a => a.Name?.Contains("TravelBridge.API", StringComparison.OrdinalIgnoreCase) == true);
        }

        [Fact]
        public void Contracts_ShouldNotReference_Core()
        {
            // Arrange - Contracts should be independent
            var contractsAssembly = typeof(TravelBridge.Contracts.Requests.AvailabilitySearchRequest).Assembly;
            var referencedAssemblies = contractsAssembly.GetReferencedAssemblies();

            // Act & Assert
            Assert.DoesNotContain(referencedAssemblies,
                a => a.Name?.Contains("TravelBridge.Core", StringComparison.OrdinalIgnoreCase) == true);
        }

        #endregion

        #region Interface Existence Tests

        [Fact]
        public void Core_ShouldHave_IHotelProvider_Interface()
        {
            // Arrange
            var coreAssembly = typeof(TravelBridge.Core.Services.PricingConfig).Assembly;

            // Act
            var interfaceType = coreAssembly.GetType("TravelBridge.Core.Interfaces.IHotelProvider");

            // Assert
            Assert.NotNull(interfaceType);
            Assert.True(interfaceType.IsInterface);
        }

        [Fact]
        public void Core_ShouldHave_IPaymentProvider_Interface()
        {
            // Arrange
            var coreAssembly = typeof(TravelBridge.Core.Services.PricingConfig).Assembly;

            // Act
            var interfaceType = coreAssembly.GetType("TravelBridge.Core.Interfaces.IPaymentProvider");

            // Assert
            Assert.NotNull(interfaceType);
            Assert.True(interfaceType.IsInterface);
        }

        [Fact]
        public void Core_ShouldHave_IEmailService_Interface()
        {
            // Arrange
            var coreAssembly = typeof(TravelBridge.Core.Services.PricingConfig).Assembly;

            // Act
            var interfaceType = coreAssembly.GetType("TravelBridge.Core.Interfaces.IEmailService");

            // Assert
            Assert.NotNull(interfaceType);
            Assert.True(interfaceType.IsInterface);
        }

        [Fact]
        public void Core_ShouldHave_IGeocodingProvider_Interface()
        {
            // Arrange
            var coreAssembly = typeof(TravelBridge.Core.Services.PricingConfig).Assembly;

            // Act
            var interfaceType = coreAssembly.GetType("TravelBridge.Core.Interfaces.IGeocodingProvider");

            // Assert
            Assert.NotNull(interfaceType);
            Assert.True(interfaceType.IsInterface);
        }

        #endregion

        #region Entity Tests

        [Fact]
        public void Core_ShouldHave_ReservationEntity()
        {
            // Arrange
            var coreAssembly = typeof(TravelBridge.Core.Services.PricingConfig).Assembly;

            // Act
            var entityType = coreAssembly.GetType("TravelBridge.Core.Entities.ReservationEntity");

            // Assert
            Assert.NotNull(entityType);
            Assert.False(entityType.IsAbstract);
        }

        [Fact]
        public void ReservationEntity_ShouldInherit_EntityBase()
        {
            // Arrange
            var reservationEntity = typeof(TravelBridge.Core.Entities.ReservationEntity);
            var entityBase = typeof(TravelBridge.Core.Entities.EntityBase);

            // Assert
            Assert.True(reservationEntity.IsSubclassOf(entityBase));
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void WebHotelierPropertiesService_Implements_IHotelProvider()
        {
            // Arrange
            var serviceType = typeof(TravelBridge.API.Services.WebHotelier.WebHotelierPropertiesService);

            // Assert
            Assert.True(typeof(IHotelProvider).IsAssignableFrom(serviceType));
        }

        [Fact]
        public void VivaService_Implements_IPaymentProvider()
        {
            // Arrange
            var serviceType = typeof(TravelBridge.API.Services.Viva.VivaService);

            // Assert
            Assert.True(typeof(IPaymentProvider).IsAssignableFrom(serviceType));
        }

        [Fact]
        public void SmtpEmailSender_Implements_IEmailService()
        {
            // Arrange
            var serviceType = typeof(TravelBridge.API.Services.SmtpEmailSender);

            // Assert
            Assert.True(typeof(IEmailService).IsAssignableFrom(serviceType));
        }

        [Fact]
        public void PricingService_Implements_IPricingService()
        {
            // Arrange
            var serviceType = typeof(TravelBridge.API.Services.PricingService);

            // Assert
            Assert.True(typeof(IPricingService).IsAssignableFrom(serviceType));
        }

        #endregion

        #region Endpoint Extensions Tests

        [Fact]
        public void EndpointExtensions_Exists()
        {
            // Arrange
            var extensionsType = typeof(TravelBridge.API.Helpers.Extensions.EndpointExtensions);

            // Assert
            Assert.NotNull(extensionsType);
            Assert.True(extensionsType.IsClass);
            Assert.True(extensionsType.IsAbstract); // Static class
            Assert.True(extensionsType.IsSealed);   // Static class
        }

        [Fact]
        public void EndpointExtensions_HasMapApiEndpointsMethod()
        {
            // Arrange
            var extensionsType = typeof(TravelBridge.API.Helpers.Extensions.EndpointExtensions);

            // Act
            var method = extensionsType.GetMethod("MapApiEndpoints");

            // Assert
            Assert.NotNull(method);
            Assert.True(method.IsStatic);
            Assert.True(method.IsPublic);
        }

        #endregion
    }
}
