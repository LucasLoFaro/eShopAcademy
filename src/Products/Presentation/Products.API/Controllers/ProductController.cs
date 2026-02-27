using Domain.Products.Contracts;
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
        public async Task<IActionResult> Get(
            [FromQuery] string? sort = null,
            [FromQuery] string? cat = null,
            [FromQuery] bool? deals = null)
        {
            var products = (await _productService.GetAllAsync()).ToList();

            if (!string.IsNullOrEmpty(cat))
                products = products.Where(p =>
                    p.Category != null && p.Category.Name.Contains(cat, StringComparison.OrdinalIgnoreCase)).ToList();

            if (deals == true)
                products = products.Where(p => p.IsDeal).ToList();

            products = sort switch
            {
                "best-sellers" => products.Where(p => p.IsBestSeller || p.ReviewCount > 0)
                    .OrderByDescending(p => p.ReviewCount).ToList(),
                "new" => products.Where(p => p.IsNewRelease)
                    .OrderByDescending(p => p.CreatedAt).ToList(),
                "price-asc" => products.OrderBy(p => p.DealPrice ?? p.Price).ToList(),
                "price-desc" => products.OrderByDescending(p => p.DealPrice ?? p.Price).ToList(),
                "rating" => products.OrderByDescending(p => p.Rating).ToList(),
                _ => products
            };

            return Ok(products);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] ProductSearchFilter filter)
        {
            var result = await _productService.SearchAsync(filter);
            return Ok(result);
        }

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
