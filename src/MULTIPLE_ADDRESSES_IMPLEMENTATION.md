# Multiple Addresses Feature - Implementation Summary

## Overview
Enhanced the checkout flow to support multiple saved addresses per customer with descriptions (e.g., "Home", "Work").

## Backend Changes

### 1. Domain Layer
- **`SavedAddress.cs`** - New entity with:
  - `Id`, `CustomerId`, `Description`, `Address`, `IsDefault`, timestamps
  
- **`Customer.cs`** - Updated to include:
  - `List<SavedAddress> SavedAddresses` property
  - Kept legacy `Address` property for backward compatibility

### 2. Infrastructure Layer
- **`ICustomerRepository.cs`** - Added methods:
  - `AddAddressAsync` - Create new saved address
  - `UpdateAddressAsync` - Modify existing address
  - `DeleteAddressAsync` - Remove address (auto-sets new default if needed)
  - `SetDefaultAddressAsync` - Mark address as default

- **`CustomerRepository.cs`** - Implemented all methods with:
  - Automatic default handling
  - First address auto-set as default
  - Default reassignment on deletion

### 3. API Layer
- **`Customers.Api/Program.cs`** - Added endpoints:
  - `GET /customers/{customerId}/addresses` - List all addresses
  - `POST /customers/{customerId}/addresses` - Create address
  - `PUT /customers/{customerId}/addresses/{addressId}` - Update address
  - `DELETE /customers/{customerId}/addresses/{addressId}` - Delete address
  - `POST /customers/{customerId}/addresses/{addressId}/set-default` - Set default

### 4. Messaging
- **`CustomerEvent.cs`** - Base event class for customer events
- **`CustomerAddressUpdatedEvent.cs`** - Event published when address changes
- **`CustomerAddressUpdatedEventConsumer.cs`** - Consumer to persist address updates
- **Updated `OrderService.cs`** - Publishes address update event after order placement

## Frontend Changes

### 1. Type Definitions
- **`types.ts`** - Added:
  - `SavedAddress` interface
  - Updated `Customer` to include `savedAddresses: SavedAddress[]`

### 2. Components
- **`AddressSelector.tsx`** - New component:
  - Visual cards for saved addresses
  - Shows default badge
  - Edit/delete icons appear on selected address
  - "Add New Address" button
  - Empty state handling

- **`AddressStep.tsx`** - Major refactor:
  - Dual-mode: "select" (saved addresses) or "form" (new/edit)
  - Address selection with auto-fill
  - Read-only fields when address selected
  - Edit button enables form editing
  - Delete confirmation modal
  - "Save for later" checkbox with description field
  - Back button to switch modes
  - Map preview updates on selection/input

### 3. Updated Components
- **`CheckoutPage.tsx`** - TODO: Pass `savedAddresses` to AddressStep
- **`useCustomer.ts`** - TODO: Add hooks for address management

## User Experience Flow

### First-Time User
1. Opens checkout ? sees address form
2. Can check "Save this address for later"
3. Enters label (e.g., "Home")
4. Completes form ? address saved

### Returning User
1. Opens checkout ? sees saved addresses
2. Selects desired address ? auto-fills form
3. Fields become read-only
4. Edit icon ? enables editing
5. Delete icon ? shows confirmation
6. "Add New Address" ? shows empty form

### Address Management
- Edit: Click edit icon ? form becomes editable ? save updates
- Delete: Click delete ? confirm ? address removed
- Default: First address auto-default, can change via API
- Multiple: Unlimited addresses with descriptions

## Database Schema (MongoDB)

```json
{
  "savedAddresses": [
    {
      "id": "guid",
      "customerId": "guid",
      "description": "Home",
      "address": {
        "street": "Main St",
        "number": "123",
        "additionalInformation": "Apt 4B",
        "zipCode": "12345",
        "city": "New York",
        "state": "NY",
        "country": "USA"
      },
      "isDefault": true,
      "createdAt": "2025-02-10T...",
      "modifiedAt": "2025-02-10T..."
    }
  ]
}
```

## API Examples

### Create Address
```http
POST /customers/{customerId}/addresses
Content-Type: application/json

{
  "description": "Work",
  "address": {
    "street": "Office Blvd",
    "number": "456",
    "zipCode": "54321",
    "city": "San Francisco",
    "country": "USA"
  },
  "isDefault": false
}
```

### Update Address
```http
PUT /customers/{customerId}/addresses/{addressId}
Content-Type: application/json

{
  "description": "Home (Updated)",
  "address": { ... },
  "isDefault": true
}
```

### Delete Address
```http
DELETE /customers/{customerId}/addresses/{addressId}
```

## Next Steps (TODOs)

1. **Frontend Integration**:
   - Create `useCustomerAddresses` hook
   - Update `CheckoutPage` to fetch and pass saved addresses
   - Implement actual delete API call in AddressStep
   - Handle address save on checkout completion

2. **Testing**:
   - Unit tests for repository methods
   - Integration tests for API endpoints
   - E2E tests for checkout flow

3. **Enhancements**:
   - Address validation service
   - Geocoding for coordinates
   - Address autocomplete
   - Set/unset default from UI

## Benefits

? **User Convenience** - Quick address selection
? **Data Quality** - Consistent addresses
? **Flexibility** - Multiple addresses supported
? **Backward Compatible** - Legacy address field preserved
? **Scalable** - Clean separation of concerns
? **Event-Driven** - Address updates via messaging
