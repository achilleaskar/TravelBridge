namespace TravelBridge.Core.Interfaces
{
    /// <summary>
    /// Interface for payment provider operations.
    /// Implementations: VivaService, future: Stripe, PayPal, etc.
    /// </summary>
    public interface IPaymentProvider
    {
        /// <summary>
        /// Gets the unique provider identifier.
        /// </summary>
        int ProviderId { get; }

        /// <summary>
        /// Gets the provider name for display purposes.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Creates a payment order and returns the order/checkout code.
        /// </summary>
        /// <param name="request">Payment request details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The payment order code for redirecting to payment page.</returns>
        Task<string> CreatePaymentOrderAsync(PaymentOrderRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a completed payment transaction.
        /// </summary>
        /// <param name="orderCode">The order code from payment creation.</param>
        /// <param name="transactionId">The transaction ID from payment callback.</param>
        /// <param name="expectedAmount">The expected payment amount.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with transaction details.</returns>
        Task<PaymentValidationResult> ValidatePaymentAsync(string orderCode, string transactionId, decimal expectedAmount, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Request to create a payment order.
    /// </summary>
    public class PaymentOrderRequest
    {
        /// <summary>
        /// Amount to charge in the smallest currency unit (e.g., cents).
        /// </summary>
        public required long Amount { get; init; }

        /// <summary>
        /// Customer email for receipt.
        /// </summary>
        public required string CustomerEmail { get; init; }

        /// <summary>
        /// Customer full name.
        /// </summary>
        public required string CustomerFullName { get; init; }

        /// <summary>
        /// Customer phone (optional).
        /// </summary>
        public string? CustomerPhone { get; init; }

        /// <summary>
        /// Payment description/reference.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Merchant reference (e.g., reservation ID).
        /// </summary>
        public string? MerchantReference { get; init; }

        /// <summary>
        /// URL to redirect on successful payment.
        /// </summary>
        public string? SuccessUrl { get; init; }

        /// <summary>
        /// URL to redirect on failed payment.
        /// </summary>
        public string? FailureUrl { get; init; }
    }

    /// <summary>
    /// Result of payment validation.
    /// </summary>
    public class PaymentValidationResult
    {
        /// <summary>
        /// Whether the payment is valid and successful.
        /// </summary>
        public required bool IsValid { get; init; }

        /// <summary>
        /// The validated order code.
        /// </summary>
        public string? OrderCode { get; init; }

        /// <summary>
        /// The transaction ID.
        /// </summary>
        public string? TransactionId { get; init; }

        /// <summary>
        /// The actual amount charged.
        /// </summary>
        public decimal? Amount { get; init; }

        /// <summary>
        /// Payment status from provider.
        /// </summary>
        public string? Status { get; init; }

        /// <summary>
        /// Error message if validation failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static PaymentValidationResult Success(string orderCode, string transactionId, decimal amount, string status)
            => new()
            {
                IsValid = true,
                OrderCode = orderCode,
                TransactionId = transactionId,
                Amount = amount,
                Status = status
            };

        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        public static PaymentValidationResult Failure(string errorMessage)
            => new()
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
    }
}
