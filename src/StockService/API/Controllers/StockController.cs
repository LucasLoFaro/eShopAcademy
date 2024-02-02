using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/actores")]
    public class StockController : ControllerBase
    {
        //public readonly 

        [HttpGet]
        public async Task<IReadOnlyList<Stock>> Get()
        { 
            
        }
    }
}
