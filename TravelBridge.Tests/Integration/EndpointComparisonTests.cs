using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TravelBridge.Tests.Integration;

/// <summary>
/// Integration tests to verify refactored endpoints return same results as production.
/// Run these tests manually to compare local vs production responses.
/// 
/// Prerequisites:
/// 1. Start local API: cd TravelBridge.API && dotnet run
/// 2. Run these tests: dotnet test TravelBridge.Tests --logger "console;verbosity=detailed"
/// </summary>
[TestClass]
public class EndpointComparisonTests
{
    private const string ProductionBaseUrl = "https://tb.codepulse.gr";
    private const string LocalBaseUrl = "http://localhost:5299";
    
    private static readonly HttpClient _productionClient = new() { BaseAddress = new Uri(ProductionBaseUrl), Timeout = TimeSpan.FromSeconds(120) };
    private static readonly HttpClient _localClient = new() { BaseAddress = new Uri(LocalBaseUrl), Timeout = TimeSpan.FromSeconds(120) };
    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    #region Helper Methods

    /// <summary>
    /// Compares two JSON strings and returns detailed differences
    /// </summary>
    private static (bool areEqual, string differences, int diffCount) CompareJsonResponses(string prodJson, string localJson)
    {
        if (prodJson == localJson)
            return (true, string.Empty, 0);

        var differences = new System.Text.StringBuilder();
        int diffCount = 0;
        
        try
        {
            using var prodDoc = JsonDocument.Parse(prodJson);
            using var localDoc = JsonDocument.Parse(localJson);
            
            CompareJsonElements(prodDoc.RootElement, localDoc.RootElement, "$", differences, ref diffCount, maxDiffs: 20);
        }
        catch (Exception ex)
        {
            differences.AppendLine($"Error parsing JSON: {ex.Message}");
            differences.AppendLine($"Production JSON length: {prodJson.Length}");
            differences.AppendLine($"Local JSON length: {localJson.Length}");
        }

        return (false, differences.ToString(), diffCount);
    }

    private static void CompareJsonElements(JsonElement prod, JsonElement local, string path, System.Text.StringBuilder differences, ref int diffCount, int maxDiffs)
    {
        if (diffCount >= maxDiffs)
        {
            if (diffCount == maxDiffs)
            {
                differences.AppendLine($"... and more differences (stopped at {maxDiffs})");
                diffCount++;
            }
            return;
        }

        if (prod.ValueKind != local.ValueKind)
        {
            differences.AppendLine($"❌ TYPE MISMATCH at {path}: Production={prod.ValueKind}, Local={local.ValueKind}");
            diffCount++;
            return;
        }

        switch (prod.ValueKind)
        {
            case JsonValueKind.Object:
                var prodProps = prod.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                var localProps = local.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                
                foreach (var prop in prodProps.Keys.Union(localProps.Keys).Distinct().OrderBy(x => x))
                {
                    if (diffCount >= maxDiffs) break;
                    
                    var newPath = $"{path}.{prop}";
                    if (!prodProps.ContainsKey(prop))
                    {
                        differences.AppendLine($"➕ EXTRA IN LOCAL at {newPath}");
                        diffCount++;
                    }
                    else if (!localProps.ContainsKey(prop))
                    {
                        differences.AppendLine($"➖ MISSING IN LOCAL at {newPath}");
                        diffCount++;
                    }
                    else
                    {
                        CompareJsonElements(prodProps[prop], localProps[prop], newPath, differences, ref diffCount, maxDiffs);
                    }
                }
                break;

            case JsonValueKind.Array:
                var prodArray = prod.EnumerateArray().ToList();
                var localArray = local.EnumerateArray().ToList();
                
                if (prodArray.Count != localArray.Count)
                {
                    differences.AppendLine($"📊 ARRAY LENGTH at {path}: Production={prodArray.Count}, Local={localArray.Count}");
                    diffCount++;
                }
                
                var minCount = Math.Min(prodArray.Count, localArray.Count);
                for (int i = 0; i < minCount && diffCount < maxDiffs; i++)
                {
                    CompareJsonElements(prodArray[i], localArray[i], $"{path}[{i}]", differences, ref diffCount, maxDiffs);
                }
                break;

            case JsonValueKind.String:
                var prodStr = prod.GetString();
                var localStr = local.GetString();
                if (prodStr != localStr)
                {
                    differences.AppendLine($"📝 STRING at {path}:");
                    differences.AppendLine($"   PROD: \"{Truncate(prodStr, 80)}\"");
                    differences.AppendLine($"   LOCAL: \"{Truncate(localStr, 80)}\"");
                    diffCount++;
                }
                break;

            case JsonValueKind.Number:
                var prodNum = prod.GetDecimal();
                var localNum = local.GetDecimal();
                if (prodNum != localNum)
                {
                    differences.AppendLine($"🔢 NUMBER at {path}: Production={prodNum}, Local={localNum}");
                    diffCount++;
                }
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                if (prod.GetBoolean() != local.GetBoolean())
                {
                    differences.AppendLine($"✓✗ BOOLEAN at {path}: Production={prod.GetBoolean()}, Local={local.GetBoolean()}");
                    diffCount++;
                }
                break;
        }
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (value == null) return "(null)";
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    private async Task<(string prodJson, string localJson)> GetRawResponses(string endpoint)
    {
        Console.WriteLine($"Testing endpoint: {endpoint}");
        
        var prodTask = _productionClient.GetStringAsync(endpoint);
        var localTask = _localClient.GetStringAsync(endpoint);
        
        await Task.WhenAll(prodTask, localTask);
        
        return (await prodTask, await localTask);
    }

    private void AssertJsonEqual(string prodJson, string localJson, string testName)
    {
        var (areEqual, differences, diffCount) = CompareJsonResponses(prodJson, localJson);
        
        Console.WriteLine($"\n{'=',-60}");
        Console.WriteLine($"TEST: {testName}");
        Console.WriteLine($"Production JSON size: {prodJson.Length:N0} bytes");
        Console.WriteLine($"Local JSON size: {localJson.Length:N0} bytes");
        Console.WriteLine($"{'=',-60}");
        
        if (areEqual)
        {
            Console.WriteLine($"✅ PASS - JSON responses match exactly!");
        }
        else
        {
            Console.WriteLine($"❌ FAIL - Found {diffCount}+ differences:\n");
            Console.WriteLine(differences);
            
            Assert.Fail($"JSON responses don't match. Found {diffCount}+ differences. See test output for details.");
        }
    }

    #endregion

    #region Autocomplete Tests

    [TestMethod]
    public async Task Autocomplete_Samos_ShouldMatchProduction()
    {
        var endpoint = "/api/plugin/autocomplete?searchQuery=Samos";
        var (prodJson, localJson) = await GetRawResponses(endpoint);
        AssertJsonEqual(prodJson, localJson, "Autocomplete_Samos");
    }

    [TestMethod]
    public async Task Autocomplete_Athens_ShouldMatchProduction()
    {
        var endpoint = "/api/plugin/autocomplete?searchQuery=Athens";
        var (prodJson, localJson) = await GetRawResponses(endpoint);
        AssertJsonEqual(prodJson, localJson, "Autocomplete_Athens");
    }

    [TestMethod]
    public async Task Autocomplete_Crete_ShouldMatchProduction()
    {
        var endpoint = "/api/plugin/autocomplete?searchQuery=Crete";
        var (prodJson, localJson) = await GetRawResponses(endpoint);
        AssertJsonEqual(prodJson, localJson, "Autocomplete_Crete");
    }

    #endregion

    #region SubmitSearch Tests

    [TestMethod]
    public async Task SubmitSearch_Athens_ShouldMatchProduction()
    {
        var checkin = "15/06/2026";
        var checkout = "20/06/2026";
        var bbox = "[23.686932,37.948804,23.790157,38.032552]-37.97757-23.729275";
        var encodedBbox = Uri.EscapeDataString(bbox);
        
        var endpoint = $"/api/plugin/submitSearch?checkin={checkin}&checkOut={checkout}&bbox={encodedBbox}&adults=2&rooms=1&searchTerm=Athens";
        var (prodJson, localJson) = await GetRawResponses(endpoint);
        AssertJsonEqual(prodJson, localJson, "SubmitSearch_Athens");
    }

    [TestMethod]
    public async Task SubmitSearch_Crete_ShouldMatchProduction()
    {
        var checkin = "15/06/2026";
        var checkout = "20/06/2026";
        var bbox = "[23.377258,34.730628,26.447346,35.773147]-35.340013-25.134348";
        var encodedBbox = Uri.EscapeDataString(bbox);
        
        var endpoint = $"/api/plugin/submitSearch?checkin={checkin}&checkOut={checkout}&bbox={encodedBbox}&adults=2&rooms=1&searchTerm=Crete";
        var (prodJson, localJson) = await GetRawResponses(endpoint);
        AssertJsonEqual(prodJson, localJson, "SubmitSearch_Crete");
    }

    #endregion

    #region Hotel Full Info Tests

    [TestMethod]
    public async Task HotelFullInfo_ATLASKIATH_ShouldMatchProduction()
    {
        var endpoint = "/api/hotel/HotelFullInfo?checkIn=25/02/2026&checkOut=28/02/2026&adults=2&children=0&rooms=1&party=&hotelId=1-ATLASKIATH";
        var (prodJson, localJson) = await GetRawResponses(endpoint);
        AssertJsonEqual(prodJson, localJson, "HotelFullInfo_ATLASKIATH");
    }

    [TestMethod]
    public async Task HotelFullInfo_VAROSVILL_ShouldMatchProduction()
    {
        var endpoint = "/api/hotel/HotelFullInfo?checkIn=15/06/2026&checkOut=20/06/2026&adults=2&children=0&rooms=1&party=&hotelId=1-VAROSVILL";
        var (prodJson, localJson) = await GetRawResponses(endpoint);
        AssertJsonEqual(prodJson, localJson, "HotelFullInfo_VAROSVILL");
    }

    #endregion

    #region Room Info Tests

    [TestMethod]
    public async Task RoomInfo_ATLASKIATH_STDBL_ShouldMatchProduction()
    {
        var endpoint = "/api/hotel/roomInfo?HotelId=1-ATLASKIATH&roomId=STDBL";
        var (prodJson, localJson) = await GetRawResponses(endpoint);
        AssertJsonEqual(prodJson, localJson, "RoomInfo_ATLASKIATH_STDBL");
    }

    [TestMethod]
    public async Task RoomInfo_VAROSVILL_ShouldMatchProduction()
    {
        // Use LGDBL which is a valid room code for VAROSVILL (from availability response)
        var endpoint = "/api/hotel/roomInfo?HotelId=1-VAROSVILL&roomId=LGDBL";
        var (prodJson, localJson) = await GetRawResponses(endpoint);
        AssertJsonEqual(prodJson, localJson, "RoomInfo_VAROSVILL_LGDBL");
    }

    #endregion

    #region Availability Tests

    [TestMethod]
    public async Task Availability_VAROSVILL_ShouldMatchProduction()
    {
        var checkin = "15/06/2026";
        var checkout = "20/06/2026";
        
        var endpoint = $"/api/hotel/hotelRoomAvailability?hotelId=1-VAROSVILL&checkin={checkin}&checkOut={checkout}&adults=2&rooms=1";
        var (prodJson, localJson) = await GetRawResponses(endpoint);
        AssertJsonEqual(prodJson, localJson, "Availability_VAROSVILL");
    }

    [TestMethod]
    public async Task Availability_ATLASKIATH_ShouldMatchProduction()
    {
        var endpoint = "/api/hotel/hotelRoomAvailability?hotelId=1-ATLASKIATH&checkin=25/02/2026&checkOut=28/02/2026&adults=2&rooms=1";
        var (prodJson, localJson) = await GetRawResponses(endpoint);
        AssertJsonEqual(prodJson, localJson, "Availability_ATLASKIATH");
    }

    #endregion
}
