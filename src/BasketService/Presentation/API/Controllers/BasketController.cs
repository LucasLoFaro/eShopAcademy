using Core.Domain.Entities;
using Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace API.Controllers
{
    [Route("api/BasketController")]
    [ApiController]
    public class BasketController : ControllerBase
    {
        private IDatabase _basket;
        public BasketController(IRedisDatabaseClient _dbClient)
        {
            _basket = _dbClient.GetSession();
        }

        [HttpGet]
        public async Task Basket()
        {
           var data = await _basket.HashGetAllAsync("sad");
            Console.WriteLine(data);
        }
    }
}
