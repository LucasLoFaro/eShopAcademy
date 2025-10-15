using Domain.Stock.Entities;

namespace Infrastructure.Data;

public interface IStockRepository
{
    Task<IReadOnlyList<Stock>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Stock>> GetByProductGuidAsync(Guid productGuid, CancellationToken ct = default);

    Task<Stock> GetByProductIdAsync(Guid productGuid, CancellationToken ct = default);
    //Task<Stock> GetByProductGuidAndWarehouseAsync(Guid productGuid, Warehouse warehouse);

    Task<Stock> AddOrUpdateAsync(Stock stock, CancellationToken ct = default);
}
