using Core.Domain.Entities;

namespace Core.Application.Interfaces.Services
{
    public interface IMessagingServiceClient
    {
        public Task SendProductUpdate(Product product);
        public Task SendProductDelete(Product product);
    }
}
