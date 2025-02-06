using Core.Domain.DTOs;

namespace Infrastructure.Services.Interfaces;

public interface IMessagingServiceClient
{
    public Task SendStockUpdate(AlterStockDTO stock);
}