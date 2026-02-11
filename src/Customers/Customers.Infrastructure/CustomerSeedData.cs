using Domain.Customers.Entities;
using Domain.Customers.Enums;

namespace Customers.Infrastructure.Data;

public static class CustomerSeedData
{
    public static async Task InitializeAsync(CustomerDbContext context, CancellationToken ct = default)
    {
        var exists = await context.Customers.EstimatedDocumentCountAsync() > 0;
        if (exists)
            return;

        var customers = new List<Customer>
        {
            new()
            {
                Id = new Guid("b618d24e-ae60-4143-9ff5-bb6d2f10c555"),
                Name = "Lucas Lo Faro",
                Mail = "lucaslofaro@gmail.com",
                Phone = "1160456045",
                Status = CustomerStatus.Active,
                Address = new Address
                {
                    Street = "Soldado de la Independencia",
                    Number = "1111",
                    ZipCode = "1426",
                    City = "Buenos Aires",
                    AdditionalInformation = "Piso 32 Departamento A"
                }
            },
            new()
            {
                Id = new Guid("31207041-ab19-4044-aea9-97c08ab777cc"),
                Name = "Maria Garcia",
                Mail = "maria.garcia@example.com",
                Phone = "1145678901",
                Status = CustomerStatus.Active,
                Address = new Address
                {
                    Street = "Avenida Corrientes",
                    Number = "2450",
                    ZipCode = "1046",
                    City = "Buenos Aires",
                    AdditionalInformation = "Oficina 5B"
                }
            },
            new()
            {
                Id = new Guid("f956ea3e-063b-46ca-8365-550ab847c715"),
                Name = "Juan Perez",
                Mail = "juan.perez@example.com",
                Phone = "1198765432",
                Status = CustomerStatus.Inactive,
                Address = new Address
                {
                    Street = "Calle Florida",
                    Number = "800",
                    ZipCode = "1005",
                    City = "Buenos Aires",
                    AdditionalInformation = string.Empty
                }
            }
        };

        await context.Customers.InsertManyAsync(customers, cancellationToken: ct);
    }
}
