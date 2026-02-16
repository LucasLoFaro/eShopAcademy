# MongoDB GUID Serialization Fix

## Problem

**Error:**
```
MongoDB.Bson.BsonSerializationException: An error occurred while serializing the CustomerId property of class Domain.Customers.Entities.SavedAddress: GuidSerializer cannot serialize a Guid when GuidRepresentation is Unspecified.
```

**Root Cause:**
MongoDB's BSON serializer doesn't know how to represent .NET `Guid` values in the database without explicit configuration.

## Solution

Added `[BsonRepresentation(BsonType.String)]` attribute to the `CustomerId` property in `SavedAddress`, following the same pattern used in `BaseEntity`.

## Changes Made

### SavedAddress.cs

**Before:**
```csharp
namespace Domain.Customers.Entities;

public class SavedAddress : BaseEntity
{
    public Guid CustomerId { get; set; }  // ? No serialization guidance
    public string Description { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public bool IsDefault { get; set; }
}
```

**After:**
```csharp
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Customers.Entities;

public class SavedAddress : BaseEntity
{
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Guid CustomerId { get; set; }  // ? Serializes as string
    public string Description { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public bool IsDefault { get; set; }
}
```

## Consistent with BaseEntity Pattern

This follows the exact same pattern already established in `BaseEntity`:

```csharp
public class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();  // ? Already using this pattern
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
```

## MongoDB Storage

**Stored as string in MongoDB:**
```json
{
  "_id": "507f1f77-bcf8-6cd7-9943-9011ace45678",
  "customerId": "a3bb189e-8bf9-3888-9912-ace4e6543002",
  "description": "Order 12ab34cd",
  "address": { ... },
  "isDefault": true
}
```

## Benefits

? **Consistent** - Uses the same approach as BaseEntity.Id  
? **Simple** - Just a data annotation, no global registration needed  
? **Maintainable** - Clear and explicit per-property  
? **Standard** - Follows MongoDB C# driver conventions

## Testing

The `CustomerAddressUpdatedEventConsumer` should now work without errors:

```csharp
// This will now succeed
await _customerRepository.AddAddressAsync(customerId, savedAddress);
```

**Verify:**
1. Place an order with a shipping address
2. Check logs for: `[CustomerAddressUpdate] Added new address`
3. Check MongoDB - address should be saved with CustomerId as string

