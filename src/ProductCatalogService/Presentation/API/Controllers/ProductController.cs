using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace API.Controllers
{
    [Route("api/product")]
    [ApiController]
    public class ProductController : ControllerBase
    {

        private readonly IProductService productService;
        public ProductController(IProductService _productService)
        {
            productService = _productService;
        }
        // GET: api/<ProductController>
        [Route("MostExpensive")]
        [HttpGet]
        async public Task<IActionResult> Get()
        {
            try
            {
                return Ok(await productService.GetMostExpensiveProduct());
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
