# TravelBridge.Contracts - API Contract Guidelines

> **CRITICAL**: This document defines which files in this project are part of the Frontend API contract and MUST NOT be modified.

## ⚠️ DO NOT MODIFY - Frontend Depends On These

The following files/folders are returned directly to the Frontend (WordPress site) and **must not be changed** without coordinating with the FE team:

### API Requests & Responses (`Contracts/`)
```
Contracts/
├── AutoCompleteResponse.cs          ❌ DO NOT MODIFY
├── BaseWebHotelierResponse.cs       ❌ DO NOT MODIFY
├── MultiAvailabilityRequest.cs      ❌ DO NOT MODIFY
├── SingleAvailabilityRequest.cs     ❌ DO NOT MODIFY
└── Responses/
    ├── DataSuccess.cs               ❌ DO NOT MODIFY
    ├── HotelInfoFullResponse.cs     ❌ DO NOT MODIFY
    ├── HotelInfoResponse.cs         ❌ DO NOT MODIFY
    ├── PreparePaymentResponse.cs    ❌ DO NOT MODIFY
    ├── RoomInfoResponse.cs          ❌ DO NOT MODIFY
    └── SuccessfulPaymentResponse.cs ❌ DO NOT MODIFY
```

### Models Used in API Responses (`Models/`)
```
Models/
└── Hotels/
    ├── BaseHotelInfo.cs             ❌ DO NOT MODIFY
    ├── HotelData.cs                 ❌ DO NOT MODIFY
    ├── HotelOperation.cs            ❌ DO NOT MODIFY
    ├── PhotoInfo.cs                 ❌ DO NOT MODIFY
    ├── RoomInfo.cs                  ❌ DO NOT MODIFY
    ├── SingleHotelRate.cs           ❌ DO NOT MODIFY
    └── SingleHotelRoom.cs           ❌ DO NOT MODIFY
```

### Common Models in API Responses (`Common/`)
```
Common/
├── Alternative.cs                   ❌ DO NOT MODIFY
├── BaseRate.cs                      ❌ DO NOT MODIFY
├── BBox.cs                          ❌ DO NOT MODIFY
├── HotelRate.cs                     ❌ DO NOT MODIFY
├── Location.cs                      ❌ DO NOT MODIFY
├── LocationInfo.cs                  ❌ DO NOT MODIFY
├── MultiRate.cs                     ❌ DO NOT MODIFY
├── PartyItem.cs                     ❌ DO NOT MODIFY
├── RateProperties.cs                ❌ DO NOT MODIFY
├── WebHotel.cs                      ❌ DO NOT MODIFY
├── Board/
│   ├── BaseBoard.cs                 ❌ DO NOT MODIFY
│   ├── Board.cs                     ❌ DO NOT MODIFY
│   └── BoardTextBase.cs             ❌ DO NOT MODIFY
├── Payments/
│   ├── PartialPayment.cs            ❌ DO NOT MODIFY
│   ├── PaymentWH.cs                 ❌ DO NOT MODIFY
│   ├── PricingInfo.cs               ❌ DO NOT MODIFY
│   └── StringAmount.cs              ❌ DO NOT MODIFY
└── Policies/
    ├── CancellationFee.cs           ❌ DO NOT MODIFY
    └── ChildrenPolicy.cs            ❌ DO NOT MODIFY
```

### Plugin Models (`Plugin/`)
```
Plugin/
├── AutoComplete/
│   ├── AutoCompleteHotel.cs         ❌ DO NOT MODIFY
│   └── AutoCompleteLocation.cs      ❌ DO NOT MODIFY
└── Filters/
    ├── Filter.cs                    ❌ DO NOT MODIFY
    └── FilterValue.cs               ❌ DO NOT MODIFY
```

## ✅ Safe to Modify (Internal Only)

These files are internal utilities and can be modified:

```
Helpers/
└── Converters/
    ├── IntToStringJsonConverter.cs  ✅ Internal utility
    └── NullableDateTimeConverter.cs ✅ Internal utility

Common/
└── Enums.cs                         ⚠️ BE CAREFUL - some enums may be in responses
```

## Why This Matters

The Frontend (WordPress booking plugin) depends on the exact JSON structure of these responses. Any changes to:
- Property names (especially `[JsonPropertyName]` attributes)
- Property types
- Nested object structures
- Enum values

...will break the Frontend without warning.

## Adding New Fields

If you need to add new fields to existing responses:
1. ✅ Adding optional properties is usually safe
2. ❌ Removing properties will break FE
3. ❌ Renaming properties will break FE
4. ❌ Changing types will break FE

## Related Documentation

- See `TravelBridge.API/owned-rooms-implementation-plan.md` for the Owned Rooms feature plan
- Provider abstractions are in `TravelBridge.Providers.Abstractions` (internal, not exposed to FE)
