using System.Text.Json.Serialization;

namespace TravelBridge.Contracts.Contracts.Responses
{
    public class SuccessfulPaymentResponse : BaseWebHotelierResponse
    {
        public SuccessfulPaymentResponse()
        {
        }

        public SuccessfulPaymentResponse(string error, string errorCode)
        {
            ErrorCode = errorCode;
            ErrorMessage = error;
        }

        public DataSuccess Data { get; set; }

        /// <summary>
        /// Keep the old JSON property name "successfullPayment" (with double 'l') for backward compatibility with WordPress plugin.
        /// </summary>
        [JsonPropertyName("successfullPayment")]
        public bool SuccessfulPayment { get; set; }
    }
}
