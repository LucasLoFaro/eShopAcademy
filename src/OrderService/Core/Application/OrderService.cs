using Core.Application.Interfaces;
using Core.Domain.Contracts;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Domain.Contracts;
using Data;


namespace Application;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _db;
    private readonly IStockServiceClient _stockClient;
    private readonly IProductServiceClient _productClient;
    private readonly ICustomerServiceClient _customerClient;
    private readonly IPaymentServiceClient _paymentClient;
    private readonly IOrderMessagingClient _orderMessagingClient;

    public OrderService(
        IOrderRepository repository,
        IStockServiceClient stockClient,
        IProductServiceClient productClient,
        IPaymentServiceClient paymentClient,
        ICustomerServiceClient customerClient,
        IOrderMessagingClient messagePublisher)
    {
        _db = repository;
        _stockClient = stockClient;
        _productClient = productClient;
        _paymentClient = paymentClient;
        _customerClient = customerClient;
        _orderMessagingClient = messagePublisher;
    }

    public Task<List<Order>> GetAllOrders(CancellationToken ct = default)
        => _db.GetAllAsync(ct);

    public Task<Order?> GetOrderById(Guid id, CancellationToken ct = default)
        => _db.GetByIdAsync(id, ct);

    // TODO: When success, empty basket
    //       Return a custom response with the result and possible errors
    //       Replace mocks for real services
    public async Task<PlaceOrderResponse> PlaceOrderAsync(OrderRequest request, CancellationToken ct = default)
    {
        var order = new Order
        {
            CustomerId = request.CustomerId,
            Status = OrderStatus.Created
        };

        var customer = await _customerClient.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null)
            throw new InvalidOperationException($"Customer not found");
        order.Customer = customer;

        foreach (var item in request.Items)
        {
            var updatedProduct = await _productClient.GetProductByIdAsync(item.ProductID);
            if (updatedProduct.Price != item.Price)
                throw new InvalidOperationException($"Price changed for product {updatedProduct.Name}");

            order.Items.Add(new OrderItem
            {
                ProductID = item.ProductID,
                Quantity = item.Quantity,
                Product = updatedProduct
            });
        }

        order.TotalPrice = order.Items.Sum(i => i.Quantity * i.Price);

        var notificationUrl = "http://payment-api/api/payments/webhook";
        var payment = await _paymentClient.InitPaymentAsync(order.TotalPrice, "USD", notificationUrl, order.Id);
        if (payment == null)
            throw new InvalidOperationException("There was an error creating the payment");

        order.PaymentId = payment.Id;
        order.Payment = payment;

        var reserve = await _stockClient.ReserveStockAsync(order.Id, request.Items, ct);
        if (!reserve.Success)
            throw new InvalidOperationException($"The following products have run out of stock: {string.Join(", ", reserve.OutOfStockProducts)}");

        order.ReservationId = (Guid) reserve.ReservationId!;
        
        await _db.AddAsync(order);

        await _orderMessagingClient.PublishOrderSubmitted(order);

        // TODO: Add Automapper
        return new()
        {
            OrderId = order.Id,
            PaymentUrl = new Uri(order.Payment.PaymentURL),
            Status = order.Status
        };
    }

    public Task RemoveOrder(Guid id, CancellationToken ct = default)
        => _db.RemoveByIdAsync(id, ct);
}