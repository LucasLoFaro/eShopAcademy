using Infrastructure.Services;
using Domain.Stock.Contracts;
using Domain.Stock.Entities;


namespace Infrastructure.Data;

public static class StockSeedData
{
    // Must match the fixed GUIDs from ProductSeedData
    static readonly Guid[] ProductIds =
    [
        Guid.Parse("10000000-0001-0000-0000-000000000001"),
        Guid.Parse("10000000-0001-0000-0000-000000000002"),
        Guid.Parse("10000000-0001-0000-0000-000000000003"),
        Guid.Parse("10000000-0001-0000-0000-000000000004"),
        Guid.Parse("10000000-0001-0000-0000-000000000005"),
        Guid.Parse("10000000-0001-0000-0000-000000000006"),
        Guid.Parse("10000000-0002-0000-0000-000000000001"),
        Guid.Parse("10000000-0002-0000-0000-000000000002"),
        Guid.Parse("10000000-0002-0000-0000-000000000003"),
        Guid.Parse("10000000-0002-0000-0000-000000000004"),
        Guid.Parse("10000000-0002-0000-0000-000000000005"),
        Guid.Parse("10000000-0003-0000-0000-000000000001"),
        Guid.Parse("10000000-0003-0000-0000-000000000002"),
        Guid.Parse("10000000-0003-0000-0000-000000000003"),
        Guid.Parse("10000000-0003-0000-0000-000000000004"),
        Guid.Parse("10000000-0003-0000-0000-000000000005"),
        Guid.Parse("10000000-0004-0000-0000-000000000001"),
        Guid.Parse("10000000-0004-0000-0000-000000000002"),
        Guid.Parse("10000000-0004-0000-0000-000000000003"),
        Guid.Parse("10000000-0004-0000-0000-000000000004"),
        Guid.Parse("10000000-0004-0000-0000-000000000005"),
        Guid.Parse("10000000-0005-0000-0000-000000000001"),
        Guid.Parse("10000000-0005-0000-0000-000000000002"),
        Guid.Parse("10000000-0005-0000-0000-000000000003"),
        Guid.Parse("10000000-0005-0000-0000-000000000004"),
        Guid.Parse("10000000-0005-0000-0000-000000000005"),
        Guid.Parse("10000000-0006-0000-0000-000000000001"),
        Guid.Parse("10000000-0006-0000-0000-000000000002"),
        Guid.Parse("10000000-0006-0000-0000-000000000003"),
        Guid.Parse("10000000-0007-0000-0000-000000000001"),
        Guid.Parse("10000000-0007-0000-0000-000000000002"),
        Guid.Parse("10000000-0007-0000-0000-000000000003"),
        Guid.Parse("10000000-0007-0000-0000-000000000004"),
        Guid.Parse("10000000-0007-0000-0000-000000000005"),
        Guid.Parse("10000000-0008-0000-0000-000000000001"),
        Guid.Parse("10000000-0008-0000-0000-000000000002"),
        Guid.Parse("10000000-0008-0000-0000-000000000003"),
        Guid.Parse("10000000-0008-0000-0000-000000000004"),
        Guid.Parse("10000000-0008-0000-0000-000000000005"),
        Guid.Parse("10000000-0008-0000-0000-000000000006"),
        Guid.Parse("10000000-0007-0000-0000-000000000006"),
        Guid.Parse("10000000-0006-0000-0000-000000000004"),
    ];

    public static async Task InitializeAsync(
        StockDbContext context,
        StockMessagingClient messaging,
        CancellationToken ct = default)
    {
        var stockExists = await context.Stocks.EstimatedDocumentCountAsync() > 0;
        if (!stockExists)
        {
            var rng = new Random(42);
            var stocks = ProductIds.Select(id => new Stock
            {
                ProductID = id,
                Quantity = rng.Next(3, 25),
                Warehouse = rng.Next(2) == 0 ? "WH-01" : "WH-02"
            }).ToList();

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
                            new StockItem { ProductID = ProductIds[0], Quantity = 1 },
                            new StockItem { ProductID = ProductIds[1], Quantity = 2 }
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
