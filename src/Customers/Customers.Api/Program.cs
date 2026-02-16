using Customers.Infrastructure.Data;
using Domain.Customers.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithSwagger();

builder.Services.AddSingleton(sp =>
    new CustomerDbContext(builder.Configuration.GetConnectionString("customers"), "customers"));
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    await CustomerSeedData.InitializeAsync(db);
}

app.MapGet("/customers", async (ICustomerRepository repo, CancellationToken ct) =>
{
    var customers = await repo.GetAllAsync(ct);
    return Results.Ok(customers);
});

app.MapGet("/customers/{id}", async (Guid id, ICustomerRepository repo, CancellationToken ct) =>
{
    var customer = await repo.GetByIdAsync(id, ct);
    return customer is not null ? Results.Ok(customer) : Results.NotFound();
});

app.MapPost("/customers", async (Customer customer, ICustomerRepository repo, CancellationToken ct) =>
{
    var created = await repo.CreateAsync(customer, ct);
    return Results.Created($"/customers/{created.Id}", created);
});

app.MapPut("/customers/{id}", async (Guid id, Customer customer, ICustomerRepository repo, CancellationToken ct) =>
{
    var updated = await repo.UpdateAsync(id, customer, ct);
    return updated is not null ? Results.Ok(updated) : Results.NotFound();
});

app.MapDelete("/customers/{id}", async (Guid id, ICustomerRepository repo, CancellationToken ct) =>
{
    var deleted = await repo.DeleteAsync(id, ct);
    return deleted ? Results.NoContent() : Results.NotFound();
});

// Address management endpoints
app.MapGet("/customers/{customerId}/addresses", async (Guid customerId, ICustomerRepository repo, CancellationToken ct) =>
{
    var customer = await repo.GetByIdAsync(customerId, ct);
    return customer is not null ? Results.Ok(customer.SavedAddresses) : Results.NotFound();
});

app.MapPost("/customers/{customerId}/addresses", async (Guid customerId, SavedAddress address, ICustomerRepository repo, CancellationToken ct) =>
{
    try
    {
        var created = await repo.AddAddressAsync(customerId, address, ct);
        return Results.Created($"/customers/{customerId}/addresses/{created.Id}", created);
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.MapPut("/customers/{customerId}/addresses/{addressId}", async (Guid customerId, Guid addressId, SavedAddress address, ICustomerRepository repo, CancellationToken ct) =>
{
    var updated = await repo.UpdateAddressAsync(customerId, addressId, address, ct);
    return updated is not null ? Results.Ok(updated) : Results.NotFound();
});

app.MapDelete("/customers/{customerId}/addresses/{addressId}", async (Guid customerId, Guid addressId, ICustomerRepository repo, CancellationToken ct) =>
{
    var deleted = await repo.DeleteAddressAsync(customerId, addressId, ct);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.MapPost("/customers/{customerId}/addresses/{addressId}/set-default", async (Guid customerId, Guid addressId, ICustomerRepository repo, CancellationToken ct) =>
{
    var success = await repo.SetDefaultAddressAsync(customerId, addressId, ct);
    return success ? Results.NoContent() : Results.NotFound();
});

app.UseDefaultEndpoints();

app.Run();
