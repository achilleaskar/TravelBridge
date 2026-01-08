-- ========================================
-- Phase 3: Owned Inventory Seed Data
-- Development/Testing Hotels
-- ========================================

-- Sample Hotel 1: Beach Resort
INSERT INTO OwnedHotels (Code, Name, Description, Type, Rating, Latitude, Longitude, City, Address, Country, PostalCode, CheckInTime, CheckOutTime, IsActive, DateCreated)
VALUES 
('OWNBEACH01', 'Sunset Beach Resort', 
 'Luxury beachfront resort with stunning ocean views and world-class amenities. Perfect for families and couples seeking a relaxing getaway.',
 'Resort', 5,
 37.9375, 23.9467, -- Coordinates for Athens area (example)
 'Athens', 'Poseidonos Avenue 42', 'Greece', '16777',
 '15:00', '11:00', 
 TRUE, UTC_TIMESTAMP());

-- Sample Hotel 2: City Hotel
INSERT INTO OwnedHotels (Code, Name, Description, Type, Rating, Latitude, Longitude, City, Address, Country, PostalCode, CheckInTime, CheckOutTime, IsActive, DateCreated)
VALUES 
('OWNCITY01', 'Metropolitan Suites',
 'Modern city hotel in the heart of Athens. Walking distance to major attractions, shopping, and dining.',
 'Hotel', 4,
 37.9838, 23.7275,
 'Athens', 'Syntagma Square 15', 'Greece', '10563',
 '14:00', '12:00',
 TRUE, UTC_TIMESTAMP());

-- ========================================
-- Room Types for Beach Resort
-- ========================================

INSERT INTO OwnedRoomTypes (HotelId, Code, Name, Description, MaxAdults, MaxChildren, MaxTotalOccupancy, BasePricePerNight, DefaultTotalUnits, IsActive, DateCreated)
SELECT 
    h.Id,
    'STDROOM',
    'Standard Room',
    'Comfortable room with garden view. Features queen bed, private bathroom, air conditioning, and free WiFi.',
    2, 1, 3,
    120.00,
    20, -- 20 standard rooms available
    TRUE,
    UTC_TIMESTAMP()
FROM OwnedHotels h WHERE h.Code = 'OWNBEACH01';

INSERT INTO OwnedRoomTypes (HotelId, Code, Name, Description, MaxAdults, MaxChildren, MaxTotalOccupancy, BasePricePerNight, DefaultTotalUnits, IsActive, DateCreated)
SELECT 
    h.Id,
    'SEAVIEW',
    'Sea View Room',
    'Spacious room with balcony and panoramic sea views. King bed, luxury bathroom with bathtub, mini-bar included.',
    2, 2, 4,
    180.00,
    15, -- 15 sea view rooms available
    TRUE,
    UTC_TIMESTAMP()
FROM OwnedHotels h WHERE h.Code = 'OWNBEACH01';

INSERT INTO OwnedRoomTypes (HotelId, Code, Name, Description, MaxAdults, MaxChildren, MaxTotalOccupancy, BasePricePerNight, DefaultTotalUnits, IsActive, DateCreated)
SELECT 
    h.Id,
    'FAMILYSUITE',
    'Family Suite',
    'Two-bedroom suite perfect for families. Separate living area, two bathrooms, kitchenette, and large balcony with sea view.',
    4, 3, 7,
    350.00,
    8, -- 8 family suites available
    TRUE,
    UTC_TIMESTAMP()
FROM OwnedHotels h WHERE h.Code = 'OWNBEACH01';

-- ========================================
-- Room Types for City Hotel
-- ========================================

INSERT INTO OwnedRoomTypes (HotelId, Code, Name, Description, MaxAdults, MaxChildren, MaxTotalOccupancy, BasePricePerNight, DefaultTotalUnits, IsActive, DateCreated)
SELECT 
    h.Id,
    'BUSROOM',
    'Business Room',
    'Modern room designed for business travelers. Work desk, ergonomic chair, high-speed internet, and complimentary coffee.',
    2, 0, 2,
    95.00,
    30, -- 30 business rooms available
    TRUE,
    UTC_TIMESTAMP()
FROM OwnedHotels h WHERE h.Code = 'OWNCITY01';

INSERT INTO OwnedRoomTypes (HotelId, Code, Name, Description, MaxAdults, MaxChildren, MaxTotalOccupancy, BasePricePerNight, DefaultTotalUnits, IsActive, DateCreated)
SELECT 
    h.Id,
    'DXROOM',
    'Deluxe Room',
    'Upgraded room with city view. Premium bedding, Nespresso machine, rain shower, and welcome amenities.',
    2, 1, 3,
    140.00,
    20, -- 20 deluxe rooms available
    TRUE,
    UTC_TIMESTAMP()
FROM OwnedHotels h WHERE h.Code = 'OWNCITY01';

-- ========================================
-- Sample Inventory with Seasonal Pricing
-- ========================================
-- Note: The InventorySeedService will automatically create inventory for 
-- all active room types for the next 400 days on application startup.
-- This section provides optional manual seed data for specific dates with 
-- custom pricing (e.g., weekend surcharge, holiday pricing).

-- Example: Weekend pricing for Beach Resort Standard Rooms (next 8 weekends)
-- Assuming today is a weekday, this sets higher prices for upcoming weekends

-- Get next Saturday and Sunday dates dynamically (example for first weekend)
SET @today = CURDATE();
SET @next_saturday = DATE_ADD(@today, INTERVAL (6 - WEEKDAY(@today)) DAY);

-- Beach Resort - Standard Room - Weekend Premium Pricing (+20%)
INSERT INTO OwnedInventoryDaily (RoomTypeId, Date, TotalUnits, ClosedUnits, HeldUnits, ConfirmedUnits, PricePerNight, LastModifiedUtc)
SELECT 
    rt.Id,
    DATE_ADD(@next_saturday, INTERVAL week WEEK) as Date,
    rt.DefaultTotalUnits,
    0, 0, 0,
    rt.BasePricePerNight * 1.20, -- 20% weekend premium
    UTC_TIMESTAMP()
FROM OwnedRoomTypes rt
JOIN OwnedHotels h ON rt.HotelId = h.Id
WHERE h.Code = 'OWNBEACH01' AND rt.Code = 'STDROOM'
AND week IN (0, 1, 2, 3); -- Next 4 Saturdays

-- Beach Resort - Sea View - Weekend Premium Pricing (+25%)
INSERT INTO OwnedInventoryDaily (RoomTypeId, Date, TotalUnits, ClosedUnits, HeldUnits, ConfirmedUnits, PricePerNight, LastModifiedUtc)
SELECT 
    rt.Id,
    DATE_ADD(@next_saturday, INTERVAL week WEEK) as Date,
    rt.DefaultTotalUnits,
    0, 0, 0,
    rt.BasePricePerNight * 1.25, -- 25% weekend premium
    UTC_TIMESTAMP()
FROM OwnedRoomTypes rt
JOIN OwnedHotels h ON rt.HotelId = h.Id
WHERE h.Code = 'OWNBEACH01' AND rt.Code = 'SEAVIEW'
AND week IN (0, 1, 2, 3);

-- ========================================
-- Testing Scenarios
-- ========================================

-- Scenario 1: Stop-sell specific dates (hotel renovation/maintenance)
-- Close 5 rooms in Beach Resort Standard for next week
/*
UPDATE OwnedInventoryDaily
SET ClosedUnits = 5,
    LastModifiedUtc = UTC_TIMESTAMP()
WHERE RoomTypeId = (SELECT Id FROM OwnedRoomTypes WHERE Code = 'STDROOM' AND HotelId = (SELECT Id FROM OwnedHotels WHERE Code = 'OWNBEACH01'))
  AND Date BETWEEN DATE_ADD(CURDATE(), INTERVAL 7 DAY) AND DATE_ADD(CURDATE(), INTERVAL 14 DAY);
*/

-- Scenario 2: Special event pricing (Greek Easter, Summer peak, etc.)
-- Set higher prices for peak season (example: July-August)
/*
INSERT INTO OwnedInventoryDaily (RoomTypeId, Date, TotalUnits, ClosedUnits, HeldUnits, ConfirmedUnits, PricePerNight, LastModifiedUtc)
SELECT 
    rt.Id,
    d.Date,
    rt.DefaultTotalUnits,
    0, 0, 0,
    rt.BasePricePerNight * 1.50, -- 50% peak season premium
    UTC_TIMESTAMP()
FROM OwnedRoomTypes rt
JOIN OwnedHotels h ON rt.HotelId = h.Id
CROSS JOIN (
    SELECT DATE_ADD('2026-07-01', INTERVAL n DAY) as Date
    FROM (SELECT 0 as n UNION SELECT 1 UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5 UNION SELECT 6 
          UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION SELECT 10 UNION SELECT 11 UNION SELECT 12 
          UNION SELECT 13 UNION SELECT 14 UNION SELECT 15 UNION SELECT 16 UNION SELECT 17 UNION SELECT 18 
          UNION SELECT 19 UNION SELECT 20 UNION SELECT 21 UNION SELECT 22 UNION SELECT 23 UNION SELECT 24 
          UNION SELECT 25 UNION SELECT 26 UNION SELECT 27 UNION SELECT 28 UNION SELECT 29 UNION SELECT 30) dates
) d
WHERE h.Code = 'OWNBEACH01'
  AND d.Date BETWEEN '2026-07-01' AND '2026-08-31';
*/

-- ========================================
-- Verification Queries
-- ========================================

-- Check created hotels
-- SELECT Id, Code, Name, City, Rating, IsActive FROM OwnedHotels;

-- Check room types
-- SELECT h.Code as HotelCode, rt.Code as RoomCode, rt.Name, rt.BasePricePerNight, rt.DefaultTotalUnits
-- FROM OwnedRoomTypes rt
-- JOIN OwnedHotels h ON rt.HotelId = h.Id
-- ORDER BY h.Code, rt.Code;

-- Check inventory count per room type
-- SELECT h.Code as HotelCode, rt.Code as RoomCode, COUNT(*) as InventoryRows
-- FROM OwnedInventoryDaily inv
-- JOIN OwnedRoomTypes rt ON inv.RoomTypeId = rt.Id
-- JOIN OwnedHotels h ON rt.HotelId = h.Id
-- GROUP BY h.Code, rt.Code
-- ORDER BY h.Code, rt.Code;

-- ========================================
-- Notes
-- ========================================
-- 1. The InventorySeedService will automatically populate inventory for the next 400 days on startup
-- 2. Use the admin endpoints to adjust capacity and stop-sell specific dates
-- 3. Weekend/seasonal pricing can be set manually via OwnedInventoryDaily.PricePerNight
-- 4. Composite IDs for searching: "0-OWNBEACH01", "0-OWNCITY01"
-- 5. Test party configurations: [{"adults":2}], [{"adults":2,"children":[5,10]}], multiple rooms
