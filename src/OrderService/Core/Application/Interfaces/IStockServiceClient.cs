using Core.Domain.Entities;
using Domain.Contracts;

namespace Core.Application.Interfaces;

public interface IStockServiceClient
{
    Task<StockReservationResponse> ReserveStockAsync(List<Item> items);
}