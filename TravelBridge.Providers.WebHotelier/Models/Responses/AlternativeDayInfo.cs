namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    public class AlternativeDayInfo
    {
        public string date { get; set; }
        public DateTime dateOnly { get; set; }
        //public string rm_type { get; set; }
        //public int rate_id { get; set; }
        public string status { get; set; }
        public decimal price { get; set; }
        //public int excluded_charges { get; set; }
        public decimal retail { get; set; }
        //public int discount { get; set; }
        //public string currency { get; set; }
        public int min_stay { get; set; }
        //public string rm_name { get; set; }
        //public string rate_name { get; set; }
        //public string board_id { get; set; }
    }
}
