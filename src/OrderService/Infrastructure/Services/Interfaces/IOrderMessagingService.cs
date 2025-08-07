using Core.Domain.Contracts;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Defines an abstraction for sending order related messages to the
/// messaging infrastructure.  Implementations are responsible for
/// connecting to the appropriate service bus and publishing the
/// commands used to start the order saga.
/// </summary>
public interface IOrderMessagingService
{
    /// <summary>
    /// Publishes a request to submit an order.  The implementation
    /// should generate a new correlation identifier and publish a
    /// SubmitOrder command containing the provided request data.
    /// </summary>
    /// <param name="orderRequest">The order request containing customer
    /// and item information.</param>
    Task SubmitOrder(OrderRequest orderRequest);
}