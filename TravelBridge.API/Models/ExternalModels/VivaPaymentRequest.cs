namespace TravelBridge.API.Models.ExternalModels
{
    public class VivaPaymentRequest
    {
        public int Amount { get; set; }  // Amount in cents (e.g., 1000 = €10.00)
        public string CustomerTrns { get; set; }
        public VivaCustomer Customer { get; set; }
        public string SourceCode { get; set; }
        public string MerchantTrns { get; set; }
        public int PaymentTimeout { get; } = 300;
        public string DynamicDescriptor { get;} = "My Diakopes";
        public List<string> Tags { get; internal  set; }

        // Constructor (optional)
        public VivaPaymentRequest()
        {
            Tags = new List<string>() { "my-diakopes tag"};
        }
    }

    public class VivaCustomer
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string CountryCode { get; set; }
        public string RequestLang { get; set; }

        // Constructor (optional)
        public VivaCustomer() { }
    }
}
