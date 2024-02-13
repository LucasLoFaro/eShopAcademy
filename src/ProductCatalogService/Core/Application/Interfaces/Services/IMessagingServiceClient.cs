using Domain.Entities;

namespace Services.Interfaces
{
    public interface IMessagingServiceClient
    {
        public Task SendProductUpdate(Product product);
        public Task SendProductDelete(Product product);
    }
}
