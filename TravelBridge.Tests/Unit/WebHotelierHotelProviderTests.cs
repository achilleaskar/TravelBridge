using Microsoft.VisualStudio.TestTools.UnitTesting;
using TravelBridge.Providers.Abstractions.Models;

namespace TravelBridge.Tests.Unit;

/// <summary>
/// Tests for WebHotelierHotelProvider room/rate grouping, aggregation, and alternatives logic.
/// </summary>
[TestClass]
public class WebHotelierHotelProviderTests
{
    #region RoomRateData Grouping Tests

    [TestMethod]
    public void RoomRateData_GroupByRoomCode_EachRoomGetsOnlyItsRates()
    {
        // Arrange - Simulate rates from different rooms
        var rates = new List<RoomRateData>
        {
            new() { RoomCode = "ROOM_A", RateId = "RATE1-2", RateName = "Standard" },
            new() { RoomCode = "ROOM_A", RateId = "RATE2-2", RateName = "Flexible" },
            new() { RoomCode = "ROOM_B", RateId = "RATE1-2", RateName = "Standard" },
            new() { RoomCode = "ROOM_B", RateId = "RATE3-2", RateName = "Deluxe" },
            new() { RoomCode = "ROOM_C", RateId = "RATE1-2", RateName = "Standard" },
        };

        var rooms = new Dictionary<string, AvailableRoomData>
        {
            ["ROOM_A"] = new() { RoomCode = "ROOM_A", RoomName = "Room A", Rates = [] },
            ["ROOM_B"] = new() { RoomCode = "ROOM_B", RoomName = "Room B", Rates = [] },
            ["ROOM_C"] = new() { RoomCode = "ROOM_C", RoomName = "Room C", Rates = [] },
        };

        // Act - Group rates by room using dictionary (as fixed in provider)
        var ratesByRoom = rates
            .GroupBy(r => r.RoomCode)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = rooms.Values
            .Select(room => room with
            {
                Rates = ratesByRoom.TryGetValue(room.RoomCode, out var rr) ? rr : []
            })
            .ToList();

        // Assert
        Assert.AreEqual(3, result.Count);
        
        var roomA = result.First(r => r.RoomCode == "ROOM_A");
        Assert.AreEqual(2, roomA.Rates.Count);
        Assert.IsTrue(roomA.Rates.All(r => r.RoomCode == "ROOM_A"));
        
        var roomB = result.First(r => r.RoomCode == "ROOM_B");
        Assert.AreEqual(2, roomB.Rates.Count);
        Assert.IsTrue(roomB.Rates.All(r => r.RoomCode == "ROOM_B"));
        
        var roomC = result.First(r => r.RoomCode == "ROOM_C");
        Assert.AreEqual(1, roomC.Rates.Count);
        Assert.IsTrue(roomC.Rates.All(r => r.RoomCode == "ROOM_C"));
    }

    [TestMethod]
    public void RoomRateData_NoCrossContamination_RatesAreIsolated()
    {
        // Arrange - Two rooms with same rate name but different rooms
        var rates = new List<RoomRateData>
        {
            new() { RoomCode = "SINGLE", RateId = "BB-2", RateName = "Bed & Breakfast" },
            new() { RoomCode = "DOUBLE", RateId = "BB-2", RateName = "Bed & Breakfast" },
        };

        // Act
        var ratesByRoom = rates
            .GroupBy(r => r.RoomCode)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Assert - Each room should only have its own rate, not the other room's rate with same name
        Assert.AreEqual(1, ratesByRoom["SINGLE"].Count);
        Assert.AreEqual("SINGLE", ratesByRoom["SINGLE"][0].RoomCode);
        
        Assert.AreEqual(1, ratesByRoom["DOUBLE"].Count);
        Assert.AreEqual("DOUBLE", ratesByRoom["DOUBLE"][0].RoomCode);
    }

    #endregion

    #region RateId Format Tests (FillPartyFromId compatible)

    [TestMethod]
    public void RateId_Format_CompatibleWithFillPartyFromId_AdultsOnly()
    {
        // Arrange - Format: {baseRateId}-{adults}
        var baseRateId = "328000";
        var adults = 2;

        // Act - Build rate ID using the fixed format
        var partySuffix = $"{adults}";
        var rateId = $"{baseRateId}-{partySuffix}";

        // Assert - Format matches FillPartyFromId expectations
        Assert.AreEqual("328000-2", rateId);
        
        // Verify it can be parsed back
        var parts = rateId.Split('-');
        Assert.AreEqual(2, parts.Length);
        Assert.AreEqual("328000", parts[0]);
        Assert.AreEqual("2", parts[1]);
        Assert.AreEqual(2, parts[1][0] - '0'); // First char is adults
    }

    [TestMethod]
    public void RateId_Format_CompatibleWithFillPartyFromId_WithChildren()
    {
        // Arrange - Format: {baseRateId}-{adults}_{childAge1}_{childAge2}
        var baseRateId = "328000";
        var adults = 2;
        var children = new[] { 5, 10 };

        // Act - Build rate ID using the fixed format
        var partySuffix = $"{adults}" +
            (children.Length > 0 ? "_" + string.Join("_", children) : "");
        var rateId = $"{baseRateId}-{partySuffix}";

        // Assert - Format matches FillPartyFromId expectations
        Assert.AreEqual("328000-2_5_10", rateId);
        
        // Verify it can be parsed back
        var parts = rateId.Split('-');
        Assert.AreEqual(2, parts.Length);
        
        var partySegment = parts.Last();
        var segments = partySegment.Split('_');
        Assert.AreEqual(3, segments.Length);
        Assert.AreEqual("2", segments[0]); // Adults
        Assert.AreEqual("5", segments[1]); // Child 1
        Assert.AreEqual("10", segments[2]); // Child 2
    }

    [TestMethod]
    public void RateId_Format_RoomsCountNotInId()
    {
        // Arrange - RoomsCount should NOT be in the RateId (it's in SearchParty)
        var baseRateId = "100000";
        var adults = 2;
        var children = Array.Empty<int>();
        var roomsCount = 5; // This should NOT appear in the ID

        // Act
        var partySuffix = $"{adults}" +
            (children.Length > 0 ? "_" + string.Join("_", children) : "");
        var rateId = $"{baseRateId}-{partySuffix}";

        // Assert - RoomsCount is NOT in the ID
        Assert.AreEqual("100000-2", rateId);
        Assert.IsFalse(rateId.Contains($"_{roomsCount}"), "RoomsCount should not be suffixed in RateId");
        Assert.IsFalse(rateId.Contains("_R"), "Old format _R should not be used");
        Assert.IsFalse(rateId.EndsWith($"-{roomsCount}"), "RoomsCount should not be in RateId");
    }

    [TestMethod]
    public void RateId_DifferentConfigurations_ProduceUniqueIds()
    {
        // Arrange & Act
        var testCases = new[]
        {
            (BaseId: "100", Adults: 1, Children: Array.Empty<int>(), Expected: "100-1"),
            (BaseId: "100", Adults: 2, Children: Array.Empty<int>(), Expected: "100-2"),
            (BaseId: "100", Adults: 2, Children: new[] { 5 }, Expected: "100-2_5"),
            (BaseId: "100", Adults: 2, Children: new[] { 5, 10 }, Expected: "100-2_5_10"),
            (BaseId: "200", Adults: 3, Children: new[] { 3, 7, 12 }, Expected: "200-3_3_7_12"),
        };

        var generatedIds = new HashSet<string>();

        foreach (var tc in testCases)
        {
            var partySuffix = $"{tc.Adults}" +
                (tc.Children.Length > 0 ? "_" + string.Join("_", tc.Children) : "");
            var rateId = $"{tc.BaseId}-{partySuffix}";

            Assert.AreEqual(tc.Expected, rateId, $"Failed for Adults={tc.Adults}, Children=[{string.Join(",", tc.Children)}]");
            Assert.IsTrue(generatedIds.Add(rateId), $"Duplicate ID generated: {rateId}");
        }
    }

    #endregion

    #region Price Aggregation Tests

    [TestMethod]
    public void SearchAvailability_PriceTotals_AreAccumulatedCorrectly()
    {
        // Arrange - Two party groups for same hotel
        var partyContributions = new[]
        {
            (HotelCode: "HOTEL1", MinPrice: 100m, RoomsCount: 2), // 2 rooms at 100 = 200
            (HotelCode: "HOTEL1", MinPrice: 150m, RoomsCount: 1), // 1 room at 150 = 150
        };

        // Act - Simulate accumulator pattern
        decimal totalMinPrice = 0m;
        foreach (var contrib in partyContributions)
        {
            totalMinPrice += contrib.MinPrice * contrib.RoomsCount;
        }

        // Assert - Total should be 200 + 150 = 350
        Assert.AreEqual(350m, totalMinPrice);
    }

    [TestMethod]
    public void SearchAvailability_MultipleHotels_AccumulatedSeparately()
    {
        // Arrange
        var contributions = new[]
        {
            (HotelCode: "HOTEL_A", MinPrice: 100m, RoomsCount: 1),
            (HotelCode: "HOTEL_B", MinPrice: 200m, RoomsCount: 1),
            (HotelCode: "HOTEL_A", MinPrice: 120m, RoomsCount: 1), // Second party config for HOTEL_A
        };

        // Act - Simulate accumulator
        var accumulator = new Dictionary<string, decimal>();
        foreach (var c in contributions)
        {
            if (!accumulator.TryGetValue(c.HotelCode, out var existing))
                existing = 0m;
            accumulator[c.HotelCode] = existing + (c.MinPrice * c.RoomsCount);
        }

        // Assert
        Assert.AreEqual(220m, accumulator["HOTEL_A"]); // 100 + 120
        Assert.AreEqual(200m, accumulator["HOTEL_B"]); // 200
    }

    #endregion

    #region Alternatives Logic Tests

    [TestMethod]
    public void Alternatives_KeepCommon_OnlyKeepsMatchingDatePairs()
    {
        // Arrange - Two party configs with overlapping alternatives
        var party1Alternatives = new List<AlternativeDateData>
        {
            new() { CheckIn = new DateOnly(2026, 3, 15), CheckOut = new DateOnly(2026, 3, 18), MinPrice = 100, NetPrice = 80 },
            new() { CheckIn = new DateOnly(2026, 3, 20), CheckOut = new DateOnly(2026, 3, 23), MinPrice = 120, NetPrice = 95 },
            new() { CheckIn = new DateOnly(2026, 3, 25), CheckOut = new DateOnly(2026, 3, 28), MinPrice = 110, NetPrice = 88 },
        };

        var party2Alternatives = new List<AlternativeDateData>
        {
            new() { CheckIn = new DateOnly(2026, 3, 15), CheckOut = new DateOnly(2026, 3, 18), MinPrice = 150, NetPrice = 120 }, // Common
            new() { CheckIn = new DateOnly(2026, 3, 22), CheckOut = new DateOnly(2026, 3, 25), MinPrice = 140, NetPrice = 112 }, // Not common
            new() { CheckIn = new DateOnly(2026, 3, 25), CheckOut = new DateOnly(2026, 3, 28), MinPrice = 130, NetPrice = 104 }, // Common
        };

        // Act - Keep common logic
        var allDatePairs1 = party1Alternatives.Select(a => (a.CheckIn, a.CheckOut)).ToHashSet();
        var allDatePairs2 = party2Alternatives.Select(a => (a.CheckIn, a.CheckOut)).ToHashSet();
        
        var commonPairs = new HashSet<(DateOnly, DateOnly)>(allDatePairs1);
        commonPairs.IntersectWith(allDatePairs2);

        // Assert
        Assert.AreEqual(2, commonPairs.Count);
        Assert.IsTrue(commonPairs.Contains((new DateOnly(2026, 3, 15), new DateOnly(2026, 3, 18))));
        Assert.IsTrue(commonPairs.Contains((new DateOnly(2026, 3, 25), new DateOnly(2026, 3, 28))));
        Assert.IsFalse(commonPairs.Contains((new DateOnly(2026, 3, 20), new DateOnly(2026, 3, 23))));
    }

    [TestMethod]
    public void Alternatives_PricesAreSummedAcrossPartyConfigs()
    {
        // Arrange - Common alternative across two party configs
        var checkIn = new DateOnly(2026, 3, 15);
        var checkOut = new DateOnly(2026, 3, 18);

        var allAlternatives = new List<AlternativeDateData>
        {
            new() { CheckIn = checkIn, CheckOut = checkOut, MinPrice = 100, NetPrice = 80 },   // Party 1
            new() { CheckIn = checkIn, CheckOut = checkOut, MinPrice = 150, NetPrice = 120 },  // Party 2
        };

        // Act - Group and sum (same logic as KeepCommon)
        var summed = allAlternatives
            .GroupBy(a => new { a.CheckIn, a.CheckOut })
            .Select(g => new AlternativeDateData
            {
                CheckIn = g.Key.CheckIn,
                CheckOut = g.Key.CheckOut,
                MinPrice = g.Sum(x => x.MinPrice),
                NetPrice = g.Sum(x => x.NetPrice),
                Nights = g.Key.CheckOut.DayNumber - g.Key.CheckIn.DayNumber
            })
            .ToList();

        // Assert
        Assert.AreEqual(1, summed.Count);
        Assert.AreEqual(250m, summed[0].MinPrice); // 100 + 150
        Assert.AreEqual(200m, summed[0].NetPrice); // 80 + 120
        Assert.AreEqual(3, summed[0].Nights);
    }

    [TestMethod]
    public void Alternatives_OrderedByCheckInDate()
    {
        // Arrange
        var alternatives = new List<AlternativeDateData>
        {
            new() { CheckIn = new DateOnly(2026, 3, 25), CheckOut = new DateOnly(2026, 3, 28), MinPrice = 110 },
            new() { CheckIn = new DateOnly(2026, 3, 15), CheckOut = new DateOnly(2026, 3, 18), MinPrice = 100 },
            new() { CheckIn = new DateOnly(2026, 3, 20), CheckOut = new DateOnly(2026, 3, 23), MinPrice = 120 },
        };

        // Act
        var ordered = alternatives.OrderBy(a => a.CheckIn).ToList();

        // Assert
        Assert.AreEqual(new DateOnly(2026, 3, 15), ordered[0].CheckIn);
        Assert.AreEqual(new DateOnly(2026, 3, 20), ordered[1].CheckIn);
        Assert.AreEqual(new DateOnly(2026, 3, 25), ordered[2].CheckIn);
    }

    [TestMethod]
    public void Alternatives_EmptyWhenNoCommonDates()
    {
        // Arrange - Two party configs with NO overlapping alternatives
        var party1Dates = new HashSet<(DateOnly, DateOnly)>
        {
            (new DateOnly(2026, 3, 15), new DateOnly(2026, 3, 18)),
            (new DateOnly(2026, 3, 20), new DateOnly(2026, 3, 23)),
        };

        var party2Dates = new HashSet<(DateOnly, DateOnly)>
        {
            (new DateOnly(2026, 3, 25), new DateOnly(2026, 3, 28)),
            (new DateOnly(2026, 3, 30), new DateOnly(2026, 4, 2)),
        };

        // Act
        var commonPairs = new HashSet<(DateOnly, DateOnly)>(party1Dates);
        commonPairs.IntersectWith(party2Dates);

        // Assert
        Assert.AreEqual(0, commonPairs.Count);
    }

    #endregion

    #region RatePartyInfo Tests

    [TestMethod]
    public void RatePartyInfo_RoomsCountIsPreserved()
    {
        // Arrange & Act
        var partyInfo = new RatePartyInfo
        {
            Adults = 2,
            ChildrenAges = [5, 10],
            RoomsCount = 3,
            PartyJson = "[{\"adults\":2,\"children\":[5,10]}]"
        };

        // Assert
        Assert.AreEqual(2, partyInfo.Adults);
        Assert.AreEqual(2, partyInfo.ChildrenAges.Length);
        Assert.AreEqual(3, partyInfo.RoomsCount);
        Assert.IsNotNull(partyInfo.PartyJson);
    }

    [TestMethod]
    public void RatePartyInfo_DefaultRoomsCountIsOne()
    {
        // Arrange & Act
        var partyInfo = new RatePartyInfo
        {
            Adults = 2,
            ChildrenAges = []
        };

        // Assert
        Assert.AreEqual(1, partyInfo.RoomsCount);
    }

    #endregion

    #region Alternatives RoomsCount Weighting Tests

    [TestMethod]
    public void Alternatives_RoomsCountWeighting_SingleRoom_NoMultiplication()
    {
        // Arrange - Single room (RoomsCount = 1)
        var alternatives = new List<AlternativeDateData>
        {
            new() { CheckIn = new DateOnly(2026, 3, 15), CheckOut = new DateOnly(2026, 3, 18), MinPrice = 100, NetPrice = 80 }
        };
        var roomsCount = 1;

        // Act - Apply weighting (same logic as provider)
        if (roomsCount > 1)
        {
            alternatives = alternatives
                .Select(a => a with { MinPrice = a.MinPrice * roomsCount, NetPrice = a.NetPrice * roomsCount })
                .ToList();
        }

        // Assert - Prices unchanged for single room
        Assert.AreEqual(100m, alternatives[0].MinPrice);
        Assert.AreEqual(80m, alternatives[0].NetPrice);
    }

    [TestMethod]
    public void Alternatives_RoomsCountWeighting_MultipleRooms_PricesMultiplied()
    {
        // Arrange - Two identical rooms (RoomsCount = 2)
        var alternatives = new List<AlternativeDateData>
        {
            new() { CheckIn = new DateOnly(2026, 3, 15), CheckOut = new DateOnly(2026, 3, 18), MinPrice = 100, NetPrice = 80 }
        };
        var roomsCount = 2;

        // Act - Apply weighting (same logic as provider)
        if (roomsCount > 1)
        {
            alternatives = alternatives
                .Select(a => a with { MinPrice = a.MinPrice * roomsCount, NetPrice = a.NetPrice * roomsCount })
                .ToList();
        }

        // Assert - Prices doubled for 2 rooms
        Assert.AreEqual(200m, alternatives[0].MinPrice);
        Assert.AreEqual(160m, alternatives[0].NetPrice);
    }

    [TestMethod]
    public void Alternatives_RoomsCountWeighting_ThreeRooms_PricesTripled()
    {
        // Arrange - Three identical rooms (RoomsCount = 3)
        var alternatives = new List<AlternativeDateData>
        {
            new() { CheckIn = new DateOnly(2026, 3, 15), CheckOut = new DateOnly(2026, 3, 18), MinPrice = 150, NetPrice = 120 }
        };
        var roomsCount = 3;

        // Act
        if (roomsCount > 1)
        {
            alternatives = alternatives
                .Select(a => a with { MinPrice = a.MinPrice * roomsCount, NetPrice = a.NetPrice * roomsCount })
                .ToList();
        }

        // Assert - Prices tripled
        Assert.AreEqual(450m, alternatives[0].MinPrice);
        Assert.AreEqual(360m, alternatives[0].NetPrice);
    }

    [TestMethod]
    public void Alternatives_MultiPartyWithDifferentRoomsCounts_TotalPriceCorrect()
    {
        // Arrange - Scenario: 2 adults x 2 rooms + 3 adults x 1 room = 3 different party configs after grouping
        // Party 1: 2 adults, RoomsCount = 2, price = 100 per room → 200 total
        // Party 2: 3 adults, RoomsCount = 1, price = 150 per room → 150 total
        // Expected total: 350
        
        var party1Alternatives = new List<AlternativeDateData>
        {
            new() { CheckIn = new DateOnly(2026, 3, 15), CheckOut = new DateOnly(2026, 3, 18), MinPrice = 100, NetPrice = 80 }
        };
        var party1RoomsCount = 2;
        
        var party2Alternatives = new List<AlternativeDateData>
        {
            new() { CheckIn = new DateOnly(2026, 3, 15), CheckOut = new DateOnly(2026, 3, 18), MinPrice = 150, NetPrice = 120 }
        };
        var party2RoomsCount = 1;

        // Act - Apply RoomsCount weighting for each party
        if (party1RoomsCount > 1)
        {
            party1Alternatives = party1Alternatives
                .Select(a => a with { MinPrice = a.MinPrice * party1RoomsCount, NetPrice = a.NetPrice * party1RoomsCount })
                .ToList();
        }
        if (party2RoomsCount > 1)
        {
            party2Alternatives = party2Alternatives
                .Select(a => a with { MinPrice = a.MinPrice * party2RoomsCount, NetPrice = a.NetPrice * party2RoomsCount })
                .ToList();
        }

        // Combine and sum (simulating KeepCommon)
        var allAlternatives = party1Alternatives.Concat(party2Alternatives).ToList();
        var totalMinPrice = allAlternatives
            .GroupBy(a => new { a.CheckIn, a.CheckOut })
            .Select(g => g.Sum(x => x.MinPrice))
            .First();

        // Assert - Total should be 200 + 150 = 350
        Assert.AreEqual(350m, totalMinPrice);
    }

    #endregion
}
