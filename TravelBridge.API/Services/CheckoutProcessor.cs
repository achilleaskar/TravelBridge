using System.Globalization;
using TravelBridge.API.Contracts;
using TravelBridge.API.Helpers;
using TravelBridge.Contracts.Models.Hotels;

namespace TravelBridge.API.Services;

/// <summary>
/// Handles checkout-related business logic for payment calculations and room pricing.
/// Extracted from CheckoutResponse DTO to keep it as a pure data container.
/// </summary>
public static class CheckoutProcessor
{
    /// <summary>
    /// Calculates and merges payments for the checkout response.
    /// Updates TotalPrice, Payments, and PartialPayment on the response.
    /// </summary>
    /// <param name="response">The checkout response to process</param>
    public static void CalculatePayments(CheckoutResponse response)
    {
        if (!DateTime.TryParseExact(response.CheckIn, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime checkinDate))
        {
            throw new InvalidOperationException("Invalid check-in date format.");
        }

        response.TotalPrice = response.Rooms.Sum(r => r.TotalPrice);
        response.Payments = response.Rooms.SelectMany(r => r.RateProperties.Payments).ToList();
        response.PartialPayment = General.FillPartialPayment(response.Payments, checkinDate);
        response.Payments = [];
        
        if (response.PartialPayment != null && 
            (response.PartialPayment.prepayAmount + response.PartialPayment.nextPayments.Sum(a => a.Amount)) != response.TotalPrice)
        {
            throw new InvalidOperationException("Payments calculation failure.");
        }
    }
}
