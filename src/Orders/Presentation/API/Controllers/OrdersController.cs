using Core.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Domain.Orders.Contracts;
using Domain.Orders.Entities;
using API.Sse;


namespace API.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private IOrderService _orderService;
    private readonly OrderStatusStreamService _stream;

    public OrdersController(IOrderService orderService, OrderStatusStreamService stream)
    {
        _orderService = orderService;
        _stream = stream;
    }

    [HttpGet]
    public async Task<List<Order>> GetAllOrders(CancellationToken cancellationToken)
        => await _orderService.GetAllOrders(cancellationToken);

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
        => await _orderService.GetOrderById(id, cancellationToken) is Order order ?
            Ok(order) : NotFound();

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest request, CancellationToken cancellationToken)
    {
        // TODO: Handle all possible outcomes.
        var order = await _orderService.PlaceOrderAsync(request, cancellationToken);
        return Accepted(order);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveOrder(Guid id, CancellationToken cancellationToken)
    {
        await _orderService.RemoveOrder(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id}/stream")]
    public async Task StreamOrderStatus(Guid id, CancellationToken ct)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var reader = _stream.Subscribe(id);

        try
        {
            // Send initial heartbeat so the client knows the connection is alive
            await Response.WriteAsync($"event: connected\ndata: {{\"orderId\":\"{id}\"}}\n\n", ct);
            await Response.Body.FlushAsync(ct);

            await foreach (var json in reader.ReadAllAsync(ct))
            {
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
        finally
        {
            _stream.Unsubscribe(id, reader);
        }
    }
}
