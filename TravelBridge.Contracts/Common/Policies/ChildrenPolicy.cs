namespace TravelBridge.Contracts.Common.Policies
{
    public class ChildrenPolicy
    {
        [JsonPropertyName("allowed")]
        public byte Allowed { get; set; }

        [JsonPropertyName("age_from")]
        public int AgeFrom { get; set; }

        [JsonPropertyName("age_to")]
        public int AgeTo { get; set; }

        [JsonPropertyName("policy")]
        public string Policy { get; set; }
    }
}
