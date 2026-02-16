# Customers.Messaging Project Setup

## Overview
Successfully moved the messaging consumers from `Customers.Api` to a dedicated `Customers.Messaging` Worker Service project for better separation of concerns.

## Changes Made

### 1. Project Structure

**Customers.Messaging.csproj** - Added references:
```xml
<ItemGroup>
  <ProjectReference Include="..\..\ServiceDefaults\ServiceDefaults.csproj" />
  <ProjectReference Include="..\..\Domain\Common.Domain\Common.Domain.csproj" />
  <ProjectReference Include="..\..\Domain\Customers.Domain\Customers.Domain.csproj" />
  <ProjectReference Include="..\Customers.Infrastructure\Customers.Data.csproj" />
</ItemGroup>
```

### 2. Consumer Migration

**Moved:**
- `Customers.Api/Consumers/CustomerAddressUpdatedEventConsumer.cs`
  
**To:**
- `Customers.Messaging/Consumers/CustomerAddressUpdatedEventConsumer.cs`

**Updated namespace:** `Customers.Api.Consumers` ? `Customers.Messaging.Consumers`

### 3. Program.cs Configuration

**Customers.Messaging/Program.cs:**
```csharp
using Customers.Infrastructure.Data;
using Customers.Messaging.Consumers;
using ServiceDefaults;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit(assemblies: Assembly.GetExecutingAssembly());

// Register Customer Repository and Database
builder.Services.AddSingleton(sp =>
    new CustomerDbContext(builder.Configuration.GetConnectionString("customers")!, "customers"));
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

var host = builder.Build();
host.Run();
```

**Key features:**
- Uses `Host.CreateApplicationBuilder` (Worker Service pattern)
- Configures MassTransit with auto-consumer discovery
- Registers CustomerRepository for database access
- Connects to MongoDB customers database

### 4. Customers.Api Cleanup

**Removed from Customers.Api/Program.cs:**
- MassTransit configuration
- Consumer registration
- `using Customers.Api.Consumers;`
- `using MassTransit;`

**Result:** Customers.Api is now purely focused on HTTP endpoints without messaging concerns.

### 5. AppHost Integration

**AppHost.cs:**
```csharp
// Customer
var customersApi = builder.AddProject<Projects.Customers_Api>("eshopacademy-customers-api");
var customersMessaging = builder.AddProject<Projects.Customers_Messaging>("eshopacademy-customers-messaging");
```

**EnvironmentSetup.cs:**
- Added `customersMessaging` parameter to `SetupLocalInfrastructure`
- Called `CustomersExtensions.ConfigureMessaging(customersMessaging, customersdb, rabbit)`

**CustomersExtensions.cs:**
```csharp
public static void ConfigureMessaging(
    IResourceBuilder<ProjectResource> customersMessaging,
    IResourceBuilder<MongoDBDatabaseResource> customersdb,
    IResourceBuilder<RabbitMQServerResource> rabbit)
{
    customersMessaging
        .WithReference(customersdb)
        .WaitFor(customersdb)
        .WithReference(rabbit)
        .WaitFor(rabbit)
        .WithCommonEnvironments();
}
```

## Architecture Benefits

### ? Separation of Concerns
- **Customers.Api** - HTTP endpoints only
- **Customers.Messaging** - Event-driven message processing

### ? Scalability
- Can scale API and messaging independently
- API handles synchronous requests
- Messaging processes asynchronous events

### ? Maintainability
- Clear boundaries between responsibilities
- Easier to test and debug
- Follows microservices best practices

### ? Resilience
- Messaging service can restart without affecting API
- Handles backpressure naturally via message queue
- Can process events at its own pace

## Consumer Behavior

**CustomerAddressUpdatedEventConsumer:**
- Listens for `CustomerAddressUpdatedEvent` from Orders service
- **Checks if address already exists** in customer's saved addresses (by matching street, number, zip, city)
- **If address exists:** Updates legacy `Address` field only (for backward compatibility)
- **If address is new:** 
  - Adds it to `SavedAddresses` collection
  - Creates description from Order ID (e.g., "Order 12ab34cd")
  - Sets as default if it's the customer's first saved address
- Logs all operations for observability
- Handles missing customers gracefully

### Smart Deduplication
The consumer prevents duplicate addresses by comparing:
- Street (case-insensitive)
- Number (case-insensitive)
- Zip Code (case-insensitive)
- City (case-insensitive)

This means if a customer uses the same shipping address for multiple orders, it's only saved once.

## Message Flow

```
Order Placement
     ?
OrderService publishes CustomerAddressUpdatedEvent
     ?
RabbitMQ Queue
     ?
Customers.Messaging (Worker Service)
     ?
CustomerAddressUpdatedEventConsumer
     ?
Updates Customer.Address in MongoDB
```

## Running the Services

**Local Development:**
1. AppHost starts both services automatically
2. Customers.Messaging connects to:
   - MongoDB (customers database)
   - RabbitMQ (message broker)
3. Listens for events continuously as a background worker

**Production:**
- Deploy as separate containers
- Scale independently based on load
- Monitor message queue depth

## Testing

**Verify consumer is working:**
1. Place an order with a shipping address
2. Check Customers.Messaging logs for:
   ```
   [CustomerAddressUpdate] Updating address for customer {CustomerId} from order {OrderId}
   ```
3. Query customer in MongoDB - should see updated `Address` field

## Future Enhancements

Potential additional consumers for Customers.Messaging:
- `CustomerCreatedEventConsumer` - Sync from external systems
- `CustomerEmailUpdatedEventConsumer` - Update from auth provider
- `CustomerDeletedEventConsumer` - GDPR compliance
- `CustomerPreferencesUpdatedEventConsumer` - Marketing preferences

## Deployment

**Docker Compose / Kubernetes:**
```yaml
customers-messaging:
  image: customers-messaging:latest
  environment:
    - ConnectionStrings__customers=mongodb://...
    - ConnectionStrings__rabbit=amqp://...
  depends_on:
    - mongodb
    - rabbitmq
```

## Monitoring

**Key Metrics:**
- Message processing rate
- Consumer lag (messages waiting)
- Processing errors
- Database update latency

**Logs to monitor:**
- `[CustomerAddressUpdate]` prefix for all operations
- Errors during customer lookup
- Database update failures
