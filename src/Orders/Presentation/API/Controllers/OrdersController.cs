using Core.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Domain.Orders.Contracts;
using Domain.Orders.Entities;


namespace API.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<List<Order>> GetAllOrders()
        => await _orderService.GetAllOrders();

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
        => await _orderService.GetOrderById(id) is Order order ? 
            Ok(order) : NotFound();

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest request)
    {
        // TODO: Handle all possible outcomes.
        var order = await _orderService.PlaceOrderAsync(request);
        return Accepted(order);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveOrder(Guid id)
    {
        await _orderService.RemoveOrder(id);
        return NoContent();
    }
}
