using Microsoft.AspNetCore.Mvc;
using Core.Domain.Entities;
using Core.Domain.Contracts;
using Infrastructure.Services.Interfaces;


namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderMessagingService _orderMessaging;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersController"/> class.
        /// </summary>
        /// <param name="orderMessaging">Service used to publish order commands to the
        /// message bus.</param>
        public OrdersController(IOrderMessagingService orderMessaging)
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
