using Data.Interfaces;
using Data;
using Domain;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BasketController : ControllerBase
    {
        private IBasketRepository _basketRepository;

        private readonly ILogger<BasketController> _logger;

        public BasketController(ILogger<BasketController> logger, IBasketRepository basketRepository)
        {
            _logger = logger;
            _basketRepository = basketRepository;
        }

        [HttpGet("/basket/clientId")]
        public ActionResult GetBasketByClientID(Guid clientID)
        {
            var basket = _basketRepository.GetBasketByClientId(clientID);
            return basket != null ? Ok(basket) : NotFound();
        }

        [HttpPost("/basket/clientId/add")]
        public ActionResult AddProductToBasket(Guid clientID, [FromBody] Item item)
        {
            return _basketRepository.AddProductToBasket(clientID, item) == 0 ? Ok() : NotFound();
        }

        [HttpPost("/basket/clientId/remove")]
        public ActionResult RemoveProductFromBasket(Guid clientID, [FromBody] Item item)
        {
            return _basketRepository.RemoveProductFromBasket(clientID, item) == 0 ? Ok() : NotFound();
        }
    }
}
