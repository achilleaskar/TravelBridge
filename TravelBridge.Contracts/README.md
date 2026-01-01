# TravelBridge.Contracts

## Purpose
This is the **contracts layer** of TravelBridge. It contains:
- **Request DTOs** - API input models
- **Response DTOs** - API output models  
- **Mapping profiles** - AutoMapper configurations (future)

## Architecture Rules
1. ✅ **No dependencies** on other TravelBridge projects
2. ✅ **Pure DTOs only** - no business logic
3. ✅ **Shared by API and Infrastructure** for consistency
4. ✅ **Can be packaged** as NuGet for external clients

## Folder Structure
- `Requests/` - API request DTOs
- `Responses/` - API response DTOs
- `Mappings/` - AutoMapper profiles (future)

## Usage
- API endpoints use these contracts for input/output
- Infrastructure services can use these for external API communication
- Future mobile apps/clients can reference this package
