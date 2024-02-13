using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces.Services;

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
        {
            try
            {
                return Ok(await _productService.GetAllAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new
                    {
                        ex = ex.Message,
                        innerEx = ex.InnerException?.Message
                    });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                return Ok(await _productService.GetByIdAsync(id));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new {
                        ex = ex.Message,
                        innerEx = ex.InnerException?.Message
                    });
            }
        }

        [HttpGet("MostExpensive")]
        async public Task<IActionResult> GetMostExpensive()
        {
            try
            {
                return Ok(await _productService.GetMostExpensive());
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Product product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _productService.AddOrUpdateAsync(product);
                return StatusCode(StatusCodes.Status201Created, "Product created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while adding product: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to add product");
            }
        }

        [HttpPut()]
        public async Task<IActionResult> Put([FromBody] Product product)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _productService.AddOrUpdateAsync(product);

                return StatusCode(StatusCodes.Status201Created, "Product updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while updating product: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update product");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                Product product = await _productService.GetByIdAsync(id);
                if (product == null)
                    return BadRequest("Product " + id + " not found.");

                await _productService.DeleteAsync(product);
                return StatusCode(StatusCodes.Status201Created, "Product deleted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while deleting product: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete product");
            }
        }
    }
}
