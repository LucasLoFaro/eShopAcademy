using Domain.Entities;

namespace Infrastructure.Data
{
    public interface IStockRepository
    {
        Task<IReadOnlyList<Stock>> GetAllAsync();

        Task<IReadOnlyList<Stock>> GetByProductGuidAsync(Guid productGuid);

        Task<Stock> GetByProductGuidAndWarehouseAsync(Guid productGuid, Warehouse warehouse);

        Task<bool> UpdateAsync(Stock stock);
        Task<Stock> AddAsync(Stock stock);

    }
}
