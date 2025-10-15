using Core.Application.Interfaces;
using Domain.Order.Entities;


namespace Infrastructure.Clients;

public class FakeProductServiceClient : IProductServiceClient
{
    public async Task<Product> GetProductByIdAsync(Guid productId)
        => new Product()
        {
            ID = new Guid("8b6e2e1d-6f1d-4b0d-9f6c-1a4c7b9d2e8f"),
            Name = "Logitech MX Master 3S",
            Price = 123.12f,
            Description = "Best mouse ever",
            CategoryName = "Accesories",
            Image = "https://m.media-amazon.com/images/I/61ni3t1ryQL._AC_SL1500_.jpg",
            Stock = 100
        };
}

public class FakeCustomerServiceClient : ICustomerServiceClient
{
    public async Task<Customer> GetCustomerByIdAsync(Guid customerId)
        => new Customer()
        {
            Name = "Lucas Lo Faro",
            Mail = "lucaslofaro@gmail.com",
            Phone = "1160456045",
            Address = new()
            {
                Street = "Soldado de la independencia",
                Number = "1111",
                ZipCode = "1426",
                City = "Buenos Aires",
                AdditionalInformation = "Piso 32 Departamento A"
            }
        };
}

public class FakePaymentGrpcClient : IPaymentServiceClient
{
    public Task<Payment> InitPaymentAsync(double amount, string currency, string notificationUrl, Guid orderId, CancellationToken ct = default)
        => Task.FromResult(new Payment()
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            OrderId = orderId,
            Status = Domain.Order.Enums.PaymentStatus.Pending,
            PaymentURL = $"https://psp.com/payment/{Guid.NewGuid().ToString()}",
            ProviderTransactionId = Guid.NewGuid().ToString()
        });
}