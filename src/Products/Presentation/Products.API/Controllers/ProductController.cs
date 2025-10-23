using Domain.Products.Entities;
using Microsoft.AspNetCore.Mvc;
using Core.Application.Interfaces.Services;

namespace API.Controllers
{
    [Route("api/product")]
    [ApiController]
    public class ProductController : ControllerBase
    {

        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        async public Task<IActionResult> Get()
            => Ok(await _productService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
            => await _productService.GetByIdAsync(id) is Product product? base.Ok(product) : base.NotFound();

        [HttpGet("MostExpensive")]
        async public Task<IActionResult> GetMostExpensive()
            => Ok(await _productService.GetMostExpensive());

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Product product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _productService.AddOrUpdateAsync(product);
            return StatusCode(StatusCodes.Status201Created, "Product created successfully");
        }

        [HttpPut()]
        public async Task<IActionResult> Put([FromBody] Product product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

           
            await _productService.AddOrUpdateAsync(product);
            return StatusCode(StatusCodes.Status201Created, "Product updated successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            Product? product = await _productService.GetByIdAsync(id);
            if (product == null)
                return BadRequest("Product " + id + " not found.");

            await _productService.DeleteAsync(product);
            return StatusCode(StatusCodes.Status201Created, "Product deleted successfully");
        }
    }
}
