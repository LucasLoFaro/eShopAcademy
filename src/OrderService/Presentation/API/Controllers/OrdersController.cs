using Core.Domain.Contracts;
using Core.Domain.Entities;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderMessagingClient _orderMessaging;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersController"/> class.
        /// </summary>
        /// <param name="orderMessaging">Service used to publish order commands to the
        /// message bus.</param>
        public OrdersController(OrderMessagingClient orderMessaging)
        {
            _orderMessaging = orderMessaging;
        }

        [HttpGet]
        public IEnumerable<Order> GetAllOrders()
        {
            // TODO: implement retrieval from a persistent store once available.
            return new List<Order>();
        }

        [HttpGet("{id}")]
        public Order GetOrder(Guid id)
        {
            // TODO: implement retrieval from a persistent store once available.
            return new();
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest request)
        {
            // Delegate the submission of the order to the messaging service.
            await _orderMessaging.SubmitOrder(request);
            // Return 202 Accepted to indicate processing has started.
            return Accepted();
        }
    }
}
