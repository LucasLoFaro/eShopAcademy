using Microsoft.AspNetCore.Mvc;
using Core.Domain.Entities;
using Core.Domain.Contracts;


namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        public OrdersController(){ }

        [HttpGet]
        public IEnumerable<Order> GetAllOrders()
        {
            return new List<Order>();
        }

        [HttpGet("{id}")]
        public Order GetOrder(Guid id)
        {
            return new ();
        }

        [HttpPost()]
        public Order PlaceOrder(OrderRequest request)
        {
            return new();
        }
    }
}
