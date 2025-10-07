using Infrastructure.Services;
using Core.Domain.Contracts;
using Core.Domain.Entities;


namespace Infrastructure.Data;

public static class StockSeedData
{
    public static async Task InitializeAsync(
        StockDbContext context,
        StockMessagingClient messaging,
        CancellationToken ct = default)
    {
        var stockExists = await context.Stocks.EstimatedDocumentCountAsync() > 0;
        if (!stockExists)
        {
            var stocks = new List<Stock>
            {
                new()
                {
                    ProductID = Guid.Parse("8b6e2e1d-6f1d-4b0d-9f6c-1a4c7b9d2e8f"),
                    Quantity = 4,
                    Warehouse = "WH-01"
                },
                new()
                {
                    ProductID = Guid.Parse("c2d3f8a1-9b4d-41ea-b77f-5e20b39a48a6"),
                    Quantity = 7,
                    Warehouse = "WH-01"
                },
                new()
                {
                    ProductID = Guid.Parse("4f2a0b63-8e3d-4f7d-9b5c-1e19f9c3d21b"),
                    Quantity = 2,
                    Warehouse = "WH-02"
                }
            };

            await context.Stocks.InsertManyAsync(stocks, cancellationToken: ct);

            foreach (var stock in stocks)
                await messaging.SendStockUpdate(new AlterStockRequest(stock), ct);
        }

        var reservationExists = await context.Reservations.EstimatedDocumentCountAsync() > 0;
        if (!reservationExists)
        {
            var reservation = new StockReservation
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Items =
                [
                    new ReservationItem
                    {
                        Warehouse = "WH-01",
                        Items =
                        [
                            new StockItem
                            {
                                ProductID = Guid.Parse("8b6e2e1d-6f1d-4b0d-9f6c-1a4c7b9d2e8f"),
                                Quantity = 1
                            },
                            new StockItem
                            {
                                ProductID = Guid.Parse("c2d3f8a1-9b4d-41ea-b77f-5e20b39a48a6"),
                                Quantity = 2
                            }
                        ]
                    }
                ],
                CreatedAt = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddMinutes(5)
            };

            await context.Reservations.InsertOneAsync(reservation, cancellationToken: ct);
        }
    }
}
