using TravelBridge.API.Models.WebHotelier;

namespace TravelBridge.API.Contracts
{
    public class SuccessfullPaymentResponse : BaseWebHotelierResponse
    {
        public SuccessfullPaymentResponse()
        {

        }

        public SuccessfullPaymentResponse(string error, string errorCode)
        {
            ErrorCode = errorCode;
            ErrorMessage = error;
        }

        public DataSucess Data { get; set; }

        public bool SuccessfullPayment { get; set; }
    }
    public class DataSucess
    {
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
        public int ReservationId { get; set; }

        public string HotelName { get; set; }
    }
}
