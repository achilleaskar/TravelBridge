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

        public bool SuccessfulPayment { get; set; }
    }
}
