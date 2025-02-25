namespace TravelBridge.API.Helpers
{
    public static class General
    {
        public static string CreateParty(int adults, string? children)
        {
            // Check if children is null or empty and build JSON accordingly
            if (string.IsNullOrWhiteSpace(children))
            {
                return $"[{{\"adults\":{adults}}}]";
            }
            else
            {
                return $"[{{\"adults\":{adults},\"children\":[{children}]}}]";
            }
        }
    }
}
