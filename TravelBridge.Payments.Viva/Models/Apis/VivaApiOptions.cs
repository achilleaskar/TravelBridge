namespace TravelBridge.Payments.Viva.Models.Apis
{
    public class VivaApiOptions : BaseApiOptions
    {
        public string AuthUrl { get; set; }
        public string ApiSecret { get; set; }
        public string SourceCode { get; set; }
        public string SourceCodeTravelProject { get; set; }
    }
}
