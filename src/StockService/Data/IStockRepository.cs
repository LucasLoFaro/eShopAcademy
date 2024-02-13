using Domain.Entities;

namespace Infrastructure.Data
{
    public interface IStockRepository
    {
        Task<IReadOnlyList<Stock>> GetAllAsync();

        Task<IReadOnlyList<Stock>> GetByProductGuidAsync(string productGuid);

        Task<Stock> GetByProductGuidAndWarehouseAsync(string productGuid, string warehouse);

        Task<bool> UpdateAsync(Stock stock);
        Task<Stock> AddAsync(Stock stock);

    }
}
