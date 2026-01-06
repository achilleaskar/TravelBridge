namespace TravelBridge.Providers.Abstractions;

/// <summary>
/// A composite identifier that combines a provider ID with a provider-specific value.
/// Format: "{providerId}-{value}" where providerId is an integer.
/// Examples: "1-VAROSRESID", "0-123", "1-A-B-C" (value can contain dashes).
/// </summary>
/// <remarks>
/// This type uses fast parsing via IndexOf (no string.Split) to minimize allocations.
/// </remarks>
public readonly record struct CompositeId
{
    /// <summary>
    /// The provider identifier (e.g., 0 for Owned, 1 for WebHotelier).
    /// </summary>
    public int ProviderId { get; }

    /// <summary>
    /// The provider-specific value (the part after the first dash).
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new CompositeId with the specified provider ID and value.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="value">The provider-specific value.</param>
    /// <exception cref="ArgumentException">Thrown when value is null or empty.</exception>
    public CompositeId(int providerId, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(value));
        }

        ProviderId = providerId;
        Value = value;
    }

    /// <summary>
    /// Attempts to parse a composite ID from a string.
    /// </summary>
    /// <param name="input">The input string in format "{providerId}-{value}".</param>
    /// <param name="id">When this method returns, contains the parsed CompositeId if successful.</param>
    /// <returns>true if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string? input, out CompositeId id)
    {
        id = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Find the first dash - split only on the first dash
        int dashIndex = input.IndexOf('-');
        if (dashIndex <= 0) // Must have at least one character before the dash
        {
            return false;
        }

        if (dashIndex >= input.Length - 1) // Must have at least one character after the dash
        {
            return false;
        }

        // Parse the provider ID (before the dash)
        ReadOnlySpan<char> providerSpan = input.AsSpan(0, dashIndex);
        if (!int.TryParse(providerSpan, out int providerId))
        {
            return false;
        }

        // Extract the value (after the dash)
        string value = input.Substring(dashIndex + 1);
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        id = new CompositeId(providerId, value);
        return true;
    }

    /// <summary>
    /// Parses a composite ID from a string.
    /// </summary>
    /// <param name="input">The input string in format "{providerId}-{value}".</param>
    /// <returns>The parsed CompositeId.</returns>
    /// <exception cref="ArgumentException">Thrown when the input format is invalid.</exception>
    public static CompositeId Parse(string? input)
    {
        if (!TryParse(input, out var id))
        {
            throw new ArgumentException(
                $"Invalid composite ID format: '{input}'. Expected format: '{{providerId}}-{{value}}' where providerId is an integer.",
                nameof(input));
        }

        return id;
    }

    /// <summary>
    /// Returns the string representation of the composite ID in format "{providerId}-{value}".
    /// </summary>
    public override string ToString() => $"{ProviderId}-{Value}";
}
