using Domain.DTOs;
using Domain.Entities;

namespace Application.Managers
{
    public interface IStockManager
    {
        Task<Stock> DecreaseStock(AlterStockDTO alterStock);
        Task<IReadOnlyList<Stock>> GetAllAsync();
        Task<Stock> GetByProductGuidAndWarehouseAsync(string productGuid, string warehouse);
        Task<IReadOnlyList<Stock>> GetByProductGuidAsync(string productGuid);
        Task<Stock> IncreaseStock(AlterStockDTO alterStock);
    }
}